# Puntos de Emisión (SRI Ecuador)

## 1. Definición y Reglas
El punto de emisión corresponde a la caja registradora, terminal de facturación, o sistema automatizado de emisión electrónica dentro de un establecimiento específico.

- **Campo XML:** `<ptoEmi>` (en `infoTributaria`).
- **Formato:** Exactamente 3 dígitos numéricos (`[0-9]{3}`).
- **Rango:** `001` a `999`. No se permite `000`.
- **Independencia de Numeración:** Cada punto de emisión mantiene su propio contador secuencial independiente por cada tipo de comprobante.

## 2. Invariante de Serie
La serie tributaria se compone de la concatenación del establecimiento y el punto de emisión:
$$\text{Serie} = \text{Establecimiento (3)} + \text{PuntoEmisión (3)} \quad \text{Ejemplo: } 001\text{-}002$$
