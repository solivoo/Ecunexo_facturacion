from pathlib import Path

import xmlschema

base = Path(__file__).resolve().parent
xsd = base / "ValidadorNotaCredito_V1.1.0.xsd"
xml = base / "NotaDeCredito_V1.1.0"

schema = xmlschema.XMLSchema11(str(xsd))
schema.validate(str(xml))
print("VALID: OK")
