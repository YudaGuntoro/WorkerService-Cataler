=======================================
Cataler WorkerService API - Scaffold & Migration Guide
=======================================
Project: Web API for monitoring & managing machine cycle times
Platform: ASP.NET Core 8.0
Database: MySQL 8.x
EF Core Provider: Pomelo.EntityFrameworkCore.MySql

------------------------------------------------------------
📦 NuGet Packages Required
------------------------------------------------------------
Install these packages (via CLI or Package Manager):

dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
dotnet add package Dapper --version 2.0.123
dotnet add package Swashbuckle.AspNetCore --version 6.5.0

------------------------------------------------------------
📁 Folder Structure (Recommendation)
------------------------------------------------------------
- Web.API/                      → Startup project (API)
- Web.API.Domain/              → Domain models (entities, interfaces)
- Web.API.Persistence/         → EF Core DbContext & repository layer

📦 Web.API.Persistence
 └── 📁 DbContext
      └── 📄 ApplicationDbContext.cs  ← DbContext di sini

📦 Web.API.Domain
 └── 📁 Entities
      ├── 📄 MachineMaster.cs
      ├── 📄 LogCycletime.cs
      └── ... (entitas lainnya)
------------------------------------------------------------
🔧 EF Core Scaffold Commands (Database First)
------------------------------------------------------------
1. Scaffold specific table (e.g. `log_cycletime` only):

dotnet ef dbcontext scaffold "server=localhost;database=cataler;user=root;password=root_native" Pomelo.EntityFrameworkCore.MySql -o Models --table log_cycletime

2. Scaffold all tables into a folder:

dotnet ef dbcontext scaffold "server=localhost;database=cataler;user=root;password=root_native" Pomelo.EntityFrameworkCore.MySql -o Models

3. Scaffold using connection string from appsettings.json:

dotnet ef dbcontext scaffold "Name=ConnectionStrings:Default" Pomelo.EntityFrameworkCore.MySql -o Models

4. Scaffold into domain layer with custom context name:

dotnet ef dbcontext scaffold "server=localhost;database=cataler;user=root;password=root_native" Pomelo.EntityFrameworkCore.MySql --output-dir Entities --context ApplicationDbContext --project "D:\Project\TOHO_Yuda\PT Cataler Indonesia\WorkerService\Web.API.Domain" --startup-project "D:\Project\TOHO_Yuda\PT Cataler Indonesia\WorkerService\Web.API" --force

Note:
- `--context` = Nama file dan nama class untuk DbContext yang dihasilkan
- `--force` = Overwrite jika file sudah ada

5. Scaffold Fix Sesuai Folder Structure

dotnet ef dbcontext scaffold "server=localhost;database=cataler;user=root;password=root_native" Pomelo.EntityFrameworkCore.MySql --context AppDbContext --context-dir Context --context-namespace Web.API.Persistence.Context --output-dir ../Web.API.Domain/Entities --namespace Web.API.Domain.Entities --project "D:\Project\TOHO_Yuda\PT Cataler Indonesia\WorkerService\Web.API.Persistence" --startup-project "D:\Project\TOHO_Yuda\PT Cataler Indonesia\WorkerService\Web.API" --force

6. Scaffold Fix TinyInt As Bool

dotnet ef dbcontext scaffold "server=localhost;database=cataler;user=root;password=root_native;TreatTinyAsBoolean=true" Pomelo.EntityFrameworkCore.MySql --context AppDbContext --context-dir Context --context-namespace Web.API.Persistence.Context --output-dir ..\Web.API.Domain\Entities --namespace Web.API.Domain.Entities --project "D:\Project\TOHO_Yuda\PT Cataler Indonesia\WorkerService\Web.API.Persistence" --startup-project "D:\Project\TOHO_Yuda\PT Cataler Indonesia\WorkerService\Web.API" --force

------------------------------------------------------------
⚙️ EF Core Migration Command (Optional)
------------------------------------------------------------

Untuk code-first migration:

dotnet ef migrations add InitialCreate --project "D:\Project\TOHO_Yuda\PT Cataler Indonesia\WorkerService\Web.API.Persistence" --startup-project "D:\Project\TOHO_Yuda\PT Cataler Indonesia\WorkerService\Web.API"

------------------------------------------------------------
🛠️ PowerShell Scaffold (Paket Manager Console)
------------------------------------------------------------

Jika kamu pakai Visual Studio:

Scaffold-DbContext "server=localhost;database=cataler;user=root;password=root_native" Pomelo.EntityFrameworkCore.MySql -OutputDir Entities -Context CatalerContext -Project "D:\Project\TOHO_Yuda\PT Cataler Indonesia\WorkerService\Web.API.Domain" -StartupProject "D:\Project\TOHO_Yuda\PT Cataler Indonesia\WorkerService\Web.API" -Tables log_cycletime -Force

------------------------------------------------------------
📝 Catatan Tambahan
------------------------------------------------------------

- `ApplicationDbContext` bisa disiapkan manual, lalu copy semua `DbSet<>` dari hasil scaffold.
- Jika scaffold langsung pakai `--context ApplicationDbContext`, maka file `ApplicationDbContext.cs` akan di-*overwrite*.
- Untuk struktur folder yang lebih bersih, pertimbangkan memindahkan:
    - Entities ke folder `Web.API.Domain/Entities`
    - DbContext ke `Web.API.Persistence/ApplicationDbContext.cs`

------------------------------------------------------------
📝 Masuk Folder
------------------------------------------------------------

cd "D:\Project\TOHO_Yuda\PT Cataler Indonesia\WorkerService\Web.API"

dotnet run --urls=http://localhost:1234
Web.API.exe --urls=http://localhost:1234

// Port Forwarding

:: MQTT TCP
netsh interface portproxy add v4tov4 listenaddress=0.0.0.0 listenport=1883 connectaddress=127.0.0.1 connectport=1883
netsh interface portproxy add v4tov4 listenaddress=0.0.0.0 listenport=8883 connectaddress=127.0.0.1 connectport=8883

:: WebSocket & Dashboard
netsh interface portproxy add v4tov4 listenaddress=0.0.0.0 listenport=8083 connectaddress=127.0.0.1 connectport=8083
netsh interface portproxy add v4tov4 listenaddress=0.0.0.0 listenport=18083 connectaddress=127.0.0.1 connectport=18083

sc create WorkerService binPath= "C:\Users\Administrator\Documents\Toho\Compose\Copy\WorkerService\WorkerService.exe" start= auto DisplayName= "Worker Service (Toho)"
sc create WorkerLogger binPath= "C:\Users\Administrator\Documents\Toho\Compose\Copy\WorkerLogger\WorkerLogger.exe" start= auto DisplayName= "Worker Logger Service"
sc create WorkerNotification binPath= "C:\Users\Administrator\Documents\Toho\Compose\Copy\WorkerNotification\WorkerNotification.exe" start= auto DisplayName= "Worker Notification Service"

:: Start service
sc start <ServiceName>

:: Stop service
sc stop <ServiceName>

:: Cek status
sc query <ServiceName>

:: Hapus service
sc delete <ServiceName>

Store Procedure

- SHOW EVENTS FROM cataler
- SHOW EVENTS FROM cataler LIKE 'cataler_evt_purge_mrh_3days';

Copy Event From Local to Server

SET GLOBAL event_scheduler = ON;

DROP EVENT IF EXISTS cataler.MirrorCardNoToProduct_Event;

CREATE DEFINER=`root`@`localhost` EVENT cataler.MirrorCardNoToProduct_Event
ON SCHEDULE EVERY 1 SECOND
STARTS CURRENT_TIMESTAMP
ON COMPLETION PRESERVE
ENABLE
DO CALL cataler.MirrorCardNoToProduct(1);


============================================================
END OF FILE
============================================================