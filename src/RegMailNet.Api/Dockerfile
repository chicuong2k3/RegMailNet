FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/RegMailNet.Api/RegMailNet.Api.csproj", "src/RegMailNet.Api/"]
COPY ["src/RegMailNet/RegMailNet.csproj", "src/RegMailNet/"]
COPY ["RegMailNet.slnx", "."]
RUN dotnet restore "src/RegMailNet.Api/RegMailNet.Api.csproj"

COPY src/RegMailNet.Api/ src/RegMailNet.Api/
COPY src/RegMailNet/ src/RegMailNet/
RUN dotnet publish "src/RegMailNet.Api/RegMailNet.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Install Python and pip for Camoufox anti-detect browser
RUN apt-get update && \
    apt-get install -y --no-install-recommends python3 python3-pip python3-venv && \
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
