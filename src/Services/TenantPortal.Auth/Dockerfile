FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Services/TenantPortal.Auth/TenantPortal.Auth.csproj", "src/Services/TenantPortal.Auth/"]
COPY ["src/Shared/TenantPortal.Shared/TenantPortal.Shared.csproj", "src/Shared/TenantPortal.Shared/"]

RUN dotnet restore "src/Services/TenantPortal.Auth/TenantPortal.Auth.csproj"
COPY . .
WORKDIR "/src/src/Services/TenantPortal.Auth"
RUN dotnet build "TenantPortal.Auth.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TenantPortal.Auth.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TenantPortal.Auth.dll"]