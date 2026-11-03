# Protocolo de Autorización SRI para Notas de Crédito

La transmisión de Notas de Crédito comparte los WebServices SOAP del esquema offline del SRI con las Facturas, utilizando la Clave de Acceso generada con código de documento `04`.

---

## 1. Endpoints Oficiales (SOAP)

- **Ambiente Pruebas (1):**
  - Recepción: `https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl`
  - Autorización: `https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl`
- **Ambiente Producción (2):**
  - Recepción: `https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl`
  - Autorización: `https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl`

---

## 2. Puntos de Falla Críticos en Autorización de NC

Al autorizar una Nota de Crédito, el SRI realiza validaciones cruzadas adicionales contra sus bases de datos:

1. **Existencia del Sustento:**
   El SRI verifica si el comprobante referenciado (`numDocModificado`) existe y está autorizado para ese mismo emisor y adquirente. Si no existe, devuelve `ERROR 35`.
2. **Validación de Sobredescuento:**
   El SRI totaliza el acumulado de Notas de Crédito emitidas previamente para esa misma Factura. Si la suma de las NC supera el valor original de la factura, devuelve `ERROR 43`.
3. **Consistencia de la Tarifa de IVA:**
   El SRI compara el `codigoPorcentaje` del detalle de la NC con la fecha declarada en `fechaEmisionDocSustento`. Si no coincide la tarifa histórica, devuelve `ERROR 45`.
