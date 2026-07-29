FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY DBmcp.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish DBmcp.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine
WORKDIR /app

# SqlClient needs ICU; Alpine defaults to invariant globalization.
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DBmcp.dll"]
