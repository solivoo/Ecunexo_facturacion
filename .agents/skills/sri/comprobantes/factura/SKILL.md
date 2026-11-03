---
name: sri-factura
description: >-
  Módulo de Factura Electrónica (Comprobante Tipo 01, Esquema Offline v1.1.0) del SRI de Ecuador.
  Define el modelo de dominio, máquina de estados, cálculos contables, validaciones de esquema,
  mapeo XML y catálogo de errores para EcuNexo.
---

# Factura Electrónica SRI Ecuador (Comprobante 01)

Esta especificación contiene las reglas exclusivas y el comportamiento para la emisión, validación, firma y autorización de Facturas Electrónicas (Tipo 01) en EcuNexo.

---

## 1. Archivos del Módulo

- **Modelo de Dominio:** [domain.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/domain.md) (Entidades y conceptos)
- **Máquina de Estados y Workflow:** [workflow.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/workflow.md) (Ciclo de vida y transiciones)
- **Invariantes del Sistema:** [invariants.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/invariants.yaml) (Reglas normativas clasificadas)
- **Cálculos Contables:** [calculations.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/calculations.yaml) (Fórmulas sin mezclar con validaciones)
- **Condiciones de Validación:** [validations.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/validations.yaml) (Predicados booleanos evaluables)
- **Mapeo XML v1.1.0:** [xml-mapping.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/xml-mapping.yaml) (Transformación Dominio -> XML)
- **WebService & Autorización:** [authorization.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/authorization.md) (Protocolo SOAP y respuestas)
- **Catálogo de Errores SRI:** [errors.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/errors.yaml) (Códigos de rechazo y resolución)
- **Especificaciones de Prueba:** [tests/specs.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/tests/specs.yaml) (Escenarios Given/When/Then)

---

## 2. Reglas Compartidas Requeridas

Al procesar una factura electrónica, se deben consultar las siguientes especificaciones base:
- Identificación del cliente: [identificacion-contribuyente.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/identificacion-contribuyente.md)
- Secuencial y Serie: [secuenciales.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/secuenciales.md) y [establecimientos.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/establecimientos.md)
- Tarifas de Impuestos e IVA: [impuestos.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/impuestos.md)
- Precisión Decimal y Redondeo: [redondeo-decimales.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/redondeo-decimales.md)
- Generación de Clave de Acceso: [clave-acceso.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/clave-acceso.md)
- Firma Digital: [firma-electronica.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/firma-electronica.md)
