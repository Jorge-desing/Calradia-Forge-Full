# Revisión del framework de dependencias

Esta revisión define la ruta actual de extensibilidad de Calradia Forge. ForgeWeave es el framework principal para las nuevas herramientas cooperativas. Los diagnósticos de parches ofrecen una instantánea neutral propiedad de Forge; un observador reflectivo opcional busca la superficie pública de consulta esperada en un ensamblado ya cargado llamado `0Harmony`. Esa comprobación es una señal diagnóstica limitada del runtime, no una prueba de compatibilidad con Harmony. Harmony no es una dependencia de Forge ni el flujo recomendado para autores.

## ForgeWeave: el framework actual

ForgeWeave es un framework de eventos original e independiente, propiedad de Calradia Forge. Ejecuta manejadores de extensiones únicamente desde callbacks adaptadores de Forge que corresponden a puntos de ciclo de vida conocidos de Bannerlord. No descubre métodos arbitrarios, emite IL, reemplaza callbacks del juego, carga ensamblados ni requiere un framework de terceros.

El autor registra un `IForgeEventHandler` mediante `ForgeApi.Events`. Su suscripción declara un descriptor globalmente único, tipo de evento, prioridad, referencias opcionales `Before`/`After` para el mismo evento y prioridad, nivel de acceso, filtro escalar de coincidencia exacta, límite de fallos y, para `Pulse`, un intervalo mínimo. Forge expone únicamente datos escalares copiados; los manejadores no reciben objetos vivos de Bannerlord ni servicios de pruebas de Forge.

| Necesidad del autor | Comportamiento de ForgeWeave | Límite deliberado |
| --- | --- | --- |
| Ejecutar trabajo tras un punto de ciclo de vida conocido | Despacha eventos explícitos de Forge listo, pantalla, contexto, campaña, misión, agente y pulso acotado | No intercepta métodos arbitrarios |
| Coordinar manejadores creados independientemente | Calcula de forma determinista el orden por prioridad y las relaciones `Before`/`After` de igual prioridad | Bloquea restricciones ausentes, entre eventos o prioridades, autorreferentes y cíclicas en vez de inferirlas |
| Restringir cambios de estado | Aplica contexto del descriptor, modo de prueba y salvaguardas de copia de campaña a manejadores con escritura | No concede una capacidad general sobre objetos o servicios del juego |
| Evitar que una extensión defectuosa oculte las demás | Aísla excepciones, registra fallos y pone en cuarentena los manejadores al alcanzar el límite declarado | No puede terminar por la fuerza código C# síncrono arbitrario |
| Investigar comportamiento de extensiones | Captura salud copiada de manejadores, tiempos, recuentos de eventos, hallazgos y un diario acotado de despachos en el juego, el complemento de escritorio y reportes JSON/HTML | No atribuye automáticamente el rendimiento de todo el juego ni un fallo de terceros a una extensión |
| Verificar de nuevo un evento de ciclo de vida conocido | Reproduce un evento retenido y copiado solo tras consentimiento del manejador y comprobaciones de contexto exacto | No inyecta cargas arbitrarias, intercepta métodos ni elude compuertas de escritura |

Estas son propiedades operativas concretas para herramientas cooperativas. No afirman que ForgeWeave sustituya una biblioteca de parches para todos los casos. Quien necesite alterar un método arbitrario de terceros requiere un diseño de integración independiente y validación para ese método.

## Seguridad y modelo de ejecución

El adaptador de Bannerlord encola observaciones escalares del ciclo de vida y procesa una cantidad acotada en el hilo del juego. La cola, los datos copiados, las referencias de orden, el diario y los hallazgos tienen capacidad limitada. Una suscripción `Pulse` debe declarar un intervalo de 250 a 60.000 milisegundos; tanto el pulso del host de Forge como el intervalo individual limitan las entregas.

ForgeWeave comprueba contexto y acceso antes de invocar un manejador. `Observe` y el trabajo de diagnóstico pueden ejecutarse en sus contextos válidos declarados. `CampaignWrite` y `MissionWrite` usan las compuertas existentes de modo de prueba y copia de campaña. `StopPropagation` termina la entrega a los manejadores posteriores de ForgeWeave para ese evento; nunca cancela, reemplaza ni bloquea el callback original de Bannerlord.

Replay Lab conserva solo evidencia completa de eventos del host: secuencia, tipo, contexto lógico, tiempo, carga escalar copiada y resultado original. `Disabled` es el modo predeterminado. `ObserveOnly` es para manejadores de solo lectura; `Live` sigue sujeto a las mismas comprobaciones de acceso, modo de prueba y copia de campaña que una primera entrega. Una reproducción recibe su propia secuencia y nombra la fuente mediante `ForgeEvent.SourceSequence`; Forge nunca registra una reproducción como fuente nueva. Los registros ausentes o caducados, el contexto incompatible, la falta de un manejador autorizado y los rechazos de compuertas de escritura son resultados explícitos, no reintentos implícitos.

La acción de protocolo `framework` devuelve una copia de `ForgeWeaveSnapshot`, `event-journal` devuelve despachos recientes acotados y `replay` acepta una secuencia retenida en un juego conectado. El registro, la tubería y la selección de filas de escritorio no aceptan eventos arbitrarios. La acción Framework del juego y la sección Framework de escritorio consumen los mismos datos, por lo que un autor puede inspeccionar el orden declarado, restricciones bloqueadas, fallos, cuarentena, evidencia de reproducción y tiempos sin añadir una dependencia de diagnóstico. Consulta [FORGEWEAVE.es.md](FORGEWEAVE.es.md) y [SDK.es.md](SDK.es.md) para la API y sus límites.

## Rutas existentes del SDK

El registro heredado del SDK v1 aún admite pruebas, comandos y proveedores de diagnóstico. Las bibliotecas compartidas proporcionan servicios tipados y versionados con comprobaciones de propiedad y ciclo de vida. Los filtros y manejadores ForgeWeave son aditivos desde SDK v3: no cambian esos contratos existentes y sus IDs comparten el espacio global de identificadores del SDK.

Patch Blueprint Preflight sigue siendo una ayuda de autoría independiente y de solo lectura. `IPatchBlueprintProvider` proporciona declaraciones inertes con metadatos exactos de ensamblado, tipo, miembro y parámetros. Forge solo las resuelve contra ensamblados ya cargados, informa problemas estructurales y nunca aplica una declaración, llama a su callback, infiere el orden final de ejecución ni cambia un parche. Así, el preflight es útil sin convertir ForgeWeave en un motor de parches. Consulta [PATCH_BLUEPRINTS.es.md](PATCH_BLUEPRINTS.es.md).

## Diagnósticos de parches y observación externa opcional

`ForgePatchDiagnostics` devuelve un `ForgePatchDiagnosticsSnapshot` acotado con hooks y registros de reemplazo de métodos propiedad de Forge, recuentos y notas. La acción de protocolo `patch-diagnostics` es el punto de entrada actual de solo lectura. El observador externo opcional busca en los ensamblados cargados proporcionados la identidad esperada `0Harmony`, resuelve `HarmonyLib.Harmony` desde ese mismo ensamblado y comprueba que exista la consulta pública estática `GetAllPatchedMethods()` con retorno `IEnumerable` y la consulta `GetPatchInfo(MethodBase)` con retorno de valor. Esta superficie de consulta esperada no promete compatibilidad con una versión de Harmony ni con una combinación de mods. El observador no carga ni distribuye Harmony y no crea una dependencia en compilación. Las observaciones externas incluyen identidad limitada del destino, propietarios, categorías de parches, metadatos de orden declarados e identidad de métodos de parche. Son evidencia para revisión: compartir un destino no demuestra un conflicto y una cadena de propietario no equivale automáticamente al ID de un módulo Bannerlord.

El observador es opcional y de solo lectura: nunca aplica, retira ni reordena código de terceros. No puede ver otros backends de parches ni demostrar compatibilidad o coexistencia. Si falta el ensamblado, el tipo o la superficie de consulta esperada, Forge informa de esa observación sin cambiar su propio comportamiento. Esta señal diagnóstica está acotada al enumerar ensamblados/destinos y al copiar la salida; las consultas externas y los getters públicos siguen ejecutándose sincrónicamente dentro del proceso y no están aislados. La evidencia del Atlas nativo 0.4.0 en [VALIDACION-25.2.0.es.md](VALIDACION-25.2.0.es.md) es histórica y no valida ForgeWeave.

## Calidad y límites de validación

ForgeWeave debe evaluarse con comprobaciones reproducibles de registro, despacho ordenado, restricciones inválidas, compuertas de acceso/contexto, datos copiados, límites de pulso, propagación, aislamiento de excepciones, cuarentena, telemetría, reproducción autorizada, contexto incompatible, rechazo de compuertas de escritura, prevención de bucles y serialización de reportes. Una sesión nativa breve puede verificar que el host anuncia `framework`, `event-journal` y `replay`, devuelve una instantánea, reproduce el ejemplo `ForgeReady` de solo lectura, abre el panel y termina normalmente. No demuestra comportamiento de campaña, misión, compatibilidad ni rendimiento.

Esta revisión no establece novedad, superioridad universal ni reemplazo total. El valor de ForgeWeave reside en su límite de ciclo de vida declarado, planificación cooperativa determinista, permisos, contención y diagnósticos compartidos. La compatibilidad con una combinación concreta de mods requiere evidencia propia.

## Referencias históricas

- [API de consulta y parches de Harmony](https://harmony.pardeike.net/api/HarmonyLib.Harmony.html)
- [Metadatos de parches](https://harmony.pardeike.net/api/HarmonyLib.Patches.html)
- [Metadatos de cada parche](https://harmony.pardeike.net/api/HarmonyLib.Patch.html)

Estas referencias oficiales aportan contexto sobre las API de consulta y metadatos de parches de Harmony y sobre el Atlas histórico. El observador opcional actual se prueba con un fixture aislado que ofrece la identidad esperada `0Harmony` y una superficie pública de consulta; esto no verifica una versión instalada de Harmony, una combinación concreta de mods ni la coexistencia en runtime. El adaptador sigue siendo opcional y no crea una dependencia de Forge.
