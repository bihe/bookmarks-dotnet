## fronted build-phase
FROM node:lts-alpine AS FRONTEND-BUILD
WORKDIR /fronted-build
COPY ./src/UI .
RUN npm install -g @angular/cli@latest && npm install && npm run build -- --prod --base-href /ui/

## backend build-phase
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS BACKEND-BUILD
ARG buildtime_variable_version=1.0.0-local
ENV AppVersion=${buildtime_variable_version}
WORKDIR /backend-build
COPY ./src/Api ./src/Api
COPY ./src/Store ./src/Store
COPY ./Directory.Build.props .
COPY ./Directory.Build.targets .
COPY ./global.json .
RUN dotnet publish --self-contained true -r alpine-x64 -c Release /p:PublishTrimmed=true -v m -o output ./src/Api/Api.csproj

## runtime build
FROM alpine:3

LABEL author="henrik@binggl.net"
LABEL description="Manage bookmarks independent of browsers."
LABEL version=1

# Add some libs required by .NET runtime
RUN apk add --no-cache libstdc++ libintl

WORKDIR /opt/bookmarks
RUN mkdir -p /opt/bookmarks/_logs && mkdir -p /opt/bookmarks/_icons

## copy assets and build results from prior steps
COPY --from=BACKEND-BUILD /backend-build/output /opt/bookmarks/
COPY --from=FRONTEND-BUILD /fronted-build/dist/bookmarks-ui /opt/bookmarks/wwwroot/ui

EXPOSE 3000
ENV ASPNETCORE_ENVIRONMENT Production
ENV ASPNETCORE_URLS http://*:3000
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT 1

# Do not run as root user
## alpine specific user/group creation
RUN addgroup -g 1000 -S bookmarks && \
    adduser -u 1000 -S bookmarks -G bookmarks

RUN chown -R bookmarks:bookmarks /opt/bookmarks
USER bookmarks

CMD ["/opt/bookmarks/Api"]
