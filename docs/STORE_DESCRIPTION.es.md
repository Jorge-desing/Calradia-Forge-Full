# Forja de Calradia — herramientas verificadas para mods de Bannerlord

Forja de Calradia es una suite de desarrollo en vista previa para Bannerlord Native 1.4.8. Diagnostica una carpeta de mod seleccionada, consulta evidencia de sesión y metadatos de ensamblados instalados, crea puntos de partida editables del SDK, registra páginas Gauntlet independientes y verifica eventos conservados de ForgeWeave mediante Replay Lab protegido.

No requiere Harmony, MCM, ButterLib ni otro framework de tiempo de ejecución. ForgeWeave es un marco cooperativo original; no promete intercepción universal de métodos ni afirma reemplazar herramientas que resuelven un problema distinto.

Las comprobaciones de archivos en escritorio son verificables: los formatos locales admitidos producen evidencia acotada; las entradas ausentes o incompatibles muestran **No ejecutado** o **No compatible**. Los informes conservan analizador, procedencia, archivo y línea cuando existen, gravedad, recomendación, límites y evidencia sin procesar.

El módulo del juego añade un catálogo SDK validado para páginas Gauntlet propias, ayuda contextual offline e inspección PE/.NET con una copia optativa de versión tras vista previa. El banco de recursos comprueba metadatos de sprites, límites de piezas y dimensiones de cabecera PNG, además de las comprobaciones limitadas de presencia/marcador TPAC. AsmResolver se incluye en Modules y Desktop; DocFX solo se ejecuta al generar documentación. Desktop conserva sus tres temas tácticos, operaciones MVVM, evidencia limitada y navegación de favoritas/recientes. Las descargas están separadas: `CalradiaForge-Modules-22.0.0.zip`, `CalradiaForge-Source-SDK-22.0.0.zip` y `CalradiaForge-Desktop-22.0.0.zip`. Desktop no contiene EXE anfitrión y usa .NET 8 Desktop Runtime.





