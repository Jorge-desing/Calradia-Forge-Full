# Validación de Calradia Forge 20.0.0

## Comprobaciones automatizadas

- Compilación Release de la solución: correcta, con 0 advertencias y 0 errores.
- Suite ForgeWeave: 24 aprobadas, 0 fallidas.
- Suite de transporte y MVVM del escritorio: 38 aprobadas, 0 fallidas.
- Render WPF: 261 casos aprobados, incluidos recorridos del banco de trabajo, idiomas/escalas, muestras de temas y pruebas de seguridad de inspección/edición de ensamblados.
- Auditoría de localización: 13 idiomas nativos con 394 claves cada uno; 13 diccionarios del escritorio con paridad de claves.
- Matriz visual estructural del escritorio: 55 casos breves aprobados.
- DocFX: generación local del sitio estático correcta, sin advertencias ni errores.
- Auditoría de archivos: registrada en `artifacts/package-audit-2000.json` tras generar los paquetes.

## Cobertura del banco de ensamblados

Los fixtures de render inspeccionan un ensamblado administrado y rechazan entradas nativas incompatibles o malformadas. Verifican que la vista previa no escriba archivos, que el parcheo conserve el hash original, cree un respaldo y valide los metadatos de salida. La ruta de escritura utiliza un archivo temporal y elimina la salida parcial si falla. El analizador de Desktop lee metadatos y no carga ni ejecuta la DLL inspeccionada.

## Límites

Se detuvo el ejecutable de pruebas combinado heredado después de permanecer más de un minuto sin salida; por ello su resultado quedó **sin completar**. Las suites enfocadas indicadas arriba se ejecutaron por separado y pasaron. No se inició Bannerlord ni se cargó campaña, batalla o prueba prolongada. La integración real dentro del juego queda sin verificar en esta validación.
