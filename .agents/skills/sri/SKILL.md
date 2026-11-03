---
name: sri
description: >-
  Paquete de conocimiento de dominio y normativa tributaria del Servicio de Rentas Internas (SRI)
  de Ecuador para comprobantes electrónicos (Esquema Offline v2.32, XML v1.1.0).
  Contiene routing de comprobantes (Factura, Nota de Crédito), reglas compartidas (RUC, Cédula,
  Secuenciales, Impuestos IVA/ICE, Clave de Acceso Módulo 11, Firma XAdES-BES, Redondeo Half-Up),
  invariantes normativas, contratos de mapeo XML y especificaciones de tests para EcuNexo.
---

# Paquete de Dominio: Ecuador SRI Comprobantes Electrónicos

Este paquete encapsula todo el conocimiento tributario, normativo y de arquitectura de software para la emisión, validación, firma y autorización de comprobantes electrónicos bajo el esquema **Offline v2.32** del **Servicio de Rentas Internas (SRI)** del Ecuador.

---

## 1. Arquitectura en 4 Capas

```
┌─────────────────────────────────────────────────────────────┐
│ 1. CAPA NORMATIVA (sources/, shared/)                       │
│    Ficha Técnica SRI, Resoluciones NAC, Catálogos Oficiales │
├─────────────────────────────────────────────────────────────┤
│ 2. CAPA DE REGLAS (invariants.yaml, validations.yaml)       │
│    Invariantes formales (SRI / BUSINESS / TECHNICAL)        │
├─────────────────────────────────────────────────────────────┤
│ 3. CAPA DE CONTRATOS TÉCNICOS (xml-mapping.yaml, schemas/)  │
│    Mapeo de Dominio -> DTO Tributario -> XML SRI v1.1.0     │
├─────────────────────────────────────────────────────────────┤
│ 4. CAPA DE COMPORTAMIENTO DEL AGENTE (workflow.md, tests/)  │
│    State machine de emisión, prohibiciones y test specs     │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Router Dinámico de Conocimiento

El agente **NO** debe cargar toda la documentación en un solo paso. Debe aplicar **revelación progresiva** cargando únicamente los módulos requeridos para la tarea en curso:

### Si la tarea involucra Facturas Electrónicas (Tipo 01):
1. Cargar el dominio y workflow de factura:
   - [domain.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/domain.md)
   - [workflow.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/workflow.md)
2. Cargar las reglas y contratos de cálculo:
   - [calculations.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/calculations.yaml)
   - [invariants.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/invariants.yaml)
   - [validations.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/validations.yaml)
3. Cargar las reglas compartidas necesarias:
   - [impuestos.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/impuestos.md)
   - [redondeo-decimales.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/redondeo-decimales.md)
   - [identificacion-contribuyente.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/identificacion-contribuyente.md)
   - [secuenciales.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/secuenciales.md)
   - [clave-acceso.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/clave-acceso.md)
   - [firma-electronica.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/firma-electronica.md)
4. Cargar mapeo XML y esquema solo si se genera o valida XML:
   - [xml-mapping.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/xml-mapping.yaml)
   - [Factura_V1.1.0.xsd](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/schemas/factura/Factura_V1.1.0.xsd)

### Si la tarea involucra Notas de Crédito (Tipo 04):
1. Cargar el dominio y workflow de nota de crédito:
   - [domain.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/domain.md)
   - [workflow.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/workflow.md)
2. Cargar las reglas y contratos de cálculo:
   - [calculations.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/calculations.yaml)
   - [invariants.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/invariants.yaml)
   - [validations.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/validations.yaml)
3. Cargar las reglas compartidas necesarias:
   - [impuestos.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/impuestos.md)
   - [redondeo-decimales.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/redondeo-decimales.md)
   - [identificacion-contribuyente.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/identificacion-contribuyente.md)
   - [secuenciales.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/secuenciales.md)
   - [clave-acceso.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/clave-acceso.md)
   - [firma-electronica.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/firma-electronica.md)
4. Cargar mapeo XML y esquema solo si se genera o valida XML:
   - [xml-mapping.yaml](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/nota-credito/xml-mapping.yaml)
   - [ValidadorNotaCredito_V1.1.0.xsd](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/schemas/nota-credito/ValidadorNotaCredito_V1.1.0.xsd)

---

## 3. Principios Fundamentales del Sistema

1. **No inventar reglas tributarias:** Toda regla o catálogo debe estar documentado en `sources/` con su resolución o ficha técnica correspondiente.
2. **Separación estricta de responsabilidades:**
   - **Cálculo:** Determina cómo se obtienen los valores numéricos (`calculations.yaml`).
   - **Validación:** Determina si un documento cumple los criterios de validez (`validations.yaml`).
3. **Independencia Tecnológica:** El conocimiento de esta skill no depende de .NET, React o PostgreSQL. Describe el **QUÉ**. La arquitectura de software (`Billing.Core`, `Billing.Api`, etc.) describe el **CÓMO**.
4. **Arquitectura de Transformación de Comprobantes:**
   $$\text{Modelo de Dominio} \longrightarrow \text{DTO Tributario} \longrightarrow \text{XML SRI} \longrightarrow \text{Firma XAdES-BES} \longrightarrow \text{WebService SRI}$$
   *NUNCA acoplar la base de datos o el modelo interno directamente al XML.*
5. **Spec-Driven Testing:** Toda regla normativa de severidad `error` debe tener al menos una prueba unitaria derivada en `Billing.Core.Tests`.

---

## 4. Clasificación de Reglas (Taxonomía)

Cada regla del sistema se identifica con un prefijo unívoco según su origen:

| Tipo | Prefijo Ejemplo | Propósito | Ejemplo |
| :--- | :--- | :--- | :--- |
| **SRI** | `FAC-SRI-xxx` | Regla impuesta directamente por la ley, ficha técnica o resoluciones del SRI. | Clave de acceso debe tener 49 dígitos con Módulo 11. |
| **BUSINESS** | `FAC-BUS-xxx` | Regla del negocio u operativa propia de EcuNexo. | No permitir emitir factura a crédito si el cliente supera su cupo. |
| **TECHNICAL** | `FAC-TEC-xxx` | Restricción técnica de infraestructura, base de datos o API. | Campo de nota adicional máximo 300 caracteres sanitizados UTF-8. |

---

## 5. Prohibiciones Estrictas para el Agente (Anti-Patrones)

> [!CAUTION]
> El agente que implemente o modifique código en EcuNexo tiene terminantemente **PROHIBIDO**:
> 1. **Inventar códigos tributarios o tarifas de impuestos:** Solo se admiten los códigos tipificados en [impuestos.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/impuestos.md).
> 2. **Asumir longitudes o máscaras de campos:** Todo campo debe respetar las longitudes mínimas y máximas de los XSD del SRI (ej. secuencial siempre de 9 dígitos con relleno de ceros a la izquierda).
> 3. **Modificar la precisión decimal arbitrariamente:** En `Billing.Core`, montos finales deben redondearse a 2 decimales usando `MidpointRounding.AwayFromZero` (Half-Up). Precios y cantidades aceptan de 2 a 6 decimales según [redondeo-decimales.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/shared/redondeo-decimales.md).
> 4. **Generar XML aproximado o manual sin validación XSD:** Cualquier XML emitido debe pasar la validación estricta del esquema XSD antes del firmado.
> 5. **Ignorar los estados del flujo de emisión:** No asumir que el envío al WebService autoriza automáticamente la factura. Se debe respetar la máquina de estados definida en [workflow.md](file:///home/solivo/Documentos/ecunexo/Facturacion/.agents/skills/sri/comprobantes/factura/workflow.md).
> 6. **Permitir Facturas a Consumidor Final superiores a $50.00 USD:** La normativa ecuatoriana prohíbe emitir a consumidor final por montos mayores a $50.00 USD.
