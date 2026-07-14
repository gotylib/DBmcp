FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY DBmcp.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish DBmcp.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DBmcp.dll"]
