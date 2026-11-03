# Modelo de Dominio: Nota de Crédito Electrónica

Este documento define el modelo conceptual agnóstico de la Nota de Crédito (Tipo 04) en EcuNexo y su relación con el documento de sustento.

---

## 1. Definición y Propósito Tributario

La Nota de Crédito es el documento tributario complementario emitido por el vendedor para anular operaciones, aceptar devoluciones o conceder descuentos y rebajas posteriores a la emisión de una Factura autorizada.

> [!IMPORTANT]
> Una Nota de Crédito **NUNCA** existe de manera aislada. Exige una relación de precedencia estricta con un comprobante de venta legalmente autorizado de sustento (`codDocModificado = "01"`, `numDocModificado` y `fechaEmisionDocSustento`).

---

## 2. Estructura Jerárquica del Agregado

```
NotaCredito (Aggregate Root)
│
├── id (UUID v7 interno)
├── emisor (RUC, Razón Social, Nombre Comercial, DirMatriz, Régimen)
├── establecimiento (Código 3 dígitos, Dirección)
├── puntoEmision (Código 3 dígitos)
├── secuencial (Número correlativo 9 dígitos)
├── fechaEmision (DateOnly >= fechaEmisionDocSustento)
├── comprador (Mismos datos de la Factura de sustento)
│
├── sustento
│   ├── modifiedInvoiceId (UUID de la Factura autorizada)
│   ├── codDocModificado ("01" Factura)
│   ├── numDocModificado (Máscara "XXX-XXX-XXXXXXXXX")
│   └── fechaEmisionDocSustento (DateOnly de la Factura original)
│
├── motivo (Cadena de 1 a 300 caracteres normalizada)
│
├── detalles[] (Líneas objeto de la modificación/devolución)
│   ├── codigoInterno
│   ├── descripcion
│   ├── cantidad (Decimal 2 a 6 dígitos)
│   ├── precioUnitario (Decimal 2 a 6 dígitos)
│   ├── descuento (Decimal 2 dígitos)
│   ├── precioTotalSinImpuesto
│   └── impuestos[] (Heredados estrictamente de la fecha de la factura)
│
├── totales
│   ├── totalSinImpuestos (Suma de precioTotalSinImpuesto)
│   ├── totalConImpuestos[] (Agrupados por tarifa histórica)
│   └── valorModificacion (Monto total con impuestos a disminuir)
│
├── claveAcceso (49 dígitos con Tipo 04 y Módulo 11)
└── estado (Draft, Validated, Signed, Sent, Authorized, etc.)
```

---

## 3. Impactos Multidominio tras la Autorización

Al autorizarse formalmente por el SRI, la Nota de Crédito desencadena tres efectos automáticos en EcuNexo:

### A. Dominio de Inventarios (Kárdex)
- **Si el motivo implica Devolución Física de Bienes:**
  - Se genera movimiento de entrada a bodega por devolución.
  - Se incrementa el stock disponible.
  - Se actualiza el **Costo Promedio Ponderado (CPP)** del producto.
- **Si el motivo es Descuento o Corrección de Precio:**
  - No genera movimiento físico ni altera cantidades en bodega.

### B. Dominio de Cartera (Cuentas por Cobrar - A/R)
- Disminuye el saldo adeudado de la Factura de origen.
- Si el saldo llega a `$0.00`, la factura pasa a estado cancelada/anulada.
- Si la factura ya había sido cobrada en su totalidad, el valor de la NC se registra como **Crédito a Favor del Cliente (Anticipo)**.

### C. Dominio Contable
- Reversión proporcional de los ingresos por ventas.
- Reversión proporcional del débito fiscal del IVA.
- Disminución de la cuenta por cobrar del cliente o reconocimiento de saldo a favor.
- Si hubo retorno físico de stock: reingreso a la cuenta de Inventario y acreditación al Costo de Ventas.
