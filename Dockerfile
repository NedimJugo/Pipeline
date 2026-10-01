# Stage 1: Build Frontend (Vite + React)
FROM node:20-alpine AS frontend-build
WORKDIR /app/frontend

COPY frontend/package*.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

# Stage 2: Build & Publish Backend (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src

COPY ["backend/Pipeline.sln", "./"]
COPY ["backend/src/Pipeline.Domain/Pipeline.Domain.csproj", "src/Pipeline.Domain/"]
COPY ["backend/src/Pipeline.Application/Pipeline.Application.csproj", "src/Pipeline.Application/"]
COPY ["backend/src/Pipeline.Infrastructure/Pipeline.Infrastructure.csproj", "src/Pipeline.Infrastructure/"]
COPY ["backend/src/Pipeline.Api/Pipeline.Api.csproj", "src/Pipeline.Api/"]
COPY ["backend/tests/Pipeline.Tests/Pipeline.Tests.csproj", "tests/Pipeline.Tests/"]

RUN dotnet restore "src/Pipeline.Api/Pipeline.Api.csproj"

COPY backend/ .
WORKDIR "/src/src/Pipeline.Api"
RUN dotnet publish "Pipeline.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Copy frontend distribution to backend wwwroot for SPA hosting
COPY --from=frontend-build /app/frontend/dist /app/publish/wwwroot

# Stage 3: Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
RUN mkdir -p /app/data && chown -R appuser:appgroup /app/data
USER appuser

COPY --from=backend-build /app/publish .
ENTRYPOINT ["dotnet", "Pipeline.Api.dll"]
