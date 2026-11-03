# Precisión Decimal y Redondeo (SRI Ecuador)

Esta especificación detalla las reglas de precisión y redondeo numérico exigidas por la Ficha Técnica v2.32 del SRI y aplicadas en el dominio contable de EcuNexo.

---

## 1. Matriz de Precisión por Campo (XML Factura v1.1.0)

| Campo XML | Descripción | Decimales Permitidos | Ejemplo Válido |
| :--- | :--- | :---: | :--- |
| `<cantidad>` | Cantidad de unidades facturadas | **2 a 6 decimales** | `1.00`, `2.500000`, `0.0125` |
| `<precioUnitario>` | Precio unitario del bien o servicio | **2 a 6 decimales** | `10.50`, `0.003450`, `125.99` |
| `<descuento>` | Descuento aplicado en la línea | **2 decimales exactos** | `0.00`, `5.25` |
| `<precioTotalSinImpuesto>` | Subtotal de la línea tras descuento | **2 decimales exactos** | `21.00`, `150.35` |
| `<baseImponible>` | Base imponible del impuesto | **2 decimales exactos** | `21.00`, `100.00` |
| `<tarifa>` | Porcentaje de impuesto | **2 decimales exactos** | `15.00`, `0.00`, `5.00` |
| `<valor>` | Monto calculado de impuesto | **2 decimales exactos** | `3.15`, `0.00` |
| `<totalSinImpuestos>` | Subtotal consolidado del comprobante | **2 decimales exactos** | `121.00` |
| `<propina>` | Propina voluntaria | **2 decimales exactos** | `0.00` |
| `<importeTotal>` | Total a pagar del comprobante | **2 decimales exactos** | `139.15` |
| `<total>` (en `<pagos>`) | Monto asignado a forma de pago | **2 decimales exactos** | `139.15` |

---

## 2. Método Oficial de Redondeo: Half-Up (AwayFromZero)

El SRI utiliza redondeo aritmético simétrico al más cercano. Si el dígito siguiente al último decimal a conservar es $\ge 5$, se redondea hacia arriba en magnitud (Half-Up).

### Implementación en C# / .NET
En `Billing.Core`, todo redondeo monetario debe invocar explícitamente:

```csharp
decimal valorRedondeado = Math.Round(valorOriginal, 2, MidpointRounding.AwayFromZero);
```

> [!CAUTION]
> En .NET, `Math.Round(x, 2)` por defecto utiliza *ToEven* (redondeo bancario), lo cual genera diferencias de 1 centavo con el SRI en valores como `2.425` ($\text{ToEven} \to 2.42$ vs $\text{AwayFromZero} \to 2.43$). **Siempre especificar `MidpointRounding.AwayFromZero`**.
