# CareFlow verification checklist

## Automated checks

From the repository root:

```powershell
dotnet run --project .\CareFlow.Verification\CareFlow.Verification.csproj
```

The runner creates a temporary SQLite database, applies the EF Core migration, calls `BookingService` directly, and deletes the database afterwards. Each passing check prints `PASS`; any failure stops the run with an error.

Checks count rows straight from the `Bookings` and `BookingServices` tables and compare per-service booked counts, so a half-saved booking or a changed capacity would be noticed.

| Scenario | What the runner does | Rules covered |
|---|---|---|
| AC-01 View availability | Reads availability for a date and compares it with the seeded capacities; confirms a booking on one date does not change another date. | BR-09 |
| AC-02 Multi-service booking | Books Complete Blood Count and Urinalysis together; confirms one booking row, two booking-service rows, and one unit consumed per service. | BR-04 |
| AC-03 Invalid or fully booked request | Submits requests with 0 services, 4 services, a duplicate service, a past date, an unknown patient, and an unknown service. Separately requests a free service together with a full one (CBC + ECG). Each is rejected, and row counts and booked counts are unchanged. | BR-01, BR-02, BR-03, BR-05, BR-06, BR-10 |
| AC-04 Stale availability | Reads ECG as available, books its last spot through another call, then submits a request based on the old view; the request is rejected and ECG is not overbooked. | BR-05, BR-08 |
| AC-05 Database failure | Adds a temporary SQLite trigger that fails when a booking-service row is inserted. The booking fails with a database error; no booking or booking-service row remains and no capacity changes. After the trigger is dropped, the same request succeeds. | BR-07 |
| AC-06 Persistence | Reads all bookings and their services through a new database factory on the same database file. | BR-09 |

How the scenarios are simulated:

- AC-04 uses two sequential calls on one connection, not two users at once. The ECG daily capacity is lowered to 1 in the test database so the scenario is quick to set up.
- AC-05 uses a controlled failure (the trigger). Source-level evidence is in `BookingService.CreateBookingAsync`, where the capacity check and all inserts run inside one transaction that is committed only at the end.
- AC-06 simulates a restart with a new database factory. The manual restart check below covers the real application.

Last runner output (paste it here after running):

```
(paste the PASS lines from your latest run)
```

## Manual checks in the Avalonia app

Tick a box only after you have done the check.

- [ ] The app starts on a fresh database and the patient list and service list appear.
- [ ] A booking with two services is confirmed and appears in the list and detail view.
- [ ] The remaining capacity of both services goes down by one after that booking.
- [ ] Closing and reopening the app still shows the booking (real AC-06).
- [ ] A service with no remaining spots cannot be selected.
- [ ] Choosing a past date or no service shows a clear message and saves nothing.

## Not covered

- Real concurrent users (not required by the task).
- Automated UI tests; the interface is checked manually.
