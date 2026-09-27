## Contrato JSON del inventario

La validación del diff ahora requiere que `assets` sea un arreglo JSON explícito. Un objeto de activo individual ya no se convierte implícitamente en una colección de un elemento por la enumeración de PowerShell; los reportes con forma incorrecta se rechazan antes de comparar.

El tamaño de origen declarado debe ser mayor que la cabecera fija y no superar 134217728 bytes (128 MiB), el mismo límite aplicado por el lector de inventarios. Junto con los controles de la revisión anterior, la tabla de contenidos debe ser positiva y caber dentro del rango posterior a la cabecera.

## Pruebas y límites

`tests/CalradiaForge.TpacInventoryCompare.Tests.bat` pasó con un fixture de objeto escalar `assets` rechazado y otro válido en forma de arreglo. También se rechaza un informe que declara 134217729 bytes; los informes dentro del límite conservan la comparación, normalización temporal y exportación JSON.

El lector real continúa sin validarse en esta máquina: Windows bloquea el ensamblado descargado. Estos fixtures comprueban el contrato JSON del comparador, no la lectura de TPAC ni la autenticidad de informes externos. No se modificaron paquetes ni archivos del juego y no se reconstruyeron distribuciones.
