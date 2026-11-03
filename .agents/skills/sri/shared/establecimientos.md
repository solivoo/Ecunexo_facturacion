# Establecimientos (SRI Ecuador)

## 1. Definición y Reglas
El establecimiento identifica la sucursal, agencia o local físico/virtual del emisor registrado en su RUC ante el SRI.

- **Campo XML:** `<estab>` (en `infoTributaria`).
- **Formato:** Exactamente 3 dígitos numéricos (`[0-9]{3}`).
- **Rango:** `001` a `999`. No se permite `000`.
- **Casa Matriz:** Convencionalmente el establecimiento principal es `001`.

## 2. Direcciones Asociadas
- `<dirMatriz>`: Dirección de la casa matriz registrada en el RUC. Obligatoria en `infoTributaria`.
- `<dirEstablecimiento>`: Dirección física donde opera el establecimiento emisor. Obligatoria en el bloque del comprobante (`infoFactura`, `infoNotaCredito`, etc.).
