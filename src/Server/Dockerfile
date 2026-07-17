FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY Common/*.csproj ./Common/
COPY Domain/*.csproj ./Domain/
COPY FlightReportQueue/*.csproj ./FlightReportQueue/
COPY Protocol/*.csproj ./Protocol/
COPY UserReportContracts/*.csproj ./UserReportContracts/
COPY Server/*.csproj ./Server/
RUN dotnet restore ./Server/Server.csproj

COPY Common/. ./Common/
COPY Domain/. ./Domain/
COPY FlightReportQueue/. ./FlightReportQueue/
COPY Protocol/. ./Protocol/
COPY UserReportContracts/. ./UserReportContracts/
COPY Server/. ./Server/
RUN dotnet publish ./Server/Server.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5100
EXPOSE 5101

ENTRYPOINT ["dotnet", "Server.dll"]
