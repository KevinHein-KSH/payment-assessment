# Payment Assessment

A checkout application with a .NET 8 Web API backend and a Next.js frontend, integrated
with the 2C2P sandbox (Payment API v4.3, Singapore / SGD). MySQL holds durable state and
the uniqueness guarantees, and Redis provides caching and distributed locking.

## Structure

```text
backend/                               .NET 8 solution (PaymentAssessment.sln)
  src/
    PaymentAssessment.Domain/          entities and enums
    PaymentAssessment.Application/     use cases (checkout, notification, lookups) and interfaces
    PaymentAssessment.Infrastructure/  EF Core/MySQL, Redis cache + lock, 2C2P adapter
    PaymentAssessment.Api/             controllers, Swagger, DI setup
  tests/
    PaymentAssessment.Api.Tests/       xUnit integration tests against real MySQL and Redis
frontend/                              Next.js (App Router, TypeScript, Tailwind)
compose.yaml                           MySQL, Redis and the API
```

Dependencies point inward: `Api → Application → Domain`, and Infrastructure implements
the interfaces declared in Application (`IPaymentProvider`, `IDistributedLock`,
`ICacheService`, `ITransactionRepository`, `ILookupRepository`).

## Prerequisites

- .NET SDK 8.0
- Node.js 20 or later
- Docker (for MySQL and Redis)
- A tunnel such as ngrok, so 2C2P can reach the backend notification URL

## Setup

### 1. Configure 2C2P credentials

```bash
cp backend/src/PaymentAssessment.Api/appsettings.Development.example.json \
   backend/src/PaymentAssessment.Api/appsettings.Development.json
```

- Fill in the Singapore sandbox merchant ID and secret key from
  https://developer.2c2p.com/docs/sandbox.
- Do this before `docker compose up`: compose mounts this file, and if it's missing
  Docker creates an empty folder in its place.
- The file is git-ignored; never commit it.

### 2. Configure database credentials

```bash
cp .env.example .env
```

- Set `MYSQL_USER`, `MYSQL_PASSWORD` and `MYSQL_ROOT_PASSWORD` in `.env` (git-ignored).
  Compose refuses to start without them.
- For `dotnet run` outside Docker, put the same user and password in the `MySql`
  connection string in `appsettings.Development.json`; .NET does not read `.env`.
- MySQL reads these only when its volume is first created. After changing them, run
  `docker compose down -v`.

### 3. Expose the API for 2C2P notifications

2C2P must reach `BackendReturnUrl`, so expose the API with a tunnel (ngrok shown; any
tunnel works):

```bash
ngrok http 5080 --url https://<your-ngrok-domain>
```

Then set in `appsettings.Development.json` under `TwoC2P`:

```json
"BackendReturnUrl": "https://<your-ngrok-domain>/api/payments/2c2p/notification",
"FrontendReturnUrl": "http://localhost:3000/result"
```

## Run

**Option A: everything in Docker.** API, MySQL and Redis:

```bash
docker compose up
```

**Option B: API from source** (what we use while developing):

```bash
docker compose up -d mysql redis
cd backend/src/PaymentAssessment.Api
dotnet run --launch-profile http
```

Either way:

- API: http://localhost:5080
- Swagger UI: http://localhost:5080/swagger (Development environment only), with request and
  response examples for every endpoint (`Swagger/SwaggerExamples.cs`)
- Migrations and seed data are applied automatically on startup.

**Frontend:**

```bash
cd frontend
cp .env.example .env.local   # NEXT_PUBLIC_API_BASE_URL, default http://localhost:5080
npm install
npm run dev
```

- http://localhost:3000

## Using the app

1. Open http://localhost:3000, pick a customer and a product, keep
   "Pay with a new payment method" selected, and click **Checkout**.
2. On the 2C2P hosted page, choose card payment and use a sandbox test card
   ([2C2P test cards](https://developer.2c2p.com/docs/reference-test-information-sg)):

   | Card | Number | CVV | Expiry | OTP |
   |---|---|---|---|---|
   | Visa, succeeds | `4111 1111 1111 1111` | `123` | any future date | `123456` |
   | Mastercard, succeeds | `5555 5555 5555 4444` | `123` | any future date | `123456` |
   | Visa, declined | `4444 3333 2222 1111` | `123` | any future date | `123456` |

   For a **Recurring** product (Monthly or Annual Membership) the hosted page offers card only;
   tick the save-card option to save the card for that customer.
   **Saved card:** pick a customer with a saved card, select it instead of "Pay with a new payment
   method", and click **Checkout**. There is no hosted page; the browser goes straight to the Result page
   (or to the 3DS page first if the bank asks for it).
3. 2C2P returns the browser to the Result page, which reads the status from our database and
   polls every 3 seconds until the backend notification arrives.
4. On success, **Back to checkout** keeps the customer selected, so a newly saved card is listed.
   On a failed or cancelled payment, **Go back** returns to Checkout with the same customer and
   product selected, ready to retry.

## Tests

The tests run the real API against the compose MySQL and Redis, with 2C2P replaced by a fake
where a test must not call the sandbox. They need `appsettings.Development.json` (step 1) for
the database login.

```bash
docker compose up -d mysql redis
cd backend
dotnet test PaymentAssessment.sln                                            # all 30 tests
dotnet test PaymentAssessment.sln --filter "FullyQualifiedName~IdempotencyTests"  # idempotency + concurrency
```

| Test class | Covers |
|---|---|
| `IdempotencyTests` | 10 parallel checkouts with one key → exactly 1 row and 1 2C2P call; retry replays the same invoice without calling 2C2P; interrupted requests resolved by Payment Inquiry |
| `RedisLockTests` | A held lock blocks a second owner; an expired owner can't release the next owner's lock |
| `NotificationTests` | Signature check, amount/currency check, success/failure/in-progress, duplicate notifications, currency-converted payloads; a tokenized notification saves the card once and refreshes the cached card list; an empty `customerToken` saves no card |
| `RecurringCheckoutTests` | Recurring products request a saved card (card only + tokenize), OneTime products don't |
| `SavedCardCheckoutTests` | Saved-card payment confirmed by Payment Inquiry; Do Payment `2000` with a declined Inquiry ends Failed; another customer's card → 404 with nothing written; 3DS returns the 3DS URL and Inquiry confirms on return; a retry never charges the card twice |
| `InquiryFallbackTests` | A Pending hosted payment that Inquiry reports as cancelled becomes Failed with the reason; one Inquiry can't find stays Pending |
| `CheckoutValidationTests` | Missing `Idempotency-Key`, unknown customer or product |

## API

| Method | Path | Purpose |
|---|---|---|
| GET | `/api/customers` | Seeded customers (cached) |
| GET | `/api/products` | Seeded products (cached) |
| GET | `/api/customers/{id}/cards` | A customer's saved cards, masked only (cached) |
| POST | `/api/checkout` | Header `Idempotency-Key`; body `customerId`, `productId`, optional `customerCardId`. Returns `invoiceNo`, `status`, `redirectUrl` (hosted page or 3DS; null when a saved-card payment needs no redirect) |
| POST | `/api/payments/2c2p/notification` | 2C2P backend notification (`backendReturnUrl`) |
| GET | `/api/transactions/{invoiceNo}` | Status and details for the Result page, never cached |

Errors use RFC 7807 Problem Details: 400 missing key, 404 unknown customer, product or invoice,
or a saved card that isn't this customer's, 409 a checkout with the same key is already running.

## How it works

### Payment flow (new payment method)

1. `POST /api/checkout` validates the customer and product, inserts a **Pending** transaction,
   and requests a Payment Token from 2C2P (JWT signed with HMAC SHA-256). For a Recurring
   product the request adds `paymentChannel: ["CC"]` and `tokenize: true`.
2. The frontend sends the browser to the returned hosted-page URL.
3. 2C2P POSTs a signed notification to the backend. The handler verifies the signature,
   checks the amount and currency against the stored transaction, and sets **Succeeded** or
   **Failed** with a single `UPDATE … WHERE status = 'Pending'`. That makes repeated
   notifications harmless.
4. If a successful notification carries a `customerToken` (the customer ticked save-card), the
   token and masked card number are saved to `customer_card` for that customer. The unique index
   on `(customer_id, customer_token)` makes a repeated notification skip the insert, and the
   customer's cached card list is cleared.
5. **Inquiry fallback:** cancelling on the hosted page sends no notification (sandbox finding),
   so `GET /api/transactions/{invoiceNo}` asks Payment Inquiry while the row is Pending and the
   customer was sent to 2C2P. The answer goes through the same checks and `WHERE status = 'Pending'`
   update as a notification; a cancel (`0003`) becomes Failed. "Not found" keeps it Pending,
   because the customer may still be paying.
6. Only a verified notification or a verified Payment Inquiry marks a payment Succeeded;
   the browser redirect never does.

### Payment flow (saved card)

1. `POST /api/checkout` with `customerCardId` first checks that the card belongs to the customer
   (one query; another customer's card → 404 before anything is written).
2. Payment Token with `paymentChannel: ["CC"]`, `customerToken: [token]`, `customerTokenOnly: true`,
   `request3DS: "N"`. 2C2P still returns a `webPaymentUrl`; it is never stored, so a replay can't send
   the customer to the hosted page.
3. **Do Payment** (plain JSON, not a JWT) charges the card:
   - `2000` "completed" is **not** success: the same request calls Payment Inquiry and applies the answer
     (amount check, `WHERE status = 'Pending'`). Usually the response is already Succeeded or Failed.
   - `1001` returns the 3DS URL as `redirectUrl` and stores it, so when the browser returns to the Result
     page the Inquiry fallback confirms the payment.
   - Anything else → Failed with 2C2P's reason.
4. A 2C2P notification for the same invoice may also arrive; both write only `WHERE status = 'Pending'`.
5. If the request dies after Do Payment, the row is Pending with no redirect URL. The retry takes the
   interrupted-request path (Payment Inquiry) and never calls Do Payment again.

### Idempotency and locking

The frontend creates one `Idempotency-Key` per purchase and keeps it in `sessionStorage`,
so a refresh or double-click reuses it. A new customer or product gets a new key, and the key
is cleared once the payment is final. On the backend, each checkout passes four checks:

| # | Check | Result |
|---|---|---|
| 1 | Customer and product exist? | No → 404 |
| 2 | Redis lock `lock:checkout:{key}` free? | No → **409**, another request with this key is running |
| 3 | A transaction with this key already exists? | Yes → **replay**: same `invoiceNo` and redirect URL, current status, no 2C2P call |
| 4 | New key | Insert the transaction, call 2C2P, store the redirect URL |

- **Lock:** `SET key token NX PX 60000`, released by a Lua compare-and-delete script so a request
  whose lock expired can never release someone else's. The 60 s TTL covers the longest checkout
  (up to three 2C2P calls at a 10 s timeout each); the lock is released as soon as the request ends.
- **Why Redis, not an in-memory lock:** it works across several API instances.
- **Final guard:** a unique index on `transaction.idempotency_key`. If the lock expired or Redis
  is down, the duplicate insert is rejected and the request gets 409.
- **Interrupted request:** if a request died after calling 2C2P but before saving the redirect
  URL, the retry finds a Pending row without a redirect URL. It asks **Payment Inquiry** instead
  of creating a second Payment Token: paid → Succeeded (after the amount check), still processing
  → stays Pending, declined or not found → Failed.

### Caching

Customers, products and saved cards use Redis cache-aside with a 10-minute TTL. A customer's
cards cache is cleared whenever a notification saves a card (`LookupService.SaveCardAsync`). Transaction status is never cached: the Result page always reads it
fresh.

### Provider isolation

All 2C2P details (JWT signing, field names, response codes, currency handling) live in
`TwoC2PPaymentProvider`. Application code works with neutral types: `PaymentTokenResult`,
`PaymentNotification`, and `PaymentStatus { Succeeded, Failed, Pending, NotFound }`.

## Decisions and assumptions

- **Added columns:** `transaction.payment_token` stores the Payment Token from 2C2P;
  `transaction.redirect_url` stores the original redirect URL, so a retry can return it without
  calling 2C2P again.
- **Concurrent requests get 409** rather than waiting; the client's retry then gets the replay.
- **A reused key with a different body** replays the original purchase; the key identifies the
  purchase.
- **Currency conversion (sandbox finding):** with merchant `JT01`, 2C2P settles in THB and returns
  our SGD amount in `fxAmount`/`fxCurrencyCode`. The adapter compares against the amount we
  requested, whichever currency 2C2P settles in.
- **A card is saved only from a successful notification**, and on every successful notification,
  not only the one that changed the status: the unique index stops duplicates, and a crash between
  marking Succeeded and saving the card can't lose it.
- **Saved cards belong to a customer:** 2C2P returned the same `customerToken` for the same test
  card for two customers, so each customer gets their own `customer_card` row for it.
- **Cancel (sandbox finding):** a cancel on the hosted page sends no backend notification; Payment
  Inquiry then returns `0003 "Customer Cancellation"`. Hence the Result-page Inquiry fallback.
- **Saved-card sandbox run:** Do Payment with `request3DS: "N"` returned `2000`, and Inquiry `0000` in
  the notification's shape. The 3DS (`1001`) path is covered by a test, not by a sandbox run.
- **Another customer's card gets 404, not 403**, so the API doesn't confirm that the card id exists.
- **Payment Inquiry uses `/payment/4.3/`**, matching the assessment's API version. `4.5` returns
  `channelCode: "CC"` instead of the card brand.
- **Money** is `DECIMAL(12,2)`. No card number, CVV or expiry is ever stored; only masked numbers
  and 2C2P customer tokens.

## Verification status

Every flow below is implemented; nothing is left as a scaffold. "Sandbox" means a real run against
the 2C2P sandbox (merchant `JT01`, Oct 18, 2026); "test" means the automated tests.

| Flow | Verified by |
|---|---|
| OneTime payment, hosted page | Sandbox: Succeeded from the backend notification |
| Recurring payment that saves a card | Sandbox: card saved once and listed for that customer; a replayed notification added no second card |
| Saved-card payment | Sandbox: Do Payment `2000` → Inquiry `0000` → Succeeded; a retry with the same key made no 2C2P call |
| Saved-card 3DS (`1001`) | Test only: with `request3DS: "N"` the sandbox returned `2000` |
| Declined card, Go back, retry | Sandbox: `4001` → Failed → retry with a new key → Succeeded |
| Cancel on the hosted page | Sandbox: no notification arrives; Inquiry `0003` → Failed |
| Concurrent checkouts with one key | Test: 10 parallel requests → 1 row, 1 2C2P call. Sandbox: 5 parallel requests → 1×200, 4×409, then replay |
| Interrupted request recovery | Test, plus a sandbox Inquiry for an unpaid invoice (`2002`) |
| Another customer's saved card | Sandbox and test: 404, nothing written |
| Redis down | Not tested; by design the unique index still prevents duplicate rows (see below) |

## Known limitations

- **Redis down:** checkouts continue without the lock and the unique index prevents duplicate
  rows and duplicate Payment Tokens. In that window, an interrupted-request recovery could
  run while the original request is still in flight.
- **Inquiry fallback cost:** the Result page polls every 3 seconds, and each poll of a Pending
  hosted payment makes one Inquiry call. With real traffic this would be throttled per invoice.
- **A second payment provider** would need a `provider` column on `transaction`, so notifications
  and Inquiry reach the right provider, plus provider selection (keyed services or a small
  factory). The interfaces and neutral types are already in place.

## What I'd improve with more time

- Throttle the Result-page Inquiry per invoice instead of one call per poll.
- Test the 2C2P adapter itself against recorded sandbox payloads (a stub `HttpMessageHandler`); today
  the tests swap the whole provider for a fake, and the adapter is covered by sandbox runs.
- Run the tests on Testcontainers instead of the dev database, so test rows don't mix with demo data.
- Verify saved-card 3DS with a 3DS-enrolled test card, and test the Redis-down path.
- Add the `provider` column and provider selection described above.
