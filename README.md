# Car Rental

Sign up, sign in, search a fleet of cars by date and location, and book one. ASP.NET Core 10,
Clean Architecture, Minimal APIs, EF Core, PostgreSQL, and a dependency-free browser front end
served from the same host.

## Quick start

Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download) and Docker.

```bash
docker compose up -d                     # PostgreSQL on localhost:5432
dotnet run --project src/CarRental.Api   # http://localhost:5080
```

First run migrates the database and seeds two roles, an admin account and a 16-car demo fleet.
It is idempotent, so restarting changes nothing.

**Seeded admin:** `admin@carrental.local` / `Admin#12345` — change it before the app is
reachable by anyone else. Create a customer account from the sign-up page.

API docs (Development only): <http://localhost:5080/scalar/v1>

```bash
dotnet test    # starts its own database; nothing else need be running
```

## Architecture

```
src/
  CarRental.Domain           entities, enums, the Result/Error types, the error catalog
  CarRental.Application      services, contracts, validators, repository interfaces
  CarRental.Infrastructure   EF Core, Identity, repositories, JWT, email
  CarRental.Api              Minimal API endpoints, problem details, wwwroot
tests/
  CarRental.IntegrationTests the real app over HTTP against a real PostgreSQL container
  CarRental.UnitTests        domain rules and validators, no I/O
```

Each project depends only on the one inside it, with one deliberate exception: ASP.NET Core
Identity. `ApplicationUser : IdentityUser<Guid>` lives in Domain, and the services take
`UserManager<ApplicationUser>` directly rather than behind an interface of our own. Wrapping
Identity would mean re-declaring most of its surface for no gain, so it is treated as part of the
platform, like `DateTimeOffset`. Everything that touches EF Core, Npgsql, JWT or SMTP does stay
behind an interface.

**No exceptions for control flow.** Every service method returns `Result<TValue>` holding either
a value or a list of `Error`s. Implicit conversions keep the call sites clean:

```csharp
if (car is null)   return CarErrors.NotFound;   // Error   -> Result<T>
return reservation.ToResponse();                // TValue  -> Result<T>
```

`ResultExtensions` is the only place a `Result` becomes a response, and `ProblemExtensions` maps
`ErrorType` to a status code. Every failure — a `Result` error, a thrown exception, a framework
404 — comes back as an RFC 9457 problem document in one of two shapes:

| Error type | Status | Body |
| --- | --- | --- |
| `Validation` | 400 | `errors` keyed by **field name** — `{"email": ["Enter a valid email address."]}` |
| everything else | mapped per type | `errorCode` with a **dotted domain code** — `"car.unavailable"` |

That split is what lets the browser attach a message to the input it belongs to while still
recognising a business failure by a stable code.

FluentValidation runs in an endpoint filter before the handler, so a service never sees a
malformed request. Repositories stage changes; nothing is written until a service calls
`IUnitOfWork.SaveChangesAsync`.

**Time.** Every instant is a `DateTimeOffset`; calendar dates stay `DateOnly`, because a pickup
day is not an instant. `UtcDateTimeOffsetConverter` normalises to UTC on write, since Npgsql
refuses a non-zero offset on `timestamptz`.

## Authentication

ASP.NET Core Identity for users, roles and passwords; JWT bearer tokens for requests.

- **Access token** — 15 minutes, carries `sub`, `email`, name and role claims.
- **Refresh token** — 7 days, stored in our own table, and **rotated**: redeeming one revokes it,
  so a leaked token is usable at most once.
- **Lockout** — five failed attempts locks the account for fifteen minutes.
- **Password reset** — Identity's DataProtector token: encrypted, signed, valid one hour, and
  bound to the security stamp so it works exactly once. Sent over SMTP via MailKit when
  `Smtp:Host` is set; otherwise the link is written to the log so the flow stays testable.
- **Roles** — `Customer` and `Admin`, seeded on startup. Fleet management is admin-only.

Sign-in failures are deliberately identical for a wrong password and an unknown account, and
`forgot-password` always answers 202, so neither can be used to discover registered addresses.

> Identity leaves its `EmailIndex` non-unique — `RequireUniqueEmail` is enforced by `UserManager`
> alone. `ApplicationUserConfiguration` marks it unique so the database enforces it too.

## API

All endpoints are under `/api`. Everything except the auth group requires a bearer token.

| Method | Route | Notes |
| --- | --- | --- |
| `POST` | `/auth/register` | Creates a customer and signs in. 409 if the email is taken. |
| `POST` | `/auth/login` | Returns an access + refresh token pair. |
| `POST` | `/auth/refresh` | Rotates the pair. The presented token is revoked. |
| `POST` | `/auth/logout` | Revokes every refresh token for the caller. |
| `POST` | `/auth/forgot-password` | Emails a reset link. Always 202. |
| `POST` | `/auth/reset-password` | Sets a new password from the link's token. |
| `GET` | `/cars` | Search. Filters below. |
| `GET` | `/cars/locations` | Distinct pickup locations, for the filter dropdown. |
| `GET` | `/cars/{id}` | One car. |
| `POST` `PUT` `DELETE` | `/cars`, `/cars/{id}` | Fleet management. **Admin only.** |
| `POST` | `/reservations` | Book a car. 409 if the dates overlap a confirmed booking. |
| `GET` | `/reservations` | The caller's reservations, upcoming and past. |
| `PUT` | `/reservations/{id}` | Move one that has not started to new dates, repriced. |
| `POST` | `/reservations/{id}/cancel` | Cancel one that has not started. |
| `GET` `PUT` | `/profile` | Read and update personal details. |
| `PUT` | `/profile/password` | Change password; signs other devices out. |

**Search filters:** `query`, `location`, `pickupDate`, `returnDate`, `category`, `transmission`,
`fuel`, `minSeats`, `minDailyRate`, `maxDailyRate`, `sortBy` (`price_asc`, `price_desc`,
`year_desc`, `seats_desc`), `page`, `pageSize`.

Supplying both dates hides any car with a confirmed booking overlapping the range, so results are
always genuinely bookable. Both dates are inclusive — a same-day rental bills one day.

**No double bookings, even under load.** The availability check reads before it writes, so
concurrent requests all see a car as free. A PostgreSQL exclusion constraint
(`daterange` + `btree_gist`, scoped to confirmed rows) is what actually settles it; the losing
request gets `409 car.unavailable` rather than a raw constraint error, and a cancellation
genuinely frees the days.

## Front end

Plain ES modules and CSS in `src/CarRental.Api/wwwroot`. No build step, no npm — the API serves
them itself, so it is one deployable and there is no CORS to configure.

| Page | Purpose |
| --- | --- |
| `signup.html` | The 12-field registration form, with a live password strength meter. |
| `signin.html` | Sign in, with a link into password recovery. |
| `index.html` | Dashboard: search + fleet, reservations, booking history, profile settings. |
| `forgot-password.html` / `reset-password.html` | Password recovery. |

Colours come from foothillsolutions.com: navy `#1e3a8a`, its `135deg` gradient partner `#3b82f6`,
gold `#fbbf24` as the accent. Gold never carries text on a light background — it is about 1.9:1
against white — so it fills and outlines only. Light and dark themes both follow the OS setting.

## Tests

**196 tests: 170 integration and 26 unit.** The only prerequisite is a working Docker daemon.

`CarRentalApiFactory` starts a `postgres:16-alpine` container with Testcontainers and boots the
app through `WebApplicationFactory<Program>`. Nothing is stubbed except `IEmailSender`: the tests
run the real pipeline, endpoints, DI graph, EF model and migrations, so a passing run proves the
migrations apply and the SQL is valid on PostgreSQL. Between tests the schema is truncated with
Respawn and re-seeded, so no test sees another's data. One shared container keeps it near 28s.

Test classes hold assertions and nothing else. `CarRentalApi` is a typed façade taking and
returning the production contract records, so a renamed field breaks the tests at compile time:

```csharp
var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 7, 11))).ShouldBeCreated();

reservation.TotalDays.ShouldBe(5);
response.ShouldBeConflict(CarErrors.Unavailable);
```

Failure assertions take an `Error` from the Domain catalog rather than a copied string;
`ErrorCodeContractTests` separately pins the published literals, which is what guards the wire
contract. Coverage spans auth and lockout, the reset-token lifecycle, every search filter,
all seven ways two date ranges can overlap, and the schema itself — indexes, column types,
delete rules, and that passwords are stored as PBKDF2-HMAC-SHA512 at 100,000 iterations.

## Configuration

`src/CarRental.Api/appsettings.json`:

| Key | Meaning |
| --- | --- |
| `ConnectionStrings:Default` | PostgreSQL connection string. |
| `Jwt:Key` | HMAC signing key, **32+ characters**. Validated at startup. |
| `Jwt:AccessTokenMinutes` / `Jwt:RefreshTokenDays` | Token lifetimes. |
| `ClientApp:BaseUrl` | Origin used to build the password reset link. |
| `Smtp:Host` | Mail server. **Empty means reset links are logged, not sent.** |
| `Smtp:Port` / `Smtp:Security` | `587` + `StartTls`, or `465` + `SslOnConnect`. |
| `Smtp:Username` / `Smtp:Password` | Omit both for a server accepting unauthenticated relay. |
| `Seed:Enabled` | Migrate and seed on startup. |
| `Seed:AdminEmail` / `Seed:AdminPassword` | The seeded admin account. |

The committed `Jwt:Key` is a development placeholder. Supply real secrets outside source control:

```bash
dotnet user-secrets --project src/CarRental.Api set "Jwt:Key" "$(openssl rand -base64 48)"
dotnet user-secrets --project src/CarRental.Api set "Smtp:Host" "smtp-relay.brevo.com"
dotnet user-secrets --project src/CarRental.Api set "Smtp:Username" "<your-login>"
dotnet user-secrets --project src/CarRental.Api set "Smtp:Password" "<your-smtp-key>"
```

Adding a migration:

```bash
dotnet ef migrations add <Name> \
  --project src/CarRental.Infrastructure --startup-project src/CarRental.Api \
  --output-dir Persistence/Migrations
```
