# Facturación electrónica (SRI Ecuador)

Autor: **Sergio Olivo**  
Versión: **1.0.0**

API .NET 10 — capas: `Api` → `Business` → `Core` ← `Infrastructure`.

## Arquitectura (stacks)

| Stack | Base de datos |
|---|---|
| Licencias | `licensing_ecunexo` (aparte) |
| Cliente API + Facturación | misma BD `ecunexo` (schemas distintos) |

Post-autorización SRI, el contenedor de Facturación puede llamar a **Cliente API** (`ecunexo_api`) para egreso de inventario — no a la SPA admin.

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
2. Env desde `deploy/portainer/.env.example`
3. API: puerto `${BILLING_HTTP_PORT:-8080}` (si 8080 está ocupado, usa otro)

### Base de datos

- **Standalone:** Postgres del compose (`POSTGRES_*`, `POSTGRES_HOST=postgres`).
- **Compartida con Cliente:** setear `BILLING_CONNECTION_STRING` al mismo Postgres que `ecunexo_api` (`Database=ecunexo`). El servicio `postgres` local es opcional.

### Egreso inventario → Cliente API

Variables (mapean a `InventoryEgress__*`):

- `INVENTORY_EGRESS_ENABLED` — `false` mientras Cliente no esté desplegado
- `INVENTORY_EGRESS_BASE_URL` — URL de **ecunexo_api** (ej. `http://api:8080` en red Docker, o `http://host:5088`), **no** la SPA
- `INVENTORY_EGRESS_API_KEY` — misma clave que `InventoryEgress__ApiKey` / `INVENTORY_EGRESS_API_KEY` en el stack Cliente

## Notas

- `xml/` — esquemas XSD SRI (factura / nota de crédito); la API los copia al publicar.
- Historial de versión: [`CHANGELOG.md`](CHANGELOG.md)
