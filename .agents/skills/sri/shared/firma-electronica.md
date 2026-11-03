# Firma Electrónica XAdES-BES (SRI Ecuador)

Esta especificación describe los estándares técnicos y requisitos de seguridad para la firma digital de comprobantes electrónicos en Ecuador.

---

## 1. Estándar de Firma

El SRI exige el estándar internacional **XAdES-BES (XML Advanced Electronic Signatures - Basic Electronic Signature)** en modo *enveloped* (la firma va contenida dentro del propio documento XML como nodo `<ds:Signature>`).

- **Esquema XSD:** `xmldsig-core-schema.xsd`.
- **Algoritmo de Canonicalización:** `http://www.w3.org/TR/2001/REC-xml-c14n-20010315`.
- **Algoritmo de Firma:** `http://www.w3.org/2000/09/xmldsig#rsa-sha1` o RSA con SHA-256.
- **Algoritmo de Digest:** `http://www.w3.org/2000/09/xmldsig#sha1` o SHA-256.

---

## 2. Requisitos del Certificado Digital

1. **Formato de Archivo:** Contenedor seguro **PKCS#12** con extensiones `.p12` o `.pfx`, protegido por contraseña.
2. **Entidades de Certificación Acreditadas:** El certificado debe haber sido emitido por una entidad de certificación acreditada por **ARCOTEL** en Ecuador:
   - Banco Central del Ecuador (BCE)
   - Security Data Seguridad en Datos y Firma Digital S.A.
   - Consejo de la Judicatura (ICERT-EC)
   - UANATACA Ecuador
   - ANFAC Autoridad de Certificación Ecuador
   - Datamedia
3. **Tipo de Certificado:** Certificado de persona jurídica (representante legal) o persona natural con RUC.

---

## 3. Invariantes de Validación previas al Firmado

- **Vigencia del Certificado:** $\text{FechaActual} \ge \text{NotBefore} \land \text{FechaActual} \le \text{NotAfter}$. Si el certificado está vencido, abortar firma inmediatamente (mitiga Error 50 del SRI).
- **Concordancia de RUC:** El RUC contenido en los atributos del certificado (`Subject Alternative Name` / `OID 1.3.6.1.4.1.37244.1...` o `SERIALNUMBER`/`UID`) debe coincidir obligatoriamente con el RUC emisor declarado en `<infoTributaria><ruc>`.
- **Inmutabilidad Post-Firma:** Una vez firmado el XML, ningún nodo ni carácter (incluidos espacios en blanco) puede ser modificado; de lo contrario, el SRI devolverá `ERROR 50: FIRMA INVÁLIDA O ALTERADA`.
