# CareFlow

CareFlow is a small Avalonia desktop application for outpatient diagnostic bookings. Staff can review daily service capacity, book one to three different services for an existing fictional patient, and view confirmed bookings. The booking service rechecks capacity and saves each booking as one SQLite transaction, so a failed request does not leave partial records.

## Requirements

- .NET 10 SDK
- Windows, Linux, or macOS desktop environment supported by Avalonia

## Pair members

- Student 1: [Enter name]
- Student 2: [Enter name]

The `CareFlow.sln` solution contains the app and its verification runner. From this folder's root, `dotnet build .\CareFlow.sln -m:1` builds both projects.

## Run the application

1. Open a terminal in the `CareFlow` folder.
2. Restore dependencies and start the app:

   ```powershell
   dotnet restore
   dotnet run
   ```

On first launch, CareFlow applies the included EF Core migration and adds the fictional sample patients and diagnostic services. The database is stored at `%LOCALAPPDATA%\CareFlow\careflow.db` on Windows (the equivalent local application data folder on other platforms). Confirmed bookings remain there between runs.

To use a different database file, set `CAREFLOW_DB_PATH` before starting CareFlow. For example:

```powershell
$env:CAREFLOW_DB_PATH = "$PWD\careflow.db"
dotnet run
```

To apply migrations manually, run these commands from the `CareFlow` folder. The app also runs pending migrations automatically at startup.

```powershell
dotnet tool restore
dotnet ef database update
```

## Sample information

CareFlow creates three fictional patients: Alex Santos, Jamie Cruz, and Taylor Reyes. The services and daily capacities are Complete Blood Count (10), Urinalysis (8), Chest X-Ray (6), and ECG (5). The application does not include patient management, staff accounts, cancellations, or medical records.

## Project layout

- `CareFlow/Models` contains the four relational entities.
- `CareFlow/Data` contains EF Core configuration and the design-time context factory.
- `CareFlow/Migrations` contains the reproducible initial database migration.
- `CareFlow/Services/BookingService.cs` validates requests, checks availability, and coordinates the transaction.
- `CareFlow/ViewModels` holds the MVVM screen state and commands.
- `CareFlow/Views` contains the Avalonia XAML interface.
- `CareFlow.Verification` is a small runnable check for acceptance scenarios AC-01 through AC-06.
- `RelationalDesign.md` and `VerificationChecklist.md` summarize the database and checks.

## Verify the acceptance scenarios

Run the verification program from the output folder:

```powershell
dotnet run --project .\CareFlow.Verification\CareFlow.Verification.csproj
```

The program creates and deletes its own temporary SQLite database. It checks persisted capacity, multi-service bookings, invalid requests, a stale availability attempt, rollback after a controlled database failure, and retrieval through a fresh database context factory.

## Acknowledgement

AI assistance was used to help implement the application and prepare its supporting documentation. The student should review the code and be ready to explain the workflow and transaction boundary.
