FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

COPY Common/*.csproj ./Common/
COPY Protocol/*.csproj ./Protocol/
COPY Client/*.csproj ./Client/
RUN dotnet restore ./Client/Client.csproj

COPY Common/. ./Common/
COPY Protocol/. ./Protocol/
COPY Client/. ./Client/
RUN dotnet publish ./Client/Client.csproj -c Release -o /app/publish

ENTRYPOINT ["dotnet", "/app/publish/Client.dll"]
