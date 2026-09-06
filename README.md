# Facturación electrónica (SRI Ecuador)

Autor: **Sergio Olivo**  
Versión: **1.0.0**

API .NET 10 — capas: `Api` → `Business` → `Core` ← `Infrastructure`.

## Local

Requisitos: .NET 10 SDK, PostgreSQL 16+.

```bash
cp Billing.Api/appsettings.Development.local.json.example \
   Billing.Api/appsettings.Development.local.json

dotnet restore Billing.slnx
dotnet build Billing.slnx
dotnet test Billing.slnx
dotnet run --project Billing.Api
```

API: `http://localhost:5203`

En Development: OpenAPI en `/openapi/v1.json` y UI Scalar en `/scalar`.

Secretos solo en `appsettings*.local.json` o variables de entorno (no se suben a git). Plantilla de env Docker: `deploy/portainer/.env.example`.

## Portainer

1. Stack con compose: `docker-compose.yml` (raíz del repo)
2. Env desde `deploy/portainer/.env.example` (definir `POSTGRES_PASSWORD`)
3. API: puerto `${BILLING_HTTP_PORT:-8080}`

Servicios: `postgres` + `billing-api`.

## Notas

- `xml/` — esquemas XSD SRI (factura / nota de crédito); la API los copia al publicar.
- Historial de versión: [`CHANGELOG.md`](CHANGELOG.md)
