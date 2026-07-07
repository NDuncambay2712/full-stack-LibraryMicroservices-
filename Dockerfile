# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY CirculationService/CirculationService.csproj CirculationService/
RUN dotnet restore CirculationService/CirculationService.csproj

# Copy all source code
COPY CirculationService/ CirculationService/

# Build and publish
WORKDIR /src/CirculationService
RUN dotnet publish CirculationService.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Railway injects PORT at runtime; use shell form so $PORT expands correctly
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "CirculationService.dll"]
