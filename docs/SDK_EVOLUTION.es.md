# Evolución del SDK e inicio para desarrolladores

La ruta principiante de Calradia Forge genera archivos nativos antes de iniciar el
juego. La ruta avanzada ofrece eventos cooperativos y hooks solicitados explícitamente.
Estas capacidades tienen contratos de ciclo de vida y seguridad diferentes.

## Matriz de capacidades y procedencia

La auditoría inicial del rumbo usó el HEAD histórico
`59789ad04db7f6ff6c7864f5a6a11d190ff4e406`, con contrato SDK 12. El trabajo
independiente de hooks/API 13, incluidos Finalizer/Transpiler y las revisiones
protegidas 108–110, se confirmó después en el HEAD
`d8c0def6ab3604ded8600bacd81db50cd581bd32`. Ahora es la referencia estable y no se
atribuye a este objetivo de onboarding. Este objetivo conserva la versión de producto
25.2.0 y no modifica `ForgeApi.Version`.

| Capacidad | Fuente | Límite de evidencia |
| --- | --- | --- |
| ForgeWeave | `src/CalradiaForge.Core/ForgeWeaveEngine.cs` | Despacho cooperativo de eventos del adaptador; no intercepta cualquier método. |
| Hooks Prefix/Postfix | `src/CalradiaForge.Core/ForgeHookService.cs` | Presentes en HEAD inicial; registro/aplicación explícitos y controles del host. No se certifican modificaciones nativas concurrentes. |
| Finalizer/Transpiler | API 13 en HEAD comprometido `d8c0def` | Trabajo independiente de hooks que ya forma parte de la referencia estable; este objetivo de onboarding no lo implementa ni lo valida por separado. |
| Reemplazo completo de métodos | `src/CalradiaForge.Sdk/ForgeDetour.cs` | Escritor nativo experimental; los fixtures seriales no prueban escrituras concurrentes seguras. |
| `ForgePatchDiagnostics` | `src/CalradiaForge.Core/ForgePatchDiagnostics.cs`, `Models.cs`, `ExternalPatchRuntimeInspector.cs` | Registros acotados propiedad de Forge y una señal diagnóstica acotada opcional cuando un ensamblado `0Harmony` ya cargado expone la superficie pública estática de consulta esperada `HarmonyLib.Harmony.GetAllPatchedMethods()` y `GetPatchInfo(MethodBase)`. No es una prueba de compatibilidad con una versión o combinación de mods. Sin dependencia de compilación ni distribución de Harmony; los destinos compartidos son señales para revisión, no un veredicto de conflicto. |
| Builders de tropas/ítems | Builders SDK y `ForgeNoviceHub` | Generación XML nativa; la carga en el motor requiere otra comprobación. |
| Generación Gauntlet | `ForgeUiContracts` y `GauntletComposer` | Generación estática de prefab/ViewModel/localización; no reemplaza el renderer. |
| Hot reload de DLL | Sin runtime de módulos soportado | Pospuesto. El editor de ensamblados y catálogo simulado no demuestran recarga. |
| Árboles de comportamiento | Solo scaffold de combate | No hay runtime de árboles validado; primero se necesita un caso táctico reproducible. |

El [análisis interactivo](sdk-evolution-analysis.html) y la copia modificada de Descargas
se preservan por solicitud del usuario. Este HTML es material de referencia, no la fuente
técnica mantenida; la matriz respaldada por código de esta página gobierna las afirmaciones
actuales de capacidades.

## NuGet e inicio desde IDE

Preparar `CalradiaForge.Sdk` y la plantilla como paquetes NuGet locales versionados.
Usar el feed recién generado para las pruebas, sin depender de la caché global.
El módulo generado usa `net472`, declara su dependencia de Calradia Forge y obtiene
referencias licenciadas mediante un `GameBin` local explícito. Forge suministra el
SDK durante la ejecución: el consumidor no distribuye otro SDK ni DLL del juego.

La misma plantilla .NET se puede instalar para CLI, Visual Studio y Rider. La
generación y compilación CLI se verifican por separado de la integración visual en
los IDE. Véanse [plantillas Microsoft](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates)
y [plantillas Rider](https://www.jetbrains.com/help/rider/Install_custom_project_templates.html).
Crear paquetes locales no equivale a publicarlos en nuget.org. La publicación requiere
destino autorizado, credenciales y hashes de los paquetes comprobados.

Desde un checkout Git con el entorno Python del proyecto preparado:

```bat
tools\Pack-CalradiaForge-Developer-Templates.bat --no-pause
tools\Test-CalradiaForge-Developer-Onboarding.bat --no-pause
```

El launcher imprime un feed e informe únicos bajo `artifacts/sdk-evolution/onboarding/`.
La prueba vuelve a empaquetar en su propio feed, instala en una colmena aislada de
plantillas, genera un módulo temporal, restaura con caché aislada y compila con el
`GamePath` local. `--portable-only` verifica empaquetado/generación/restauración sin
afirmar compilación contra el juego; `--game-path "<Bannerlord root>"` selecciona
la instalación licenciada. El workflow portable usa ese modo y no publica paquetes.

Para instalar la plantilla comprobada en el entorno normal, usar su ruta real del
informe: `dotnet new install <CalradiaForge.Mod.Template.25.2.0.nupkg>`.
Crear el módulo con `dotnet new calradiaforge-mod -n MyForgeMod` y restaurar usando
el feed local indicado. Configurar `BANNERLORD_GAME_PATH` o la propiedad MSBuild
`GamePath` antes de compilar. No se supone que un feed público contenga esta versión.

El runner registra la instantánea del HEAD comprometido y los hashes del proyecto y
builders SDK superpuestos explícitamente, además del README incluido como asset. En el
informe final, API 13 procede del HEAD comprometido; este objetivo de onboarding no
agregó API pública SDK. El informe respalda la composición de fuentes y los paquetes
locales indicados, no un contrato público sin confirmar ni su publicación en un feed público.

## Contenido estático y ejemplo

Usar una sola implementación XML para los generadores principiantes y builders SDK.
Verificar componentes de ítems, referencias de equipo, IDs NPCCharacter y registros
del manifiesto contra Native de la instalación objetivo. Conservar los hashes de
fuentes en el informe sin distribuir las entradas XML propietarias.
El verificador ContentShowcase imprime los hashes instalados actuales de ambos XSD
y de `weapons.xml` de Native, usado para comparar el `horse_whip` del ejemplo. Los
hashes documentados siguen siendo la referencia observada al crear el ejemplo; una
diferencia local exige revisión, pero por sí sola no demuestra corrupción ni invalida
la comprobación.

El [ejemplo de contenido](../examples/CalradiaForge.ContentShowcase/README.es.md)
contiene una tropa, su ítem de equipo y una página estática. Su BAT aprobó determinismo,
esquemas instalados, referencias y compilación local.
Abrir la página no crea contenido dinámicamente ni ejecuta pruebas. Los recursos
generados deben ser deterministas y pasivos; las etiquetas localizadas se separan
de los IDs técnicos. Validar bindings antes de revisar el renderer real en un menú seguro.

## Interoperabilidad e investigación de runtime

`ForgePatchDiagnostics` devuelve registros acotados de hooks y reemplazos propiedad de
Forge, conteos, notas, frescura y una observación `ExternalRuntime`. Su observador
reflectivo opcional comprueba la superficie pública estática de consulta esperada
`HarmonyLib.Harmony.GetAllPatchedMethods()` y `GetPatchInfo(MethodBase)` en un ensamblado
ya cargado llamado `0Harmony`. Es una señal diagnóstica acotada del runtime, no una prueba
de compatibilidad con una versión o combinación de mods. El proyecto no referencia, carga
ni distribuye Harmony. Una observación externa no disponible, no soportada, incompleta o
vacía no dice nada sobre otros backends de parches. Los destinos compartidos son señales
para revisión, no prueba de conflicto. Forge no reordena ni revierte código de terceros.
Una identidad de método o etiqueta de propietario es evidencia, no autenticación.
Los errores de hooks/ForgeWeave se atribuyen solo en callbacks controlados por Forge;
una línea de fuente exacta requiere símbolos correspondientes. No se capturan todos
los cierres nativos.

### Migración desde los diagnósticos del Atlas de Harmony

La superficie de diagnósticos específica de Harmony se retira en favor de los diagnósticos
de parches propiedad de Forge: la superficie pública `HarmonyDiagnostics`, los DTO
específicos de Harmony, `SessionReport.Harmony` y la acción de protocolo `harmony` se
reemplazan por `ForgePatchDiagnostics`, `SessionReport.PatchDiagnostics` y
`patch-diagnostics`. El cambio afecta los contratos de Core/informes/protocolo:
`ForgeProtocol.Version` es 2 y `ForgeProtocol.EnvelopeVersion` permanece en 1. No cambia
el contrato del SDK ni modifica `ForgeApi.Version`.

Medir el recorrido por separado del trabajo seleccionado. ForgeTimeSlicer utiliza
IDs estables, pero ProcessBatch recorre toda la colección. Seleccionar una de 24
cubetas no elimina ese recorrido. Los benchmarks portables miden el arnés, no la
latencia por frame del juego. No se aceptan optimizaciones solo por ahorro teórico.

Investigar recarga explícita de datos/configuración escalar por separado de las
cachés de recursos y sustitución de ensamblados. .NET Framework descarga ensamblados
mediante su AppDomain, no individualmente; véase [carga de ensamblados Microsoft](https://learn.microsoft.com/en-us/dotnet/standard/assembly/load-unload).
No presentar entradas simuladas `cf.reload_prefabs` como comandos reales del juego.

`src/CalradiaForge.Sdk/ModSettings.cs` ofrece registro, lecturas de caché y resultados
explícitos de guardado; no expone recarga. Editar un archivo no prueba que un consumidor
existente reciba valores nuevos. Un prototipo posterior debería leer datos escalares
acotados, validar un candidato completo, intercambiarlo en el hilo propietario e
informar rechazos conservando el estado anterior. No recargaría entidades, ensamblados
ni cachés Gauntlet; es investigación, no una API entregada.

## Validación y transferencia de conocimientos

Ejecutar compilación/pruebas mediante BAT. Separar evidencia de fuentes, paquetes
y motor en vivo; CI portable no dispone de TaleWorlds licenciadas. Verificar contenido
de paquetes, manifiestos, determinismo y generación aislada de plantilla. Las medidas BAT
iniciales y los límites de evidencia están en Rev115 y Rev116; las correcciones posteriores,
comprobaciones actuales y transferencia de conocimientos se anexan hasta Rev129. El smoke local NuGet/plantilla
es distinto de la auditoría canónica y el manifiesto hash de los ZIP; no se afirma
publicación pública de ninguno de esos paquetes.

Enseñar procedimientos en `calradia-forge-dotnet` y sus referencias, enlazar la regla
de inicio desde las tres guías raíz y conservar instrucciones especializadas en la
skill existente. Preservar copias upstream, sincronizar invariantes y validar skills
con `tools/Validate-CalradiaForge-Skills.bat`.

Anexar revisiones protegidas a la cadena existente sin reescribir comprobaciones
históricas incompletas. Separar el commit del objetivo del índice inicial. Empaquetado
y verificación en el motor siguen siendo necesarios para sus respectivas afirmaciones.

### Evidencia registrada durante la implementación

- `tools\Validate-CalradiaForge-Onboarding-Knowledge.bat` aprobó paridad de instrucciones
  raíz, enlaces locales, Ruff y validación de nueve skills modificadas.
- Ejecución histórica inicial del fixture enfocado: `tests\CalradiaForge.PatchDiagnostics.Tests.bat --no-pause`
  aprobó 14 casos diagnósticos aislados con la implementación anterior del adaptador.
  Este runtime simulado no demuestra coexistencia Harmony en vivo; la cifra no es el
  resultado final de la implementación endurecida posterior.
- `tools\Run-CalradiaForge-Python-Checks.bat --ci --no-pause` aprobó Ruff, 20 casos de
  archivos/recursos, cinco de imágenes, cinco de auditoría y verificaciones de sprites fuente.
- `tests\CalradiaForge.AssetPipeline.Tests.bat --no-pause` aprobó 23/23 casos,
  incluida la validación de versión `v1.0.0`, placeholders y dependencia Forge de la
  plantilla fuente, y la versión actual del producto para manifests de módulos reales.
- La validación aislada de onboarding más reciente aprobó mediante
  `tools\Test-CalradiaForge-Developer-Onboarding.bat --no-pause`. La evidencia vigente es
  `artifacts/sdk-evolution/onboarding/20261002T033001Z-00c3d2e2/source-package-report.json`.
  Registra el HEAD del repositorio `d940b1ccfbcb88d82a264ccd0cf931fb80489071`, contrato SDK
  13 estable y de árbol de trabajo, `workingTreeApiChangesIncluded: false`, los marcos
  `net472;net8.0` y la versión de producto 25.2.0. El informe excluye de la instantánea
  del paquete el cambio pendiente de `src/CalradiaForge.Sdk/ForgeTimeSlicer.cs` en el árbol.
- Esta ejecución empaquetó `CalradiaForge.Sdk.25.2.0.nupkg` (SHA-256
  `bf39fdd302ae8b456697aaaef9f13b335fa766715247a2d271f0cf08dae4e0ed`) y
  `CalradiaForge.Mod.Template.25.2.0.nupkg` (SHA-256
  `5dd97b521c851889f3c0c2cbabdacb0df4cd5db5a8c01791d4954ce1c4e66521`). Son artefactos
  locales bajo `artifacts/sdk-evolution/onboarding/20261002T033001Z-00c3d2e2/feed/`;
  no se publicaron en un feed público. El informe registra el contrato SDK de HEAD,
  superposiciones del proyecto SDK y tres builders actuales, además del README SDK actual
  como asset. Sus hashes SHA-256 son: superposición del proyecto SDK
  `4e84aae7b49028ce23177ced3f49aeed2f39f437f23ba5ee17d600f781266223`,
  `ForgeTroopBuilder.cs` `21030d195be815452e5cf121756272b522adac34b28620f5bb22ff9de8ef3ebc`,
  `ForgeItemBuilder.cs` `14a69238d4223d219f691cd577dc47dfa292bfed2602f96b803ed7d11ee45265`,
  `ForgeNoviceHub.cs` `0f0d4cc5cae85548e662384f764821adb7e2f6346dbdb4d5f2ee1b25c40b8be1`
  y el README SDK incluido como asset `eaa3e654ccddc9cf34f169b83e1d614cda10139f35afb3a78fec7d1138498321`.
- La ejecución más reciente aprobó el empaquetado SDK y de la plantilla, su instalación
  en una colmena aislada, la generación del módulo, la validación del manifiesto y la
  dependencia y la restauración desde el feed SDK local producido. El módulo generado
  compiló contra el GameBin local licenciado para `net472`, con 0 advertencias y 0 errores.
  La restauración seleccionó la DLL SDK como recurso de compilación y no incluyó una DLL
  SDK de runtime; la salida tampoco contenía DLL SDK ni DLL TaleWorlds. El empaquetado de
  la plantilla usó solo entradas fuente copiadas a la instantánea ignorada de cada
  ejecución, no el `bin/obj` de la plantilla compartida.
- El informe del mismo día `20261002T003054Z-ecaf86a0` se conserva como evidencia
  histórica. Registra el mismo HEAD del repositorio y los mismos contratos SDK, pero es
  anterior a la exclusión del cambio pendiente de `ForgeTimeSlicer.cs` en el árbol. Sus
  hashes de paquete fueron SDK
  `05037336dec31a54773f4d51b3c8c6edc1e4e0ff1f1a86753b7ae9dad9859c9e` y plantilla
  `aa57e9d4cc0d9ac56172fc5e13e469fe0ce2b5fc9ff2e1849a581237a6af87b4`, bajo
  `artifacts/sdk-evolution/onboarding/20261002T003054Z-ecaf86a0/feed/`. Sus hashes de
  procedencia fueron superposición del proyecto SDK
  `4e84aae7b49028ce23177ced3f49aeed2f39f437f23ba5ee17d600f781266223`,
  `ForgeTroopBuilder.cs` `21030d195be815452e5cf121756272b522adac34b28620f5bb22ff9de8ef3ebc`,
  `ForgeItemBuilder.cs` `14a69238d4223d219f691cd577dc47dfa292bfed2602f96b803ed7d11ee45265`,
  `ForgeNoviceHub.cs` `9fb58a0b86657e6ccc3d57371e7c8c7146c04a7a8ff6d7a6d77934cb8e2884a2`
  y README SDK incluido como asset `eaa3e654ccddc9cf34f169b83e1d614cda10139f35afb3a78fec7d1138498321`.
- El informe de contrato 13 anterior `20261001T184300Z-5a8bfd73` se conserva como
  evidencia histórica. Registra el HEAD confirmado `d8c0def6ab3604ded8600bacd81db50cd581bd32`;
  el trabajo de hooks del contrato 13 ya estaba confirmado de forma independiente y el
  objetivo de onboarding no cambió esa API. Sus hashes de paquete fueron SDK
  `78c6362f07f9a6049a63b05febc375904f650530c0ac02c397c12f823e81307e` y plantilla
  `95d3f897362504fc01d63403f343d197ad827208e4a8d981487ac193c61b11f7`, bajo
  `artifacts/sdk-evolution/onboarding/20261001T184300Z-5a8bfd73/feed/`. Sus hashes de
  procedencia fueron superposición del proyecto SDK
  `4e84aae7b49028ce23177ced3f49aeed2f39f437f23ba5ee17d600f781266223`,
  `ForgeTroopBuilder.cs` `21030d195be815452e5cf121756272b522adac34b28620f5bb22ff9de8ef3ebc`,
  `ForgeItemBuilder.cs` `d06d5d99893eae3164592c08cae42813fbe593ac8356971f152f553809ddabfa`,
  `ForgeNoviceHub.cs` `738dc197daa15080efd25639cb790c24687d0d94ba37c33446c2fce10e3df17e`
  y README SDK incluido como asset `eaa3e654ccddc9cf34f169b83e1d614cda10139f35afb3a78fec7d1138498321`.
  El informe más reciente lo sustituye para la procedencia actual del paquete. Aprobar
  este smoke de CLI/paquetes no verifica la integración visual de Visual Studio o Rider,
  la carga en Bannerlord ni el comportamiento dentro del juego.
- El informe previo de contrato 12 `20261001T182942Z-385d88e6` y las ejecuciones
  anteriores `20261001T181729Z-7da8eac8` / `20261001T181755Z-35fc8d32` se conservan como
  evidencia histórica; el informe aislado de contrato 13 indicado arriba los sustituye
  para la procedencia actual del paquete. Aprobar el smoke de CLI/paquetes no verifica
  la integración visual de Visual Studio o Rider, la carga en Bannerlord ni el
  comportamiento dentro del juego.
- `tools\Append-CalradiaForge-Improvement-Record.bat --verify` verificó 113 registros
  existentes. Esto no significa que se haya anexado la revisión de este objetivo.

- `tools\Test-CalradiaForge-ContentShowcase.bat --no-pause` aprobó generación
  determinista, XSD Items/NPCCharacters, manifiesto, mesh/referencias y página estática,
  además de compilación limpia `net472`, Core 405/405 y ForgeWeave 73/73.
  Evidencia ignorada: `artifacts/ContentShowcase-20261001-rerun1.log`.
- Las medianas de cinco muestras fueron 1,945 ms (128 entidades × 128 repeticiones),
  0,9962 ms (2.048 × 8) y 0,2476 ms (16.384 × 1), con cero asignaciones medidas
  del hilo síncrono del fixture. Las repeticiones difieren: son medidas del arnés,
  no mejoras comparativas ni tiempos de frames Bannerlord. No se optimizó a partir de ellas.

- El pipeline BAT integrado aprobó Core 405/405, ForgeWeave 73/73, Desktop 65/65
  y 294 casos de render WPF con 182 pases de layout. La repetición registró 17.341 ms
  en el arnés de render, no latencia de la aplicación. Evidencia:
  `artifacts/sdk-evolution/package-final.log`. La auditoría de archivos rechazó después
  la versión independiente del módulo consumidor de la plantilla; la auditoría corregida
  sigue siendo un gate separado y estas suites aprobadas no certifican los archivos.
- El BAT aislado de diagnóstico Harmony aprobó 11/11 contra la fuente combinada;
  el orden determinista de propietarios y los cuatro tipos de patch se cubren por separado
  en la regresión Core integrada. El BAT de aceptación stateless aprobó los cuatro gates.

Archivos finales, commit acotado, interfaz de los IDE y carga/render nativos siguen
siendo comprobaciones independientes.

### Seguimiento de endurecimiento de seguridad

- Seguimiento histórico de endurecimiento de seguridad: el BAT aislado
  `tests\CalradiaForge.PatchDiagnostics.Tests.bat --no-pause` aprobó 21/21 al restringir
  el adaptador opcional a la identidad del ensamblado
  `0Harmony` suministrado, miembros públicos y firmas exactas de consulta. El fixture
  simulado verifica que no se invoquen ensamblados señuelo, miembros privados ni
  sobrecargas inválidas; no demuestra coexistencia con Harmony en vivo.
- El adaptador distingue `NotRequested` de un escaneo completo `NotLoaded`. Un escaneo
  parcial queda como `Incomplete`. Los límites de enumeración/salida no limitan CPU,
  asignaciones ni efectos internos de llamadas externas síncronas y getters públicos;
  se ejecutan dentro del proceso y no están en un sandbox.

### Evidencia final de validación del objetivo — 01/10/2026

- La ejecución enfocada final de `tests\CalradiaForge.PatchDiagnostics.Tests.bat --no-pause`
  aprobó 27/27 casos. Las ejecuciones históricas previas de 14 casos (fixture inicial)
  y 21 casos (seguimiento de endurecimiento) describen estados separados del árbol de
  trabajo; no son conteos acumulativos ni sustituyen esta ejecución final.
- La ejecución integrada final `tools\Run-CalradiaForge-Tests.bat --no-pause` terminó
  con compilaciones `net472` y `net8.0` limpias (0 advertencias, 0 errores), Core 403/403,
  ForgeWeave 73/73, Desktop 65/65 y 295 casos de render WPF con 308 llamadas de layout/render.
  Los conteos y tiempos del arnés no son latencia observada de la aplicación.
- `tools\Test-CalradiaForge-Developer-Onboarding.bat --no-pause` y
  `tools\Test-CalradiaForge-ContentShowcase.bat --no-pause` aprobaron sus flujos locales
  de plantilla y muestra estática. Estas comprobaciones no demuestran integración visual
  con Visual Studio/Rider, publicación pública de paquetes ni carga/render en Bannerlord.
- Los límites de salida y enumeración acotan los registros diagnósticos producidos por
  Forge, pero no pueden imponer un límite estricto al coste de runtime o las asignaciones
  de la reflexión en proceso, `MethodBase.GetParameters()` ni getters/enumeradores externos
  síncronos. Esas llamadas no están en un sandbox. Para esta evidencia no se usó un runtime
  Harmony en vivo, sesión Bannerlord, campaña ni batalla.

### Seguimiento de conocimiento verificado y contratos del showcase — 02-10-2026

- El generador del showcase comprueba ahora que los bindings `@Property` del prefab tengan miembros `[DataSourceProperty]` correspondientes y que las claves de localización usadas por el ViewModel generado y el XML nativo de contenido existan en los catálogos de inglés y español. Las regresiones negativas cubren una anotación ausente y claves faltantes en cualquiera de los dos idiomas. Es una comprobación estática; no demuestra carga ni render Gauntlet en Bannerlord.
- La guía de depuración y simulación ahora distingue afinidad al hilo del motor de las afirmaciones generales de corrupción de memoria, marca los ejemplos de simulación como ilustrativos y exige verificar los claims de runtime contra ensamblados locales y un caso reproducible. La guía de time-slicing se ajustó al hash determinista por ID, los buckets desiguales y el recorrido completo que realiza `ForgeTimeSlicer`. Los conteos de héroes y afirmaciones de caídas de frames sin medir quedan identificados como hipótesis.
- El BAT del showcase aprobó sus regresiones de bindings/localización, la compilación limpia del módulo `net472`, Core 411/411 y ForgeWeave 73/73. La validación de las skills de depuración y simulación actualizadas también aprobó. Son comprobaciones de fuente y fixtures; el render dentro de Bannerlord sigue sin verificarse.
- La validación canónica de distribución e integridad se repite después de este apéndice. No se inicia Bannerlord ni Modding Kit.

### Corrección de evidencia — diagnóstico y tiempos de campaña — 02-10-2026

- Los archivos `.cfcrash` de Forge son informes JSON de texto. El formateador ahora usa la referencia existente de Newtonsoft.Json para conservar como JSON válido las rutas Windows, comillas, saltos de línea, tabulaciones y otros caracteres de control. El BAT Core ahora ejecuta las regresiones del gate de flanco F10 y de serialización; esto prueba el formateador, no F10 ni Bannerlord en vivo.
- Se conservan los informes históricos de validación 25.0.0–25.2.0 como registros de sus ejecuciones. Sus frases «time-slicing modulo-24 anti-lag» y «rutas calientes con cero asignaciones GC» no prueban que se midiera el callback de campaña completo. El arnés sintético `ProcessBatch` no mide el recorrido de `Hero.AllAliveHeroes`, las asignaciones del callback de campaña ni la latencia dentro del juego.
- La documentación de agentes ahora coincide con las herramientas configuradas para `BugHunterAgent` y el flag no interactivo del BAT Core. La descripción del análisis de fallos se limita al texto de excepción `.cfcrash` y al manejo de metadatos de `.dmp`/`.sav`.
- El seguimiento bilingüe protegido está en [Rev125](CalradiaForge-Registro-Mejoras-Rev125.docx); su entrada de integridad enlaza desde Rev124 sin reescribir registros anteriores.

### Seguimiento de conocimiento respaldado por fuentes — 02-10-2026

- `ForgeAgentMemory` es un almacén C# acotado para agentes dentro del juego; sus niveles semántico, episódico y procedimental son conceptualmente análogos a partes de CoALA, pero no implementan el marco completo de agentes de lenguaje. `CoALAAgentMemory` de la orquestación Python es una API separada. El inspector Desktop usa perfiles de muestra y los etiqueta como tales; los límites actuales del SDK son 2.048 IDs de agentes, 128 entradas semánticas por agente, 512 episodios (128 por tipo) y 128 entradas procedimentales por agente.
- `ForgeNoviceHub.GenerateCombatAiComponentScaffold` ahora rechaza identificadores C# inválidos o reservados y escapa comillas, barras inversas, controles y unidades sustitutas Unicode en los literales de sonido generados. Las regresiones cubren esos límites de entrada. El código generado todavía debe compilarse contra las referencias licenciadas del juego y revisarse en un entorno autorizado antes de afirmar que funciona como integración de combate.
- `ForgeTradeSimulator` sigue siendo una calculadora ilustrativa basada en fórmulas, no un modelo de mercado/taller conectado al motor. `ForgeTimeSlicer` selecciona el trabajo elegible, pero recorre la colección de origen; no se ha medido el callback completo de campaña ni se ha establecido un beneficio de latencia o asignaciones dentro del juego.
- Las guías de interfaz presentan dashboards, datos de muestra, dossiers, lemas y prominencia de Split Deck como opciones según la tarea. La recarga Gauntlet con `Ctrl + ~` / `ui.toggle_debug_mode` sigue siendo un reporte no verificado y dependiente de la versión, no una pasarela admitida del proyecto.
- Un `SyncData` vacío del comportamiento solo demuestra que no añade campos serializados propios; no garantiza compatibilidad de todo el guardado ni del conjunto de mods. La regresión estática, el arnés y la evidencia en Bannerlord en vivo se mantienen separados.

### Corrección del estado diagnóstico y de la telemetría de memoria acotada — 02-10-2026

- `ExternalPatchRuntimeInspector` informa `Truncated` solo cuando un límite configurado de salida o recopilación recorta resultados. Una consulta fallida, un iterador con errores o un ensamblado omitido puede dejar el informe `Incomplete` sin afirmar que la salida recopilada se truncó. La suite BAT aislada de regresión aprobó 28/28; los fixtures no prueban el comportamiento de cualquier runtime externo cargado.
- `AgentCognitiveMemoryBehavior` aumenta los contadores de telemetría semántica y episódica solo cuando el almacén acotado del SDK acepta la escritura. Una regresión llena la cuota global de agentes, verifica los rechazos y confirma que esos intentos no aumentan los contadores. La suite Core integrada aprobó 415/415.
- `ForgeTimeSlicer` asigna IDs nulos y vacíos al bucket cero. Los llamadores deben proporcionar IDs estables y no vacíos para obtener una distribución determinista útil; el helper no evita recorrer la colección de origen. Las pruebas cubren `GetBucket` y `ShouldProcess`; no se cambió el comportamiento ni se afirma una mejora de rendimiento.
- El inspector de memoria de agentes Desktop es una muestra ilustrativa inspirada en CoALA, no el registro C# del SDK ni un snapshot de memoria en vivo. Las pruebas de render ahora exigen el estado de evidencia `Sample` y el texto explícito de muestra, en lugar de la expectativa obsoleta de runtime verificado.
- Evidencia BAT integrada en este árbol de trabajo: compilaciones `net472`/`net8.0`/Desktop limpias, Core 415/415, Patch Diagnostics 28/28, ForgeWeave 73/73, Desktop 65/65 y 295 casos WPF con 308 pases de layout/render. Los tiempos son del arnés. No se verificaron proceso Bannerlord, campaña, batalla, runtime de terceros, extensión de IDE ni publicación pública de NuGet.

### Evidencia final del objetivo — 02-10-2026 (Registro Rev127)

- La ejecución final de `tools\Run-CalradiaForge-Tests.bat --no-pause` aprobó con compilaciones limpias `net472`, `net8.0` y Desktop (cero advertencias/errores), Core 415/415, Patch Diagnostics 30/30, ForgeWeave 73/73, Desktop 65/65 y 295 casos WPF con 320 pases de layout/render. Los 20.903 ms corresponden solo al arnés.
- `tools\Verify-CalradiaForge-StatelessBehavior.bat` aprobó 4/4 verificaciones. `tools\Run-CalradiaForge-Python-Checks.bat --ci --no-pause` aprobó Ruff 0.16.9, 25 pruebas de assets/archivos, cinco de imágenes, 18 casos de auditoría de agentes, 15 del orquestador offline, tres del launcher CLI y comprobaciones Gauntlet/sprites. La ejecución `--ledger` verificó 42/42 pares EN/ES.
- Los 29 directorios de skills modificados aprobaron `quick_validate.py` mediante el launcher mantenido. El BAT de conocimientos de onboarding validó paridad/enlaces de las guías comunes, Ruff y nueve skills. El ledger protegido estaba verificado hasta 126 registros antes del nuevo append; Rev127 se anexa y verifica por separado.
- La prueba aislada de onboarding `20261003T012628Z-f3bebd08` empaquetó e instaló en un hive aislado los paquetes 25.2.0 del SDK y la plantilla, generó un módulo consumidor, lo restauró desde el feed local y compiló `net472` contra referencias licenciadas del GameBin local sin advertencias/errores. El informe indica contrato SDK estable 13 y `workingTreeApiChangesIncluded: false`; no es una publicación pública ni una prueba dentro del juego. Hashes: SDK `2f4aa6863129bd8ea67ae6e7b8b71de643975d962c0e007e02f38ece5f98718c`; plantilla `f9b3a74577e9ae5b9caa7134b37dab8214111396a876fad8e57fd499e2115a64`.
- El BAT del showcase aprobó la generación determinista, las comprobaciones de esquema Native Items/NPCCharacters, el manifiesto/referencias y una compilación limpia `net472` del módulo generado. Estas comprobaciones estáticas no prueban el render Gauntlet ni la carga en Bannerlord.
- No se inició el juego ni Modding Kit. La carga/render en Bannerlord, la coexistencia real con Harmony, la publicación pública de NuGet y la integración con mercados de Visual Studio/Rider siguen sin verificar. Este objetivo no afirma una mejora de rendimiento en runtime.

### Conciliación de evidencia conservada — 02-10-2026 (Registro Rev128)

- El último log integrado conservado, `artifacts/sdk-evolution/post-final-reviewed-20261002.log`, registra Core 410/410, Patch Diagnostics 28/28, ForgeWeave 73/73, Desktop 65/65 y 295 casos WPF con 308 pases de layout/render. La duración de render registrada es 24.476 ms del arnés.
- Rev127 informó Core 415/415, Patch Diagnostics 30/30, 320 pases y 20.903 ms. No se encontró un log retenido que reproduzca ese conjunto exacto, así que esos valores quedan como reportados y no verificables independientemente desde este checkout. La falta de un log coincidente no demuestra que la ejecución no ocurriera.
- El benchmark `ProcessBatch` es sintético y no mide un callback de campaña completo ni latencia en Bannerlord.
- El validador del showcase ahora rechaza rutas de idioma incorrectas o que escapan del directorio, archivos de cadenas ausentes, botones de cierre sin foco y bindings de rótulo incorrectos. Su regresión BAT valida contratos de fuente, esquemas, salida determinista y la compilación del módulo generado; no demuestra render Gauntlet en vivo.

### Validación final del objetivo — 02-10-2026 (Registro Rev129)

- La última ejecución BAT integrada aprobó compilaciones limpias `net472`, `net8.0` y Desktop, sin advertencias/errores; Core 416/416, ForgeWeave 73/73, Desktop 65/65 y 295 casos WPF con 320 pases de layout/render. Sus 19.288 ms son del arnés, no latencia de la aplicación abierta.
- El BAT dedicado de Patch Diagnostics aprobó 30/30. La prueba de onboarding generó, restauró y compiló un consumidor `net472` temporal desde paquetes locales aislados de SDK/plantilla, sin advertencias y con referencias licenciadas de GameBin. Pasaron el showcase estático y la aceptación de comportamiento stateless; también validaron las 29 skills modificadas y la auditoría de conocimiento de las guías comunes.
- El perfil base de Python `--ci` pasó; `--ledger` verificó 42/42 pares de documentos técnicos mantenidos en inglés y español. Este apéndice bilingüe se registra aparte en el historial protegido. Se intentó instalar las dependencias opcionales de Antigravity, pero pip falló con `InvalidChunkLength`; el perfil que requiere esas dependencias queda sin verificar. No se inició Bannerlord ni Modding Kit, y el analizador TPAC profundo indicó que faltaba su biblioteca/fixture local.
- La inspección de Harmony sigue siendo opcional y de solo lectura en el límite de Forge: consulta mediante reflexión un `0Harmony` compatible que ya esté cargado y nunca carga ni modifica Harmony. Las llamadas externas síncronas no están en sandbox ni limitadas internamente en tiempo; los fixtures no prueban compatibilidad con mods reales de terceros. El producto permanece en 25.2.0 y SDK API 13; no se afirma publicación pública ni integración con mercados de IDE.
