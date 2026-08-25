# Postman — Catálogos e impuestos (uno por uno)

Pipeline completo (XML, XSD, firma, Infisical): ver [`docs/01-factura-electronica-pipeline.md`](../docs/01-factura-electronica-pipeline.md).

Base URL: `http://localhost:5203`

Cada impuesto se crea con **un POST = un registro** (mismo contrato que la UI).  
Respuesta típica:

```json
{ "created": 1, "skipped": 0, "errors": [] }
```

Si ya existe: `{ "created": 0, "skipped": 1, "errors": [] }`

---

## A) Tarifas de impuesto (`billing."TaxRates"`)

**Ruta (todas):** `POST {{baseUrl}}/api/v1/catalogs/tax-rates`  
**Header:** `Content-Type: application/json`

| # | Archivo body | Descripción SRI |
|---|--------------|-----------------|
| 01 | `catalogos/tarifas-iva/01-iva-0.json` | IVA 0 % — codigo 2, codigoPorcentaje 0 |
| 02 | `catalogos/tarifas-iva/02-iva-12.json` | IVA 12 % (histórico hasta 2024-03-31) |
| 03 | `catalogos/tarifas-iva/03-iva-14.json` | IVA 14 % (histórico hasta 2024-03-31) |
| **04** | **`catalogos/tarifas-iva/04-iva-15.json`** | **IVA 15 % — obligatorio para factura demo** |
| 05 | `catalogos/tarifas-iva/05-iva-5.json` | IVA 5 % |
| 06 | `catalogos/tarifas-iva/06-iva-no-objeto.json` | No objeto de IVA |
| 07 | `catalogos/tarifas-iva/07-iva-exento.json` | IVA exento |
| 08 | `catalogos/tarifas-iva/08-ice-0.json` | ICE 0 % |
| 09 | `catalogos/tarifas-iva/09-isd-0.json` | ISD 0 % |

### Ejemplo mínimo (solo facturar hoy)

**POST** `/api/v1/catalogs/tax-rates`

```json
{
  "taxCode": "2",
  "rateCode": "4",
  "description": "IVA 15%",
  "rate": 15,
  "validFrom": "2024-04-01"
}
```

### Verificar

**GET** `/api/v1/catalogs/tax-rates?taxCode=2&rateCode=4&date=2024-06-01`

---

## B) Retenciones (`billing."WithholdingRates"`)

**Ruta (todas):** `POST {{baseUrl}}/api/v1/catalogs/withholding-rates`  
**Header:** `Content-Type: application/json`

| # | Archivo body | Uso típico |
|---|--------------|------------|
| 01 | `catalogos/retenciones/01-renta-303-honorarios.json` | Comprobante retención — honorarios 10 % |
| 02 | `catalogos/retenciones/02-renta-304-servicios-intelecto.json` | Servicios intelecto 2 % |
| 03 | `catalogos/retenciones/03-renta-307-arrendamiento.json` | Arrendamiento 10 % |
| 04 | `catalogos/retenciones/04-renta-312-bienes-muebles.json` | Bienes muebles 1.75 % |
| 05 | `catalogos/retenciones/05-renta-3440-otras-2.json` | Otras renta 2 % |
| 06 | `catalogos/retenciones/06-iva-ret-1-10pct.json` | Retención IVA 10 % |
| 07 | `catalogos/retenciones/07-iva-ret-9-30pct.json` | Retención IVA 30 % |
| 08 | `catalogos/retenciones/08-iva-ret-10-70pct.json` | Retención IVA 70 % |
| 09 | `catalogos/retenciones/09-iva-ret-11-100pct.json` | Retención IVA 100 % |
| 10 | `catalogos/retenciones/10-isd-ret-4580.json` | Retención ISD 5 % |

### Verificar retención

**GET** `/api/v1/catalogs/withholding-rates?taxType=1&retentionCode=303&date=2024-06-01`

---

## C) Flujo factura (después de cargar al menos IVA 15 %)

Importar colección: `Ecunexo-Billing-MVP.postman_collection.json`

| # | Método | Ruta |
|---|--------|------|
| 20 | GET | `/api/v1/catalogs/tax-rates?taxCode=2&rateCode=4&date=2024-06-01` |
| 21 | POST | `/api/v1/emitters` |
| 22 | POST | `/api/v1/emitters/{{emitterId}}/certificates` |
| 23 | POST | `/api/v1/emitters/{{emitterId}}/invoices` |
| 24 | POST | `.../invoices/{{invoiceId}}/sign` |
| 25 | POST | `.../invoices/{{invoiceId}}/submit-reception` |
| 26 | POST | `.../invoices/{{invoiceId}}/authorize-poll` |
| 27 | GET | `.../invoices/{{invoiceId}}/sri-status` |

---

## D) Menú y permisos (gluBox)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/v1/billing/menu` | Menú completo |
| GET | `/api/v1/billing/menu?permissions=facturacion:read,...` | Menú filtrado |
| GET | `/api/v1/billing/permissions?mvpOnly=true` | Catálogo permisos |

JSON ejemplo: `postman/menu/billing-menu.example.json`  
Documentación: `Facturacion SRI/20 - Menú módulo y permisos RBAC.md`

---

## Orden recomendado en Postman

1. Ejecutar **01 → 09** (tarifas) o solo **04** si quieres lo mínimo.
2. Ejecutar **01 → 10** (retenciones) solo si vas a probar comprobante de retención.
3. Continuar flujo factura **20 → 27**.
