#!/usr/bin/env node
// .ai-roster/scripts/sync-agents.js
//
// Single source of truth -> three runtimes.
//   SOURCE:  .ai-roster/team.yaml  +  .ai-roster/<role>_instructions.md
//   EMIT (environment: claude-code):  .claude/agents/<id>.md    (true subagents, fresh context)
//   EMIT (environment: opencode):     .opencode/agents/<id>.md  (markdown + YAML frontmatter)
//   EMIT (environment: antigravity):  .agents/agents/<id>/agent.json
//
// Why not claude.json/customCommands? Claude Code never reads that; and slash commands run in the
// CURRENT conversation (no fresh context), violating the "independent subagent" rule. Subagents
// live in .claude/agents/<id>.md with YAML frontmatter (name, description, tools, model).

const fs = require('fs');
const path = require('path');

let yaml;
try {
  yaml = require('js-yaml');
} catch (e) {
  console.error('FATAL: js-yaml is not resolvable. Run:  corepack pnpm add -D js-yaml -w');
  process.exit(1);
}

const ROSTER_DIR = path.resolve(__dirname, '..');               // .ai-roster
const PROJECT_ROOT = path.resolve(__dirname, '..', '..');       // repo root
const CLAUDE_AGENTS_DIR = path.join(PROJECT_ROOT, '.claude', 'agents');
// Claude Code auto-generates skills from .claude/agents/*.md — no manual slash-commands needed.
const OPENCODE_AGENTS_DIR = path.join(PROJECT_ROOT, '.opencode', 'agents');
const AGY_AGENTS_DIR = path.join(PROJECT_ROOT, '.agents', 'agents');

function ensureDirSync(dirPath) {
  if (!fs.existsSync(dirPath)) fs.mkdirSync(dirPath, { recursive: true });
}

function readInstructions(file) {
  const p = path.join(ROSTER_DIR, file);
  if (!fs.existsSync(p)) {
    // Fail loudly — the old script silently threw on a missing frontend_instructions.md.
    throw new Error(`Missing instructions_file: ${file} (expected at ${p})`);
  }
  return fs.readFileSync(p, 'utf8');
}

// Team-wide rules (.ai-roster/rules/*.md) are appended to EVERY agent's body so they bind
// mechanically, not just as documentation humans hope agents read.
function readTeamRules() {
  const dir = path.join(ROSTER_DIR, 'rules');
  if (!fs.existsSync(dir)) return '';
  const files = fs.readdirSync(dir).filter((f) => f.endsWith('.md')).sort();
  if (!files.length) return '';
  const sections = files.map((f) => fs.readFileSync(path.join(dir, f), 'utf8').trim());
  return `\n\n---\n\n# TEAM RULES (apply to every task, from .ai-roster/rules/)\n\n${sections.join('\n\n')}\n`;
}

function emitClaudeAgent(id, cfg, body) {
  ensureDirSync(CLAUDE_AGENTS_DIR);
  const model = cfg.claude_model_alias || 'sonnet';
  const toolList = (cfg.tools && cfg.tools.length) ? cfg.tools : ['Read', 'Grep', 'Glob', 'Bash'];
  // Delegation guard: Claude Code subagents can only invoke the Agent/Task tool if it's in this
  // list. It's never in the default, so any agent carrying `write_scope` (i.e. an engineer, which
  // must work its own issue without spawning helpers — see [[wave-gate-workflow]] on independent
  // fresh-context roles) staying off the default list is sufficient; warn if one adds it anyway.
  if (cfg.write_scope && (toolList.includes('Agent') || toolList.includes('Task'))) {
    console.warn(`[Claude Code] WARNING: ${id} has write_scope set but also lists Agent/Task in tools — this re-enables delegation, which write_scope-bearing (engineer) agents should not have.`);
  }
  const tools = toolList.join(', ');
  const desc = `${cfg.display_name}${cfg.stage ? ` — stage ${cfg.stage}` : ''}`.replace(/\n/g, ' ');
  // Unlike opencode, Claude Code subagent frontmatter has no per-agent directory-glob permission
  // block — path-scoped rules only exist project-wide in .claude/settings.json, shared by every
  // tool call regardless of which subagent issued it. So a `write_scope` here can only be written
  // guidance (the same class of guarantee as the "never touch apps/web" prose already in
  // backend_instructions.md/worker_instructions.md), not a mechanically enforced boundary like
  // opencode's `permission.edit`. Surface it anyway so a future claude-code engineer isn't silently
  // unscoped.
  const scopeNote = (cfg.write_scope && cfg.write_scope.length)
    ? `\n## Write scope (repo convention, not tool-enforced on this runtime)\nOnly edit files under: ${cfg.write_scope.join(', ')}. Ask before touching anything else.\n`
    : '';
  // `effort:` (low|medium|high|xhigh|max) overrides the session effort level for this subagent;
  // thinking depth follows effort on this runtime, so there is no separate thinking field.
  const effort = cfg.claude_effort ? `effort: ${cfg.claude_effort}\n` : '';
  const frontmatter =
    `---\nname: ${id}\ndescription: ${JSON.stringify(desc)}\ntools: ${tools}\nmodel: ${model}\n${effort}---\n\n`;
  const out = path.join(CLAUDE_AGENTS_DIR, `${id}.md`);
  fs.writeFileSync(out, frontmatter + body + scopeNote);
  console.log(`[Claude Code] ${cfg.display_name} -> .claude/agents/${id}.md (model: ${model}${cfg.claude_effort ? `, effort: ${cfg.claude_effort}` : ''})`);
}

// emitClaudeCommand removed — Claude Code auto-generates agent-invoking skills directly
// from .claude/agents/*.md. The old dispatcher slash commands added a wasteful double-hop
// (main model → Agent tool → subagent) that undermined the "fresh context" guarantee.

function buildOpencodePermission(cfg, team) {
  // Worktrees live outside the project root (github_flow.sh: WORKTREE_ROOT defaults to
  // ~/.agent-worktrees/tekram-delivery-assessment, shared by every engineer regardless of runtime — see
  // workflow.worktree_roots.claude_code in team.yaml, the actual configured path). Touching a
  // path outside the project root requires `external_directory` allow, or opencode prompts on
  // every read/edit inside a worktree. Read itself defaults to "allow" repo-wide already; we set
  // it explicitly since we're now populating other permission keys on the same agent.
  // Grant the whole ~/.agent-worktrees root, not just the careeree subfolder — it also holds
  // .lanes/ (workflow.lane_lock_dir), which engineers need to read for lane state.
  const agentWorktreesRoot = path.dirname(team.workflow.worktree_roots.claude_code);
  const worktreeGlob = `${agentWorktreesRoot.replace(
    process.env.HOME || require('os').homedir(),
    '~'
  )}/**`;
  const permission = {
    read: 'allow',
    external_directory: {
      [worktreeGlob]: 'allow',
      // System scratch dir — read only. macOS symlinks /tmp -> /private/tmp; both forms are
      // registered since it's unclear whether opencode resolves symlinks before glob-matching.
      // `edit` below has no /tmp entry, so its "*": "ask" fallback still applies to writes there.
      '/tmp/**': 'allow',
      '/private/tmp/**': 'allow',
    },
    // No delegation: deny every subagent on the `task` tool so opencode drops it from the tool
    // description entirely. `permission.task` is the current (non-deprecated) mechanism — there
    // is no `delegation` field in opencode.
    task: { '*': 'deny' },
  };
  // Write access is scoped to the engineer's own area; anything else falls to "ask" so opencode
  // interactively prompts the founder for approval instead of silently allowing or hard-blocking
  // a cross-directory edit. `edit` covers write/edit/patch (opencode docs). The docs don't specify
  // whether glob keys match relative or absolute paths, and worktrees have a different absolute
  // prefix than the main repo — so each scope pattern is registered both bare and `**/`-prefixed
  // to match under either anchoring.
  // A DEFINED write_scope always emits the permission block — including an EMPTY one, which
  // yields edit: {'*': 'ask'} with no allow globs (e.g. tech-lead: verify/dispatch only, every
  // file edit needs founder approval). Only an absent write_scope leaves opencode defaults.
  if (cfg.write_scope) {
    const edit = { '*': 'ask' };
    for (const pattern of cfg.write_scope) {
      edit[pattern] = 'allow';
      edit[`**/${pattern}`] = 'allow';
    }
    permission.edit = edit;
  }
  return permission;
}

function emitOpencodeAgent(id, cfg, body, team) {
  // opencode reads project agents from .opencode/agents/<name>.md (plural is the current
  // convention; singular .opencode/agent/ is only kept for backwards compatibility). The file
  // name IS the agent name. `model` is a fully-qualified provider/model-id — the `deepseek`
  // provider is declared in opencode.json at the repo root; a model id opencode can't resolve
  // fails at invocation, so keep the two in sync.
  ensureDirSync(OPENCODE_AGENTS_DIR);
  const model = cfg.opencode_model || 'deepseek/deepseek-v4-flash';
  const desc = `${cfg.display_name}${cfg.stage ? ` — stage ${cfg.stage}` : ''}`.replace(/\n/g, ' ');
  // Run these engineers as primary agents (they drive their own worktree session end-to-end).
  const mode = cfg.opencode_mode || 'primary';
  // Tool access: file modification is gated entirely through `permission.edit` below (opencode
  // docs: "edit" covers the edit/write/patch tools), so those booleans are omitted here to avoid
  // two systems fighting over the same tool call.
  const tools = cfg.opencode_tools || { bash: true, read: true, grep: true, glob: true };
  const frontmatterObj = {
    description: desc,
    mode,
    model,
    ...(cfg.opencode_temperature != null ? { temperature: cfg.opencode_temperature } : {}),
    tools,
    permission: buildOpencodePermission(cfg, team),
    ...(cfg.opencode_options || {}),
  };
  // Hand-built YAML strings don't scale to nested, glob-keyed permission trees (quoting "*" and
  // "**/apps/web/**" correctly by hand is error-prone) — dump via js-yaml instead.
  const frontmatter = `---\n${yaml.dump(frontmatterObj, { lineWidth: -1 })}---\n\n`;
  const out = path.join(OPENCODE_AGENTS_DIR, `${id}.md`);
  fs.writeFileSync(out, frontmatter + body);
  console.log(`[opencode] ${cfg.display_name} -> .opencode/agents/${id}.md (model: ${model})`);
}

function emitAntigravityAgent(id, cfg, body) {
  const dir = path.join(AGY_AGENTS_DIR, id);
  ensureDirSync(dir);
  if (!cfg.model || String(cfg.model).startsWith('TODO-VERIFY')) {
    console.warn(`[Antigravity] WARNING: ${id} model is "${cfg.model}". Verify against Antigravity's live model menu before use — a wrong id fails silently at runtime.`);
  }
  // Antigravity's real permission schema was never verified against live docs (unlike opencode's
  // `permission.edit`, researched and confirmed) — same TODO-VERIFY caveat as `model` above. This
  // field is carried through so a future antigravity agent isn't silently unscoped, but it is NOT
  // confirmed to mechanically restrict writes until checked against Antigravity's real config.
  if (cfg.write_scope && cfg.write_scope.length) {
    console.warn(`[Antigravity] WARNING: ${id} write_scope is informational only — TODO-VERIFY against Antigravity's real permission schema before trusting it to restrict writes.`);
  }
  const agyConfig = {
    name: cfg.display_name,
    model: cfg.model || 'TODO-VERIFY-antigravity-model',
    thinking: cfg.thinking || 'low',
    system_instruction: body,
    skills: cfg.skills || [],
    ...(cfg.write_scope ? { write_scope_TODO_VERIFY: cfg.write_scope } : {}),
  };
  fs.writeFileSync(path.join(dir, 'agent.json'), JSON.stringify(agyConfig, null, 2));
  console.log(`[Antigravity] ${cfg.display_name} -> .agents/agents/${id}/agent.json (model: ${agyConfig.model})`);
}

function syncAgents() {
  console.log('Syncing AI team configuration from team.yaml ...');
  const team = yaml.load(fs.readFileSync(path.join(ROSTER_DIR, 'team.yaml'), 'utf8'));

  let claude = 0, opencode = 0, antigravity = 0;
  const teamRules = readTeamRules();
  for (const [id, cfg] of Object.entries(team.agents)) {
    const body = readInstructions(cfg.instructions_file) + teamRules;
    if (cfg.environment === 'claude-code') {
      emitClaudeAgent(id, cfg, body);
      claude++;
    } else if (cfg.environment === 'opencode') {
      emitOpencodeAgent(id, cfg, body, team);
      opencode++;
    } else if (cfg.environment === 'antigravity') {
      emitAntigravityAgent(id, cfg, body);
      antigravity++;
    } else {
      throw new Error(`Unknown environment "${cfg.environment}" for agent "${id}"`);
    }
  }
  console.log(`\nDone. ${claude} Claude Code subagent(s), ${opencode} opencode agent(s), ${antigravity} Antigravity agent(s).`);
  console.log('Claude Code: invoke via the Agent tool with subagent_type: <id>.');
  console.log('opencode: agents available under .opencode/agents/<id>.md (deepseek provider in opencode.json).');
  console.log('Antigravity: agents available under .agents/agents/<id>/.');
}

try {
  syncAgents();
} catch (err) {
  console.error('FATAL:', err.message);
  process.exit(1);
}
