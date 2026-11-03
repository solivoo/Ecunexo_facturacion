---
name: sri-nota-credito
description: >-
  Módulo de Nota de Crédito Electrónica (Comprobante Tipo 04, Esquema Offline v1.1.0) del SRI de Ecuador.
  Cubre modelo de dominio, vinculación con factura de sustento, regla de oro de IVA por fecha histórica,
  máquina de estados, afectación a Kárdex (CPP) y Cartera (A/R), mapeo XML y mitigación de errores SRI.
---

# Nota de Crédito Electrónica SRI Ecuador (Comprobante 04)

Esta especificación contiene las reglas exclusivas, contratos técnicos y comportamiento de software para la emisión, validación, firma y autorización de **Notas de Crédito Electrónicas (Tipo 04)** en EcuNexo.

---

## 1. Archivos del Módulo

- **Modelo de Dominio:** [domain.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/domain.md) (Conceptos, documento de sustento, Kárdex y Cartera)
- **Máquina de Estados y Workflow:** [workflow.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/workflow.md) (Ciclo de vida, validaciones previas y post-autorización)
- **Invariantes del Sistema:** [invariants.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/invariants.yaml) (Reglas normativas `NC-SRI`, `NC-BUS`, `NC-TEC`)
- **Cálculos Contables y Financieros:** [calculations.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/calculations.yaml) (`valorModificacion`, recálculo de CPP Kárdex)
- **Condiciones de Validación:** [validations.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/validations.yaml) (Predicados booleanos evaluables)
- **Mapeo XML v1.1.0:** [xml-mapping.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/xml-mapping.yaml) (Transformación Dominio -> XML `<notaCredito>`)
- **WebService & Autorización:** [authorization.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/authorization.md) (Protocolo SOAP y polling)
- **Catálogo de Errores SRI:** [errors.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/errors.yaml) (Errores 35, 43, 45, 50, 70)
- **Especificaciones de Prueba:** [tests/specs.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/tests/specs.yaml) (Escenarios BDD Given/When/Then)

---

## 2. Reglas Compartidas Requeridas

Al procesar una Nota de Crédito, se deben consultar los siguientes módulos de `shared/`:
- Identificación de clientes: [identificacion-contribuyente.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/identificacion-contribuyente.md)
- Secuenciales y Serie: [secuenciales.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/secuenciales.md) y [establecimientos.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/establecimientos.md)
- Tarifas de Impuestos e IVA: [impuestos.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/impuestos.md)
- Precisión Decimal y Redondeo: [redondeo-decimales.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/redondeo-decimales.md)
- Clave de Acceso (`codDoc = 04`): [clave-acceso.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/clave-acceso.md)
- Firma Digital XAdES-BES: [firma-electronica.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/firma-electronica.md)
