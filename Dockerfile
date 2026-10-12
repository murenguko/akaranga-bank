FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AkarangaBank.Web.csproj", "./"]
RUN dotnet restore "AkarangaBank.Web.csproj"

COPY . .
RUN dotnet publish "AkarangaBank.Web.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

RUN mkdir -p /app/Storage/Documents \
    && chown -R $APP_UID:$APP_UID /app/Storage
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "AkarangaBank.Web.dll"]
