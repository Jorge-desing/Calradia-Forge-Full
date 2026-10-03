# Muestra de contenido de Calradia Forge

Este ejemplo instalable muestra XML estático de `Items` y `NPCCharacters`, los builders del SDK que lo generan y una pequeña página Gauntlet propiedad del módulo. Apunta al runtime `net472` del juego y no agrega API, dependencias ni comportamiento a Calradia Forge.

Ejecuta `tools\Test-CalradiaForge-ContentShowcase.bat` desde el repositorio para regenerar el ejemplo de forma determinista, validarlo contra `Items.xsd` y `NPCCharacters.xsd` del juego instalado, compilar el módulo, ejecutar el arnés acotado de división temporal y lanzar la suite Core mediante su BAT. Define `BANNERLORD_GAME_PATH` si el juego está instalado en otra ubicación.

El módulo de salida es `modules/CalradiaForgeContentShowcase`; el nombre de la carpeta coincide con el ID del manifiesto. Depende de Native, SandBoxCore y CalradiaForge. La tropa usa el objeto del módulo `cfcs_practice_whip`. El objeto reutiliza la malla y el cuerpo Native `horse_whip`, ya que este ejemplo no incluye arte propio. La tropa es una definición estática; no se agrega a una plantilla de grupo ni a un árbol de tropas.

La página se registra mediante `ForgeApi.RegisterWhenAvailable` y solo se abre al seleccionarla desde el flujo existente de páginas de extensión de Calradia Forge. Contiene texto de solo lectura localizado y un comando para cerrar; abrirla no ejecuta pruebas, no genera entidades ni cambia el estado del juego. Las comprobaciones estáticas y de XSD no demuestran que Bannerlord cargue el módulo ni validan comportamiento de juego o render Gauntlet en vivo.

Consulta la [procedencia del esquema](SCHEMA-PROVENANCE.es.md) para ver los archivos instalados y hashes usados al crear el ejemplo.
