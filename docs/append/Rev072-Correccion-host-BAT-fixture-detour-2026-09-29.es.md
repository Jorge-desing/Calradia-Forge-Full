# Rev072 — Corrección del host BAT del fixture de detour y del copy del panel

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Launcher del fixture de detour, guía de parches Gauntlet y catálogos nativos.

## Correcciones

- El fixture aislado de detour es una biblioteca `net472` x64. `Run-DetourFixture.bat` la compila y carga su punto de entrada de prueba en un proceso desechable x64 de Windows PowerShell. El fixture ya no genera ni inicia `CalradiaForge.DetourFixture.exe`, y el BAT falla de forma segura si aparece un apphost.
- Se descartó un ensayo como DLL ejecutable de .NET 8: con ese JIT, el fixture serial no observó el detour en la etapa 4. La prueba volvió a `net472`, que coincide con el runtime de Bannerlord. El ensayo fallido no se cuenta como evidencia aprobada.
- El copy de Gauntlet ahora separa Patch Blueprint Preflight, de solo lectura; Harmony Atlas, inventario histórico de solo lectura; y ForgeWeave Replay. Siete claves corregidas están traducidas en los 13 catálogos nativos.

## Evidencia de validación

- El BAT del fixture completó las seis etapas en un host x64 desechable, devolvió `15 → 32 → 15` y verificó la reversión exacta.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` compiló sin advertencias ni errores. ForgeWeave pasó 73/73, Desktop 63/63 y el renderizado WPF pasó 292 casos. El último render midió 14.399 ms en el arnés; no representa la latencia de interacción de la aplicación.
- La regeneración de localización informó 13 catálogos con 737 claves cada uno. La auditoría generada informó `valid: true`.
- No se inició Bannerlord ni el Modding Kit. No se invocó la generación de paquetes en esta corrección. El fixture serial no demuestra seguridad ante ejecución concurrente; el backend sigue siendo experimental. `ForgeApi.Version` permanece en 11.
