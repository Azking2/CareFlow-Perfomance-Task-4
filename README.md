# CareFlow

CareFlow is a small Avalonia desktop application for outpatient diagnostic bookings. Staff can review each service's remaining daily capacity, book one to three different services for an existing fictional patient on one date, and view confirmed bookings. A booking is saved as one database transaction, so a request that cannot be fully accommodated leaves no partial records.

## Pair members

- Student 1: Canarejo, John Paul
- Student 2: Dales, James A.

## Requirements

- .NET 10 SDK
- A Windows, Linux, or macOS desktop environment supported by Avalonia

Technology: C#, Avalonia UI (XAML), MVVM (CommunityToolkit.Mvvm), Entity Framework Core with SQLite, dependency injection.

## Build and run

Run these from the repository root (the folder containing `CareFlow.sln`):

```powershell
dotnet restore
dotnet build .\CareFlow.sln -m:1
dotnet run --project .\CareFlow\CareFlow.csproj
```

## Database and sample data

CareFlow uses SQLite. On first launch the app applies the included EF Core migration, which creates the tables and adds the fictional sample data. No manual database setup is needed.

- Default database file: `%LOCALAPPDATA%\CareFlow\careflow.db` on Windows (the local application data folder on other platforms). Confirmed bookings stay there between runs.
- To use a different file, set `CAREFLOW_DB_PATH` before starting the app:

  ```powershell
  $env:CAREFLOW_DB_PATH = "$PWD\careflow.db"
  dotnet run --project .\CareFlow\CareFlow.csproj
  ```

- To apply migrations manually (optional, since the app does it at startup), run from the `CareFlow` folder:

  ```powershell
  dotnet tool restore
  dotnet ef database update
  ```

Sample data: three fictional patients (Alex Santos, Jamie Cruz, Taylor Reyes) and four services with daily capacities: Complete Blood Count (10), Urinalysis (8), Chest X-Ray (6), ECG (5). There are no passwords or credentials anywhere in the project.

## How it works

| Layer | Where | Responsibility |
|---|---|---|
| View | `CareFlow/Views` | Avalonia XAML only |
| ViewModel | `CareFlow/ViewModels` | Screen state and commands; no EF Core or database access |
| Application logic and business rules | `CareFlow/Services` | `BookingService` validates requests and runs the transaction; `DatabaseInitializer` applies migrations; `BookingRejectedException` marks business-rule failures |
| Persistence | `CareFlow/Data`, `CareFlow/Models`, `CareFlow/Migrations` | EF Core context, entities, migration |

Booking transaction (`BookingService.CreateBookingAsync`):

1. Cheap checks first: date not in the past (BR-03), one to three services (BR-02), no duplicates (BR-06).
2. Inside one database transaction: confirm the patient exists (BR-01), load the services, and recheck each service's remaining capacity for that date (BR-04, BR-05, BR-08).
3. Insert the booking and all its service rows, then commit (BR-07). Any failure before the commit rolls everything back.

Business-rule failures show a clear message in the status bar. Unexpected database errors show a generic message and the booking is not reported as saved (BR-10).

## Verify the acceptance scenarios

```powershell
dotnet run --project .\CareFlow.Verification\CareFlow.Verification.csproj
```

The runner uses a temporary SQLite database and covers AC-01 to AC-06, including a stale-availability case and a controlled database failure. See `VerificationChecklist.md` for what each check does and the manual checks in the app.

## Documentation

- `RelationalDesign.md`: tables, keys, and relationships
- `VerificationChecklist.md`: acceptance scenarios, how they were checked, and what is not covered

## Acknowledgement

AI assistance (Claude) was used to help implement parts of the application, refactor it, expand the verification runner, and prepare the supporting documentation. Both members reviewed the code and are responsible for it.# CareFlow

CareFlow is a small Avalonia desktop application for outpatient diagnostic bookings. Staff can review each service's remaining daily capacity, book one to three different services for an existing fictional patient on one date, and view confirmed bookings. A booking is saved as one database transaction, so a request that cannot be fully accommodated leaves no partial records.

## Pair members

- Student 1: Canarejo, John Paul
- Student 2: Dales, James A.

## Requirements

- .NET 10 SDK
- A Windows, Linux, or macOS desktop environment supported by Avalonia

Technology: C#, Avalonia UI (XAML), MVVM (CommunityToolkit.Mvvm), Entity Framework Core with SQLite, dependency injection.

## Build and run

Run these from the repository root (the folder containing `CareFlow.sln`):

```powershell
dotnet restore
dotnet build .\CareFlow.sln -m:1
dotnet run --project .\CareFlow\CareFlow.csproj
```

## Database and sample data

CareFlow uses SQLite. On first launch the app applies the included EF Core migration, which creates the tables and adds the fictional sample data. No manual database setup is needed.

- Default database file: `%LOCALAPPDATA%\CareFlow\careflow.db` on Windows (the local application data folder on other platforms). Confirmed bookings stay there between runs.
- To use a different file, set `CAREFLOW_DB_PATH` before starting the app:

  ```powershell
  $env:CAREFLOW_DB_PATH = "$PWD\careflow.db"
  dotnet run --project .\CareFlow\CareFlow.csproj
  ```

- To apply migrations manually (optional, since the app does it at startup), run from the `CareFlow` folder:

  ```powershell
  dotnet tool restore
  dotnet ef database update
  ```

Sample data: three fictional patients (Alex Santos, Jamie Cruz, Taylor Reyes) and four services with daily capacities: Complete Blood Count (10), Urinalysis (8), Chest X-Ray (6), ECG (5). There are no passwords or credentials anywhere in the project.

## How it works

| Layer | Where | Responsibility |
|---|---|---|
| View | `CareFlow/Views` | Avalonia XAML only |
| ViewModel | `CareFlow/ViewModels` | Screen state and commands; no EF Core or database access |
| Application logic and business rules | `CareFlow/Services` | `BookingService` validates requests and runs the transaction; `DatabaseInitializer` applies migrations; `BookingRejectedException` marks business-rule failures |
| Persistence | `CareFlow/Data`, `CareFlow/Models`, `CareFlow/Migrations` | EF Core context, entities, migration |

Booking transaction (`BookingService.CreateBookingAsync`):

1. Cheap checks first: date not in the past (BR-03), one to three services (BR-02), no duplicates (BR-06).
2. Inside one database transaction: confirm the patient exists (BR-01), load the services, and recheck each service's remaining capacity for that date (BR-04, BR-05, BR-08).
3. Insert the booking and all its service rows, then commit (BR-07). Any failure before the commit rolls everything back.

Business-rule failures show a clear message in the status bar. Unexpected database errors show a generic message and the booking is not reported as saved (BR-10).

## Verify the acceptance scenarios

```powershell
dotnet run --project .\CareFlow.Verification\CareFlow.Verification.csproj
```

The runner uses a temporary SQLite database and covers AC-01 to AC-06, including a stale-availability case and a controlled database failure. See `VerificationChecklist.md` for what each check does and the manual checks in the app.

## Documentation

- `RelationalDesign.md`: tables, keys, and relationships
- `VerificationChecklist.md`: acceptance scenarios, how they were checked, and what is not covered

## Acknowledgement

AI assistance (Claude) was used to help implement parts of the application, refactor it, expand the verification runner, and prepare the supporting documentation. Both members reviewed the code and are responsible for it.
