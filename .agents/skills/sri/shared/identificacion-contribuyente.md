# Identificación del Contribuyente (SRI Ecuador)

Esta especificación cubre la validación y tipificación de identidades fiscales para emisores y receptores (compradores) conforme a la **Tabla 6 del SRI**.

---

## 1. Catálogo de Tipos de Identificación (Tabla 6)

| Código | Tipo de Identificación | Sujetos Aplicables | Longitud / Formato |
| :---: | :--- | :--- | :--- |
| **04** | **RUC** | Personas naturales y jurídicas nacionales o extranjeras domiciliadas | Exactamente 13 dígitos numéricos |
| **05** | **Cédula** | Ciudadanos ecuatorianos y extranjeros con cédula de identidad | Exactamente 10 dígitos numéricos |
| **06** | **Pasaporte** | Personas extranjeras no domiciliadas | Alfanumérico, máximo 20 caracteres |
| **07** | **Consumidor Final** | Ventas al detal sin datos del cliente | Cadena fija: `9999999999999` (13 nueves) |
| **08** | **Identificación del Exterior**| Personas o empresas extranjeras | Alfanumérico, máximo 20 caracteres |

---

## 2. Reglas de Validación de Cédula (Módulo 10)

Una Cédula ecuatoriana válida consta de 10 dígitos numéricos:
1. **Dígitos 1-2 (Código de Provincia):** Debe estar entre `01` y `24`, o ser `30` (ecuatorianos en el exterior).
2. **Dígito 3 (Tercer dígito):** Debe ser menor a `6` (es decir, entre `0` y `5`).
3. **Dígito 10 (Dígito Verificador):** Se calcula con el algoritmo de **Módulo 10 (Luhn modificado con multiplicadores 2 y 1 alternados)**:
   - Se multiplican los primeros 9 dígitos alternadamente por `2` y `1` de izquierda a derecha.
   - Si el producto de una multiplicación es $\ge 10$, se le resta `9`.
   - Se suman todos los resultados intermedios.
   - El residuo de la suma respecto a 10 se resta de 10: $\text{DV} = (10 - (\text{Suma} \pmod{10})) \pmod{10}$.
   - Si $\text{Suma} \pmod{10} == 0$, entonces $\text{DV} = 0$.
   - El resultado debe ser idéntico al dígito 10.

---

## 3. Reglas de Validación de RUC (13 Dígitos)

Todo RUC consta de 13 dígitos. Los últimos 3 o 4 dígitos identifican el establecimiento (`001` a `999`):

### A. RUC de Persona Natural
- Primeros 10 dígitos: Cédula de identidad válida (cumple algoritmo Módulo 10).
- Dígitos 11 a 13: Código del establecimiento, usualmente `001`. Nunca puede ser `000`.

### B. RUC de Sociedad Privada y Extranjeros Domiciliados
- Dígitos 1-2: Código de provincia válido (`01` a `24`, o `30`).
- Dígito 3: Debe ser igual a **`9`**.
- Dígitos 4 a 9: Número secuencial.
- Dígito 10: Dígito Verificador calculado con **Módulo 11**:
  - Coeficientes: `4, 3, 2, 7, 6, 5, 4, 3, 2` (para dígitos 1 a 9).
  - Suma ponderada = $\sum (\text{dígito}_i \times \text{coef}_i)$.
  - Residuo = $\text{Suma} \pmod{11}$.
  - $\text{DV} = 11 - \text{Residuo}$. Si $\text{Residuo} = 0 \implies \text{DV} = 0$. Si $\text{Residuo} = 1$, el RUC es inválido.
- Dígitos 11 a 13: `001` o número de establecimiento registrado. Nunca `000`.

### C. RUC de Entidad Pública
- Dígitos 1-2: Código de provincia válido (`01` a `24`, o `30`).
- Dígito 3: Debe ser igual a **`6`**.
- Dígitos 4 a 8: Número secuencial.
- Dígito 9: Dígito Verificador calculado con **Módulo 11**:
  - Coeficientes: `3, 2, 7, 6, 5, 4, 3, 2` (para dígitos 1 a 8).
  - Si $\text{Residuo} = 0 \implies \text{DV} = 0$.
- Dígitos 10 a 13: `0001` o establecimiento.

---

## 4. Invariante Legal de Consumidor Final (Regla FAC-SRI-004)

> [!WARNING]
> Conforme a la **Resolución NAC-DGERCGC22-00000057**:
> - Se permite emitir facturas con adquirente Consumidor Final (`tipoIdentificacionComprador = "07"` y `identificacionComprador = "9999999999999"`) **únicamente si el Importe Total es $\le \$50.00\text{ USD}$**.
> - Si el importe total excede los USD 50.00, el sistema **DEBE RECHAZAR** la emisión y exigir RUC, Cédula o Pasaporte del comprador.
