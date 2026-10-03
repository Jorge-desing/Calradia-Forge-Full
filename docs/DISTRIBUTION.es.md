# Preparación de la distribución

`Directory.Build.props` es la fuente de la versión del producto, actualmente `25.2.0`. El pipeline canónico de empaquetado lee ese valor y rechaza una versión distinta. Una ejecución satisfactoria crea estos archivos locales en `artifacts/`:

- `CalradiaForge-Modules-25.2.0.zip`: el módulo principal junto con Examples, Price Provider, Price Consumer y Content Showcase. Contiene los ensamblados del módulo del juego y sus datos empaquetados, no la aplicación Desktop.
- `CalradiaForge-Desktop-25.2.0.zip`: el banco de trabajo WPF opcional, sus dependencias administradas, configuración de runtime, avisos, atribución y `Desktop/Run-CalradiaForge-Desktop.bat`. No incluye un `.exe` app-host y requiere el .NET 8 Windows Desktop Runtime en el equipo de destino. Su `README.md` raíz se copia de `docs/DESKTOP.md`.
- `CalradiaForge-Source-SDK-25.2.0.zip`: código fuente, ejemplos, pruebas, plantillas, localización, recursos, documentación, sitio DocFX generado y la referencia de ensamblado/XML del SDK .NET Framework dentro de `SDK/`. No incluye la salida compilada del módulo del juego ni la aplicación Desktop publicada.

Los archivos no empaquetan ensamblados de TaleWorlds, DLL del runtime Harmony, partidas guardadas, el `.venv` del repositorio ni paquetes Python de `site-packages`. Las comprobaciones de los archivos también rechazan entradas duplicadas o inseguras y scripts de desarrollo; la única excepción es el launcher de Desktop indicado arriba. El archivo Desktop se mantiene separado de la carpeta `Modules` de Bannerlord. Una auditoría o hash de paquetes no constituye un veredicto de antivirus, una validación de firma de código ni aceptación por parte de un servicio de alojamiento.

## Compilar y verificar archivos locales

Si todavía no preparaste el entorno Python del repositorio, ejecuta `tools\Setup-CalradiaForge-Python.bat --no-pause`. Después inicia el punto de entrada canónico de publicación desde cualquier directorio:

```bat
tools\Package-CalradiaForge.bat
```

El BAT delega en `tools/package.ps1`; no publica ni sube el resultado. Con las opciones predeterminadas, el pipeline restaura y compila la solución, ejecuta los BAT de Content Showcase y de la suite completa, compila la documentación y la ayuda del juego, valida los iconos y la estructura TPAC, comprueba el XML de los módulos, prepara los tres archivos y aplica verificaciones de entradas ZIP. La comprobación predeterminada del lector TPAC es estructural: no decodifica los píxeles de las texturas ni demuestra que el juego haya importado o renderizado el recurso. Si el pipeline termina correctamente, ejecuta la auditoría local de archivos y genera:

- `artifacts/package-audit-2520.json`, con metadatos de cada archivo, incluidos recuentos de entradas y valores SHA-256.
- `artifacts/package-sha256-2520.txt`, con los hashes SHA-256 de los tres archivos.

La auditoría comprueba el contenido y las raíces requeridos, rutas duplicadas o inseguras, integridad ZIP, versiones de los manifiestos de módulos, XML empaquetado, ausencia de ensamblados del juego y partidas guardadas, ausencia de ejecutables app-host y scripts no permitidos, archivos de la plantilla SDK y atribuciones de iconos requeridas. Estos informes describen los archivos locales exactos que se inspeccionaron. Compara los hashes del manifiesto con los archivos que vas a distribuir; ninguno de los informes demuestra que Bannerlord haya cargado el módulo ni que una plataforma externa haya aceptado una subida.

## Límites de distribución externa y runtime

Para Steam Workshop, consulta el [flujo oficial de publicación de TaleWorlds](https://moddocs.bannerlord.com/steam-workshop/uploading_updating_mod/). El pipeline local no exporta un módulo de cliente desde el Modding Kit, no crea ni actualiza un elemento de Workshop, no sube archivos ni verifica la instalación desde Workshop. Esos pasos y la comprobación de carga real en el juego son independientes de la creación, auditoría y generación de hashes de los ZIP.

Para Nexus u otro alojamiento, usa Modules para la instalación manual y ofrece Source-SDK y Desktop por separado cuando corresponda. El script de paquetes no sube archivos ni acepta condiciones de publicación. Decide la licencia, los créditos y los permisos de redistribución antes de publicar. El archivo fuente contiene borradores de documentación; su presencia no significa que se haya publicado una ficha.
