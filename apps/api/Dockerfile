# ==============================================================================
# Stage 1: Build & Publish
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# DK-1: Restore dependencies before copying source code to leverage Docker layer caching
COPY global.json Directory.Build.props Directory.Packages.props SupportDesk.sln ./
COPY apps/api/src/SupportDesk.Domain/SupportDesk.Domain.csproj apps/api/src/SupportDesk.Domain/
COPY apps/api/src/SupportDesk.Application/SupportDesk.Application.csproj apps/api/src/SupportDesk.Application/
COPY apps/api/src/SupportDesk.Infrastructure/SupportDesk.Infrastructure.csproj apps/api/src/SupportDesk.Infrastructure/
COPY apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj apps/api/src/SupportDesk.Presentation/
COPY apps/api/tests/SupportDesk.UnitTests/SupportDesk.UnitTests.csproj apps/api/tests/SupportDesk.UnitTests/
COPY apps/api/tests/SupportDesk.ArchitectureTests/SupportDesk.ArchitectureTests.csproj apps/api/tests/SupportDesk.ArchitectureTests/

RUN dotnet restore apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj

# Copy remaining application source code
COPY apps/api/src/ apps/api/src/

# Publish compiled output
RUN dotnet publish apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ==============================================================================
# Stage 2: Runtime Image
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# DK-3: Run as non-root user 'app' on unprivileged HTTP port 8080 without certificates
USER app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SupportDesk.Presentation.dll"]
