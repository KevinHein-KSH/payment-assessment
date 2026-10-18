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
- Swagger UI: http://localhost:5080/swagger (Development environment only)
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

3. 2C2P returns the browser to the Result page, which reads the status from our database and
   polls every 3 seconds until the backend notification arrives.
4. On a failed payment, **Go back** returns to Checkout with the same customer and product
   selected, ready to retry.

## Tests

The tests run the real API against the compose MySQL and Redis, with 2C2P replaced by a fake
where a test must not call the sandbox. They need `appsettings.Development.json` (step 1) for
the database login.

```bash
docker compose up -d mysql redis
cd backend
dotnet test PaymentAssessment.sln                                            # all 19 tests
dotnet test PaymentAssessment.sln --filter "FullyQualifiedName~IdempotencyTests"  # idempotency + concurrency
```

| Test class | Covers |
|---|---|
| `IdempotencyTests` | 10 parallel checkouts with one key → exactly 1 row and 1 2C2P call; retry replays the same invoice without calling 2C2P; interrupted requests resolved by Payment Inquiry |
| `RedisLockTests` | A held lock blocks a second owner; an expired owner can't release the next owner's lock |
| `NotificationTests` | Signature check, amount/currency check, success/failure/in-progress, duplicate notifications, currency-converted payloads |
| `CheckoutValidationTests` | Missing `Idempotency-Key`, unknown customer or product |

## API

| Method | Path | Purpose |
|---|---|---|
| GET | `/api/customers` | Seeded customers (cached) |
| GET | `/api/products` | Seeded products (cached) |
| GET | `/api/customers/{id}/cards` | A customer's saved cards, masked only (cached) |
| POST | `/api/checkout` | Header `Idempotency-Key`; body `customerId`, `productId`. Returns `invoiceNo`, `status`, `redirectUrl` |
| POST | `/api/payments/2c2p/notification` | 2C2P backend notification (`backendReturnUrl`) |
| GET | `/api/transactions/{invoiceNo}` | Status and details for the Result page, never cached |

Errors use RFC 7807 Problem Details: 400 missing key, 404 unknown customer, product or invoice,
409 a checkout with the same key is already running.

## How it works

### Payment flow (new payment method)

1. `POST /api/checkout` validates the customer and product, inserts a **Pending** transaction,
   and requests a Payment Token from 2C2P (JWT signed with HMAC SHA-256).
2. The frontend sends the browser to the returned hosted-page URL.
3. 2C2P POSTs a signed notification to the backend. The handler verifies the signature,
   checks the amount and currency against the stored transaction, and sets **Succeeded** or
   **Failed** with a single `UPDATE … WHERE status = 'Pending'`. That makes repeated
   notifications harmless.
4. Only a verified notification or a verified Payment Inquiry marks a payment Succeeded;
   the browser redirect never does.

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
cards cache is cleared when a new card is saved (`LookupService.RemoveCardsCacheAsync`, wired in
with the Recurring flow). Transaction status is never cached: the Result page always reads it
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
- **Payment Inquiry uses `/payment/4.3/`**, matching the assessment's API version. `4.5` returns
  `channelCode: "CC"` instead of the card brand.
- **Money** is `DECIMAL(12,2)`. No card number, CVV or expiry is ever stored; only masked numbers
  and 2C2P customer tokens.

## Known limitations

- **Redis down:** checkouts continue without the lock and the unique index prevents duplicate
  rows and duplicate Payment Tokens. In that window, an interrupted-request recovery could
  run while the original request is still in flight.
- **A second payment provider** would need a `provider` column on `transaction`, so notifications
  and Inquiry reach the right provider, plus provider selection (keyed services or a small
  factory). The interfaces and neutral types are already in place.
