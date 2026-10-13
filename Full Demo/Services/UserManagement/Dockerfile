FROM node:24-alpine AS web
WORKDIR /web
COPY Services/UserManagement/UserManagement.Web/package*.json ./
RUN npm ci
COPY Services/UserManagement/UserManagement.Web/ ./
# Type-check and bundle without requiring development certificates in the build container.
RUN npx tsc --noEmit && npx vite build --config vite.publish.config.ts

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY DartsStats.ServiceDefaults/ DartsStats.ServiceDefaults/
COPY Services/UserManagement/UserManagement.Host/ Services/UserManagement/UserManagement.Host/
RUN dotnet publish Services/UserManagement/UserManagement.Host/UserManagement.Host.csproj -c Release -o /app
COPY --from=web /web/dist /app/wwwroot

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "UserManagement.Host.dll"]
