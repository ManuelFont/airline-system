FROM mcr.microsoft.com/dotnet/sdk:10.0

#Chromium dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libasound2t64 \
        libatk-bridge2.0-0t64 \
        libatk1.0-0t64 \
        libatspi2.0-0t64 \
        libcairo2 \
        libcups2t64 \
        libdbus-1-3 \
        libgbm1 \
        libglib2.0-0t64 \
        libnspr4 \
        libnss3 \
        libpango-1.0-0 \
        libx11-6 \
        libxcomposite1 \
        libxdamage1 \
        libxext6 \
        libxfixes3 \
        libxkbcommon0 \
        libxrandr2 \
        libxcb1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY Domain/*.csproj ./Domain/
COPY PdfOrtReportService/*.csproj ./PdfOrtReportService/
COPY UserReportContracts/*.csproj ./UserReportContracts/
COPY UserReportServer/*.csproj ./UserReportServer/
RUN dotnet restore ./UserReportServer/UserReportServer.csproj

COPY Domain/. ./Domain/
COPY PdfOrtReportService/. ./PdfOrtReportService/
COPY UserReportContracts/. ./UserReportContracts/
COPY UserReportServer/. ./UserReportServer/
RUN dotnet publish ./UserReportServer/UserReportServer.csproj -c Release -o /app/publish

WORKDIR /app/publish

EXPOSE 5200

ENTRYPOINT ["dotnet", "UserReportServer.dll"]
