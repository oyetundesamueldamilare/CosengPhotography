# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore
COPY *.sln .
COPY CosengPhotography/CosengPhotography.csproj CosengPhotography/
COPY CosengPhotography.Frontend/CosengPhotography.Frontend.csproj CosengPhotography.Frontend/
RUN dotnet restore

# Build backend and frontend
COPY . .
RUN dotnet publish CosengPhotography/CosengPhotography.csproj -c Release -o /app/backend
RUN dotnet publish CosengPhotography.Frontend/CosengPhotography.Frontend.csproj -c Release -o /app/frontend

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published outputs
COPY --from=build /app/backend ./backend
COPY --from=build /app/frontend ./frontend

# Install supervisor to run multiple processes
RUN apt-get update && apt-get install -y supervisor

# Copy supervisor config
COPY supervisord.conf /etc/supervisor/conf.d/supervisord.conf

# Expose ports (adjust as needed)
EXPOSE 5192 5142

# Start both apps
CMD ["supervisord", "-n"]
