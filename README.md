# Payment Assessment

Checkout application with a .NET 8 Web API backend and a Next.js frontend.

> **Status: project skeleton only.** No application code yet — no database, cache or
> payment-provider integration is wired up. Setup steps, architecture notes and
> implementation decisions will be filled in as the project is built.

## Structure

```text
backend/      .NET 8 solution (PaymentAssessment)
  src/
    PaymentAssessment.Domain/          entities and domain rules
    PaymentAssessment.Application/     use cases and abstractions
    PaymentAssessment.Infrastructure/  external concerns behind interfaces
    PaymentAssessment.Api/             HTTP layer, controllers, Swagger
frontend/     Next.js (TypeScript, Tailwind, ESLint, App Router)
```

Project references: `Application → Domain`, `Infrastructure → Application`,
`Api → Application + Infrastructure`.

## Prerequisites

- .NET SDK 8.0
- Node.js 20 or later

## Local setup

### 1. Configure 2C2P credentials

Copy the example settings file:

```bash
cp backend/src/PaymentAssessment.Api/appsettings.Development.example.json \
   backend/src/PaymentAssessment.Api/appsettings.Development.json
```

- Fill in the Singapore sandbox merchant ID and secret key from
  https://developer.2c2p.com/docs/sandbox.
- Do this before `docker compose up`: compose mounts this file, and if it's missing
  Docker creates an empty folder in its place.
- The file is git-ignored; never commit it.

### 2. Expose the API for 2C2P notifications

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

## Build

```bash
dotnet build backend/PaymentAssessment.sln

cd frontend
npm install
npm run build
```

## Run

Backend — Swagger UI is registered for the Development environment only:

```bash
cd backend/src/PaymentAssessment.Api
dotnet run --launch-profile http
```

- API: http://localhost:5253
- Swagger UI: http://localhost:5253/swagger

The `https` profile serves https://localhost:7035 and needs a trusted local
certificate first (`dotnet dev-certs https --trust`).

Frontend:

```bash
cd frontend
npm run dev
```

- http://localhost:3000

## Configuration

Local settings belong in `backend/src/PaymentAssessment.Api/appsettings.Development.json`,
which is excluded by `.gitignore`. Keep credentials and secrets out of version control.
