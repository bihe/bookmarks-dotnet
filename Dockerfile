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
RUN dotnet publish --self-contained -r linux-musl-x64 -c Release -v m -o output ./src/Api/Api.csproj

## runtime build
FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine
LABEL author="henrik@binggl.net"
LABEL description="Manage bookmarks independent of browsers."
LABEL version=1

WORKDIR /opt/bookmarks.binggl.net
RUN mkdir -p /opt/bookmarks.binggl.net/_logs

## copy assets and build results from prior steps
COPY --from=BACKEND-BUILD /backend-build/output /opt/bookmarks.binggl.net/
COPY --from=FRONTEND-BUILD /fronted-build/dist/bookmarks-ui /opt/bookmarks.binggl.net/wwwroot/ui

EXPOSE 3000
ENV ASPNETCORE_ENVIRONMENT Production
ENV ASPNETCORE_URLS http://*:3000

# Do not run as root user
## alpine specific user/group creation
RUN addgroup -g 1000 -S bookmarks && \
    adduser -u 1000 -S bookmarks -G bookmarks

RUN chown -R bookmarks:bookmarks /opt/bookmarks.binggl.net
USER bookmarks

CMD ["/opt/bookmarks.binggl.net/Api"]
