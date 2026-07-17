FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

COPY Common/*.csproj ./Common/
COPY Protocol/*.csproj ./Protocol/
COPY ClientAdmin/*.csproj ./ClientAdmin/
RUN dotnet restore ./ClientAdmin/ClientAdmin.csproj

COPY Common/. ./Common/
COPY Protocol/. ./Protocol/
COPY ClientAdmin/. ./ClientAdmin/
RUN dotnet publish ./ClientAdmin/ClientAdmin.csproj -c Release -o /app/publish

ENTRYPOINT ["dotnet", "/app/publish/ClientAdmin.dll"]
