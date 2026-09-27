# Corrección del paquete Word

- Rev005 conservó el texto de Rev004, pero su serialización OOXML dejó sin resolver el prefijo `w14` citado por `mc:Ignorable`, por lo que Microsoft Word rechazó ese archivo aunque el contenedor ZIP y `python-docx` pudieran leerlo.
- Esta revisión conserva el anexo histórico de Rev005 y agrega una corrección del serializador que mantiene los prefijos OOXML requeridos; el DOCX de Rev006 se abre y se valida en Microsoft Word.
- Rev005 y su huella se conservan como evidencia del intento anterior. Rev006 es la revisión protegida y verificada para uso normal.
