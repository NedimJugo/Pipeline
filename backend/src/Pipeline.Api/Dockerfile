FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Pipeline.sln", "./"]
COPY ["src/Pipeline.Domain/Pipeline.Domain.csproj", "src/Pipeline.Domain/"]
COPY ["src/Pipeline.Application/Pipeline.Application.csproj", "src/Pipeline.Application/"]
COPY ["src/Pipeline.Infrastructure/Pipeline.Infrastructure.csproj", "src/Pipeline.Infrastructure/"]
COPY ["src/Pipeline.Api/Pipeline.Api.csproj", "src/Pipeline.Api/"]
COPY ["tests/Pipeline.Tests/Pipeline.Tests.csproj", "tests/Pipeline.Tests/"]

RUN dotnet restore "src/Pipeline.Api/Pipeline.Api.csproj"

COPY . .
WORKDIR "/src/src/Pipeline.Api"
RUN dotnet build "Pipeline.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Pipeline.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pipeline.Api.dll"]
