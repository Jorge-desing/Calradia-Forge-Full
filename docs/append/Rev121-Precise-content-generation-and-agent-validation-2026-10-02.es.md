# Rev121 — Generación precisa de contenido y validación de agentes basada en evidencia

**Fecha:** 2 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** generación de ítems del SDK, regresiones del showcase, guía de validación de agentes y verificación final.

## Problema observado y justificación técnica

`ForgeItemBuilder` redondeaba los pesos a una cifra decimal y aceptaba valores `double` no finitos, que no se pueden representar en el campo `xs:decimal` de Native Items. La primera corrección de precisión también quitó el sufijo `.0` de los valores enteros; el BAT del showcase detectó esa regresión de compatibilidad antes de corregirla.

## Solución técnica y decisiones arquitectónicas

- Serializar pesos con formato decimal XML y cultura invariable, conservando precisión fraccionaria y al menos una cifra decimal; rechazar `NaN` e infinitos en el límite del builder.
- Rechazar la generación de Banner hasta verificar un esquema Native Items específico de versión, en vez de emitir una estructura no respaldada.
- Alinear la guía del agente con la ejecución local real de herramientas offline y hacer que la validación de releases use launchers BAT mantenidos, conteos propios de cada corrida y límites explícitos entre evidencia del arnés y del runtime.

## Cambios en activos, código y dependencias

Se actualizaron `ForgeItemBuilder`, sus pruebas del SDK, las skills de validación de release y orquestación de agentes, las guías bilingües de agentes y este registro append-only. La versión del producto, `ForgeApi.Version`, las dependencias públicas del paquete y los TFM siguen iguales.

## Validación y límites de la evidencia

- `tools\Run-CalradiaForge-Tests.bat --no-pause`: compilaciones limpias de `net472`, `net8.0` y Desktop, con 0 advertencias/errores; Core 411/411, ForgeWeave 73/73, Desktop 65/65 y WPF con 295 casos/308 pases de layout-render. Los 16.366 ms de render pertenecen al arnés, no a la latencia de la aplicación.
- `tools\Test-CalradiaForge-ContentShowcase.bat --no-pause`: pasaron salida determinista, comprobaciones contra XSD locales, compilación del módulo `net472`, Core 411/411 y ForgeWeave 73/73. Los hashes del XML de muestra generado no cambiaron.
- `tools\Test-CalradiaForge-Developer-Onboarding.bat --portable-only --no-pause`: pasaron empaquetado, instalación aislada de plantilla, restore/build del módulo generado y comprobación del manifiesto. El modo portable no demuestra integración con GameBin ni carga en el juego.
- `tools\Verify-CalradiaForge-StatelessBehavior.bat`: pasaron los cuatro checks con 0 advertencias/errores de compilación.
- Una revisión estática adversarial no encontró dependencia de paquete/ensamblado Harmony ni una ruta de carga del runtime; no prueba coexistencia con mods de terceros.
- No se inició Bannerlord, campaña ni batalla. La carga/render en juego y la publicación pública en NuGet/IDE siguen sin verificar.

El producto permanece en 25.2.0 y `ForgeApi.Version` permanece en 13.
