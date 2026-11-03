# Secuenciales de Comprobantes (SRI Ecuador)

## 1. Definición y Reglas
El número secuencial es el identificador consecutivo y cronológico del comprobante dentro de una serie determinada.

- **Campo XML:** `<secuencial>` (en `infoTributaria`).
- **Formato:** Exactamente 9 dígitos numéricos (`[0-9]{9}`).
- **Rango:** `000000001` a `999999999`.
- **Relleno:** Debe estar obligatoriamente rellenado con ceros a la izquierda (ej. número 42 se representa `000000042`).

## 2. Unicidad e Invariante de Emisión
Un comprobante queda unívocamente identificado ante el SRI y en la base de datos por la tupla:
$$(\text{RUC Emisor}, \text{Tipo Comprobante}, \text{Establecimiento}, \text{Punto de Emisión}, \text{Secuencial})$$

- **Máscara para presentación (RIDE):** `001-001-000000042`.
- **Prohibición:** No pueden existir duplicados autorizados con la misma tupla. El SRI rechazará con Error 39 si se intenta registrar un secuencial ya existente con clave distinta.
