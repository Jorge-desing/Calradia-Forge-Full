# Calradia Forge 19.0.0 — validación breve

La validación tiene un alcance deliberadamente acotado. Prueba el banco de trabajo Desktop y el empaquetado sin cargar una campaña ni una batalla, modificar una partida guardada o recopilar una muestra de rendimiento prolongada.

## Desktop

- `CalradiaForge.Desktop.RenderTests`: 190 rutas explícitas de War Table, 20 rutas representativas en War Table/Parchment Light/High Contrast y escalas de 100/125/150/200%, además de 13 idiomas × 4 factores de escala, mediante la instanciación real de plantillas WPF y un binding de solo lectura del resultado sin procesar.
- La shell utiliza un catálogo explícito y `ContentControl`; al seleccionar una página crea un `WorkbenchPageViewModel`, libera la página anterior y conserva únicamente DTO de evidencia.
- El rail izquierdo presenta el catálogo mediante secciones `ToolGroupViewModel` ordenadas y plegables, con recuentos, glifos semánticos, conservación del estado expandido, desplazamiento independiente y áreas de visualización separadas y acotadas para elementos fijados y recientes.
- Los comandos de teclado están enlazados en XAML (`Ctrl+K`, `Ctrl+F`, `Ctrl+Enter`, `Esc`). Los favoritos y las herramientas recientes están acotados en la shell; no se recorre el árbol visual.
- Las preferencias de tema e idioma usan reemplazo atómico en la carpeta de datos del usuario. Los valores desconocidos o malformados recurren a War Table y al inglés.
- CommunityToolkit.Mvvm 8.4.2 proporciona la capa de compatibilidad observable y de comandos; MaterialDesignThemes 5.3.2, WPF UI 4.3.0 y MahApps.Metro.IconPacks 6.2.1 se instancian en la superficie de presentación WPF y se registran en `THIRD_PARTY_NOTICES.md`.

## Suite automatizada

La compilación breve ejecuta las comprobaciones de Core, ForgeWeave, estructura de Desktop, renderizado WPF, recursos nativos y archivos empaquetados. Las entradas de analizador ausentes o no compatibles se informan como **Not run** o **Unsupported**. La suite no afirma un resultado de juego nativo cuando la superficie de Steam no está disponible.

## Empaquetado

La distribución contiene exactamente:

- `CalradiaForge-Modules-19.0.0.zip`
- `CalradiaForge-Source-SDK-19.0.0.zip`
- `CalradiaForge-Desktop-19.0.0.zip`

Desktop incluye `Run-CalradiaForge-Desktop.bat` y no incluye un ejecutable app-host. La auditoría del archivo también exige los ensamblados publicados de CommunityToolkit.Mvvm, MaterialDesignThemes.Wpf, Wpf.Ui y MahApps.Metro.IconPacks. Se auditan manifiestos, ensamblados, XML, hashes, rutas dentro del archivo, ausencia de partidas guardadas y exclusión de DLL del juego.

## Límite de validación nativa

`Test-BannerlordPreflight.ps1` comprueba la instalación local de Steam y la versión del módulo implementado. `launch_bannerlord.ps1` usa `steam://rungameid/261550` únicamente después de la comprobación previa. Si la superficie disponible de Computer Use no puede mostrar una ventana nativa, la inspección de Framework, la reproducción de `ForgeReady` y la negociación de capacidades quedan pendientes en vez de informarse como aprobadas.

Para esta validación de versión no se ejecutó ninguna campaña, batalla, prueba de resistencia ni sondeo nativo prolongado.


