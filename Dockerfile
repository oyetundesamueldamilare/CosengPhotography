# ==========================================
# STAGE 1: Compile the Blazor Frontend (WASM)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-frontend
WORKDIR /src

# Copy solution and project file templates
COPY *.sln .
COPY CosengPhotography.Frontend/CosengPhotography.Frontend.csproj CosengPhotography.Frontend/

# Restore dependencies for frontend
RUN dotnet restore "CosengPhotography.Frontend/CosengPhotography.Frontend.csproj"

# Copy full source and publish frontend static binaries
COPY . .
RUN dotnet publish "CosengPhotography.Frontend/CosengPhotography.Frontend.csproj" -c Release -o /app/frontend-publish

# ==========================================
# STAGE 2: Compile the Backend & Merge Layers
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-backend
WORKDIR /src

COPY *.sln .
COPY CosengPhotography/CosengPhotography.csproj CosengPhotography/

# Restore dependencies for backend
RUN dotnet restore "CosengPhotography/CosengPhotography.csproj"

# Copy full source and publish backend API executable
COPY . .
RUN dotnet publish "CosengPhotography/CosengPhotography.csproj" -c Release -o /app/backend-publish

# CRITICAL STEP: Copy the static Blazor frontend compilation files directly into the Backend's wwwroot folder
COPY --from=build-frontend /app/frontend-publish/wwwroot /app/backend-publish/wwwroot

# ==========================================
# STAGE 3: Final Lightweight Runtime Image
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy the unified backend application bundle
COPY --from=build-backend /app/backend-publish .

# Render dynamically passes an injection port using the PORT environment variable.
# We expose port 10000 instead of juggling separate local debug ports.
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# Start the single unified backend web server engine
ENTRYPOINT ["dotnet", "CosengPhotography.dll"]