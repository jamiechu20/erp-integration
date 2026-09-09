# 多階段 build：sdk 階段還原+publish，執行階段只留 aspnet runtime（spec.md §13.1）
# build context 是 repo 根目錄，因為要一起複製 Domain/Application/Infrastructure 專案
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# 先只複製 csproj 還原套件，讓程式碼變動時能沿用 restore 的 layer cache
COPY src/ErpSync.Domain/ErpSync.Domain.csproj src/ErpSync.Domain/
COPY src/ErpSync.Application/ErpSync.Application.csproj src/ErpSync.Application/
COPY src/ErpSync.Infrastructure/ErpSync.Infrastructure.csproj src/ErpSync.Infrastructure/
COPY src/ErpSync.Api/ErpSync.Api.csproj src/ErpSync.Api/
RUN dotnet restore src/ErpSync.Api/ErpSync.Api.csproj

COPY src/ src/
RUN dotnet publish src/ErpSync.Api/ErpSync.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ErpSync.Api.dll"]
