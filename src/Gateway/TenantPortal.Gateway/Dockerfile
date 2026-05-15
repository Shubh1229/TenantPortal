FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Gateway/TenantPortal.Gateway/TenantPortal.Gateway.csproj", "src/Gateway/TenantPortal.Gateway/"]
COPY ["src/Shared/TenantPortal.Shared/TenantPortal.Shared.csproj", "src/Shared/TenantPortal.Shared/"]

RUN dotnet restore "src/Gateway/TenantPortal.Gateway/TenantPortal.Gateway.csproj"
COPY . .
WORKDIR "/src/src/Gateway/TenantPortal.Gateway"
RUN dotnet build "TenantPortal.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TenantPortal.Gateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TenantPortal.Gateway.dll"]