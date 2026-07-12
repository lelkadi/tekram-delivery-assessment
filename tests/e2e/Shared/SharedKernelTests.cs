namespace Tekram.E2E.Shared;

using System.Net;
using System.Net.Sockets;
using Xunit;

/// <summary>
/// Black-box coverage for issue #8 (Part 2 Slice 1.2 — shared kernel: TekramDbContext,
/// middlewares, extensions, error codes, pagination).
///
/// IMPORTANT SCOPE NOTE: #8 ships zero HTTP surface of its own — Program.cs on this branch is
/// still the bare `dotnet new web` scaffold (no `AddDbContext`, no middleware registration, no
/// routes beyond `GET /`); wiring lands in #11 (per the architect's/PM's own note on this issue's
/// PM-rejection). That makes most of #8's actual deliverable (the EF Core model in
/// TekramDbContext.cs) unreachable through the running API today, so the two facts below are the
/// full extent of what is genuinely black-box-observable for this issue right now:
///
///  - AC1 is a liveness smoke check (the process boots and serves a request at all) — the same
///    general regression class the PM's rejection was about (an invalid EF model can prevent the
///    app from ever reaching "Now listening"), even though this branch doesn't invoke the
///    DbContext at boot yet.
///  - AC2 exercises the actual defect PM found — snake_case `HasFilter` strings referencing
///    columns EF Core maps as PascalCase, causing Postgres 42703 at index-creation time — by
///    reading the REAL lane Postgres schema directly (read-only `information_schema`/`pg_indexes`
///    inspection via plain Npgsql, no EF, no `ProjectReference` to src/**, consistent with the
///    "seed/reset helper" carve-out for direct DB access; this is the closest black-box
///    equivalent available to curling an endpoint that doesn't exist yet).
/// </summary>
[Trait("issue", "8")]
public class SharedKernelTests : LiveApiTestBase
{
    private static readonly string DatabaseUrl =
        Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? "postgres://postgres:postgres@localhost:5432/tekram_lane2";

    [LiveFact]
    public async Task AC1_ApiBootsAndServesRequests()
    {
        HttpResponseMessage response;
        try
        {
            response = await Client.GetAsync("/");
        }
        catch (HttpRequestException ex)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected the API at {BaseUrl} to be reachable (shared-kernel changes must never " +
                $"prevent the app from starting), but the connection failed: {ex.Message}");
        }

        Assert.True(
            response.StatusCode != HttpStatusCode.InternalServerError,
            "API responded with 500 — the app started but is erroring, which is the exact failure " +
            "mode (an invalid EF Core model) PM's rejection of #8 was about.");
    }

    [LiveFact]
    public void AC2_PartialIndexFiltersReferenceRealColumns()
    {
        using var conn = OpenConnection(DatabaseUrl, out var reason);
        Assert.True(conn is not null,
            $"Lane Postgres must be reachable when E2E_BASE_URL is set (the API depends on it): {reason}");

        // The regression: TekramDbContext.OnModelCreating's HasFilter(...) clauses referencing a
        // column name the model doesn't actually produce — Postgres 42703 at CREATE INDEX time.
        // The merged naming convention is snake_case, per docs/database-schema.md DDL
        // (consumed_at L195, partial index "where consumed_at is null" L201). Assert the columns
        // the filters must reference actually exist, with the exact case the spec mandates, on
        // the real lane database. (Re-anchored from PascalCase to spec per #53.)
        AssertColumnExists(conn!, "auth", "otp_codes", "consumed_at");
        AssertColumnExists(conn!, "restaurants", "restaurants", "deleted_at");
        AssertColumnExists(conn!, "restaurants", "menu_items", "deleted_at");

        // And assert the partial indexes themselves exist and their WHERE clause matches those
        // real column names (this is exactly what failed with a 42703 before the fix).
        AssertIndexFilterContains(conn!, "auth", "otp_codes", "consumed_at");
        AssertIndexFilterContains(conn!, "restaurants", "restaurants", "deleted_at");
        AssertIndexFilterContains(conn!, "restaurants", "menu_items", "deleted_at");
    }

    // ---- direct-Postgres read-only helpers (no EF, no src/** reference) ----

    private static Npgsql.NpgsqlConnection? OpenConnection(string databaseUrl, out string skipReason)
    {
        skipReason = string.Empty;
        try
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            var csb = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = uri.AbsolutePath.TrimStart('/'),
                Username = userInfo[0],
                Password = userInfo.Length > 1 ? userInfo[1] : string.Empty,
            };
            var conn = new Npgsql.NpgsqlConnection(csb.ConnectionString);
            conn.Open();
            return conn;
        }
        catch (Exception ex) when (ex is SocketException or Npgsql.NpgsqlException)
        {
            skipReason = $"Could not reach lane Postgres at {databaseUrl}: {ex.Message}";
            return null;
        }
    }

    private static void AssertColumnExists(Npgsql.NpgsqlConnection conn, string schema, string table, string column)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT 1 FROM information_schema.columns " +
            "WHERE table_schema = @schema AND table_name = @table AND column_name = @column";
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);
        cmd.Parameters.AddWithValue("column", column);
        using var reader = cmd.ExecuteReader();
        Assert.True(
            reader.Read(),
            $"Expected column {schema}.{table}.\"{column}\" to exist on the real lane database " +
            "(TekramDbContext's model must produce this exact column name, snake_case per " +
            "docs/database-schema.md).");
    }

    private static void AssertIndexFilterContains(Npgsql.NpgsqlConnection conn, string schema, string table, string columnInFilter)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT indexdef FROM pg_indexes WHERE schemaname = @schema AND tablename = @table AND indexdef ILIKE @pattern";
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);
        cmd.Parameters.AddWithValue("pattern", $"%WHERE%{columnInFilter}%");
        using var reader = cmd.ExecuteReader();
        Assert.True(
            reader.Read(),
            $"Expected a partial index on {schema}.{table} filtering on \"{columnInFilter}\" — " +
            "this is the exact HasFilter(...) clause PM's rejection found broken (snake_case string " +
            "referencing a column EF Core actually maps as PascalCase, causing Postgres 42703).");
    }
}
