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
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5002
ENTRYPOINT ["dotnet", "RegMailNet.Api.dll"]
