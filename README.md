📸 CosengPhotography Project Documentation





1\. Overview

CosengPhotography is a full-stack web application designed to manage photography events, galleries, and client access. It consists of:

•	Backend (API): ASP.NET Core Web API for authentication, gallery management, and file storage.

•	Frontend (FE): Blazor WebAssembly client for user interaction.

•	Shared Library: Class library containing DTOs and interfaces used by both FE and BE.





2\. Solution Structure

CosengPhotography.slnx

│

├── CosengPhotography.API          # Backend project

│   ├── Controllers/               # API endpoints (Auth, Gallery)

│   ├── Services/                  # Blob storage, email service

│   ├── Repositories/              # Data access layer

│   ├── Interfaces/                # Contracts implemented in API

│   └── Program.cs                 # Startup configuration

│   ├── Interfaces/                # Shared contracts

│   └── Models/                    # Common models



│

├── CosengPhotography.Frontend     # Blazor WebAssembly frontend

│   ├── Pages/                     # Razor pages (Gallery, Login, Dashboard)

│   ├── Components/                # Reusable UI components

│   └── Program.cs                 # Blazor startup

│

├── CosengPhotography.Shared       # Shared class library

│   ├── Dto/                       # Data Transfer Objects

│

└── README.md                      # Documentation





3\. Technology Stack

•	Backend: ASP.NET Core Web API (.NET 8)

•	Frontend: Blazor WebAssembly (.NET 8)

•	Database: SQL Server with Entity Framework Core

•	Storage: Azure Blob Storage for photos

•	Authentication: JWT Bearer tokens with ASP.NET Identity

•	Documentation: Swagger (Swashbuckle.AspNetCore)





4\. Setup Instructions

Backend

1\.	Navigate to CosengPhotography.API.

2\.	Configure appsettings.json: 

3\.	{

4\.	  "ConnectionStrings": {

5\.	    "DefaultConnection": "Server=.;Database=CosengPhotography;Trusted\_Connection=True;"

6\.	  },

7\.	  "AzureBlobStorage": {

8\.	    "ConnectionString": "<your-blob-connection-string>",

9\.	    "ContainerName": "photos"

10\.	  },

11\.	  "Jwt": {

12\.	    "Key": "<your-secret-key>",

13\.	    "Issuer": "CosengPhotography",

14\.	    "Audience": "CosengPhotographyUsers"

15\.	  }

16\.	}

17\.	Run migrations: 

18\.	dotnet ef database update

19\.	Start the API: 

20\.	dotnet run





Frontend

1\.	Navigate to CosengPhotography.Frontend.

2\.	Configure Program.cs to point to API: 

3\.	builder.Services.AddScoped(sp => new HttpClient

4\.	{

5\.	    BaseAddress = new Uri("https://localhost:5001") // API URL

6\.	});

7\.	Run the FE: 

8\.	dotnet run







5\. Development Workflow

•	Shared DTOs: Define contracts in CosengPhotography.Shared.

•	API: Implements controllers and services using DTOs.

•	Frontend: Calls API endpoints via HttpClient and consumes DTOs.

•	CORS: API must allow FE origin (https://localhost:5002) during development.





6\. Key Features

•	Authentication: Register/Login with JWT.

•	Gallery Management: Create, update, and share galleries.

•	Photo Uploads: Store securely in Azure Blob Storage.

•	Email Notifications: Send gallery access links to clients.

•	Frontend UI: Blazor pages for event browsing and gallery access.





7\. Deployment

•	Production: Publish FE into API’s wwwroot so both run under one domain.

•	Azure Hosting: 

o	API → Azure App Service

o	Blob Storage → Azure Storage Account

o	Database → Azure SQL







8\. Contribution Guidelines

•	Always pull latest changes before pushing.

•	Use feature branches (feature/gallery-upload) and PRs.

•	Keep .gitignore updated to exclude bin/, obj/, .vs/.





9\. Future Enhancements

•	Role-based access (Admin vs Client).

•	Photo watermarking.

•	Payment integration for premium galleries.

•	Mobile-friendly UI improvements.





