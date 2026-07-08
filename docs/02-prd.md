# Product Requirements Document (PRD) — Tekram Multi-Vertical Super-App

**Document Reference:** docs/02-prd.md (finalized from docs/gemini/02-prd.md)  
**Owner:** PM — Doc Intake (Tekram Technical Lead Assessment)  
**Date:** 2026-07-08  
**Status:** Final draft — pending founder approval  
**Target Platform:** Tekram Multi-Vertical Super-App (Lebanese Market)  

> **Scope at a glance:** Only **auth + restaurants + orders** are built and graded (Part 2 —
> Coding Challenge). Every other epic in this document is **strategic vision** that informs Part 1
> (Architecture), Part 3 (Database Design), and the business strategy — it is documented, not
> implemented. See **§3 Build Scope Classification** for the authoritative core-vs-vision split.

---

## 1. Introduction

### 1.1 Project Context
This Product Requirements Document (PRD) defines the functional, technical, and operational requirements for **Tekram**, a next-generation multi-vertical on-demand super-app designed specifically for the Lebanese market. Tekram consolidates passenger transportation, food delivery, supermarket grocery shopping, and housekeeping services into a single platform. 

### 1.2 Macroeconomic & Infrastructure Challenges in Lebanon
Unlike traditional super-apps operating in stable markets, Tekram is built to withstand and exploit the unique operational challenges of the Lebanese market:
*   **Currency Volatility & Parallel Exchange Markets:** Rapid fluctuations in the value of the Lebanese Pound (LBP) require a platform that maintains its core financial ledger in United States Dollars (USD) while dynamically converting transaction values using real-time market exchange rates.
*   **Cash-Based and Unbanked Economy:** Over 80% of transactions occur via Cash on Delivery (COD) or Cash on Pickup. The unbanked population requires alternative digital top-up solutions (such as integrations with cash collection networks like OMT and Whish Money) and rigorous cash-on-hand tracking for logistics riders to prevent default risks.
*   **Landmark-Based Navigation:** The absence of a functional postal code system and high coordinate GPS inaccuracies in informal neighborhoods make traditional street addresses insufficient. Address validation must rely on structured landmark identifiers.
*   **Infrastructure Deficits (GPS Jamming and Network Drops):** Frequent cellular network drops and regional GPS spoofing (especially near airport zones and coastal regions) require offline-first mobile states, SMS fallback channels, and secure validation mechanisms (such as physical QR code check-ins) that do not rely on cellular or satellite signals.

### 1.3 Strategic Solution Architecture
Tekram addresses these challenges through three primary strategic pillars:
*   **Dual-Engine Fleet Synergy:** A shared pool of delivery riders (scooters) and taxi drivers (sedans). During off-peak transportation hours, taxi drivers can assist with bulk deliveries, while delivery riders can switch to Moto-Taxi passenger rides in congested neighborhoods.
*   **Merchant-First Commission Model:** Flat 15–20% merchant commissions paired with contractual in-store price parity guarantees, passing direct savings to consumers.
*   **Capital-Light Hybrid Grocery Model:** Leveraging partnerships with mid-sized local supermarkets rather than capital-intensive dark stores. The catalog system uses safety stock buffers to prevent inventory mismatches, transitioning to specialized non-retail dark hubs only when zone volumes reach 200 orders per day.

---

## 2. System Scope & Decoupling Architecture

```
+-----------------------------------------------------------------------------------+
|                            TEKRAM MONOLITH DECOUPLING                             |
+-----------------------------------------------------------------------------------+
                                          |
         +--------------------------------+--------------------------------+
         |                                |                                |
         v                                v                                v
 +---------------+                +---------------+                +---------------+
 |  H3 & REDIS   |                |  ASYNC WALLET |                |   OSRM LUA    |
 |  GEOSPATIAL   |                |  AGGREGATION  |                |   ROUTING     |
 +---------------+                +---------------+                +---------------+
 | Decouples SQL |                | Decouples row |                | Prioritizes   |
 | databases     |                | locks on peak |                | narrow alleys |
 | from 200Hz    |                | merchant check|                | for scooters; |
 | driver GPS    |                | out ledgers via|                | highways for  |
 | location pings|                | Redis queue   |                | sedans/taxis  |
 +---------------+                +---------------+                +---------------+
```

To maintain high scalability under a modular monolith directory structure, the system implements specific decoupling mechanisms:
1.  **Modular Monolith Boundaries:** Directory-level division of concerns (`src/Modules/Core`, `src/Modules/Billing`, etc.). Modules communicate asynchronously using internal event publishers or synchronous clean interfaces.
2.  **Table-per-Type (TPT) Schema Design:** Transactional bookings inherit from a base table `core.bookings` and extend to vertical-specific tables (`food_delivery.food_orders`, `ride_hailing.taxi_rides`, etc.) to isolate vertical concerns while maintaining unified billing and reporting.
3.  **Geospatial Tracking Decoupling:** Relational database systems are shielded from high-frequency driver location updates (e.g. 200Hz ping rates). Driver locations are updated and queried using Redis Geospatial commands (`GEOADD`, `GEOSEARCH`) and Uber H3 cell hashes in memory. Relational database writes are triggered only upon order state transitions.
4.  **Asynchronous Wallet Ledger Writes:** Decoupling wallet updates from checkouts. Sub-second API responses are achieved by pushing checkout ledgers to a Redis-backed background queue which updates cached wallet balances asynchronously, avoiding row locking on merchant and platform wallets.
5.  **Asynchronous Click Fraud & Ads Processing:** Sponsored click records are processed in background workers to prevent CPC click recording from blocking customer catalog queries.

---

## 3. Build Scope Classification (Gradable Core vs Strategic Vision)

This PRD describes Tekram's full multi-vertical platform, but the assessment builds and grades
only a narrow slice of it. Per the assessment brief, **Part 2 — Coding Challenge (25 pts, hard
gate 18/25)** requires a backend with: **JWT authentication (register/login)**, **restaurant
listing/search/pagination**, and **order creation** with stock validation, delivery-fee
calculation, coupon support, persistence, and clean API responses (bonus: Clean Architecture,
Repository Pattern, DI, Unit Tests). That — and nothing else — is the **buildable core**.
Everything else below is retained because it feeds Part 1 (Architecture), Part 3 (Database) and
the business-strategy narrative, but it is **not implemented** in this assessment.

| Capability | PRD Issues | Build scope |
|---|---|---|
| JWT auth (register / login) + email & phone OTP verification | #1, #2, #2A | **CORE** — built & graded (Part 2, Slice 1) |
| Restaurant list / search / pagination | #11 | **CORE** — built & graded (Part 2) |
| Restaurant menu (supplies order items & prices) | #12 | **CORE-supporting** — built to enable orders |
| Order creation (stock, delivery fee, coupon, persistence) | #13 | **CORE** — built & graded (Part 2) |
| Address book & user profile | #3–#7 | Vision — not built |
| Billing, wallet, ledger, multi-currency | #8–#10 | Vision — not built |
| Driver location tracking | #14 | Vision — not built |
| Grocery / supermarket commerce | #15–#18 | Vision — not built |
| Ride-hailing & vehicle allocation | #19–#22 | Vision — not built |
| Housekeeping & scheduled services | #23–#25 | Vision — not built |
| Loyalty, subscriptions, merchant ads | #26–#30 | Vision — not built |

**Deliverable paths.** The graded core follows this repo's module convention —
`src/auth/`, `src/restaurants/`, `src/orders/`, and `tests/` — per
[docs/technical-decisions.md](./technical-decisions.md) (TD-001) and the project plan §6. The
`src/Modules/<PascalCase>` paths used illustratively elsewhere in this document are **not** the
repo's layout; the core-issue deliverable paths below have been corrected to the real convention.

**Simplification of the core against the vision.** The vision-level acceptance criteria below
reference heavy infrastructure — Uber H3 geofencing, OSRM Lua routing, Redis geospatial caches,
async wallet queues, CPC sponsor prioritisation. The graded Part-2 build deliberately **excludes**
that infrastructure and satisfies the brief with documented, simpler mechanisms (e.g. a
distance- or zone-based delivery fee instead of live OSRM routing; a stock-count / boolean
availability check instead of cold-chain telemetry). The architect's Part-2 spec ratifies the
exact simplified rules before implementation. Where a core issue's criteria are trimmed for the
graded build, this is called out inline under **Graded-core note**.

**Notification gateway is mocked.** Email and phone OTP **verification logic** — OTP
generation, hashed storage, expiry, per-channel confirm, resend, and the unverified-user gate —
**is built and graded** (Slice 1, auth). Only the **outbound send** is mocked, behind
`EMAIL_MOCK` (email OTP) and `SMS_MOCK` (phone OTP) — mirroring this repo's established
per-channel `<CHANNEL>_MOCK` convention (`EMAIL_MOCK`/`BILLING_MOCK`). In mock mode the OTP is
written to the application log and returned deterministically to automated tests (no real
message is sent); real SMS/email gateway integration is an explicit later swap, **not** part of
the graded build. See issues #1 and #2A.

---

## 4. Epic Modules

This PRD groups the required functional improvements into seven Epic Modules, aligning with the project's directory layout and core priorities.

### Epic 1: Auth, Accounts & Landmark-Based Address Profiles
*   **Build scope:** Partially core — auth endpoints incl. email & phone OTP verification (#1, #2, #2A) are built & graded (Slice 1); addresses/profile (#3–#7) are vision.
*   **High-level Business Objective:** Provide secure, role-based registration, credential verification, and structured landmark address validation to bypass the lack of postal codes in Lebanon.
*   **Target Users:** Customers, Drivers/Riders, Merchants (Supermarkets/Restaurants), Administrators.
*   **Functional Scope:** Registration and JWT authentication for all user roles, mandatory email & phone OTP verification (outbound send mocked via `EMAIL_MOCK`/`SMS_MOCK`), structured landmark address configuration (`nearest_landmark` required), and VoIP caller masking routing.

### Epic 2: Multi-Currency Billing, Wallet & Net Exposure Management
*   **Build scope:** Strategic vision — not built in this assessment.
*   **High-level Business Objective:** Bypass the unbanked environment and currency volatility through a base USD ledger, short-lived LBP translation tokens, and dynamic driver cash exposure checks.
*   **Target Users:** Customers, Drivers/Riders, Money Transfer Partners (OMT/Whish).
*   **Functional Scope:** Double-entry USD ledger logging, LBP dynamic conversion at transaction timestamp (applying 1.5% markup), and driver Net Exposure calculations (`Net Exposure = Cash-on-Hand - Unpaid Earnings - Security Deposit`) with trust tiers to control credit risk.

### Epic 3: On-Demand Food Delivery Engine
*   **Build scope:** Partially core — restaurants/menu/orders (#11–#13) are built & graded; driver tracking (#14) is vision.
*   **High-level Business Objective:** Deliver prepared meals under a low-commission model (15-20%) with price parity guarantees and strict scooter-only dispatch.
*   **Target Users:** Customers, Restaurants, Scooter Riders.
*   **Functional Scope:** Restaurant menu browsing, food order placements extending `core.bookings`, scooter-only vehicle constraints, OSRM routing calculations (using narrow street profiles), and dual QR code validations at pickup and dropoff to mitigate regional GPS jamming.

### Epic 4: Capital-Light Grocery Commerce
*   **Build scope:** Strategic vision — not built in this assessment.
*   **High-level Business Objective:** Coordinate grocery shopping across supermarket partners using gig-economy picking workflows and safety stock calculations.
*   **Target Users:** Customers, Supermarkets, Gig Pickers / Riders-as-Pickers.
*   **Functional Scope:** Supermarket catalog browsing, safety stock filters (flags items out-of-stock when physical store inventory < 3), gig picking assignment rules (rider-picker vs dedicated picker), and refrigeration temperature telemetry validation.

### Epic 5: Ride-Hailing & Vehicle Allocation
*   **Build scope:** Strategic vision — not built in this assessment.
*   **High-level Business Objective:** Provide agile on-demand passenger transport while optimizing passenger vehicle allocation.
*   **Target Users:** Customers (Passengers), Sedan Drivers, Moto-Taxi Riders.
*   **Functional Scope:** Fare estimates, passenger ride requests extending `core.bookings`, Moto-Taxi (scooters) vs Standard/Premium (cars) vehicle matching, Redis spatial index tracking, and weekly fuel-indexed dynamic base rate updates.

### Epic 6: Housekeeping & Scheduled Home Services
*   **Build scope:** Strategic vision — not built in this assessment.
*   **High-level Business Objective:** Provide scheduled home services with a transparent, duration-based pricing structure.
*   **Target Users:** Customers, Housekeepers (Service Providers).
*   **Functional Scope:** Service catalog retrieval, schedule matching in geographic clusters, platform billing calculations, and scheduling adjustments/cancellation policies.

### Epic 7: Loyalty, Subscriptions, and Merchant Bidding
*   **Build scope:** Strategic vision — not built in this assessment.
*   **High-level Business Objective:** Drive platform loyalty, recurrent subscription revenue, and merchant ad monetization.
*   **Target Users:** Customers, Restaurants/Supermarket Merchants.
*   **Functional Scope:** "Tekram Pass" subscription benefits application (free delivery >$10, 5% taxi discounts, waived fees), shared points loyalty engine (100 points = $1 cashback), and merchant sponsored CPC bidding with click-fraud filtering.

---

## 5. Atomic API Issues

### Epic 1: Auth, Accounts & Landmark-Based Address Profiles

#### Issue #1: [Core] User Registration
> **Build scope:** CORE — built & graded in Part 2.
*   **Method & Route:** `POST /api/auth/register`
*   **Description:** Registers a new account and creates an associated closed-loop billing wallet.
*   **Request Payload JSON:**
    ```json
    {
      "name": "Jean Dupont",
      "email": "jean.dupont@example.com",
      "phone": "+96170123456",
      "password": "Password123!",
      "role": "customer"
    }
    ```
*   **Response Payload JSON:**
    ```json
    {
      "id": "e8a34b2c-29ad-4eb4-b9c1-840a3f892b1a",
      "name": "Jean Dupont",
      "email": "jean.dupont@example.com",
      "phone": "+96170123456",
      "role": "customer",
      "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJlOGEzNGIyYy0yOWFkLTRlYjQtYjljMS04NDBhM2Y4OTJiMWEiLCJyb2xlIjoiY3VzdG9tZXIiLCJleHAiOjE3ODk1Njk2MDB9.abcdefg...",
      "token_expires_at": "2026-07-08T16:25:59Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Enforces presence of name, email, phone, password, and role.
    - [ ] Validates phone format using Lebanese mobile format (must start with country code `+961` and be followed by 7 or 8 digits).
    - [ ] Role must be verified against permitted values: `customer`, `driver`, `merchant`, `admin`.
    - [ ] Ensures email and phone uniqueness in the database (returns `409 Conflict` on duplicates).
    - [ ] Enforces password strength rules (minimum 8 characters, at least 1 number, 1 uppercase letter).
    - [ ] Hashes the password using BCrypt or Argon2id before saving.
    - [ ] Triggers the creation of a closed-loop wallet in `billing.wallets` initialized to `0.0000` USD and `0.00` LBP balances.
    - [ ] Creates the account in an unverified state: `email_verified = false` and `phone_verified = false` (both booleans are included in the response body).
    - [ ] OTP issuance: generates two independent 6-digit numeric one-time codes (one for email, one for phone), stores each hashed with a 10-minute expiry, and dispatches them through the notification gateway.
    - [ ] Gateway is mocked: with `EMAIL_MOCK=true` / `SMS_MOCK=true` (default in this build) no real email/SMS is sent — the code is written to the application log and surfaced deterministically to automated tests (e.g. a test-only accessor); toggling the flag off routes to the real gateway, which is a later swap and not built here.
    - [ ] Returns `201 Created` with a valid JWT token on success (the JWT lets the new user call the verification endpoints in #2A).
    - [ ] Pipeline: Full State Machine (`type:code`). Parent Epic: Part 2 coding challenge.
    - [ ] Deliverable path: `src/auth/**` (+ scaffold/shared types)

#### Issue #2: [Core] User Login
> **Build scope:** CORE — built & graded in Part 2.
*   **Method & Route:** `POST /api/auth/login`
*   **Description:** Authenticates user credentials and returns a session JWT.
*   **Request Payload JSON:**
    ```json
    {
      "identifier": "+96170123456",
      "password": "Password123!"
    }
    ```
*   **Response Payload JSON:**
    ```json
    {
      "id": "e8a34b2c-29ad-4eb4-b9c1-840a3f892b1a",
      "name": "Jean Dupont",
      "email": "jean.dupont@example.com",
      "phone": "+96170123456",
      "role": "customer",
      "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJlOGEzNGIyYy0yOWFkLTRlYjQtYjljMS04NDBhM2Y4OTJiMWEiLCJyb2xlIjoiY3VzdG9tZXIiLCJleHAiOjE3ODk1Njk2MDB9.abcdefg...",
      "token_expires_at": "2026-07-08T16:25:59Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Accepts either registered email or phone number in the `identifier` field.
    - [ ] Validates the input password against the stored BCrypt/Argon2id hash.
    - [ ] Enforces rate limiting on login attempts (maximum 5 attempts per identifier/IP within 15 minutes, returning `429 Too Many Requests`).
    - [ ] Returns `401 Unauthorized` with a generic, non-specific error message for incorrect credentials to prevent user enumeration.
    - [ ] Returns `200 OK` with a valid JWT containing the user ID, email, and role on success.
    - [ ] Pipeline: Full State Machine (`type:code`). Parent Epic: Part 2 coding challenge.
    - [ ] Deliverable path: `src/auth/**`

#### Issue #2A: [Core] Verify & Resend Email / Phone OTP
> **Build scope:** CORE — built & graded in Part 2 (Slice 1, auth). Inserted as `#2A` so the
> existing #3–#30 numbering and every range cross-reference stay intact.
*   **Method & Routes:**
    - `POST /api/auth/verify/email` — confirm the email OTP
    - `POST /api/auth/verify/phone` — confirm the phone OTP
    - `POST /api/auth/verify/resend` — reissue an OTP for one channel
*   **Description:** Confirms the one-time codes issued at registration (#1) for each channel and
    resends a fresh code on demand. The verification logic is fully built and graded; only the
    outbound send is mocked (`EMAIL_MOCK` / `SMS_MOCK`).
*   **Request Payload JSON (`verify/email` and `verify/phone`):**
    ```json
    {
      "code": "123456"
    }
    ```
*   **Request Payload JSON (`verify/resend`):**
    ```json
    {
      "channel": "email"
    }
    ```
*   **Response Payload JSON (`verify/*` success):**
    ```json
    {
      "channel": "email",
      "email_verified": true,
      "phone_verified": false,
      "fully_verified": false
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Requires a valid JWT (the account created in #1).
    - [ ] Confirm: validates the submitted `code` against the stored (hashed) OTP for that channel; on a correct, unexpired, unused code it marks that channel verified (`email_verified` / `phone_verified` = `true`) and returns `200 OK` with the current verification booleans.
    - [ ] Returns `422 Unprocessable Entity` (or `400 Bad Request`) with a generic, non-enumerating message for an invalid or expired code, and does **not** mark the channel verified.
    - [ ] OTP expiry: a code is rejected once older than 10 minutes even if otherwise correct.
    - [ ] Single-use: a successfully consumed code cannot be reused.
    - [ ] Resend: issues a fresh OTP for the named `channel` (invalidating any prior outstanding code), re-dispatches it through the mocked gateway, and returns `200 OK`. Rate-limited to a maximum of 3 resends per channel per user within 15 minutes (`429 Too Many Requests` beyond that).
    - [ ] Mock behaviour is observable: with `EMAIL_MOCK` / `SMS_MOCK` enabled, the freshly issued code is logged and exposed deterministically to tests so a test can register → read the code → confirm, entirely via the running API with no real send.
    - [ ] Gating rule: an unverified account (either channel `false`) may register and log in (it holds a valid JWT), but is **blocked from placing orders** — `POST /api/food/orders` (#13) returns `403 Forbidden` with a `verification_required` error until **both** `email_verified` and `phone_verified` are `true`.
    - [ ] Pipeline: Full State Machine (`type:code`). Parent Epic: Part 2 coding challenge.
    - [ ] Deliverable path: `src/auth/**`

#### Issue #3: [Core] Create Address
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `POST /api/addresses`
*   **Description:** Creates a new address profile for landmark-based navigation.
*   **Request Payload JSON:**
    ```json
    {
      "city": "Beirut",
      "district": "Achrafieh",
      "street_name": "Sassine Street",
      "building_name": "Sassine Tower",
      "floor": "3rd Floor",
      "apartment": "Flat 3B",
      "nearest_landmark": "Next to ABC Mall",
      "additional_directions": "Take the stairs to the left after the lobby.",
      "latitude": 33.8892,
      "longitude": 35.5184,
      "address_label": "Home"
    }
    ```
*   **Response Payload JSON:**
    ```json
    {
      "id": "d04e9a78-d004-b36b-f4a0-a2b0e67b2d2a",
      "user_id": "e8a34b2c-29ad-4eb4-b9c1-840a3f892b1a",
      "city": "Beirut",
      "district": "Achrafieh",
      "street_name": "Sassine Street",
      "building_name": "Sassine Tower",
      "floor": "3rd Floor",
      "apartment": "Flat 3B",
      "nearest_landmark": "Next to ABC Mall",
      "additional_directions": "Take the stairs to the left after the lobby.",
      "latitude": 33.8892,
      "longitude": 35.5184,
      "address_label": "Home",
      "created_at": "2026-07-08T11:26:00Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Requires valid JWT authentication.
    - [ ] Enforces a mandatory `nearest_landmark` string (cannot be null, empty, or whitespace; minimum 5 characters).
    - [ ] Validates that coordinates (`latitude` between 33.0 and 34.7, `longitude` between 35.0 and 36.6) reside within Lebanon's geographic boundaries.
    - [ ] Enforces `address_label` is one of: `Home`, `Work`, `Other`.
    - [ ] Saves the address record in `core.addresses` associated with the authenticated user ID.
    - [ ] Returns `201 Created` with the saved address record.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 1.
    - [ ] Deliverable path: `src/Modules/Core`

#### Issue #4: [Core] List User Addresses
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `GET /api/addresses`
*   **Description:** Retrieves all addresses saved in the user's address book.
*   **Request Payload JSON:** None (Header contains JWT).
*   **Response Payload JSON:**
    ```json
    [
      {
        "id": "d04e9a78-d004-b36b-f4a0-a2b0e67b2d2a",
        "city": "Beirut",
        "district": "Achrafieh",
        "street_name": "Sassine Street",
        "building_name": "Sassine Tower",
        "floor": "3rd Floor",
        "apartment": "Flat 3B",
        "nearest_landmark": "Next to ABC Mall",
        "additional_directions": "Take the stairs to the left after the lobby.",
        "latitude": 33.8892,
        "longitude": 35.5184,
        "address_label": "Home",
        "created_at": "2026-07-08T11:26:00Z"
      }
    ]
    ```
*   **Acceptance Criteria:**
    - [ ] Requires valid JWT authentication.
    - [ ] Restricts database query to return only records from `core.addresses` belonging to the calling user ID.
    - [ ] Returns `200 OK` with an array of address JSON structures (returns empty array if no addresses are configured).
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 1.
    - [ ] Deliverable path: `src/Modules/Core`

#### Issue #5: [Core] Update Address
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `PUT /api/addresses/{id}`
*   **Description:** Updates details of an existing saved address.
*   **Request Payload JSON:**
    ```json
    {
      "city": "Beirut",
      "district": "Achrafieh",
      "street_name": "Sassine Street",
      "building_name": "Sassine Tower",
      "floor": "3rd Floor",
      "apartment": "Flat 3C",
      "nearest_landmark": "Next to ABC Mall",
      "additional_directions": "Take the elevator, updated apartment.",
      "latitude": 33.8892,
      "longitude": 35.5184,
      "address_label": "Home"
    }
    ```
*   **Response Payload JSON:**
    ```json
    {
      "id": "d04e9a78-d004-b36b-f4a0-a2b0e67b2d2a",
      "user_id": "e8a34b2c-29ad-4eb4-b9c1-840a3f892b1a",
      "city": "Beirut",
      "district": "Achrafieh",
      "street_name": "Sassine Street",
      "building_name": "Sassine Tower",
      "floor": "3rd Floor",
      "apartment": "Flat 3C",
      "nearest_landmark": "Next to ABC Mall",
      "additional_directions": "Take the elevator, updated apartment.",
      "latitude": 33.8892,
      "longitude": 35.5184,
      "address_label": "Home",
      "created_at": "2026-07-08T11:26:00Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Requires valid JWT authentication.
    - [ ] Returns `404 Not Found` if the address ID does not exist in the database.
    - [ ] Returns `403 Forbidden` if the requested address ID exists but does not belong to the calling user ID.
    - [ ] Enforces address validation rules, including a mandatory `nearest_landmark` field.
    - [ ] Updates the row in `core.addresses` and returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 1.
    - [ ] Deliverable path: `src/Modules/Core`

#### Issue #6: [Core] Delete Address
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `DELETE /api/addresses/{id}`
*   **Description:** Removes a saved address from the user's address book.
*   **Request Payload JSON:** None.
*   **Response Payload JSON:**
    ```json
    {
      "success": true,
      "message": "Address deleted successfully"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Requires valid JWT authentication.
    - [ ] Returns `404 Not Found` if the address ID does not exist.
    - [ ] Returns `403 Forbidden` if the address does not belong to the calling user ID.
    - [ ] Deletes the matching row in `core.addresses`.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 1.
    - [ ] Deliverable path: `src/Modules/Core`

#### Issue #7: [Core] Get User Profile
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `GET /api/users/profile`
*   **Description:** Retrieves profile parameters and membership subscription tier benefits.
*   **Request Payload JSON:** None.
*   **Response Payload JSON:**
    ```json
    {
      "id": "e8a34b2c-29ad-4eb4-b9c1-840a3f892b1a",
      "name": "Jean Dupont",
      "email": "jean.dupont@example.com",
      "phone": "+96170123456",
      "role": "customer",
      "created_at": "2026-07-08T11:26:00Z",
      "active_subscription": {
        "subscription_id": "s804f9a7-0d00-4b36-bf4a-0a2b0e67b2d3",
        "plan_name": "Tekram Pass",
        "expires_at": "2026-08-08T11:26:00Z",
        "benefits": {
          "free_delivery_threshold_usd": 10.00,
          "ride_discount_percentage": 5.0,
          "platform_fee_waived": true,
          "priority_dispatch": true
        }
      }
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Requires valid JWT authentication.
    - [ ] Fetches data from `core.accounts` using the authenticated user ID.
    - [ ] Queries the subscription status: if an active subscription exists in `subscriptions.memberships`, populated benefits are returned; otherwise, `active_subscription` returns `null`.
    - [ ] Excludes sensitive credential fields (such as hashed passwords or tokens) from the output.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 1.
    - [ ] Deliverable path: `src/Modules/Core`

---

### Epic 2: Multi-Currency Billing, Wallet & Net Exposure Management

#### Issue #8: [Billing] Wallet Balance Retrieve
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `GET /api/billing/wallet`
*   **Description:** Retrieves wallet balance in base USD, returning a dynamic conversion to LBP with a short-lived expiration token to prevent exchange rate arbitrage.
*   **Request Payload JSON:** None.
*   **Response Payload JSON:**
    ```json
    {
      "wallet_id": "w704e9a7-9d00-4b36-bf4a-0a2b0e67b2d2",
      "user_id": "e8a34b2c-29ad-4eb4-b9c1-840a3f892b1a",
      "balance_usd": 25.5000,
      "balance_lbp": 2295000.00,
      "exchange_rate": 90000.00,
      "rate_validity_token": "token_ex_90000_1789569600",
      "rate_expires_at": "2026-07-08T11:27:00Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Requires valid JWT authentication.
    - [ ] Fetches the current `balance_usd` from `billing.wallets` using the calling user ID.
    - [ ] Fetches the active system exchange rate (updated periodically from the platform's pricing configurations).
    - [ ] Translates `balance_usd` to LBP (`balance_lbp = balance_usd * exchange_rate`).
    - [ ] Generates an exchange rate token incorporating the exchange rate value and an expiration timestamp. The token must expire 60 seconds after generation (`rate_expires_at` = current UTC + 60s).
    - [ ] Avoids relational database row locks by utilizing read-uncommitted/dirty-read transactions or retrieving cached wallet values.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 2.
    - [ ] Deliverable path: `src/Modules/Billing`

#### Issue #9: [Billing] OMT/Whish Cash Top-up Webhook
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `POST /api/billing/webhooks/top-up`
*   **Description:** Incoming provider webhook to process customer wallet top-ups or driver cash clearance deposits.
*   **Request Payload JSON:**
    ```json
    {
      "provider": "Whish",
      "transaction_reference": "WHISH-TX-987654321",
      "account_barcode": "TEKRAM-USER-123456",
      "amount": 500000.00,
      "currency": "LBP",
      "signature": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
      "timestamp": "2026-07-08T11:26:00Z"
    }
    ```
*   **Response Payload JSON:**
    ```json
    {
      "status": "success",
      "transaction_reference": "WHISH-TX-987654321",
      "wallet_id": "w704e9a7-9d00-4b36-bf4a-0a2b0e67b2d2",
      "credited_amount_usd": 5.4722,
      "new_balance_usd": 30.9722
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Signature validation: Verifies request authenticity by calculating HMAC-SHA256 of the payload using the provider's shared secret key; returns `401 Unauthorized` on failure.
    - [ ] Idempotency: Checks if the `transaction_reference` already exists in `billing.ledger`. If found, returns `200 OK` directly without double-crediting the wallet.
    - [ ] Wallet lookup: Queries wallet ID associated with `account_barcode`. Returns `404 Not Found` if the barcode doesn't map to any account.
    - [ ] Multi-currency conversion: Translates deposited LBP amounts to USD. Apply a 1.5% markup/spread buffer if LBP is deposited (`USD_credited = (LBP_amount / exchange_rate) * 0.985`) to offset black-market volatility.
    - [ ] Transaction processing: Executes an atomic database transaction inserting a credit record in `billing.ledger` and updating the `balance_usd` and `balance_lbp` cache in `billing.wallets`.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 2.
    - [ ] Deliverable path: `src/Modules/Billing`

#### Issue #10: [Billing] Ledger Transactional History
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `GET /api/billing/ledger`
*   **Description:** Retrieves a paginated history of transactional ledger logs for the authenticated user.
*   **Request Payload JSON:** None (Query parameters: `page`, `limit`, `type`, `source_vertical`).
*   **Response Payload JSON:**
    ```json
    {
      "transactions": [
        {
          "id": "l904e9a7-9d00-4b36-bf4a-0a2b0e67b2d2",
          "type": "credit",
          "amount_usd": 5.4722,
          "amount_lbp": 500000.00,
          "currency_of_transaction": "LBP",
          "exchange_rate_applied": 90000.0000,
          "source_vertical": "reconciliation",
          "source_reference_id": "WHISH-TX-987654321",
          "description": "OMT/Whish top-up cash deposit",
          "created_at": "2026-07-08T11:26:00Z"
        }
      ],
      "pagination": {
        "total_count": 1,
        "total_pages": 1,
        "current_page": 1,
        "limit": 20
      }
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Requires valid JWT authentication.
    - [ ] Validates that the calling user only has access to ledger records associated with their own wallet ID.
    - [ ] Supports query pagination parameters (`page` defaults to `1`, `limit` defaults to `20`, maximum `limit` capped at `100`).
    - [ ] Supports optional filtering by `type` (`credit` or `debit`) and `source_vertical` (`taxi`, `food`, `supermarket`, `housekeeping`, `reconciliation`).
    - [ ] Returns `200 OK` with ledger details sorted by `created_at` in descending order.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 2.
    - [ ] Deliverable path: `src/Modules/Billing`

---

### Epic 3: On-Demand Food Delivery Engine

#### Issue #11: [FoodDelivery] Restaurant Browse and Search
> **Build scope:** CORE — built & graded in Part 2.
*   **Method & Route:** `GET /api/food/restaurants`
*   **Description:** Retrieves a list of active restaurants. Filters results based on location, cuisines, preparation times, and active promotions.
*   **Request Payload JSON:** None (Query parameters: `latitude`, `longitude`, `cuisine`, `max_prep_minutes`, `price_tier`, `offers_only`, `search`, `page`, `limit`).
*   **Response Payload JSON:**
    ```json
    {
      "data": [
        {
          "id": "a904d9a7-8d00-4b36-bf4a-0a2b0e67b2d1",
          "name": "La Trattoria",
          "description": "Authentic Italian wood-fired pizza and pasta.",
          "cuisine": "Italian",
          "rating": 4.7,
          "average_prep_time_minutes": 35,
          "price_tier": 2,
          "h3_index": "881f185011fffff",
          "distance_meters": 1250,
          "estimated_delivery_fee_usd": 1.50,
          "estimated_delivery_fee_lbp": 135000.00,
          "has_active_promotions": true,
          "status": "active"
        }
      ],
      "pagination": {
        "current_page": 1,
        "limit": 10,
        "total_items": 1,
        "total_pages": 1
      }
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Geofence Check: Mapped client `latitude` and `longitude` are converted to an H3 index. The system validates that this H3 index resides within an active service geofence; returns an empty data list if coordinates are outside covered geofences.
    - [ ] Sponsor Promotion Priority: Restaurants with active CPC campaigns (`advertising.ad_campaigns` status `active` for vertical `food`) are prioritized and returned at the top of the list.
    - [ ] Delivery Fee Calculation: Delivery fee is computed using OSRM routing distance (applying scooter Lua routing parameters). The base USD fee is converted to LBP using the current platform exchange rate.
    - [ ] Supports filtering on `cuisine`, `max_prep_minutes`, `price_tier`, and `offers_only`.
    - [ ] Supports case-insensitive `search` on restaurant name.
    - [ ] Pagination defaults: `page` defaults to `1`, `limit` defaults to `10`, `limit` capped at `50`; response echoes `current_page`, `limit`, `total_items`, `total_pages`.
    - [ ] Returns `200 OK` with paginated listings.
    - [ ] Pipeline: Full State Machine (`type:code`). Parent Epic: Part 2 coding challenge.
    - [ ] Deliverable path: `src/restaurants/**`

> **Graded-core note (#11):** the graded slice implements list + `search` + `cuisine`/`price_tier`
> filtering + pagination over persisted, active restaurants. H3 geofencing, CPC sponsor
> prioritisation, and live OSRM delivery-fee routing are **vision** — the graded slice may return
> a delivery-fee estimate from a documented distance/zone rule, or omit it here and compute it at
> order time (#13). The architect's Part-2 spec fixes the exact rule.

#### Issue #12: [FoodDelivery] Restaurant Menu Retrieval
> **Build scope:** CORE-supporting — built to supply items & prices to order placement.
*   **Method & Route:** `GET /api/food/restaurants/{restaurantId}/menu`
*   **Description:** Retrieves the categorized menu items and customization options for a specific restaurant.
*   **Request Payload JSON:** None (Query parameters: `category_id`, `dietary_tag`).
*   **Response Payload JSON:**
    ```json
    {
      "restaurant_id": "a904d9a7-8d00-4b36-bf4a-0a2b0e67b2d1",
      "categories": [
        {
          "category_id": "c704f9a7-0d00-4b36-bf4a-0a2b0e67b2e1",
          "category_name": "Pizzas",
          "display_order": 1,
          "items": [
            {
              "id": "e304e9a7-9d00-4b36-bf4a-0a2b0e67b2d5",
              "name": "Margherita Pizza",
              "description": "Fresh tomato sauce, mozzarella, and basil.",
              "price_usd": 7.50,
              "price_lbp": 675000.00,
              "is_available": true,
              "customization_groups": [
                {
                  "group_id": "g104f9a7-0d00-4b36-bf4a-0a2b0e67b2c1",
                  "group_name": "Size",
                  "is_required": true,
                  "max_selections": 1,
                  "options": [
                    { "option_id": "o1", "name": "Medium", "price_modifier_usd": 0.00 },
                    { "option_id": "o2", "name": "Large", "price_modifier_usd": 2.50 }
                  ]
                }
              ]
            }
          ]
        }
      ]
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Validates that the requested restaurant ID exists and is active; returns `404 Not Found` if missing or inactive.
    - [ ] Groups items by their menu categories, sorting categories and items according to their designated `display_order` index.
    - [ ] Dynamic Translation: Converts all item prices and customization price modifiers from USD to LBP at query time using the 60-second validity rate token.
    - [ ] Represents item availability flags correctly (if stock count is 0, `is_available` must return `false`).
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine (`type:code`). Parent Epic: Part 2 coding challenge.
    - [ ] Deliverable path: `src/restaurants/**`

#### Issue #13: [FoodDelivery] Place Food Order
> **Build scope:** CORE — built & graded in Part 2.
*   **Method & Route:** `POST /api/food/orders`
*   **Description:** Places a food order, executing price calculations and mapping transactional data atomically to the polymorphic TPT database schema.
*   **Request Payload JSON (Polymorphic Pricing Format):**
    ```json
    {
      "restaurant_id": "a904d9a7-8d00-4b36-bf4a-0a2b0e67b2d1",
      "delivery_address_id": "d104f9a7-0d00-4b36-bf4a-0a2b0e67b2d9",
      "payment_method": "COD_LBP",
      "coupon_code": "SUMMER10",
      "items": [
        {
          "menu_item_id": "e304e9a7-9d00-4b36-bf4a-0a2b0e67b2d5",
          "quantity": 1,
          "customization_choices": [
            { "group_id": "g104f9a7-0d00-4b36-bf4a-0a2b0e67b2c1", "option_id": "o2" }
          ]
        }
      ],
      "special_instructions": "Deliver to building entrance lobby."
    }
    ```
*   **Response Payload JSON (Dynamic Rate & Landmark):**
    ```json
    {
      "booking_id": "f504e9a7-9d00-4b36-bf4a-0a2b0e67b2d8",
      "status": "pending",
      "totals": {
        "subtotal_usd": 10.00,
        "delivery_fee_usd": 1.50,
        "small_order_surcharge_usd": 0.00,
        "discount_usd": 1.00,
        "total_usd": 10.50,
        "total_lbp": 945000.00,
        "exchange_rate_applied": 90000.00,
        "rate_token_validity_seconds": 60
      },
      "landmark_details": "Opposite OMT Office",
      "created_at": "2026-07-08T11:30:00Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Verification gate: rejects with `403 Forbidden` (`verification_required`) if the authenticated user has not verified **both** email and phone (see #2A).
    - [ ] Address verification: Resolves `delivery_address_id` to a valid record containing a non-empty `nearest_landmark` field.
    - [ ] Price parity verification: Validates item and customization prices against the active database records to prevent client-side price tampering.
    - [ ] Stock / availability validation: Rejects the order with `409 Conflict` if any ordered menu item (or chosen customization) is unavailable or out of stock at order time; the error names the offending item(s).
    - [ ] Surcharge & Fee Logic: Computes a delivery fee and, if the subtotal is below the Minimum Order Value ($7.00 USD), applies a small-order surcharge ($1.00 USD).
    - [ ] Coupon support: If `coupon_code` is present, validates it exists, is active, and is within its validity window / usage limits; applies the discount to `discount_usd` and reflects it in the totals. Returns `422 Unprocessable Entity` (or `400 Bad Request`) with a clear message for an invalid, expired, or ineligible coupon; a missing `coupon_code` is treated as no discount.
    - [ ] Persistence & atomicity: Persists the order and its line items in a single transaction so a partial failure leaves no orphaned rows.
    - [ ] Wallet verification: If `payment_method` is `WALLET`, verifies sufficient balance and deducts the USD amount.
    - [ ] Returns `201 Created` with the computed totals (subtotal, delivery fee, surcharge, discount, total) on success.
    - [ ] Pipeline: Full State Machine (`type:code`). Parent Epic: Part 2 coding challenge.
    - [ ] Deliverable path: `src/orders/**`

> **Graded-core note (#13):** this is the brief's core order endpoint — stock validation,
> delivery-fee calculation, coupon support, persistence, and API responses are all **required
> and graded**. The vision extras (OSRM routing, TPT `core.bookings`/`food_delivery.food_orders`
> split, `FoodOrderPlacedEvent` dispatch, `billing.ledger` legs) are **not** required for the
> graded build: the architect's Part-2 spec defines the concrete order/coupon/stock schema and
> a documented delivery-fee rule. `delivery_address_id` may be simplified to a plain field or a
> minimal address reference since a full address API (#3–#6) is out of graded scope.

#### Issue #14: [FoodDelivery] Driver Location Tracking
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `GET /api/food/orders/{orderId}/track`
*   **Description:** Retrieves the matched driver's real-time coordinates, dynamic ETA, and GPS resiliency indicators.
*   **Request Payload JSON:** None.
*   **Response Payload JSON:**
    ```json
    {
      "order_id": "f504e9a7-9d00-4b36-bf4a-0a2b0e67b2d8",
      "status": "ongoing",
      "driver_id": "d704f9a7-0d00-4b36-bf4a-0a2b0e67b2f4",
      "driver_name": "Rami",
      "coordinates": {
        "latitude": 33.8912,
        "longitude": 35.4984,
        "bearing": 180.5
      },
      "eta_seconds": 340,
      "gps_resiliency_status": "active",
      "requires_qr_verification": false,
      "last_updated_at": "2026-07-08T11:34:50Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Access control: Ensures the requester is the ordering customer, the matched driver, or an administrator.
    - [ ] Fast retrieval: Fetches the driver's current coordinates from the in-memory Redis Geospatial cache rather than querying relational database tables.
    - [ ] ETA Calculation: Computes distance and ETA between the driver and customer using OSRM routing.
    - [ ] GPS Spoofing / Drop Detection: If the driver's last GPS coordinate timestamp in Redis is older than 120 seconds, sets `gps_resiliency_status` to `stale_fallback` and returns the last cached position.
    - [ ] Proximity flag: If the driver's coordinate is within 20 meters of the customer's coordinate, `requires_qr_verification` is set to `true`.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 3.
    - [ ] Deliverable path: `src/Modules/FoodDelivery`

---

### Epic 4: Capital-Light Grocery Commerce

#### Issue #15: [Supermarket] Browse Supermarket Catalog
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `GET /api/supermarket/catalog`
*   **Description:** Retrieves catalog items. Supports text search and applies safety stock filters to prevent checkout issues.
*   **Request Payload JSON:** None (Query parameters: `supermarket_id`, `category_id`, `search`, `page`, `limit`).
*   **Response Payload JSON:**
    ```json
    {
      "data": [
        {
          "id": "item101",
          "supermarket_id": "b304e9a7-9d00-4b36-bf4a-0a2b0e67b2d2",
          "name": "Fresh Pasteurised Milk 1L",
          "sku": "SKU-MILK-1L",
          "brand": "Candia",
          "image_url": "https://cdn.tekram.com/items/milk1l.jpg",
          "price_usd": 1.80,
          "price_lbp": 162000.00,
          "stock_status": "in_stock"
        }
      ],
      "pagination": {
        "current_page": 1,
        "limit": 20,
        "total_items": 1,
        "total_pages": 1
      }
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Safety Stock Buffer: If physical store inventory is less than 3 units, the item is returned with `stock_status` set to `out_of_stock` (preventing customer checkouts).
    - [ ] Dynamic conversion: Translates USD item prices to LBP based on the live exchange rate.
    - [ ] Supports text search queries on item names, brands, and category matches.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 4.
    - [ ] Deliverable path: `src/Modules/Supermarket`

#### Issue #16: [Supermarket] Item Stock and Telemetry
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `GET /api/supermarket/items/{itemId}/stock`
*   **Description:** Retrieves detailed stock counts and refrigeration telemetry to ensure cold-chain compliance.
*   **Request Payload JSON:** None (Query parameters: `supermarket_id`).
*   **Response Payload JSON:**
    ```json
    {
      "item_id": "item101",
      "sku": "SKU-MILK-1L",
      "supermarket_id": "b304e9a7-9d00-4b36-bf4a-0a2b0e67b2d2",
      "physical_stock": 10,
      "safety_buffer": 3,
      "available_stock": 7,
      "prices": {
        "price_usd": 1.80,
        "price_lbp": 162000.00
      },
      "cold_chain": {
        "is_temperature_controlled": true,
        "last_telemetry_reading": "2026-07-08T11:25:00Z",
        "status": "optimal"
      }
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Calculates `available_stock` as `max(0, physical_stock - 3)`.
    - [ ] Cold-chain Telemetry Check: Queries the supermarket's refrigeration system sensors. If temperature controls indicate a failure (>4°C for dairy or frozen items), sets `cold_chain.status` to `critical` and overrides `available_stock` to `0` to block checkout.
    - [ ] Translates item prices to LBP using the current platform exchange rate.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 4.
    - [ ] Deliverable path: `src/Modules/Supermarket`

#### Issue #17: [Supermarket] Place Grocery Order
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `POST /api/supermarket/orders`
*   **Description:** Places a grocery order, determining picker assignment mode and inserting data atomically into the TPT schema.
*   **Request Payload JSON (Polymorphic Format):**
    ```json
    {
      "supermarket_id": "b304e9a7-9d00-4b36-bf4a-0a2b0e67b2d2",
      "delivery_address_id": "d104f9a7-0d00-4b36-bf4a-0a2b0e67b2d9",
      "payment_method": "WALLET",
      "items": [
        {
          "catalog_item_id": "item101",
          "quantity": 2
        }
      ],
      "special_instructions": "Ring doorbell twice."
    }
    ```
*   **Response Payload JSON (Dynamic Fees & Landmark):**
    ```json
    {
      "booking_id": "g804e9a7-9d00-4b36-bf4a-0a2b0e67b2d9",
      "status": "pending",
      "totals": {
        "subtotal_usd": 3.60,
        "picking_fee_usd": 0.70,
        "delivery_fee_usd": 2.50,
        "total_usd": 6.80,
        "total_lbp": 612000.00
      },
      "assignment_tier": "rider_picker",
      "landmark_details": "Opposite OMT Office",
      "created_at": "2026-07-08T11:35:00Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Address verification: Validates `delivery_address_id` resolves to a record containing a non-empty `nearest_landmark` field.
    - [ ] Stock availability audit: Checks ordered item counts against the safety stock buffers, rejecting the checkout if available stock is insufficient.
    - [ ] Assignment mode resolution: Orders containing more than 10 items or total package weight exceeding 10kg are tagged as `gig_picker` and require car vehicle dispatch; smaller orders are tagged `rider_picker`.
    - [ ] Monolith transaction integrity: Creates matching rows in `core.bookings` and `supermarket.supermarket_orders` atomically inside a single transaction.
    - [ ] Wallet verification: If `payment_method` is `WALLET`, verifies wallet funds, deducts base USD, and logs credit/debit legs in `billing.ledger`.
    - [ ] Returns `201 Created` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 4.
    - [ ] Deliverable path: `src/Modules/Supermarket`

#### Issue #18: [Supermarket] Picker Item Scan Status Sync
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `PUT /api/supermarket/orders/{orderId}/picker/sync`
*   **Description:** Syncs the item picking progress. Handles substitutes and triggers customer notifications via WebSockets.
*   **Request Payload JSON:**
    ```json
    {
      "picker_id": "d704f9a7-0d00-4b36-bf4a-0a2b0e67b2f4",
      "scanned_items": [
        {
          "catalog_item_id": "item101",
          "barcode": "SKU-MILK-1L",
          "scanned_qty": 2,
          "status": "found"
        },
        {
          "catalog_item_id": "item102",
          "barcode": "SKU-CHOC-50G",
          "scanned_qty": 0,
          "status": "substituted"
        }
      ],
      "substitutions": [
        {
          "original_catalog_item_id": "item102",
          "substituted_catalog_item_id": "item103",
          "scanned_qty": 1
        }
      ]
    }
    ```
*   **Response Payload JSON:**
    ```json
    {
      "booking_id": "g804e9a7-9d00-4b36-bf4a-0a2b0e67b2d9",
      "status": "ongoing",
      "totals": {
        "updated_subtotal_usd": 4.50,
        "updated_total_usd": 7.70,
        "updated_total_lbp": 693000.00
      },
      "remaining_items_to_scan": 0,
      "requires_customer_approval": true
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Access restriction: Enforces that only the assigned picker or driver can execute synchronization requests.
    - [ ] Substitution check: Validates proposed substitutes exist and have positive stock.
    - [ ] Pricing recalculation: Updates order subtotal and fees based on substitutes and quantities. Updates totals in both USD and LBP.
    - [ ] Real-time approval request: If a substitution occurs, sets `requires_customer_approval` to `true` and dispatches an instant WebSocket notification to the customer client app.
    - [ ] Order state transition: If all items are successfully scanned, transitions the booking state to `ready_for_pickup`.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 4.
    - [ ] Deliverable path: `src/Modules/Supermarket`

---

### Epic 5: Ride-Hailing & Vehicle Allocation

#### Issue #19: [RideHailing] Request Ride Fare Estimate
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `POST /api/taxi/estimate`
*   **Description:** Requests distance, duration, and fare estimates for ride-hailing classes based on fuel-indexed base rates and demand surge.
*   **Request Payload JSON (Polymorphic Format):**
    ```json
    {
      "user_id": "8400a8de-492d-4680-8368-b5064c3bce2e",
      "origin": { "latitude": 33.8938, "longitude": 35.5018 },
      "destination": { "latitude": 33.8892, "longitude": 35.4984 }
    }
    ```
*   **Response Payload JSON (Dynamic Rate Equivalent):**
    ```json
    {
      "estimates": [
        {
          "ride_class": "moto_taxi",
          "estimated_distance_meters": 1250,
          "estimated_duration_seconds": 240,
          "base_fare_usd": 1.00,
          "surge_multiplier": 1.00,
          "fare_breakdown_usd": {
            "base_fare": 1.00,
            "distance_fare": 0.50,
            "surge_surcharge": 0.00
          },
          "total_fare_usd": 1.50,
          "total_fare_lbp": 135000.00
        },
        {
          "ride_class": "standard",
          "estimated_distance_meters": 1400,
          "estimated_duration_seconds": 450,
          "base_fare_usd": 2.50,
          "surge_multiplier": 1.20,
          "fare_breakdown_usd": {
            "base_fare": 2.50,
            "distance_fare": 1.00,
            "surge_surcharge": 0.70
          },
          "total_fare_usd": 4.20,
          "total_fare_lbp": 378000.00
        }
      ],
      "exchange_rate": 90000.00
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Routing calculations: Queries OSRM with origin and destination coordinates.
    - [ ] OSRM Profile Separation: `moto_taxi` estimates use motorcycle routing profiles (narrow alleys, lane filtering); standard/premium estimates utilize passenger car routing profiles.
    - [ ] Fuel Indexing: Computes base rates dynamically mapped to the weekly pricing sheets issued by the Lebanese Ministry of Energy to offset fuel price fluctuations.
    - [ ] Surge determination: Evaluates active driver density in the origin H3 index cell. Applies surge multipliers if demand exceeds driver supply.
    - [ ] Dynamic translation: Returns estimates containing both USD and converted LBP values.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 5.
    - [ ] Deliverable path: `src/Modules/RideHailing`

#### Issue #20: [RideHailing] Place Ride Request
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `POST /api/taxi/rides`
*   **Description:** Places a passenger ride request, inserting records atomically to the polymorphic TPT database schema.
*   **Request Payload JSON:**
    ```json
    {
      "origin": { "latitude": 33.8938, "longitude": 35.5018 },
      "destination": { "latitude": 33.8892, "longitude": 35.4984 },
      "ride_class": "standard",
      "payment_method": "COD_USD",
      "fare_estimate_usd": 4.20
    }
    ```
*   **Response Payload JSON:**
    ```json
    {
      "booking_id": "r904e9a7-8d00-4b36-bf4a-0a2b0e67b2d2",
      "status": "pending",
      "ride_class": "standard",
      "origin": { "latitude": 33.8938, "longitude": 35.5018 },
      "destination": { "latitude": 33.8892, "longitude": 35.4984 },
      "fare_usd": 4.20,
      "fare_lbp": 378000.00,
      "created_at": "2026-07-08T11:40:00Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Validity check: Rejects the request if the time delta between estimate generation and creation exceeds 300 seconds.
    - [ ] Wallet verification: If `payment_method` is `WALLET`, verifies customer has sufficient USD balance.
    - [ ] Monolith transaction: Inserts records into base `core.bookings` and `ride_hailing.taxi_rides` tables atomically inside a single transaction block.
    - [ ] Emits a `RideRequestedEvent` containing the pickup coordinate H3 cell index.
    - [ ] Timeout: Automatically cancels the matching queue and transitions status to `cancelled` if no driver accepts within 180 seconds.
    - [ ] Returns `201 Created` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 5.
    - [ ] Deliverable path: `src/Modules/RideHailing`

#### Issue #21: [RideHailing] Ride Matching Status Retrieval
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `GET /api/taxi/rides/{rideId}/matching`
*   **Description:** Queries the matching status of an active ride request (polling endpoint for fallback during network drops).
*   **Request Payload JSON:** None.
*   **Response Payload JSON:**
    ```json
    {
      "booking_id": "r904e9a7-8d00-4b36-bf4a-0a2b0e67b2d2",
      "status": "accepted",
      "driver": {
        "driver_id": "d704f9a7-0d00-4b36-bf4a-0a2b0e67b2f4",
        "name": "Rami",
        "rating": 4.9,
        "phone": "+9613123456",
        "vehicle": {
          "model": "Toyota Prius",
          "color": "Silver",
          "plate_number": "M/123456"
        }
      },
      "current_distance_meters": 850,
      "eta_seconds": 180
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Access control: Restricts status checks to the ordering customer, the matched driver, or administrators.
    - [ ] Distance calculation: If the status is `accepted`, fetches driver coordinates from Redis and calculates current distance and ETA using OSRM.
    - [ ] Connection drop resiliency: If OSRM fails, returns the last known status cached in the database.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 5.
    - [ ] Deliverable path: `src/Modules/RideHailing`

#### Issue #22: [RideHailing] Driver Coordinate Tracking Update
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `POST /api/taxi/drivers/location`
*   **Description:** Updates the driver's real-time coordinate in memory and evaluates proximity events and financial exposure boundaries.
*   **Request Payload JSON:**
    ```json
    {
      "driver_id": "d704f9a7-0d00-4b36-bf4a-0a2b0e67b2f4",
      "latitude": 33.8912,
      "longitude": 35.4984,
      "bearing": 180.5,
      "speed": 22.0
    }
    ```
*   **Response Payload JSON:**
    ```json
    {
      "status": "success",
      "active_bookings": [
        {
          "booking_id": "r904e9a7-8d00-4b36-bf4a-0a2b0e67b2d2",
          "status": "ongoing",
          "proximity_action": "active_qr_code"
        }
      ],
      "credit_exposure": {
        "net_exposure_usd": 45.00,
        "limit_usd": 100.00,
        "status": "compliant"
      }
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Decoupled caching: Writes coordinates directly to Redis Geospatial storage using `GEOADD` to shield database tables from high-frequency updates.
    - [ ] Spatial Indexing: Converts latitude/longitude to an H3 cell index, updating the active driver index cache.
    - [ ] GPS Jamming Mitigation Proximity Check: If the driver has an active booking and the coordinate is within 20 meters of the destination address, triggers `active_qr_code` as `proximity_action` to enforce QR-based verification.
    - [ ] Financial Exposure Audit: Calculates the driver's Net Exposure: `Net Exposure = Cash-on-Hand - Unpaid Earnings - Security Deposit`. If Net Exposure exceeds the driver's rating-based credit limit ($100 for new, $500 for highly-rated), returns `status: limit_warning` and pauses dispatch matching assignments.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 5.
    - [ ] Deliverable path: `src/Modules/RideHailing`

---

### Epic 6: Housekeeping & Scheduled Home Services

#### Issue #23: [Housekeeping] Browse Services Catalog
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `GET /api/housekeeping/services`
*   **Description:** Retrieves the available housekeeping services catalog and baseline pricing.
*   **Request Payload JSON:** None.
*   **Response Payload JSON:**
    ```json
    {
      "data": [
        {
          "id": "c104f9a7-0d00-4b36-bf4a-0a2b0e67b2d3",
          "title": "Standard House Cleaning",
          "description": "Thorough cleaning of rooms, kitchen, and bathroom. Includes dusting and vacuuming.",
          "base_hourly_rate_usd": 8.00,
          "base_hourly_rate_lbp": 720000.00,
          "materials_fee_usd": 5.00,
          "materials_fee_lbp": 450000.00,
          "minimum_hours": 3.0,
          "image_url": "https://cdn.tekram.com/services/standard_cleaning.jpg"
        }
      ],
      "pagination": {
        "current_page": 1,
        "limit": 10,
        "total_items": 1,
        "total_pages": 1
      }
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Queries database to return active housekeeping service options.
    - [ ] Dynamic Translation: Converts USD baseline hourly rates and material fees into LBP.
    - [ ] Orders listings by priority index.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 6.
    - [ ] Deliverable path: `src/Modules/Housekeeping`

#### Issue #24: [Housekeeping] Book Housekeeper Schedule
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `POST /api/housekeeping/bookings`
*   **Description:** Schedules and reserves a housekeeping booking, validating housekeeper availability calendars and calculating platform fees.
*   **Request Payload JSON (Polymorphic Format):**
    ```json
    {
      "service_catalog_id": "c104f9a7-0d00-4b36-bf4a-0a2b0e67b2d3",
      "delivery_address_id": "d104f9a7-0d00-4b36-bf4a-0a2b0e67b2d9",
      "scheduled_start": "2026-07-10T09:00:00Z",
      "duration_hours": 4.0,
      "materials_provided": false,
      "payment_method": "WALLET",
      "special_instructions": "Key is under the mat."
    }
    ```
*   **Response Payload JSON (Dynamic Rate & Landmark):**
    ```json
    {
      "booking_id": "h304e9a7-9d00-4b36-bf4a-0a2b0e67b2d1",
      "status": "accepted",
      "scheduled_start": "2026-07-10T09:00:00Z",
      "scheduled_end": "2026-07-10T13:00:00Z",
      "billing": {
        "hourly_rate_usd": 8.00,
        "service_fee_usd": 32.00,
        "materials_fee_usd": 5.00,
        "platform_fee_usd": 0.00,
        "total_usd": 37.00,
        "total_lbp": 3330000.00
      },
      "landmark_details": "Opposite OMT Office",
      "created_at": "2026-07-08T11:45:00Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Address verification: Validates `delivery_address_id` resolves to a record containing a non-empty `nearest_landmark` field.
    - [ ] Minimum duration audit: Verifies `duration_hours` meets or exceeds the minimum hours threshold configured for the catalog item.
    - [ ] Calendar sync check: Verifies housekeeper availability, rejecting bookings if scheduling conflicts exist within the geographic cluster.
    - [ ] Tekram Pass integration: If the customer has an active subscription membership, the platform booking fee ($5.00 USD) is waived.
    - [ ] Relational schema integrity: Writes records to `core.bookings` and `housekeeping.housekeeping_bookings` atomically inside a single database transaction.
    - [ ] Wallet verification: If `payment_method` is `WALLET`, verifies funds and places a hold on the USD amount in the ledger.
    - [ ] Returns `201 Created` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 6.
    - [ ] Deliverable path: `src/Modules/Housekeeping`

#### Issue #25: [Housekeeping] Cancel or Reschedule Booking
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `PATCH /api/housekeeping/bookings/{bookingId}/schedule`
*   **Description:** Adjusts schedule coordinates or cancels a scheduled housekeeping booking, evaluating cancellation fee rules.
*   **Request Payload JSON:**
    ```json
    {
      "action": "cancel",
      "new_scheduled_start": null,
      "reason": "Change of plans."
    }
    ```
*   **Response Payload JSON:**
    ```json
    {
      "booking_id": "h304e9a7-9d00-4b36-bf4a-0a2b0e67b2d1",
      "status": "cancelled",
      "refund_details": {
        "refund_amount_usd": 37.00,
        "cancellation_fee_usd": 0.00,
        "ledger_reference_id": "l904e9a7-8d00-4b36-bf4a-0a2b0e67b2e9"
      },
      "updated_at": "2026-07-08T11:50:00Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Permissions check: Restricts access to the booking's customer or administrators.
    - [ ] Cancellation fee rules: If cancelled less than 24 hours before the scheduled start, applies a cancellation fee (equivalent to the platform booking fee); if cancelled >24 hours, processes a full refund.
    - [ ] Double-entry ledger reversals: Releases wallet holds and creates reversing credit entries in `billing.ledger` in base USD, registering the converted LBP equivalent.
    - [ ] Availability update: Frees up the housekeeper's calendar availability slots.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 6.
    - [ ] Deliverable path: `src/Modules/Housekeeping`

---

### Epic 7: Loyalty, Subscriptions, and Merchant Bidding

#### Issue #26: [Subscriptions] Retrieve Subscription Plans
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `GET /api/subscriptions/plans`
*   **Description:** Retrieves configurations and benefit structures for active subscription tiers (e.g. Tekram Pass).
*   **Request Payload JSON:** None.
*   **Response Payload JSON:**
    ```json
    [
      {
        "id": "p104e9a7-9d00-4b36-bf4a-0a2b0e67b2d2",
        "name": "Tekram Pass",
        "description": "Save on food, taxi, and supermarket deliveries.",
        "price_usd": 5.99,
        "duration_days": 30,
        "benefits": {
          "free_delivery_threshold_usd": 10.00,
          "ride_discount_percentage": 5.0,
          "platform_fee_waived": true,
          "priority_dispatch": true
        }
      }
    ]
    ```
*   **Acceptance Criteria:**
    - [ ] Reads active subscription configurations from the database.
    - [ ] Includes explicit benefit attributes (thresholds, discounts, waived fees).
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 7.
    - [ ] Deliverable path: `src/Modules/Subscriptions`

#### Issue #27: [Subscriptions] Purchase Subscription
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `POST /api/subscriptions/subscribe`
*   **Description:** Subscribes a user to a selected plan, validating funds and processing transactions atomically.
*   **Request Payload JSON:**
    ```json
    {
      "plan_id": "p104e9a7-9d00-4b36-bf4a-0a2b0e67b2d2"
    }
    ```
*   **Response Payload JSON:**
    ```json
    {
      "subscription_id": "s804f9a7-0d00-4b36-bf4a-0a2b0e67b2d3",
      "user_id": "e8a34b2c-29ad-4eb4-b9c1-840a3f892b1a",
      "plan_id": "p104e9a7-9d00-4b36-bf4a-0a2b0e67b2d2",
      "status": "active",
      "starts_at": "2026-07-08T11:26:00Z",
      "ends_at": "2026-08-08T11:26:00Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Requires valid JWT authentication.
    - [ ] Validates `plan_id` corresponds to an active configuration.
    - [ ] Wallet verification: Confirms user has sufficient USD wallet balance to pay the plan price. Returns `400 Bad Request` if funds are insufficient.
    - [ ] Transaction processing: Executes an atomic database transaction: debits the price from `billing.wallets`, logs a debit record in `billing.ledger` in USD, and inserts a membership record.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 7.
    - [ ] Deliverable path: `src/Modules/Subscriptions`

#### Issue #28: [Loyalty] Loyalty Points Balance
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `GET /api/loyalty/balance`
*   **Description:** Retrieves the user's current loyalty points balance, tier, and history.
*   **Request Payload JSON:** None.
*   **Response Payload JSON:**
    ```json
    {
      "user_id": "e8a34b2c-29ad-4eb4-b9c1-840a3f892b1a",
      "points_balance": 450,
      "current_tier": "Silver",
      "equivalent_usd_value": 4.50,
      "points_earned_total": 600,
      "points_redeemed_total": 150,
      "points_history": [
        {
          "transaction_id": "t904f9a7-0d00-4b36-bf4a-0a2b0e67b2d4",
          "type": "earn",
          "points": 50,
          "source_vertical": "food",
          "source_reference_id": "f804e9a7-0d00-4b36-bf4a-0a2b0e67b2d3",
          "created_at": "2026-07-08T10:15:00Z"
        }
      ]
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Requires valid JWT authentication.
    - [ ] Points translation: Converts points to equivalent USD cashback values using the system rule (100 points = $1.00 USD).
    - [ ] Tiers calculation: Calculates tiering based on accumulated lifetime points (Bronze < 500, Silver >= 500, Gold >= 2000).
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 7.
    - [ ] Deliverable path: `src/Modules/Loyalty`

#### Issue #29: [Ads] Create Ad Campaign
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `POST /api/ads/campaigns`
*   **Description:** Registers a new CPC sponsored campaign for a merchant.
*   **Request Payload JSON:**
    ```json
    {
      "merchant_id": "a904d9a7-8d00-4b36-bf4a-0a2b0e67b2d1",
      "vertical": "food",
      "bid_cpc_usd": 0.15,
      "daily_budget_usd": 30.00,
      "starts_at": "2026-07-09T00:00:00Z",
      "ends_at": "2026-07-16T23:59:59Z"
    }
    ```
*   **Response Payload JSON:**
    ```json
    {
      "campaign_id": "ad04e9a7-9d00-4b36-bf4a-0a2b0e67b2d2",
      "merchant_id": "a904d9a7-8d00-4b36-bf4a-0a2b0e67b2d1",
      "vertical": "food",
      "bid_cpc_usd": 0.15,
      "daily_budget_usd": 30.00,
      "accumulated_daily_spend_usd": 0.00,
      "status": "active",
      "starts_at": "2026-07-09T00:00:00Z",
      "ends_at": "2026-07-16T23:59:59Z",
      "created_at": "2026-07-08T11:26:00Z"
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Requires authentication and validates that the actor is authorized for the merchant account.
    - [ ] Bid Validation: Enforces that `bid_cpc_usd` is positive and meets the minimum CPC floor ($0.05 USD).
    - [ ] Budget Validation: Enforces that `daily_budget_usd` is positive and meets the minimum daily budget floor ($5.00 USD).
    - [ ] Saves the ad campaign to `advertising.ad_campaigns` and returns `201 Created` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 7.
    - [ ] Deliverable path: `src/Modules/Ads`

#### Issue #30: [Ads] Track Ad Click
> **Build scope:** Strategic vision — documented here, not implemented in this assessment.
*   **Method & Route:** `POST /api/ads/clicks`
*   **Description:** Tracks an ad click, checks for click fraud, and charges the merchant campaign CPC amount asynchronously to avoid runtime latency.
*   **Request Payload JSON:**
    ```json
    {
      "campaign_id": "ad04e9a7-9d00-4b36-bf4a-0a2b0e67b2d2",
      "ad_placement_id": "pl04e9a7-9d00-4b36-bf4a-0a2b0e67b2d3",
      "client_ip": "192.168.1.50",
      "user_agent": "Mozilla/5.0 (iPhone; CPU iPhone OS 16_5 like Mac OS X) AppleWebKit/605.1.15",
      "timestamp": "2026-07-08T11:26:00Z"
    }
    ```
*   **Response Payload JSON:**
    ```json
    {
      "status": "success",
      "message": "Click tracked successfully",
      "charge_applied_usd": 0.15
    }
    ```
*   **Acceptance Criteria:**
    - [ ] Campaign validation: Verifies `campaign_id` corresponds to an active ad campaign.
    - [ ] Fraud check: Evaluates request details to detect click fraud. Multiple clicks from the same `client_ip` and `user_agent` on the same `campaign_id` occurring within a 10-minute window are logged but not charged.
    - [ ] Budget enforcement: Charges the CPC fee, updating the campaign's `accumulated_daily_spend_usd`. Automatically transitions campaign status to `paused` or `exhausted` if daily budget limits are reached.
    - [ ] Performance decoupling: Offloads transaction ledger processing to an asynchronous background job queue (e.g. RabbitMQ or Redis sidecar) to ensure sub-millisecond response latency.
    - [ ] Returns `200 OK` on success.
    - [ ] Pipeline: Full State Machine. Parent Epic: Epic 7.
    - [ ] Deliverable path: `src/Modules/Ads`
