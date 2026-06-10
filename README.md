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

-- WARNING: This schema is for context only and is not meant to be run.
-- Table order and constraints may not be valid for execution.


CREATE TABLE public.__EFMigrationsHistory (
  MigrationId character varying NOT NULL,
  ProductVersion character varying NOT NULL,
  CONSTRAINT __EFMigrationsHistory_pkey PRIMARY KEY (MigrationId)
);
CREATE TABLE public.AspNetRoles (
  Id text NOT NULL,
  Name character varying,
  NormalizedName character varying,
  ConcurrencyStamp text,
  CONSTRAINT AspNetRoles_pkey PRIMARY KEY (Id)
);
CREATE TABLE public.AspNetUsers (
  Id text NOT NULL,
  FullName text NOT NULL,
  UserName character varying,
  NormalizedUserName character varying,
  Email character varying,
  NormalizedEmail character varying,
  EmailConfirmed boolean NOT NULL,
  PasswordHash text,
  SecurityStamp text,
  ConcurrencyStamp text,
  PhoneNumber text,
  PhoneNumberConfirmed boolean NOT NULL,
  TwoFactorEnabled boolean NOT NULL,
  LockoutEnd timestamp with time zone,
  LockoutEnabled boolean NOT NULL,
  AccessFailedCount integer NOT NULL,
  CONSTRAINT AspNetUsers_pkey PRIMARY KEY (Id)
);
CREATE TABLE public.Galleries (
  Id uuid NOT NULL,
  EventName character varying NOT NULL,
  CustomerEmail text NOT NULL,
  AccessPin character varying NOT NULL,
  CanDownload boolean NOT NULL,
  IsPublished boolean NOT NULL,
  CreatedAt timestamp with time zone NOT NULL,
  PublishedAt timestamp with time zone,
  PhotographerId text NOT NULL,
  OwnerId text,
  CONSTRAINT Galleries_pkey PRIMARY KEY (Id),
  CONSTRAINT FK_Galleries_AspNetUsers_OwnerId FOREIGN KEY (OwnerId) REFERENCES public.AspNetUsers(Id)
);
CREATE TABLE public.AspNetRoleClaims (
  Id integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  RoleId text NOT NULL,
  ClaimType text,
  ClaimValue text,
  CONSTRAINT AspNetRoleClaims_pkey PRIMARY KEY (Id),
  CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES public.AspNetRoles(Id)
);
CREATE TABLE public.AspNetUserClaims (
  Id integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  UserId text NOT NULL,
  ClaimType text,
  ClaimValue text,
  CONSTRAINT AspNetUserClaims_pkey PRIMARY KEY (Id),
  CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES public.AspNetUsers(Id)
);
CREATE TABLE public.AspNetUserLogins (
  LoginProvider text NOT NULL,
  ProviderKey text NOT NULL,
  ProviderDisplayName text,
  UserId text NOT NULL,
  CONSTRAINT AspNetUserLogins_pkey PRIMARY KEY (LoginProvider, ProviderKey),
  CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES public.AspNetUsers(Id)
);
CREATE TABLE public.AspNetUserRoles (
  UserId text NOT NULL,
  RoleId text NOT NULL,
  CONSTRAINT AspNetUserRoles_pkey PRIMARY KEY (UserId, RoleId),
  CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES public.AspNetRoles(Id),
  CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES public.AspNetUsers(Id)
);
CREATE TABLE public.AspNetUserTokens (
  UserId text NOT NULL,
  LoginProvider text NOT NULL,
  Name text NOT NULL,
  Value text,
  CONSTRAINT AspNetUserTokens_pkey PRIMARY KEY (UserId, LoginProvider, Name),
  CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES public.AspNetUsers(Id)
);
CREATE TABLE public.Photos (
  Id integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  GalleryId uuid NOT NULL,
  BlobUrl text NOT NULL,
  FileName text NOT NULL,
  FileSize bigint NOT NULL,
  UploadedAt timestamp with time zone NOT NULL,
  CONSTRAINT Photos_pkey PRIMARY KEY (Id),
  CONSTRAINT FK_Photos_Galleries_GalleryId FOREIGN KEY (GalleryId) REFERENCES public.Galleries(Id)
);





