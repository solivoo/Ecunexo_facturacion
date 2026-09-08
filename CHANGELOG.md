# Changelog

## Unreleased

- BD propia `billing` (independiente de Cliente); consumo HTTP desde SPA (`VITE_BILLING_API_BASE_URL` + `CORS_ORIGINS`)
- Compose/env alineados a Postgres del stack; egreso inventario sigue siendo HTTP a Cliente API

## [1.0.0] — 2026-09-06

Primera versión del sistema de facturación electrónica EcuNexo (SRI Ecuador).

### Incluye

- Arquitectura en capas: Api, Business, Core, Infrastructure
- Emisores, establecimientos, secuenciales y certificados
- Facturas electrónicas (01): borrador → XML → firma → recepción/autorización SRI
- Notas de crédito (04) sobre facturas autorizadas
- Catálogos tributarios (tarifas IVA/ICE/ISD, retenciones, reglas void)
- Outbox SRI + worker; gateway en memoria o SOAP
- Bootstrap BD (migraciones + seed de catálogos)
- Deploy Docker / Portainer

### Docs de API

- OpenAPI + Scalar en Development (`/openapi/v1.json`, `/scalar`)
