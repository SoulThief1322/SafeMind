# SafeMind User Manual
# WEBSITE IS DEPLOYED AT "safe-mind.net"
## 1. Prerequisites

- **.NET SDK 9.0.x**
- **SQL Server** (local or remote)

## 2. Installation Steps

### a. Clone the Repository
```
git clone <repository-url>
cd SafeMind
```

### b. Restore NuGet Packages
```
dotnet restore
```

### c. Update Database Connection String
- Edit `SafeMind/appsettings.json` and `SafeMind/appsettings.Development.json` to set your SQL Server connection string.

### d. Apply Database Migrations
```
dotnet ef database update --context SafeMindDbContext
dotnet ef database update --context DoctorLicensingDbContext
```

### e. Run the Application
```
dotnet run --project SafeMind/SafeMind.csproj --launch-profile https

--launch-profile is necessary in order for google login to be working
```

- The app will be available at `https://localhost:5001` or `http://localhost:5000` by default.

## 3. Default User Accounts

### a. Regular User
- **Email:** alex@gmail.com
- **Password:** Password1!

### b. Admin User
- **Email:** lyubomira.hristova@safemind.bg
- **Password:** Admin123!



## 4. Useful Terminal Commands

- **Run tests:**
  ```
  dotnet test SafeMind.Tests/SafeMind.Tests.csproj
  ```
- **Build solution:**
  ```
  dotnet build
  ```


## 5. Troubleshooting
- Ensure your database server is running and accessible.
- Check connection strings for typos.

## 6. Documentation
- See `Documentation/SafeMind.excalidraw.md` for architecture diagrams and additional notes.

---

For further help, contact the project maintainer.
