FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/GLOWAPI.Domain/GLOWAPI.Domain.csproj src/GLOWAPI.Domain/
COPY src/GLOWAPI.Application/GLOWAPI.Application.csproj src/GLOWAPI.Application/
COPY src/GLOWAPI.Infrastructure/GLOWAPI.Infrastructure.csproj src/GLOWAPI.Infrastructure/
COPY src/GLOWAPI.API/GLOWAPI.API.csproj src/GLOWAPI.API/

RUN dotnet restore src/GLOWAPI.API/GLOWAPI.API.csproj

COPY src/ src/
RUN dotnet publish src/GLOWAPI.API/GLOWAPI.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish .
COPY scripts/railway-entrypoint.sh ./railway-entrypoint.sh
RUN chmod +x ./railway-entrypoint.sh

ENTRYPOINT ["./railway-entrypoint.sh"]
