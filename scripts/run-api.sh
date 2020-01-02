#!/bin/sh
SCRIPTPATH="$( cd "$(dirname "$0")" ; pwd -P )"
export ASPNETCORE_ENVIRONMENT=Development && dotnet run -p "${SCRIPTPATH}/../src/Api/Api.csproj"
