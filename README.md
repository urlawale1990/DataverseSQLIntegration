# DataverseSQLIntegration

An Azure Functions v4 application (.NET 8) that provides bi-directional contact synchronisation between **Microsoft Dataverse (Dynamics 365)** and an **Azure SQL Database**. The solution exposes three HTTP-triggered functions that enable seamless data consistency across both platforms.

---

## 📐 Architecture Overview

```
┌──────────────┐        HTTP Trigger         ┌───────────────────────────┐
│   API Client │ ─────────────────────────── │   Azure Function App      │
└──────────────┘                             │                           │
                                             │  ┌─────────────────────┐  │
                                             │  │ CreateContactFromSQL │  │
                                             │  │ GetContactByEmail   │  │
                                             │  │ UpdateContactInBoth │  │
                                             │  └─────────────────────┘  │
                                             │         │         │        │
                                             └─────────┼─────────┼────────┘
                                                       │         │
                                          ┌────────────┘         └────────────┐
                                          ▼                                   ▼
                                  ┌───────────────┐                 ┌─────────────────┐
                                  │   Dataverse   │                 │   Azure SQL DB  │
                                  │  (Dynamics 365│                 │    Contacts     │
                                  │    Contacts)  │                 │     Table       │
                                  └───────────────┘                 └─────────────────┘
```

---

## 🚀 Features

- **SQL → Dataverse sync**: Look up a contact in SQL by email and create it in Dataverse
- **Dataverse → SQL sync**: Retrieve a contact from Dataverse and upsert it into SQL
- **Bi-directional update**: Update a contact in both Dataverse and SQL in a single call
- **OAuth 2.0 authentication**: Client credentials flow via Microsoft Identity Platform
- **Upsert logic**: Prevents duplicates — inserts on first sync, updates on subsequent syncs
- **Sync direction tracking**: Records the direction of each sync operation in SQL

---

## 🗂️ Project Structure

```
DataverseSQLIntegration/
├── Functions/
│   ├── CreateContactFromSQL.cs     # POST – Sync contact from SQL → Dataverse
│   ├── GetContactByEmail.cs        # GET  – Fetch from Dataverse, sync → SQL
│   └── UpdateContactInBoth.cs      # POST – Update contact in Dataverse and SQL
├── Models/
│   ├── ContactRequest.cs           # Request payload model
│   └── SqlContact.cs               # SQL contact entity model
├── Services/
│   ├── DataverseService.cs         # Dataverse Web API calls (Get, Create, Update)
│   ├── SqlService.cs               # SQL operations (Upsert, GetByEmail, GetAll)
│   └── TokenService.cs             # OAuth 2.0 token acquisition (client credentials)
├── Program.cs                      # Azure Functions host configuration
├── host.json                       # Function host settings with App Insights
└── DataverseSQLIntegration.csproj  # Project file (.NET 8, Azure Functions v4)
```

---

## ⚙️ Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- An **Azure SQL Database** with the `Contacts` table (see schema below)
- A **Dynamics 365 / Dataverse** environment
- An **Azure AD App Registration** with Dataverse API permissions

---

## 🗄️ SQL Table Schema

Run the following script to create the required `Contacts` table:

```sql
CREATE TABLE Contacts (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    DataverseId    NVARCHAR(50)  NULL,
    FullName       NVARCHAR(200) NOT NULL,
    Email          NVARCHAR(200) NOT NULL UNIQUE,
    MobilePhone    NVARCHAR(50)  NULL,
    SyncDirection  NVARCHAR(50)  NULL,
    CreatedOn      DATETIME      DEFAULT GETDATE(),
    LastSyncedOn   DATETIME      NULL
);
```

---

## 🔐 Configuration

### Local Development

Create a `local.settings.json` file in the project root (excluded from source control):

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "TenantId": "<your-azure-ad-tenant-id>",
    "ClientId": "<your-app-registration-client-id>",
    "ClientSecret": "<your-app-registration-client-secret>",
    "DataverseUrl": "https://<your-org>.crm.dynamics.com",
    "SqlConnectionString": "Server=<server>;Database=<db>;User Id=<user>;Password=<password>;"
  }
}
```

### Azure App Settings

When deployed to Azure, configure the same values as **Application Settings** in your Function App:

| Setting               | Description                                      |
|-----------------------|--------------------------------------------------|
| `TenantId`            | Azure AD tenant ID                               |
| `ClientId`            | App Registration client ID                       |
| `ClientSecret`        | App Registration client secret                   |
| `DataverseUrl`        | Dataverse environment URL                        |
| `SqlConnectionString` | Azure SQL connection string                      |

---

## 🔌 API Reference

### 1. `POST /api/CreateContactFromSQL`

Looks up a contact in SQL by email and creates it in Dataverse. Updates the SQL record with the new Dataverse contact ID.

**Request Body:**
```json
{
  "email": "john.doe@example.com"
}
```

**Success Response (201):**
```json
{
  "Status": "Success",
  "Message": "Contact synced from SQL to Dataverse",
  "DataverseId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "Email": "john.doe@example.com",
  "FullName": "John Doe"
}
```

---

### 2. `GET /api/GetContactByEmail?email={email}`

Retrieves a contact from Dataverse by email address and upserts the record into SQL.

**Query Parameter:** `email` *(required)*

**Success Response (200):**
```json
{
  "contactid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "fullname": "John Doe",
  "emailaddress1": "john.doe@example.com",
  "mobilephone": "+44 7000 000000"
}
```

---

### 3. `POST /api/UpdateContactInBoth`

Updates a contact in both Dataverse and SQL in a single operation.

**Request Body:**
```json
{
  "FirstName": "John",
  "LastName": "Doe",
  "EmailAddress1": "john.doe@example.com",
  "MobilePhone": "+44 7000 000000",
  "DateOfBirth": "1990-01-15",
  "ParentAccountId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "GenderCode": 1,
  "DoNotEmail": false
}
```

**Success Response (200):**
```json
{
  "Status": "Success",
  "Message": "Contact updated in both Dataverse and SQL",
  "DataverseId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "FullName": "John Doe",
  "Email": "john.doe@example.com"
}
```

---

## 🛠️ NuGet Packages

| Package | Version |
|---|---|
| `Microsoft.Azure.Functions.Worker` | 2.0.0 |
| `Microsoft.Azure.Functions.Worker.Sdk` | 2.0.0 |
| `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` | 2.0.0 |
| `Microsoft.Data.SqlClient` | 5.2.0 |

---

## 🧪 Running Locally

```bash
# Restore dependencies
dotnet restore

# Start the function app
func start
```

The functions will be available at `http://localhost:7071/api/`.

---

## ☁️ Deployment

Deploy to Azure using the Azure Functions Core Tools:

```bash
# Publish build
dotnet publish -c Release

# Deploy to Azure
func azure functionapp publish <your-function-app-name>
```

Or deploy via **Visual Studio** using the Zip Deploy publish profile included under `Properties/ServiceDependencies/`.

---

## 📊 Monitoring

Application Insights is pre-configured in `host.json` with sampling enabled and live metrics filters active. Connect your Function App to an Application Insights instance in the Azure portal for end-to-end telemetry.

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Commit your changes: `git commit -m 'Add your feature'`
4. Push to the branch: `git push origin feature/your-feature`
5. Open a Pull Request

---

## 📄 Licence

This project is licensed under the [MIT License](LICENSE).
