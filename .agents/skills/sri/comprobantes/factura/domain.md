# Modelo de Dominio: Factura Electrónica

Este documento define el modelo conceptual agnóstico de la Factura Electrónica (Tipo 01) en EcuNexo.

---

## 1. Estructura Jerárquica del Agregado

```
Factura (Aggregate Root)
│
├── id (UUID interno)
├── emisor (RUC, Razón Social, Nombre Comercial, DirMatriz, Régimen)
├── establecimiento (Código 3 dígitos, Dirección)
├── puntoEmision (Código 3 dígitos)
├── secuencial (Número correlativo 9 dígitos)
├── fechaEmision (DateOnly)
├── comprador (TipoIdentificacion, Identificacion, RazonSocial, Direccion, Email)
│
├── detalles[] (Lista de líneas de factura - al menos una)
│   ├── codigoPrincipal (SKU o código interno)
│   ├── codigoAuxiliar (opcional)
│   ├── descripcion (Nombre del producto o servicio)
│   ├── cantidad (Decimal 2 a 6 dígitos)
│   ├── precioUnitario (Decimal 2 a 6 dígitos)
│   ├── descuento (Decimal 2 dígitos)
│   ├── precioTotalSinImpuesto (cantidad * precioUnitario - descuento)
│   └── impuestos[] (Lista de tributos que gravan la línea)
│       ├── codigo (2=IVA, 3=ICE)
│       ├── codigoPorcentaje (Tarifa Tabla 17)
│       ├── tarifa (Porcentaje numérico ej. 15.00)
│       ├── baseImponible (Monto gravado)
│       └── valor (baseImponible * tarifa / 100)
│
├── totales
│   ├── subtotalSinImpuestos (Suma de precioTotalSinImpuesto de líneas)
│   ├── totalDescuento (Suma de descuentos de líneas)
│   ├── impuestosConsolidados[] (Agrupación por codigo + codigoPorcentaje)
│   │   ├── codigo
│   │   ├── codigoPorcentaje
│   │   ├── baseImponible
│   │   └── valor
│   ├── propina (0.00 por defecto)
│   └── importeTotal (subtotalSinImpuestos + suma(impuestos) + propina)
│
├── pagos[] (Formas de pago del comprobante)
│   ├── formaPago (Código Tabla 24: 01 Sin sistema financiero, 20 Otros con utilización...)
│   ├── total (Monto asignado)
│   ├── plazo (Días opcionales)
│   └── unidadTiempo (dias)
│
├── informacionAdicional (Pares clave-valor para RIDE, ej. email, dirección, notas)
├── claveAcceso (49 dígitos calculados)
└── estado (Draft, Validated, Signed, Sent, Authorized, etc.)
```

---

## 2. Definición de Conceptos de Dominio

### Factura
Documento tributario mercantil emitido por el vendedor con valor probatorio legal ante la administración tributaria ecuatoriana para respaldar la transferencia de bienes o prestación de servicios.

### Emisor
Entidad económica responsable de la emisión, identificada por su RUC y autorizada para emitir comprobantes electrónicos.

### Comprador (Adquirente / Contraparte)
Persona natural, jurídica o ente económico que adquiere los bienes o servicios.

### Detalle (Línea de Factura)
Renglón individual que representa una transacción de venta de un bien o servicio determinado, con su respectiva cantidad, precio y desglose de impuestos aplicables.

### Impuesto
Tributo nacional (IVA, ICE) aplicado a una línea o consolidado a nivel de documento, sujeto a tarifas normadas por la ley.

### Totales
Bloque financiero inmutable que consolida las bases imponibles, descuentos e impuestos generados por todas las líneas del comprobante.
