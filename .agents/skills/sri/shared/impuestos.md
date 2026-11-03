# Catálogo de Impuestos y Tarifas (SRI Ecuador)

Esta especificación detalla la tipificación de impuestos y los códigos de tarifas de IVA e ICE conforme a la Ficha Técnica Offline v2.32.

---

## 1. Códigos de Impuesto (Campo `<codigo>`)

| Código | Impuesto |
| :---: | :--- |
| **2** | **IVA** (Impuesto al Valor Agregado) |
| **3** | **ICE** (Impuesto a los Consumos Especiales) |
| **5** | **IRBPNR** (Impuesto Redimible a las Botellas Plásticas no Retornables) |

---

## 2. Catálogo de Porcentajes de IVA (Tabla 17 del SRI - Campo `<codigoPorcentaje>`)

| Código Porcentaje | Descripción | Tarifa (%) | Vigencia / Notas |
| :---: | :--- | :---: | :--- |
| **0** | Tarifa 0% | 0.00 | Productos canasta básica, medicina, insumos agropecuarios |
| **2** | Tarifa 12% | 12.00 | Histórico hasta 2024-03-31 |
| **3** | Tarifa 14% | 14.00 | Histórico temporal (Ley Solidaria 2016) |
| **4** | **Tarifa 15% (Tarifa General)** | **15.00** | **Vigente a nivel nacional desde 2024-04-01** |
| **5** | **Tarifa 5%** | **5.00** | **Vigente para Materiales de Construcción (Ley 2024)** |
| **6** | No Objeto de Impuesto | 0.00 | Servicios prestados fuera del territorio nacional |
| **7** | Exento de IVA | 0.00 | Cesión de derechos, operaciones financieras |
| **8** | IVA Diferenciado | Variable | Zonas francas / Regímenes especiales |
| **10** | Tarifa 13% | 13.00 | Histórico temporal 2024 |

---

## 3. Reglas de Validación de Impuestos

1. **Temporalidad de la Tarifa:**
   La tarifa aplicada en una Factura debe corresponder estrictamente al catálogo oficial vigente en la fecha de emisión del comprobante (`fechaEmision`).
   - Si la fecha de emisión es $\ge \text{2024-04-01}$, la tarifa general es **15%** (`codigoPorcentaje = 4`).
2. **Impuesto por Línea (`<detalles>/<detalle>/<impuestos>/<impuesto>`):**
   - `<codigo>`: `2` (para IVA).
   - `<codigoPorcentaje>`: Código según Tabla 17.
   - `<tarifa>`: Porcentaje numérico a 2 decimales (ej. `15.00`, `0.00`, `5.00`).
   - `<baseImponible>`: Monto de la línea sujeto al impuesto a 2 decimales (`cantidad * precioUnitario - descuento`).
   - `<valor>`: Monto calculado del impuesto a 2 decimales (`baseImponible * (tarifa / 100)`).
3. **Agrupación en Totales (`<infoFactura>/<totalConImpuestos>`):**
   - El XML exige que todas las líneas que compartan la misma combinación de `(codigo, codigoPorcentaje)` se consoliden en un único nodo `<totalImpuesto>`.
   - La `baseImponible` de este grupo es la suma exacta de las bases imponibles de las líneas de ese grupo.
   - El `valor` es la suma de los valores de impuesto de las líneas de ese grupo.
