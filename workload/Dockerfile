FROM mcr.microsoft.com/dotnet/aspnet:8.0-cbl-mariner AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
RUN apt-get update -y && apt-get upgrade -y

COPY . .
RUN dotnet restore ./workload.csproj
RUN dotnet publish ./workload.csproj -c Release -o /build  --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /build .

ENTRYPOINT ["dotnet", "workload.dll"]