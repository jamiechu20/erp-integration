# Mock SAP OData API：沒有專案相依，restore 只需要自己的 csproj（spec.md §13.1）
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY src/MockSap.Api/MockSap.Api.csproj src/MockSap.Api/
RUN dotnet restore src/MockSap.Api/MockSap.Api.csproj

COPY src/MockSap.Api/ src/MockSap.Api/
RUN dotnet publish src/MockSap.Api/MockSap.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MockSap.Api.dll"]
