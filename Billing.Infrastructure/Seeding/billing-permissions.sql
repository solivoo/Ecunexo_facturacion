-- Seed permisos módulo facturación (tenant module: facturacion)
-- Ejecutar en BD ecunexo. Ajustar columnas si identity.permissions difiere en tu entorno.
-- Idempotente por code.

INSERT INTO identity.permissions (id, code, name, description, module_code, is_active, created_at)
VALUES
  (gen_random_uuid(), 'facturacion:read', 'Facturación — acceso', 'Ver módulo facturación en menú', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:comprobantes:read', 'Comprobantes — lectura', 'Ver sección comprobantes', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:facturas:read', 'Facturas — consultar propias', 'Listar las facturas que emitió el usuario', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:facturas:read.all', 'Facturas — consultar todas', 'Ver todas las facturas del emisor', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:facturas:create', 'Facturas — crear', 'Crear borrador de factura', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:facturas:sign', 'Facturas — firmar', 'Firmar XML', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:facturas:transmit', 'Facturas — recepción SRI', 'Enviar recepción offline', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:facturas:authorize', 'Facturas — autorizar', 'Poll autorización SRI', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:facturas:status', 'Facturas — estado SRI', 'Consultar estado consolidado', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:notas-credito:read', 'NC — lectura', 'Notas de crédito SRI 04', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:notas-debito:read', 'ND — lectura', 'Notas de débito SRI 05', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:guias-remision:read', 'Guías — lectura', 'Guías de remisión SRI 06', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:retenciones:read', 'Retenciones — lectura', 'Comprobantes retención SRI 07', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:liquidacion-compra:read', 'Liquidación — lectura', 'Liquidación compra SRI 03', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:catalogos:read', 'Catálogos — lectura', 'Consultar tarifas y retenciones', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:catalogos:write', 'Catálogos — escritura', 'Registrar tarifas/retenciones', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:emisor:read', 'Emisor — lectura', 'Consultar datos emisor', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:emisor:write', 'Emisor — escritura', 'Registrar emisor', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:emisor:certificates', 'Emisor — certificado', 'Gestionar certificado firma', 'facturacion', true, now()),
  (gen_random_uuid(), 'facturacion:sri:read', 'SRI — monitoreo', 'Estados y autorizaciones SRI', 'facturacion', true, now())
ON CONFLICT (code) DO NOTHING;

-- Rol operador facturación (opcional): asignar permisos MVP manualmente vía role_permissions
