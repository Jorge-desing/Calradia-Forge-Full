# Rev124 — Conocimiento verificado y contratos del showcase

**Fecha:** 02-10-2026  
**Versión del producto:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Verificación estática del showcase de contenido, guías de ingeniería y documentación de evolución del SDK.

## Evidencia y corrección

Una revisión de fuente y skills encontró que el showcase estático comprobaba la paridad de los catálogos, pero no demostraba que cada binding del prefab generado tuviera una propiedad Gauntlet correspondiente ni que todas las claves referenciadas existieran en ambos catálogos. Otra revisión detectó claims de carga no medidos en un resumen histórico y un handoff de agentes: un conteo concreto de héroes y una afirmación de caídas de frames, además de una distribución uniforme exacta para los buckets de time-slicing.

El generador del showcase ahora busca propiedades `[DataSourceProperty]` correspondientes a cada atributo `@Property` del prefab y revisa las claves de localización referenciadas por el ViewModel generado y el XML nativo de contenido, tanto en inglés como en español. Regresiones negativas demuestran el rechazo de una anotación ausente y de claves faltantes en cualquiera de los dos idiomas. Las recomendaciones obsoletas de time-slicing ahora describen el comportamiento real de la fuente: hash determinista por ID, buckets desiguales y recorrido completo de la colección por parte de quien llama. Las afirmaciones de carga sin respaldo quedan marcadas como no verificadas.

Las skills de depuración y simulación distinguen la afinidad al hilo del motor de una afirmación general de corrupción de memoria, marcan los ejemplos ilustrativos como propuestas y exigen revisar fuentes y ensamblados locales antes de declarar comportamiento de runtime. La guía de depuración ahora describe `.cfcrash` como JSON de texto guardado en la carpeta del módulo y solo con `Timestamp`, `IsTerminating` y `Exception`; no deduce procedencia o afinidad de hilo de ese archivo. También mantiene los overrides de `SyncData` del mod sin persistencia y atribuye el límite de 64 entradas al journal/replay de ForgeWeave, separado del límite de 2.048 identidades de agente. La guía de rendimiento acota `GameThreadActionDispatch.RunOrPost` a sus sitios internos de llamada, sin prometer dispatch global. Las guías raíz enrutan ahora a las seis skills especialistas solicitadas. No se presenta un runtime de árboles de comportamiento ni un subsistema universal de simulación.

## Validación y límites

- `tools\Test-CalradiaForge-ContentShowcase.bat --no-pause` aprobó tras agregar las regresiones del validador. El módulo consumidor generado compiló para `net472` sin advertencias; Core aprobó 411/411 y ForgeWeave 73/73.
- `tools\Validate-CalradiaForge-Skills.bat` aprobó para las skills actualizadas de debugging y discrete-event-simulation.
- `git diff --check` aprobó. La suite completa, la auditoría canónica de paquetes/hashes y la integridad del registro protegido se repiten tras este apéndice.
- Estas comprobaciones cubren fuente generada y fixtures. No demuestran carga/render Gauntlet, rendimiento en Bannerlord ni mejoras de tiempo por frame dentro del juego. No se iniciaron Bannerlord ni Modding Kit.

Esta entrada es append-only y conserva la versión del producto 25.2.0 y los contratos SDK existentes.
