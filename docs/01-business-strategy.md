# Business Strategy & Competitor Research Report

**Market focus:** Lebanese Food Delivery & Quick-Commerce
**Drafted by:** Gemini (business-strategy draft agent)
**Audited & finalized by:** Claude (Technical Researcher agent), with WebSearch/WebFetch fact-checking
**Date:** 2026-07-08

> **Purpose note.** This document supports **Part 9 — Business Thinking** as broader market/strategy
> context. The assessment's literal Part 9 prompt ("orders dropped from 1,000/day to 100/day —
> 24h actions and 30-day recovery plan") is answered separately in `docs/business-recovery-plan.md`.
> This report is strategic vision and competitive positioning, not a scope commitment: the actual
> buildable deliverable for this assessment (Part 2) is a **food-delivery-only backend** (JWT auth,
> restaurant search, order creation). Everything below describing taxi, supermarket, housekeeping,
> pharmacy, and parcel verticals is long-horizon product strategy, included because the brief's
> Part 1 architecture asks for a design that anticipates these verticals — it is not implemented,
> scheduled, or scored as part of the coding challenge.

> **Founder flag — name collision.** A real, unrelated company already trades as **"Tekram
> Delivery"** in Beirut, offering a near-identical concept (food delivery + ride-hailing + grocery
> + a subscription) under the same name. Full research, competitor-matrix entry, SWOT, and the
> strategic implications are in §2.2, §3.4, and §4.5 — this is now treated as a real competitor,
> not a footnote.

---

## 1. Executive Summary

This report outlines a market-entry strategy and technical/architectural alignment for **Tekram**,
envisioned as a next-generation multi-vertical on-demand platform for the Lebanese market. Lebanon's
macroeconomic landscape is distinctive: a dual USD/LBP currency system, a heavily cash-based and
partly unbanked population, frequent infrastructure gaps (electricity and cellular outages), and
reliance on informal, landmark-based navigation rather than structured postal codes.

Despite these constraints, food delivery and quick-commerce remain active sectors. Incumbents —
**Toters**, **NokNok**, **GoSawa**, and, as verified during this audit, a fourth active player,
**Gozilla** (§2.1) — have validated demand but each carries structural weaknesses: high fees,
narrow vertical scope, or fulfillment models that don't suit hot food or fresh groceries. A fifth,
directly-named competitor also surfaced during this audit: the real **"Tekram Delivery"** (Beirut,
HNZ Holding, §2.2), already running a near-identical food + taxi + grocery + subscription bundle
under the same name — a genuine strategic consideration addressed in §4.5, not just a naming
curiosity.

The strategic core of this report is Tekram's **Dual-Engine Fleet Synergy**: using ride-hailing
drivers during off-peak food-delivery hours (and vice versa) to raise fleet utilization, lower unit
delivery cost, and improve driver earnings. Rather than the capital-intensive dark-store model,
Tekram would use a **partner-centric hybrid grocery model** with in-store picking agents, paired
with local payment rails (OMT, Whish Money), an operating model that tolerates offline conditions,
and a cost-controlled approach to mapping and live tracking suited to Lebanon's incomplete map data
and connectivity gaps.

**Important caveat on this section:** several competitor-specific figures below (commission
percentages, exact delivery/service fees, minimum order values) are **not publicly disclosed** by
Toters, NokNok, or GoSawa. Where the original draft stated such figures as fact, this audit has
either corrected them against a verifiable public source, or re-labeled them explicitly as
directional estimates. Treat unlabeled dollar figures for competitors as **estimates for planning
purposes**, not confirmed data — a pre-launch pricing study should verify these directly (mystery
shopping, merchant interviews) before they inform real commission/pricing decisions.

---

## 2. Competitor Landscape

The table below contrasts three incumbents operating in the Lebanese delivery and marketplace
sectors: **Toters** (premium lifestyle super-app), **NokNok** (quick-commerce grocery, now
expanding), and **GoSawa** (daily deals and voucher marketplace). §2.1 covers a fourth,
previously-omitted competitor, **Gozilla**, whose product is the closest existing *unrelated*
analog to what this report proposes for Tekram. §2.2 covers a fifth: the real, unrelated company
already trading as **"Tekram Delivery"** in Beirut — the closest analog of all, since it shares
both the concept and the name.

| Dimension / Feature | Toters | NokNok | GoSawa |
| :--- | :--- | :--- | :--- |
| **Primary Vertical** | Food delivery, grocery (Toters Fresh), courier (Butler), retail shopping | Quick-commerce grocery and daily essentials; per a September 2025 relaunch as an "Everything App," NokNok is now integrating food-delivery aggregation and retail categories (sports, beauty, electronics, home, toys) — see §3.2 | E-commerce, daily deals, hotel getaways, dining vouchers |
| **Logistics/Business Model** | Marketplace (3P merchants) with proprietary delivery fleet | Direct Q-commerce (1P dark/cloud stores) with proprietary fleet for groceries; the new aggregator layer likely adds a 3P marketplace component (not yet confirmed in detail) | Deals & e-commerce (mixed 1P/3P) using third-party couriers |
| **Real-Time GPS Tracking** | **Yes** — live map tracking with route/traffic updates | **Yes** — live driver tracking from store to doorstep | **No** — status updates only; relies on standard third-party couriers |
| **Multi-Merchant Cart** | **Partial** — separate checkouts per restaurant; some co-ordering with retail items | **N/A** for the core grocery flow (single dark store per order) | **Yes** — vouchers across different merchants can be checked out together |
| **Search & Filters** | Advanced (cuisine, prep time, price, offers, ratings, dietary tags) | Advanced (grocery search, brand, category, dietary tags) | Basic (category, deal type, location, discount rate) |
| **Scheduling** | Yes — pre-scheduling for meals and grocery slots | Yes — scheduled delivery windows at checkout | Vouchers are instant; physical products ship on a standard multi-day timeline |
| **Ratings & Reviews** | Yes — detailed feedback on restaurants, dishes, and couriers | No public evidence of customer-facing dish/store ratings (internal satisfaction surveys only, unconfirmed) | Yes — verified-buyer reviews on deals and products |
| **Loyalty Program** | **Yes — "Toters Rewards."** Confirmed to exist and to be points-based ("earn points, redeem for offers, discounts, free meals"). Specific tier names/thresholds (e.g. Silver/Gold/Platinum) are **not publicly documented** — treat any named tier structure as unconfirmed. | No confirmed points program; primarily flat discounts, coupons, flash sales | No confirmed points program; occasional wallet cashback, deal-driven value |
| **Subscription Tiers** | **Not confirmed.** No public source for a "Toters Pass" product was found during this audit. (A $5.99/month unlimited-free-delivery subscription *does* exist in this market — confirmed for competitor **Gozilla**, §2.1 — which may be the source of the original draft's claim.) | None found; focuses on low flat delivery rates | None found; focuses on high-discount flash deals |
| **Ad Placements / Ads** | Sponsored restaurant listings, home-screen banners (mechanism plausible for a marketplace of this scale; not independently itemized beyond what's publicly visible in-app) | Supplier-paid placement and upsell surfacing plausible for a retail-catalog app; not independently confirmed | Homepage banners, newsletter spotlights, sponsored social posts (consistent with a deals-marketing business) |
| **Merchant Tooling** | Toters operates a partner portal for restaurants (live orders, menu management) per its public partner-facing materials | Proprietary supply-chain/inventory tooling is consistent with a 1P dark-store model; not independently itemized | Voucher redemption/validation tooling is consistent with GoSawa's deals model; not independently itemized |
| **Payment Options** | Cash on Delivery (USD/LBP), in-app wallet ("Toters Cash," confirmed — supports both USD and LBP balances) | Cash on Delivery (USD/LBP); card payment plausible but not independently confirmed | Cash on Delivery, card, and confirmed regional cash-network rails (Whish, OMT are real, licensed Lebanese money-transfer operators) |
| **Delivery Fees** | Distance-based, starting low with surge multipliers (exact starting figure not publicly published — treat as an estimate) | A flat delivery fee was observed on NokNok's own FAQ (≈139,990 LBP at time of check, which is roughly $1.50–$1.60 at a ~90,000 LBP/USD rate) — broadly consistent with a "$1.00–$1.50" planning estimate, but this is a single data point and fees can change | Flat regional shipping fees for physical goods; vouchers are typically fee-free |
| **Platform/Service Fees** | A per-transaction service fee is plausible for a marketplace of this type; **exact amount not publicly confirmed** — treat as an estimate | A small packing/bag fee is plausible; **not publicly confirmed** — treat as an estimate | No confirmed platform fee; likely folded into voucher margin |
| **Minimum Order Value** | Merchant-defined; a $7–$10 range is a reasonable planning estimate but **not publicly confirmed** | A low fixed MOV is typical of dark-store models; **exact figure not publicly confirmed** | No minimum order value for voucher purchases |

### 2.1. A Fourth Incumbent: Gozilla

The original draft's competitor set omitted **Gozilla**, which independent research (Sensor Tower
app-ranking data, Q1 2025) shows as one of the top food-delivery-category apps in Lebanon by
downloads — ahead of NokNok in that same period. Gozilla's own marketing describes:

- **1,300+ restaurants and 300+ shops**, plus groceries and niche verticals (pet care, baby
  products, flowers, beauty/wellness, even pest control) in a single app.
- **Live order tracking.**
- A **confirmed $5.99/month subscription for unlimited free delivery** — Gozilla's own marketing
  states it is "the only app in Lebanon" with this offer.
- A **points-based rewards program** (unnamed tiers, details not published).

This matters strategically: Gozilla, not Toters, is the closest existing analog to the
multi-vertical-plus-subscription bundle this report proposes for Tekram. Any go-to-market plan
should treat Gozilla as a direct competitive threat to the "Tekram Pass" concept (§4.4.A), not
assume the bundle is unclaimed territory. Data on Gozilla's commission rates, fees, and delivery
logistics ownership was not publicly available at the time of this audit and should be gathered
directly (app testing, merchant interviews) before finalizing pricing strategy.

### 2.2. A Fifth Player: The Real "Tekram Delivery" (Beirut)

Independent research surfaced a real, unrelated Lebanese company trading under this exact name.
**Tekram Delivery** is owned by **HNZ Holding SAL** (Beirut; CEO/Founder Hiba Yazbeck), and first
launched its app in April 2023, with a public marketing launch — the "#TekramAinakYaLebnen" social
movement, run under the patronage of Lebanon's Ministry of Tourism — in August 2023. As of this
audit it is a roughly two-to-three-year-old operation, not a startup rumor: it has live apps on
both major app stores, a driver app ("Tekram Driver"), and a restaurant-partner app ("TekramRD").

Confirmed features and positioning:

- **Verticals:** food delivery (primary/flagship vertical), grocery shopping, general
  parcel/item/gift delivery, and **"Tekram Taxi"** — an in-app ride-booking feature. (Independent
  web coverage of Tekram Taxi as a standalone ride-hailing product is thin outside Tekram's own app
  store listings — treat it as a real but likely secondary feature relative to the food-delivery
  core, not a mature, independently-marketed ride-hailing service on par with Careem/Uber/Yassir,
  which are the established ride-hailing players in Lebanon.)
- **Subscription:** **"Tekram Plus"** — free delivery plus exclusive discounts, confirmed across
  multiple independent sources (also referenced in the founder-flag note at the top of this doc).
- **Loyalty:** a points program, redeemable for free meals (unnamed tiers, mechanics not detailed
  publicly).
- **AI-assisted support:** a named in-app assistant ("Zenah AI Assistant") for customer support —
  notable as an earlier and more concrete AI-feature commitment than any of Toters/NokNok/GoSawa/
  Gozilla show public evidence of.
- **Coverage:** Lebanon-focused; user reviews reference North Lebanon locations (e.g. El Mina/
  Tripoli area) as well as Beirut, suggesting coverage beyond the capital, but no official
  city-by-city coverage map was found. One low-confidence source snippet suggested a Syria
  presence — **not independently confirmed**, flagged rather than asserted.
- **Reception:** app-store aggregate ratings hover around 4.0/5, but review counts and a separate
  review-sentiment analysis are inconsistent across sources (one snapshot: 238 ratings; another:
  ~1,500) and an independent review-content analysis found a much rougher picture underneath the
  aggregate score — recurring complaints about delivery delays, app reliability/crashes, cold food
  on arrival, expired/misleading discount promotions, and customer-support responsiveness. Read
  this as "real product with real early-stage execution problems," not as a polished incumbent.
- **Payment methods:** not explicitly documented in public sources found; user reviews reference
  card payment, and cash-on-delivery is the market default (§5.4) — treat COD support as likely but
  unconfirmed for this specific app.

**Why this matters more than a naming coincidence:** this is not a hypothetical competitor — it is
a live, currently-operating company pursuing the exact same multi-vertical bundle (food + taxi +
grocery + subscription) this report proposes, under the identical brand name, in the identical
city. See §4.5 for the strategic implications.

---

## 3. SWOT Analysis of Key Competitors

### 3.1. Toters

*   **Strengths:**
    *   **Fleet scale & footprint:** Extensive driver pool and restaurant network across Lebanon.
    *   **Loyalty lock-in:** Toters Rewards is a confirmed points-based retention mechanism (exact
        tier structure not publicly documented, but the mechanic is real and long-running).
    *   **Super-app cross-selling:** Food delivery, grocery (Toters Fresh), and courier (Butler)
        under one app lowers customer acquisition cost per vertical.
*   **Weaknesses:**
    *   **Pricing perception:** Distance-based fees with surge multipliers are commonly cited by
        users as a pain point (exact figures not independently confirmed).
    *   **Merchant relations:** Marketplace commission is not publicly disclosed; regional peers in
        the same business model (e.g. Talabat) publish rates in the 15–30% range depending on
        market and tier. We use this as a directional reference only — Toters' actual rate should
        be confirmed directly with merchants before it's used in pricing decisions.
    *   **App scope/performance:** A broad feature set is a plausible source of load-time/memory
        complaints on lower-end devices; not independently benchmarked for this report.
*   **Opportunities:**
    *   Ride-hailing is not part of Toters' current public offering — an opening for a
        multi-vertical entrant, provided the entrant's driver economics are competitive.
*   **Threats:**
    *   A lower-commission entrant could pull price-sensitive merchants and pass savings to
        customers.

### 3.2. NokNok

*   **Strengths:**
    *   **Delivery speed:** NokNok's own marketing claims 15-minutes-or-less fulfillment from
        dark/cloud stores (corrected from an earlier "under 30 minutes" estimate).
    *   **Inventory accuracy:** Direct ownership of dark-store inventory is consistent with fewer
        checkout-time substitutions than a marketplace model.
    *   **Cold-chain management:** A proprietary fleet is a plausible advantage for protecting
        chilled/frozen goods during power cuts; not independently verified.
*   **Weaknesses / Correction:**
    *   **This weakness is closing.** As of a September 2025 relaunch, NokNok describes itself as
        an "Everything App" that is integrating **food-delivery aggregation** and new retail
        categories (sports, beauty, electronics, home & living, toys). The earlier characterization
        of NokNok as "confined to quick-commerce grocery, no food delivery, no retail" is **now
        outdated** and should not be relied on for competitive positioning without re-checking
        NokNok's current app scope. Ride-hailing is still not part of NokNok's offering as far as
        this audit could confirm — that gap appears to remain open.
    *   **High CapEx/OpEx:** Warehouses plus backup power for outages is a real structural cost of
        the dark-store model, independent of the aggregator expansion.
    *   **Geographic limits:** Historically concentrated in Greater Beirut and Mount Lebanon;
        expansion scope under the "Everything App" relaunch is not yet confirmed.
*   **Opportunities:**
    *   Hub-and-spoke expansion into suburban/rural areas remains open regardless of the aggregator
        pivot.
*   **Threats:**
    *   Fuel-cost inflation pressuring generator-dependent dark-store economics.
    *   Its own aggregator pivot narrows NokNok's differentiation from Toters and Gozilla, which
        may invite a price/commission war Tekram should watch rather than assume it can sit out of.

### 3.3. GoSawa

*   **Strengths:**
    *   **High discount positioning:** GoSawa's own marketing advertises discounts of **up to 90%**
        (corrected from an earlier "up to 70%" figure) — attractive to price-sensitive users, though
        actual realized discounts per deal will vary.
    *   **Asset-light model:** No kitchens, fleet, or inventory ownership.
    *   **Cash-network integration:** Support for Whish/OMT-style rails suits the unbanked segment.
*   **Weaknesses:**
    *   **No instant fulfillment:** Physical goods ship over several business days; unsuitable for
        hot food or quick-commerce groceries.
    *   **Redemption friction:** Vouchers require manual booking/validation with merchants.
    *   **No control over merchant-side experience.**
*   **Opportunities:**
    *   Instant dining-voucher redemption paired with real-time courier delivery is not something
        GoSawa currently offers.
*   **Threats:**
    *   Incumbent food-delivery apps folding a deals/voucher module into their existing
        high-frequency app (which is close to what this report proposes for Tekram itself).

### 3.4. The Real "Tekram Delivery" (Beirut, HNZ Holding)

*   **Strengths:**
    *   **Exact positioning match:** already runs the same food + taxi + grocery + subscription
        bundle this report proposes, so its wins and failures are the single most directly
        relevant data point available for this strategy.
    *   **Cultural/marketing positioning:** launched with a Ministry-of-Tourism-patronized national
        pride campaign ("#TekramAinakYaLebnen"), explicitly framing itself around Lebanese
        hospitality — a differentiated brand story versus the more generic positioning of
        Toters/NokNok/GoSawa/Gozilla.
    *   **Early AI feature commitment:** a named in-app AI support assistant is a concrete,
        shipped AI feature — ahead of any confirmed equivalent from the other four competitors.
*   **Weaknesses:**
    *   **Execution/reliability gap:** independent review analysis shows recurring complaints
        (delivery delays, app crashes, cold food, misleading discounts, weak customer support)
        well below the aggregate star rating — suggesting the operating model is right but the
        execution is not yet reliable at scale.
    *   **Thin ride-hailing evidence:** "Tekram Taxi" is real but not well-evidenced as a mature,
        independently-marketed product; it's unclear whether it meaningfully competes with
        Careem/Uber/Yassir or is a lightly-used in-app feature.
    *   **Unclear scale:** no independently confirmed data on driver count, restaurant count, order
        volume, or full city coverage — its actual market share versus Toters/Gozilla is not
        established by public sources.
*   **Opportunities (for this report's Tekram):**
    *   Its execution gaps are a legible playbook of specific failure modes to design against
        from day one (delivery ETA reliability, discount-integrity, support responsiveness) rather
        than discovering them after launch.
*   **Threats:**
    *   Being confused with, mistaken for, or perceived as copying an existing operator of the same
        name in the same city — a distinct risk from ordinary competitive threat, addressed in §4.5.

---

## 4. Tekram Strategic Recommendations

### 4.1. Dual-Engine Fleet Synergy & Vehicle Allocation Rules (Taxi + Delivery)
Unlike Toters, which is delivery-only, and Lebanese taxi operators (e.g. CTaxi, Charlie Taxi),
which do not offer food delivery, Tekram would operate a **unified driver pool** with
vehicle-vertical dispatch rules tuned to Beirut's traffic conditions.

*   **Scooter-based fleet (food & moto-taxi):** Riders toggle between hot-food delivery,
    quick-commerce grocery, and scooter passenger transport ("moto-taxi"), which is agile in
    congested streets (Mar Mikhael, Hamra) and cheap to run.
*   **Passenger-car fleet (taxi & bulk delivery):** Cars are reserved for standard/premium
    ride-hailing and bulk/heavy grocery orders, and contractually barred from low-fee hot-food
    routes (to avoid both cold food from traffic delays and poor unit economics — sedans consume
    materially more fuel per trip than scooters).
*   **Economic rationale:** cross-utilization raises driver hourly earnings and retains drivers
    with multi-channel income, without compromising SLA on either vertical.

### 4.2. Merchant-First Commission Model
Tekram's plan is a flat **15–20% commission**. The comparison figure used for Toters (25–30%) is a
**directional estimate**, not a confirmed number — Toters does not publish its commission rate.
Before committing to a specific undercut percentage, this should be validated with actual
restaurant partners currently on Toters/Gozilla/NokNok.
*   **Value proposition:** in exchange for a lower, more transparent commission, merchants would be
    contractually required to hold price parity between in-store and Tekram-app menus, so savings
    reach consumers rather than being absorbed as extra merchant margin.

### 4.3. Capital-Light Hybrid Grocery Model
Instead of building dark stores like NokNok, Tekram would partner with existing mid-sized
supermarkets:
*   **Safety stock buffers:** to guard against "phantom inventory" (in-store shoppers holding items
    not yet cleared at POS), the catalog engine marks an SKU out-of-stock once on-hand count drops
    below a small threshold (e.g. 3 units).
*   **Gig-economy picking:** for small/medium orders the delivery rider doubles as picker
    ("rider-as-picker"); larger orders route to gig-pickers paid per item/order, avoiding idle fixed
    labor costs.
*   **Hybrid micro-fulfillment dark hubs:** once a zone sustains ~200 orders/day, a small
    non-retail dark hub stocking only high-velocity SKUs can bypass supermarket aisle congestion
    and retail markups while preserving fast pick times — a staged, demand-triggered CapEx path
    rather than upfront dark-store buildout.
*   **Cold-chain integration:** pickers scan items via a picker app; store refrigeration telemetry
    feeding into the partner agreement lets cold-chain breaches auto-pause fresh-food orders from
    the affected merchant.

### 4.4. Essential Feature Blueprint

These are **product-strategy concepts for a future multi-vertical Tekram**, not features scoped
into this assessment's Part 2 backend build.

#### A. Unified Cross-Vertical Subscription ("Tekram Pass")
A monthly subscription bundling cross-vertical perks: free delivery above $10 on food/grocery, a
flat discount on taxi rides, waived housekeeping booking fees, priority dispatch in bad weather.
**Competitive note:** this is not unclaimed territory — Gozilla already runs a $5.99/month
unlimited-free-delivery subscription in this exact market (§2.1), and the real, unrelated company
"Tekram Delivery" (HNZ Holding) already runs a product called "Tekram Plus" with a similar free
delivery/discount pitch. Any pricing decision here should start from those two data points, not
from a blank slate.

#### B. Unified Multi-Vertical Loyalty Engine
A shared points ledger across verticals (e.g. 1 point/$1 on rides/food, 2 points/$1 on premium
housekeeping; 100 points = $1.00 wallet credit) — differentiated from Toters Rewards' single-vertical
points and GoSawa/NokNok's lack of a confirmed points program.

#### C. Dynamic Multi-Vertical Pricing & Surge Engine
Surge based on driver density/traffic/weather; delivery-fee indexing to Ministry of Energy fuel
pricing sheets to protect driver earnings against fuel inflation; a dynamic small-order surcharge
instead of a hard checkout block.

#### D. Self-Service Merchant Ads & Sponsored Bidding
A CPC sponsored-bidding surface in the merchant dashboard (keyword/category bidding for search
placement), with daily budget caps and auto-pause on exhaustion — a B2B revenue lever none of the
three core competitors appear to expose to merchants as self-service today (not independently
confirmed for Gozilla).

#### E. Multi-Currency Closed-Loop Wallet
A closed-loop wallet, valued at its core in USD, lets customers and riders hold and transact
balances while Lebanon's currency situation stays volatile: value is tracked in USD to remove
FX-arbitrage and parallel-market exposure, with LBP shown only as a point-in-time display
conversion. The same wallet is the mechanism for OMT/Whish top-ups and for storing change from
cash-on-delivery orders, converting it into a stable-currency balance instead of leaving it as
loose LBP cash. (The ledger and balance-update implementation are an architecture decision — see
docs/architecture.md.)

### 4.5. Strategic Consideration: A Same-Named, Same-Concept Incumbent Already Exists

This report's name and core concept ("Tekram": food + taxi + grocery + a unifying subscription)
were arrived at independently of any existing company — "Tekram" is the name assigned by this
assessment's brief, not a name chosen after market research. Independent research during this
audit (§2.2, §3.4) found that a real, unrelated company — **Tekram Delivery**, owned by HNZ
Holding — already operates exactly this bundle, under the identical name, in Beirut, since 2023.

**What this means, practically:**

1. **Naming risk, if ever taken beyond this assessment.** Operating a second, unrelated company
   under an identical consumer-facing brand name in the same city and category would create real
   confusion (and, depending on what if any trademark protection HNZ Holding holds on "Tekram" in
   Lebanon, potential legal exposure). This is not a concern for the assessment itself — the name
   is a given constraint of the brief — but it is a decision point before any real-world use of
   this brand.
2. **Differentiation, if the name were to stay.** If "Tekram" were ever used commercially, the
   existing company's visible weaknesses (§3.4 — delivery reliability, discount-integrity
   perception, support responsiveness) are the most concrete, market-validated openings available:
   out-executing a same-named incumbent on the exact same promise is a sharper strategy than
   competing against Toters/Gozilla in the abstract.
3. **No action required for this assessment.** Since the brief assigns "Tekram" as the exercise's
   name, this section is a disclosure and a decision point for the founder, not a recommendation to
   change anything in this document. The rest of this report's strategic content (fleet synergy,
   commission model, hybrid grocery model, feature blueprint) stands on its own merits regardless
   of what the eventual real-world product is named.

---

## 5. Operational Rationale (Lebanese Context)

Operating successfully in Lebanon means designing for infrastructure failure as the default case,
not the exception.

### 5.1. Mapping, Routing, and Live Tracking
Commercial mapping data for Lebanon's informal, landmark-based neighborhoods is frequently
incomplete, and third-party routing/mapping APIs are priced per call — for reference, Google's own
published pricing for route-distance calculations runs around $5 per 1,000 calculations — a
per-call cost structure that does not scale economically against a live operation continuously
tracking hundreds of drivers in real time. That's a unit-economics argument for a cost-controlled,
self-operated approach to routing and live driver tracking, tuned to local road and neighborhood
conditions (e.g. distinguishing highway-suited taxi routes from alley-suited scooter routes), with
a resilience plan for GPS interference near the airport and coastline (a check-in step at pickup
and drop-off that doesn't depend on satellite signal). The concrete routing and live-tracking
implementation, and its ongoing infrastructure cost, are architecture decisions — see
docs/architecture.md (Part 1) and docs/devops.md (Part 5).

### 5.2. Landmark-Based Address Profiles
Lebanon has no functional postal-code system, and GPS pin drift of tens of meters is common enough
to misdirect drivers. Address records should capture structured, landmark-anchored detail (street,
building, floor, apartment, nearest landmark, free-text directions) rather than relying on a map
pin alone, and last-mile calls between customer and rider should stay masked in-app rather than
exposing either party's real phone number. (The specific addressing schema and call-masking
mechanism are Part 1/Part 3 concerns.)

### 5.3. Offline-First Operation
Frequent power and network outages mean the customer and driver apps cannot assume a constant
connection: both need to keep working through a dropped connection — recording actions like
arrival or pickup locally — and reconcile automatically once connectivity returns, with a fallback
path for critical updates when a driver stays offline for an extended stretch. The transport and
synchronization mechanism that delivers this is an architecture decision — see docs/architecture.md
(Part 1).

### 5.4. Cash Liquidity, Money Transfer Integrations (OMT & Whish Money), and Rider Credit Limits
Cash on delivery is the dominant payment method in Lebanon's e-commerce/delivery market — public
sources put it anywhere from "more than two-thirds" to over 80% of transactions depending on
sector and source, so treat any single precise percentage as directional rather than a hard
statistic. This creates rider security risk, reconciliation overhead, and default risk if a static
cash-on-hand cap is used (it would also suspend active drivers mid-shift during busy periods).

*   **Net-exposure rider credit limits**, computed dynamically instead of a static cap:

$$\text{Net Exposure} = \text{Cash-on-Hand} - \text{Unpaid Rider Earnings (Digital Trips)} - \text{Rider Security Deposit}$$

*   **Trust-based tiering:** credit limits scale with rider experience/rating (e.g. a lower cap for
    new riders, higher for established, highly-rated riders).
*   **Direct supermarket cash settlement:** riders can settle goods cost in cash at pickup,
    immediately reducing their tracked cash-on-hand.
*   **OTC cash-out/top-up:** riders and customers can settle or top up balances at any OMT or
    Whish Money branch (both are real, Banque du Liban-licensed money-transfer operators, confirmed
    during this audit) — offsetting ledger balances in real time without a physical Tekram office
    visit.

### 5.5. Architectural Fit
The operating model above is deliberately designed to extend to new verticals (Pharmacy, Parcels,
and beyond) without a ground-up rebuild each time — that extensibility is a Part 1 architecture
requirement, not just a strategy aspiration. The concrete system design lives in
docs/architecture.md (Part 1), the data model in docs/database-schema.md (Part 3), and the
per-endpoint API contracts in docs/02-prd.md §5. Nothing in this report should be read as a
substitute for those documents.
