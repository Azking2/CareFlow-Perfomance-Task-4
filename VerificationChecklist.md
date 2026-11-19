# CareFlow verification checklist

Run `dotnet run --project .\CareFlow.Verification\CareFlow.Verification.csproj` from the output folder. Each passing check prints `PASS` in the terminal. The program uses a temporary SQLite file and removes it after the run.

| Scenario | How it was checked | Result |
|---|---|---|
| AC-01 Availability | Read seeded services for a future date and compare booked/remaining counts with their stored capacities. | Pass |
| AC-02 Multi-service booking | Book Complete Blood Count and Urinalysis together, then read both persisted service rows and availability. | Pass |
| AC-03 Invalid request | Submit empty, duplicate-service, past-date, and over-capacity requests; verify booking count does not increase. | Pass |
| AC-04 Stale availability | Read ECG availability, consume its last spot, then submit another ECG request and verify it is rejected. | Pass |
| AC-05 Transaction rollback | Add a temporary SQLite trigger that fails while inserting booking services; verify neither the booking header nor service rows remain. | Pass |
| AC-06 Persistence | Read the confirmed booking through a new context factory connected to the same database file. | Pass |

The checklist records executable service and database checks. The Avalonia window was also opened and smoke-checked: a two-service booking appeared in the list and detail view, and both remaining-capacity counts updated.
