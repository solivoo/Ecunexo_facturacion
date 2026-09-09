# Billing.Api — multi-stage (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Billing.slnx ./
COPY Billing.Api/Billing.Api.csproj Billing.Api/
COPY Billing.Business/Billing.Business.csproj Billing.Business/
COPY Billing.Core/Billing.Core.csproj Billing.Core/
COPY Billing.Infrastructure/Billing.Infrastructure.csproj Billing.Infrastructure/

RUN dotnet restore Billing.Api/Billing.Api.csproj

COPY Billing.Api/ Billing.Api/
COPY Billing.Business/ Billing.Business/
COPY Billing.Core/ Billing.Core/
COPY Billing.Infrastructure/ Billing.Infrastructure/
COPY xml/ xml/

RUN dotnet publish Billing.Api/Billing.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

RUN mkdir -p /data && chown $APP_UID:$APP_UID /data

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
VOLUME ["/data"]
ENTRYPOINT ["dotnet", "Billing.Api.dll"]
