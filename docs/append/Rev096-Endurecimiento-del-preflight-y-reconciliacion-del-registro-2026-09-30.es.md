# Rev096 — Endurecimiento del preflight y reconciliación del registro protegido

**Fecha:** 30 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Preflight explícito de parches y hooks, fixture desechable de detours, scripts de validación y registro documental append-only.

## Cambios

- Se reforzó el preflight de ForgeDetour antes de acceder a memoria ejecutable. Rechaza métodos P/Invoke, InternalCall y varargs; comprueba la compatibilidad del receptor implícito de instancia; compara los modificadores requeridos y opcionales de las firmas de retorno y parámetros; y rechaza destinos reservados por el servicio de hooks.
- Se añadieron regresiones Core para esas combinaciones inválidas de destino/reemplazo y para reservas de hooks frente a rutas directas y por lote. Cada rechazo comprueba que el adaptador de memoria no realice lecturas, cambios de protección, escrituras ni vaciado de caché de instrucciones, y que no quede un recibo de parche.
- Se corrigió la rama de error de compilación del BAT del fixture desechable para que devuelva siempre un código distinto de cero. El fixture sigue siendo una biblioteca net472 x64 alojada por el BAT mediante Windows PowerShell de 64 bits; el launcher rechaza la generación de un apphost EXE. No se inicia el EXE del fixture directamente.
- Se corrigió la auditoría de code smells de snapshots de parches para comprobar la ruta de snapshots inmutables que realmente usa cf.patch_status, sin exigir una referencia activa a MethodInfo.

## Validación

- tools/Run-CalradiaForge-Tests.bat --core-only --no-pause <nul pasó: compilaciones net472 y net8.0 sin advertencias ni errores, Core 395/395, ForgeWeave 73/73 y fixture serial x64 aprobado mediante su host BAT.
- El fixture comprueba ejecución serial y restauración exacta. No demuestra seguridad si otro hilo ejecuta el destino durante la modificación del código; el backend sigue siendo experimental y no es un entorno aislado para código de extensiones.
- No se inició Bannerlord ni el Modding Kit. La versión permanece en 25.2.0, ForgeApi.Version en 12 y no se generó ningún ZIP de distribución.

## Seguridad de dependencias e historial del registro

- La revisión de páginas públicas de seguridad del proyecto MonoMod RuntimeDetour y de información del paquete no permitió comprobar una auditoría de seguridad independiente publicada a la fecha. Esto no demuestra que no exista una auditoría privada o no indexada. RuntimeDetour ejecuta hooks dentro del proceso; solo debe aceptarse código de extensiones confiable. La serialización documentada de cambios en la cadena de hooks no certifica todos los ciclos de vida del host ni vuelve segura cualquier modificación concurrente de código nativo. Referencias: [uso de RuntimeDetour](https://monomod.dev/docs/RuntimeDetour/Usage.html), [hot-patching de la cadena](https://monomod.dev/docs/RuntimeDetour/implementation/ChainHotPatching.html), [avisos de seguridad del proyecto](https://github.com/MonoMod/MonoMod/security/advisories) y [política de seguridad](https://github.com/MonoMod/MonoMod/security/policy).
- El DOCX protegido y la cadena de integridad SHA-256 se reconciliaron hasta Rev095 antes de este anexo. Los anexos fuente Rev077–Rev095 y la secuencia DOCX protegida ya son continuos. Los anexos fuente Rev086–Rev094 se reconstruyeron como respaldos históricos a partir de las secciones bilingües ya publicadas en el changelog; las revisiones protegidas 001–076 se conservaron. Esta entrada reemplaza la nota desactualizada de Rev095 que indicaba que el registro seguía en Rev076 y que la reconciliación estaba pendiente.
- La auditoría Python del repositorio también señala hallazgos preexistentes y ajenos en documentación y playbooks; esta revisión del motor no afirma que estén corregidos ni que esa auditoría completa haya pasado.