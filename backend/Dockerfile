FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/TideMonitor.Core/TideMonitor.Core.csproj backend/TideMonitor.Core/
COPY backend/TideMonitor.Infrastructure/TideMonitor.Infrastructure.csproj backend/TideMonitor.Infrastructure/
COPY backend/TideMonitor.Api/TideMonitor.Api.csproj backend/TideMonitor.Api/
RUN dotnet restore backend/TideMonitor.Api/TideMonitor.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/TideMonitor.Api/TideMonitor.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TideMonitor.Api.dll"]
