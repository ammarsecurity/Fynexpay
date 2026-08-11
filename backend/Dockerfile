# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    NUGET_XMLDOC_MODE=skip

COPY src/Fynexpay.Domain/Fynexpay.Domain.csproj Fynexpay.Domain/
COPY src/Fynexpay.Application/Fynexpay.Application.csproj Fynexpay.Application/
COPY src/Fynexpay.Infrastructure/Fynexpay.Infrastructure.csproj Fynexpay.Infrastructure/
COPY src/Fynexpay.Api/Fynexpay.Api.csproj Fynexpay.Api/
RUN dotnet restore Fynexpay.Api/Fynexpay.Api.csproj --verbosity minimal

COPY src/Fynexpay.Domain/ Fynexpay.Domain/
COPY src/Fynexpay.Application/ Fynexpay.Application/
COPY src/Fynexpay.Infrastructure/ Fynexpay.Infrastructure/
COPY src/Fynexpay.Api/ Fynexpay.Api/

RUN dotnet publish Fynexpay.Api/Fynexpay.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:TreatWarningsAsErrors=false \
    -maxcpucount:1 \
    --verbosity minimal

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://127.0.0.1:5088
EXPOSE 5088
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=5 \
  CMD curl -fsS http://127.0.0.1:5088/ || exit 1
ENTRYPOINT ["dotnet", "Fynexpay.Api.dll"]
