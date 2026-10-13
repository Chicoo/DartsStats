# syntax=docker/dockerfile:1

ARG PMTILES_VERSION=1.30.3
ARG BASEMAP_ASSETS_COMMIT=028c18f713baecad011301ff7a69acc39bcc2ae7
ARG SDK_IMAGE=acrtopfas.azurecr.io/baseimages/dotnet/sdk@sha256:0c4a82ed5a6e937b9db7c841df6c3c8d604cf82d2e9f68b9be229d3fb9f96d78
ARG BASE_IMAGE=acrtopfas.azurecr.io/baseimages/dotnet/aspnet@sha256:aa518a32a793b9026b904b5643fc094d63380e3d66bec15d64311bad5de1b502

FROM ${SDK_IMAGE} AS build
USER 0
WORKDIR /src

COPY . .

RUN --mount=type=secret,id=NEXUS_USERNAME,env=NEXUS_USERNAME \
    --mount=type=secret,id=NEXUS_PASSWORD,env=NEXUS_PASSWORD \
    dotnet restore webservices/Protomaps/Protomaps.Host/Protomaps.Host.csproj \
      --configfile webservices/Protomaps/nuget.config

RUN dotnet publish webservices/Protomaps/Protomaps.Host/Protomaps.Host.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    && mkdir -p /runtime-root/app/logs /runtime-root/home/app/.config

FROM ${SDK_IMAGE} AS pmtiles
USER 0
ARG PMTILES_VERSION
ARG ASSET_CACHE_BUST

RUN apk add --no-cache ca-certificates curl python3

RUN curl -fsSL "https://github.com/protomaps/go-pmtiles/releases/download/v${PMTILES_VERSION}/go-pmtiles_${PMTILES_VERSION}_Linux_x86_64.tar.gz" \
        -o /tmp/go-pmtiles.tar.gz \
    && mkdir -p /usr/local/bin \
    && tar -xzf /tmp/go-pmtiles.tar.gz -C /usr/local/bin pmtiles \
    && chmod +x /usr/local/bin/pmtiles \
    && rm /tmp/go-pmtiles.tar.gz

RUN --mount=type=secret,id=SOURCE_PMTILES_URL,required=true \
    --mount=type=secret,id=SOURCE_PMTILES_CA,required=false \
    if [ -s /run/secrets/SOURCE_PMTILES_CA ]; then \
      mkdir -p /usr/local/share/ca-certificates; \
      cp /run/secrets/SOURCE_PMTILES_CA /usr/local/share/ca-certificates/topfas-source.crt; \
      update-ca-certificates; \
    fi \
    && test -n "$ASSET_CACHE_BUST" \
    && source_pmtiles_url="$(cat /run/secrets/SOURCE_PMTILES_URL)" \
    && test -n "$source_pmtiles_url" \
    && mkdir -p /data \
    && (pmtiles extract "$source_pmtiles_url" /data/main.pmtiles --maxzoom=7 >/tmp/pmtiles-extract.log 2>&1 \
        || (echo "PMTiles extraction failed." >&2; exit 1)) \
    && pmtiles verify /data/main.pmtiles \
    && pmtiles show /data/main.pmtiles --header-json > /tmp/header.json \
    && python3 -c "import json,sys; header=json.load(open('/tmp/header.json')); maxzoom=header.get('maxzoom', header.get('max_zoom')); sys.exit('PMTiles header did not include maxzoom.' if maxzoom is None else (0 if int(maxzoom) <= 7 else f'Invalid PMTiles maxzoom {maxzoom}; expected <= 7.'))"

FROM ${SDK_IMAGE} AS basemap-assets
USER 0
ARG BASEMAP_ASSETS_COMMIT

RUN apk add --no-cache ca-certificates curl \
    && test -n "$BASEMAP_ASSETS_COMMIT" \
    && mkdir -p /assets \
    && curl -fsSL "https://github.com/protomaps/basemaps-assets/archive/${BASEMAP_ASSETS_COMMIT}.tar.gz" \
      | tar -xz --strip-components=1 -C /assets \
    && cp -R "/assets/fonts/Noto Sans Medium" "/assets/fonts/Noto Sans Bold" \
    && test -f "/assets/fonts/Noto Sans Regular/0-255.pbf" \
    && test -f "/assets/fonts/Noto Sans Medium/0-255.pbf" \
    && test -f "/assets/fonts/Noto Sans Bold/0-255.pbf" \
    && test -f /assets/sprites/v4/light.json \
    && test -f /assets/sprites/v4/light.png

FROM ${BASE_IMAGE} AS final
ARG APP_UID=65532
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    HOME=/home/app \
    XDG_CONFIG_HOME=/home/app/.config \
    Protomaps__FilePath=/data \
    Protomaps__PublicUrlPath=/protomaps \
    Protomaps__ContentType=application/vnd.pmtiles

EXPOSE 8080

COPY --chown=${APP_UID}:${APP_UID} --from=build /app/publish .
COPY --chown=${APP_UID}:${APP_UID} --from=build /runtime-root/ /
COPY --chown=${APP_UID}:${APP_UID} --from=pmtiles /data/main.pmtiles /data/main.pmtiles
COPY --chown=${APP_UID}:${APP_UID} --from=basemap-assets /assets/fonts /data/fonts
COPY --chown=${APP_UID}:${APP_UID} --from=basemap-assets /assets/sprites /data/sprites
COPY --chown=${APP_UID}:${APP_UID} --from=basemap-assets /assets/fonts.json /data/fonts.json

USER ${APP_UID}:${APP_UID}

ENTRYPOINT ["dotnet", "Protomaps.Host.dll"]
