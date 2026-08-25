# Factura electrónica — pipeline de desarrollo

Documento de referencia para el equipo. Describe el flujo implementado en `ecunexo-facturacion-api` y el contrato con `ecunexo_admin`.

## Alcance

| Capa | Proyecto | Responsabilidad |
|------|----------|-----------------|
| UI emisión | `ecunexo_admin` | Formulario Emitir factura; cliente HTTP Billing |
| API facturación | `ecunexo-facturacion-api` | Dominio, XML, XSD, firma XAdES, stubs SRI |
| Secretos | Infisical (`keysecret…`) | PKCS#12 (Base64) + password del certificado |

La API tenant (`ecunexo_api`, puerto 5088) no genera ni firma el XML SRI. Billing corre aparte (perfil `http` → puerto **5203**).

## Pipeline SRI (orden)

```
1. Crear emisor          POST /api/v1/emitters
2. Crear factura Draft   POST /api/v1/emitters/{id}/invoices
3. Generar XML v1.1.0    (interno / preview-xml)
4. Validar XSD           Factura_V1.1.0.xsd
5. Firmar XAdES-BES      POST .../invoices/{id}/sign
6. Recepción SRI         POST .../submit-reception   (adaptador en memoria)
7. Autorización          POST .../authorize-poll     (adaptador en memoria)
```

Pasos 6–7 aún no invocan SOAP real del SRI; el adaptador `InMemorySriGateway` simula estados de transmisión.

## Endpoints relevantes

| Método | Ruta | Efecto |
|--------|------|--------|
| POST | `/api/v1/emitters` | Alta emisor + seed estab `001` / pto `001` |
| GET | `/api/v1/emitters/{emitterId}/invoices` | Listado paginado (grid) |
| POST | `/api/v1/emitters/{emitterId}/invoices` | Borrador; **secuencial automático** (candado) |
| POST | `.../invoices/{invoiceId}/preview-xml` | XML + XSD |
| POST | `.../invoices/{invoiceId}/sign` | Firma XAdES + **encola** recepción outbox |
| POST | `.../submit-reception` | Recepción sync (soporte) |
| POST | `.../authorize-poll` | Autorización sync (soporte) |
| POST | `.../sri/retry` | Reencola outbox |
| GET | `.../sri-status` | Estado documento + mensajes |

## Componentes clave

| Pieza | Ubicación |
|-------|-----------|
| Generador XML | `Billing.Infrastructure/Xml/SriFacturaXmlGenerator.cs` |
| Validador XSD | `Billing.Infrastructure/Xml/XsdElectronicDocumentXmlValidator.cs` |
| Esquema | `xml/Factura/Factura_V1.1.0.xsd` (copiado al output de la API) |
| Firma XAdES | `Billing.Infrastructure/Signing/XadesElectronicSignatureService.cs` (Yamgooo.SRI.Sign) |
| Material PKCS#12 | `InfisicalSigningCertificateProvider` |
| Cliente Infisical | `InfisicalClient` — Universal Auth + GET secret raw |

## Configuración

### Billing.Api (`appsettings` + entorno)

Sección `Infisical` (metadatos públicos en JSON; secretos solo por entorno):

| Clave | Descripción |
|-------|-------------|
| `Infisical:BaseUrl` | URL base del vault (sin `/` final) |
| `Infisical:WorkspaceId` | Project / workspace Id |
| `Infisical:Environment` | p. ej. `dev` |
| `Infisical:CertificateSecretName` | `keyfacturacion` |
| `Infisical:PasswordSecretName` | `passfacturacion` |
| `Infisical__ClientId` / `Infisical:ClientId` | Machine Identity (Universal Auth) |
| `Infisical__ClientSecret` / `Infisical:ClientSecret` | Machine Identity secret |

**Desarrollo local (recomendado):** crear  
`Billing.Api/appsettings.Development.local.json`  
(plantilla: `appsettings.Development.local.json.example`). Ese archivo está en `.gitignore` (`appsettings.*.local.json`) y se carga en `Program.cs`.

No colocar `ClientId` / `ClientSecret` en `appsettings.json` ni en `appsettings.Development.json` (están versionados).

### Admin (`ecunexo_admin`)

| Variable | Default | Uso |
|----------|---------|-----|
| `VITE_BILLING_API_BASE_URL` | `http://localhost:5203` | Base del cliente `billingApi` |

CORS en Billing permite orígenes Vite `5173`–`5175` (y `127.0.0.1` equivalentes) en desarrollo.

## Prerrequisitos de dominio

- **Catálogo IVA**: `CreateInvoice` valida tarifas (`taxCode` / `rateCode`) contra `ITaxRateRepository`. Sembrar tarifas (Postman `postman/catalogos/tarifas-iva/`) antes de emitir.
- **RUC emisor**: debe coincidir entre body de factura y emisor registrado.
- **Firma**: requiere Infisical reachable y secretos presentes; el PKCS#12 se carga en memoria (`EphemeralKeySet`), sin escribir a disco.

## Mapeo UI → `CreateInvoiceRequest`

- Al abrir Emitir factura se carga el tenant (`GET /api/v1/tenants/{id}`) y se precargan RUC (`taxId`), establecimiento (`establishmentCode`), razón social y dirección matriz.
- El formulario es una sola tarjeta: emisión + cliente + detalle.
- Si el RUC tiene 13 dígitos, el campo queda bloqueado (origen: Contabilidad → Datos SRI).
- Acciones UI:
  - **Solo borrador** → create + `preview-xml` (XSD).
  - **Firmar** → create + `preview-xml` + `POST …/sign` (XAdES + Infisical).
  - **Emitir al SRI** → firmar + `POST …/submit-reception` + `POST …/authorize-poll`.
- IVA único del comprobante → cada línea lleva un `LineTax` con `taxCode = "2"` y `rateCode` SRI (`0` / `5` / `4` para 0% / 5% / 15%).
- Helper: `ecunexo_admin/src/pages/facturacion/toCreateInvoiceBody.ts`.
- El emisor Billing se crea una vez por sesión de navegador (`sessionStorage` clave `ecunexo.billing.emitterId`). Antes de reutilizarlo se valida con `GET /api/v1/emitters/{id}`.

## Firma (paso 5)

Requisitos en el proceso Billing:

| Variable | Uso |
|----------|-----|
| `Infisical__ClientId` / `Infisical:ClientId` | Universal Auth |
| `Infisical__ClientSecret` / `Infisical:ClientSecret` | Universal Auth |

**Desarrollo local:** `Billing.Api/appsettings.Development.local.json` (gitignore + carga en `Program.cs`).

Tras firmar: estado `Signed`, `SignedXml` en memoria, `accessKey` en la respuesta.

## Recepción y autorización (pasos 6–7)

| Paso | Endpoint | Comportamiento |
|------|----------|----------------|
| 6 | `POST …/submit-reception` | `ISriGateway.SendReceptionAsync` — SOAP `validarComprobante` o stub |
| 7 | `POST …/authorize-poll` | Espera `Sri:AuthorizationPollDelaySeconds` y llama `autorizacionComprobante` |

### Configuración `Sri`

| Clave | Default | Notas |
|-------|---------|-------|
| `UseInMemoryGateway` | `true` (base) / `false` (Development) | `false` → `SoapSriGateway` contra celcer/cel |
| `DefaultEnvironment` | `Test` | `Test` = celcer; `Production` = cel |
| `AuthorizationPollDelaySeconds` | `3` | Espera SRI entre recepción y autorización |
| `Test:ReceptionUrl` / `AuthorizationUrl` | URLs oficiales celcer | Offline WS |
| `Production:…` | URLs oficiales cel | Offline WS |

En Development, `appsettings.Development.json` activa SOAP real de **pruebas**. Para volver al stub: `"UseInMemoryGateway": true` en `.local.json`.

Mapeo de estados SOAP → dominio: `RECIBIDA`→Received, `DEVUELTA`→Returned, `AUTORIZADO`→Authorized, `NO AUTORIZADO`→NotAuthorized, `EN PROCESAMIENTO`→Processing.

## Arranque local sugerido

1. PostgreSQL con cadena `ConnectionStrings:Default` (catálogo de impuestos).
2. Credenciales Infisical en `appsettings.Development.local.json`.
3. `dotnet run --project Billing.Api --launch-profile http` (puerto 5203).
4. Sembrar tarifas IVA si la BD está vacía.
5. Admin con `VITE_BILLING_API_BASE_URL` → Billing.
6. UI: **Emitir al SRI** (firma + recepción celcer + autorización).

## Persistencia y outbox SRI

- Emisores, establecimientos, configs de secuencial, facturas, XML y `sri_outbox` viven en PostgreSQL schema `billing`.
- El secuencial se asigna con **candado** (`SELECT … FOR UPDATE`) por `(emitter, estab, pto, tipoDoc)`.
- Tras firmar, la recepción/autorización se **encola** en `sri_outbox`; `SriOutboxWorker` procesa con circuit breaker y reintentos.
- Listado: `GET /api/v1/emitters/{emitterId}/invoices`.
- UI Consultar facturas consume ese listado; Emitir hace poll de `sri-status` tras firmar.

## Trabajo pendiente

- RIDE / email al receptor.
- Reintentos UI / panel de outbox dead-letter.
- Multi-documento (NC, ND, retenciones).
