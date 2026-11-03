# Clave de Acceso (SRI Ecuador)

La Clave de Acceso es el identificador único universal de 49 dígitos numéricos asignado a cada comprobante electrónico emitido bajo el Esquema Offline.

---

## 1. Estructura de los 49 Dígitos

$$\text{ClaveAcceso} = \text{Fecha (8)} + \text{Tipo (2)} + \text{RUC (13)} + \text{Amb (1)} + \text{Estab (3)} + \text{Pto (3)} + \text{Sec (9)} + \text{Código (8)} + \text{Emisión (1)} + \text{DV (1)}$$

| Posición | Longitud | Campo | Descripción |
| :---: | :---: | :--- | :--- |
| **01 - 08** | 8 | Fecha de Emisión | Formato estricto `ddMMyyyy` (ej. `24092026`) |
| **09 - 10** | 2 | Tipo de Comprobante | `01` Factura, `04` Nota de Crédito, `05` Nota de Débito, `06` Guía Remisión, `07` Retención |
| **11 - 23** | 13 | RUC del Emisor | RUC válido del emisor registrado en SRI |
| **24** | 1 | Tipo de Ambiente | `1` Pruebas (Test), `2` Producción |
| **25 - 27** | 3 | Establecimiento | Código de 3 dígitos (ej. `001`) |
| **28 - 30** | 3 | Punto de Emisión | Código de 3 dígitos (ej. `001`) |
| **31 - 39** | 9 | Secuencial | Consecutivo de 9 dígitos con ceros a la izquierda (ej. `000000001`) |
| **40 - 47** | 8 | Código Numérico | Número aleatorio o secuencial de 8 dígitos para evitar predecibilidad |
| **48** | 1 | Tipo de Emisión | `1` Normal (Obligatorio en esquema offline) |
| **49** | 1 | Dígito Verificador (DV) | Módulo 11 calculado sobre los primeros 48 dígitos |

---

## 2. Algoritmo de Cálculo: Módulo 11

El dígito verificador se calcula recorriendo los 48 dígitos de **derecha a izquierda**:
1. Ponderación cíclica: Multiplicadores `2, 3, 4, 5, 6, 7` repetidos cíclicamente:
   - Posición 48 $\times 2$, Posición 47 $\times 3$, ..., Posición 43 $\times 7$, Posición 42 $\times 2$, etc.
2. Sumar todos los productos: $\text{Suma} = \sum (\text{dígito}_i \times \text{peso}_i)$.
3. Calcular residuo: $\text{Residuo} = \text{Suma} \pmod{11}$.
4. Calcular resultado preliminar: $\text{Resultado} = 11 - \text{Residuo}$.
5. Determinar Dígito Verificador final:
   - Si $\text{Resultado} == 11 \implies \text{DV} = 0$.
   - Si $\text{Resultado} == 10 \implies \text{DV} = 1$.
   - En cualquier otro caso $\implies \text{DV} = \text{Resultado}$.

---

## 3. Invariantes de Validación

1. **Longitud:** Exactamente 48 dígitos para la base y 49 dígitos para la clave final.
2. **Coherencia Interna:** Los dígitos de fecha, tipo, RUC, establecimiento, punto de emisión y secuencial dentro de la clave deben coincidir exactamente con los nodos correspondientes en `infoTributaria`.
3. **Consistencia de Clave y Autorización:** En el esquema offline del SRI, el número de autorización emitido por el SRI coincide con la Clave de Acceso del comprobante.
