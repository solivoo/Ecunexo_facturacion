# Facturación electrónica (SRI Ecuador)

Autor: **Sergio Olivo**  
Versión: **1.0.0**

API .NET 10 — capas: `Api` → `Business` → `Core` ← `Infrastructure`.

## Arquitectura (stacks)

| Stack | Base de datos | Consumo |
|---|---|---|
| Licencias | `licensing_ecunexo` | API propia |
| Cliente | `ecunexo` (tenancy, catalog, inventory, …) | SPA → EcuNexo.Api |
| Facturación | `billing` (schema `billing`, este stack) | SPA Cliente → **Billing.Api** (HTTP) |

Las BDs son **independientes**. El admin (`ecunexo_admin`) no lee tablas de Facturación en la Postgres de Cliente: usa `VITE_BILLING_API_BASE_URL` hacia esta API.

Post-autorización SRI, Billing.Api puede llamar a **Cliente API** (`ecunexo_api`) para egreso de inventario — integración HTTP, no BD compartida.

```
ecunexo_admin ──HTTP──► Billing.Api ──► Postgres [billing]
       │
       └──HTTP──► EcuNexo.Api ──► Postgres [ecunexo]
                      ▲
                      └── HTTP billing-egress (post-SRI)
```

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

Connection string local: `Database=billing` (ver `Billing.Api/appsettings.json`).

## Portainer

1. Stack con compose: `docker-compose.yml` (raíz del repo)
2. Env desde `deploy/portainer/.env.example`
3. API: puerto `${BILLING_HTTP_PORT:-8080}` (si 8080 está ocupado, usa otro; el `.env.example` usa `8081`)
4. La imagen `ecunexo/billing-api` se **build**ea del Dockerfile (no está en Docker Hub). En Portainer usa **Update the stack** / redeploy con rebuild; **no** uses “Pull and redeploy” / “Pull images” o fallará con `pull access denied`.

### Base de datos

- Postgres del propio compose: `POSTGRES_DB=billing`, `POSTGRES_HOST=postgres`, password propio.
- No uses `BILLING_CONNECTION_STRING` hacia el Postgres del stack Cliente.
- `BILLING_CONNECTION_STRING` solo como override (p. ej. Postgres gestionado externo). Si la defines vacía en Portainer, Compose rompe el fallback: **no la crees** si no la necesitas.

### Egreso inventario → Cliente API

Variables (mapean a `InventoryEgress__*`):

- `INVENTORY_EGRESS_ENABLED` — `false` mientras Cliente no esté desplegado
- `INVENTORY_EGRESS_BASE_URL` — URL de **ecunexo_api** (ej. `http://api:8080` en red Docker, o `http://host:5088`), **no** la SPA
- `INVENTORY_EGRESS_API_KEY` — misma clave que `InventoryEgress__ApiKey` / `INVENTORY_EGRESS_API_KEY` en el stack Cliente

### SPA Cliente (consumidor)

En el stack Cliente: `VITE_BILLING_API_BASE_URL` = URL pública de esta API (ej. `http://IP:8081` o `https://billing.tudominio.com`). Rebuild de la imagen SPA tras cambiar `VITE_*`.

En este stack: `CORS_ORIGINS` = origen(es) del admin (coma-separados), p.ej. `http://IP:5173` o `https://admin.tudominio.com`.

## Runbook: sacar schema `billing` de la BD Cliente

Si en el pasado Facturación compartió la Postgres `ecunexo` del Cliente y quedó el schema `billing` ahí:

1. **Inventario** (en Postgres Cliente):

```sql
SELECT tablename FROM pg_tables WHERE schemaname = 'billing';
SELECT * FROM billing.__ef_migrations_history;
```

2. **Dump** del schema (desde un host con acceso a la BD Cliente):

```bash
pg_dump -h <host_cliente> -U <user> -d ecunexo -n billing -Fc -f billing_schema.dump
```

3. **Restore** en la Postgres del stack Facturación (`Database=billing`):

```bash
# Asegurar que existe la BD billing (el servicio postgres del compose la crea al iniciar)
pg_restore -h <host_billing> -U postgres -d billing --clean --if-exists billing_schema.dump
```

4. **Portainer Facturación:** quitar cualquier `BILLING_CONNECTION_STRING` hacia Cliente; `POSTGRES_DB=billing` y password del stack; reiniciar `billing-api`.
   - Si restauraste el dump: `BILLING_BOOTSTRAP_MIGRATE=false` (o dejar migrate on si la historia EF ya está en el dump).
   - Si la BD Facturación está vacía y no hay datos que conservar: migrate/seed on y omitir dump.

5. **Smoke:** listar/emitir factura desde el SPA (vía Billing.Api); probar egreso inventario si aplica.

6. **Limpiar BD Cliente** (solo cuando Billing ya corre contra su Postgres):

```sql
DROP SCHEMA billing CASCADE;
```

No hace falta migración EF en el repo Cliente: esas tablas nunca las modeló `EcuNexo.Api`. Conservar `inventory.invoice_stock_egresses` (recibo de egreso; referencia lógica a `billing_invoice_id`).

## Notas

- `xml/` — esquemas XSD SRI (factura / nota de crédito); la API los copia al publicar.
- Historial de versión: [`CHANGELOG.md`](CHANGELOG.md)
