FROM mcr.microsoft.com/dotnet/sdk:10.0

# Needed dependencies for chromium
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

COPY FlightReportQueue/*.csproj ./FlightReportQueue/
COPY PdfOrtReportService/*.csproj ./PdfOrtReportService/
COPY FlightReportServer/*.csproj ./FlightReportServer/
RUN dotnet restore ./FlightReportServer/FlightReportServer.csproj

COPY FlightReportQueue/. ./FlightReportQueue/
COPY PdfOrtReportService/. ./PdfOrtReportService/
COPY FlightReportServer/. ./FlightReportServer/
RUN dotnet publish ./FlightReportServer/FlightReportServer.csproj -c Release -o /app/publish

ENTRYPOINT ["dotnet", "/app/publish/FlightReportServer.dll"]
