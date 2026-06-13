FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/RegMailNet.Api/RegMailNet.Api.csproj", "src/RegMailNet.Api/"]
COPY ["src/RegMailNet/RegMailNet.csproj", "src/RegMailNet/"]
COPY ["RegMailNet.slnx", "."]
RUN dotnet restore "src/RegMailNet.Api/RegMailNet.Api.csproj"

COPY src/RegMailNet.Api/ src/RegMailNet.Api/
COPY src/RegMailNet/ src/RegMailNet/
COPY src/RegMailNet/captcha_solvers/ src/RegMailNet/captcha_solvers/
RUN dotnet publish "src/RegMailNet.Api/RegMailNet.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Install Python, pip, and GTK/X11 libs for Camoufox (Firefox-based)
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        python3 python3-pip python3-venv \
        libgtk-3-0t64 libdbus-glib-1-2 libx11-xcb1 libxcb-shape0 \
        libxt6t64 libasound2t64 libxcomposite1 libxdamage1 libxrandr2 \
        libatk1.0-0t64 libatk-bridge2.0-0t64 libatspi2.0-0t64 libcups2t64 \
        libdrm2 libgbm1 libpango-1.0-0 libcairo2 libxfixes3 \
        libxext6 libx11-6 fonts-liberation || \
    apt-get install -y --no-install-recommends \
        python3 python3-pip python3-venv \
        libgtk-3-0 libdbus-glib-1-2 libx11-xcb1 libxcb-shape0 \
        libxt6 libasound2 libxcomposite1 libxdamage1 libxrandr2 \
        libatk1.0-0 libatk-bridge2.0-0 libatspi2.0-0 libcups2 \
        libdrm2 libgbm1 libpango-1.0-0 libcairo2 libxfixes3 \
        libxext6 libx11-6 fonts-liberation && \
    ln -sf /usr/bin/python3 /usr/bin/python && \
    ln -sf /usr/bin/pip3 /usr/bin/pip && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Pre-install camoufox Python package and fetch browser binary
RUN pip install --break-system-packages camoufox && \
    python -m camoufox fetch

WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5002
ENTRYPOINT ["dotnet", "RegMailNet.Api.dll"]
