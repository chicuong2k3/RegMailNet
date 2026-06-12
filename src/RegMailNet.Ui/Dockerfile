FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/RegMailNet.Ui/RegMailNet.Ui.csproj", "src/RegMailNet.Ui/"]
COPY ["src/RegMailNet/RegMailNet.csproj", "src/RegMailNet/"]
COPY ["RegMailNet.slnx", "."]
RUN dotnet restore "src/RegMailNet.Ui/RegMailNet.Ui.csproj"

COPY src/RegMailNet.Ui/ src/RegMailNet.Ui/
COPY src/RegMailNet/ src/RegMailNet/
RUN dotnet publish "src/RegMailNet.Ui/RegMailNet.Ui.csproj" -c Release -o /app/publish

FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY src/RegMailNet.Ui/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
