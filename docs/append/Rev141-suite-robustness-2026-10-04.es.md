# Rev141 — Correcciones de robustez de la suite

**Fecha:** 04-10-2026

**Versión:** Calradia Forge 25.2.0; Forge API 13 sin cambios

**Alcance:** IPC y descarga del módulo, notificaciones de disponibilidad del SDK, planificación de campañas y localización/accesibilidad WPF.

## Problema observado y justificación técnica

El servidor named-pipe orientado a líneas usaba `ReadLineAsync` sin límite antes de comprobar el máximo de 65.536 caracteres, por lo que una solicitud incompleta o excesiva podía seguir acumulando datos. Algunas operaciones de descarga estaban agrupadas de forma que una excepción podía impedir la finalización de los pasos siguientes. La lista legacy de notificaciones de disponibilidad del SDK podía continuar notificando después de que un callback reemplazara sincrónicamente al proveedor conectado. El selector horario de campañas también debía fallar cerrado si no podía leer el reloj del motor. En WPF quedaban sin localizar las etiquetas del ciclo y del filtro de favoritos, y la acción Ping no declaraba un nombre ni una ayuda accesibles.

## Solución técnica y decisiones arquitectónicas

- Se añadió un lector UTF-8 incremental con límite de 65.536 caracteres UTF-16, cancelación y vencimiento de 15 segundos para líneas incompletas. Conserva los terminadores LF, CR y CRLF, y desconecta ante vencimiento o solicitudes malformadas/excesivas antes de deserializar JSON.
- Se aislaron los pasos de descarga del módulo y el runtime. Los fallos se registran sin impedir la desuscripción, limpieza de datos del SDK, disposición de IPC ni programación de persistencia del log de sesión.
- Se valida la generación conectada de Forge antes y después de cada callback legacy; se detienen las notificaciones restantes si un callback desconecta o reemplaza esa generación.
- La planificación de campañas devuelve false si no se puede leer el reloj del motor.
- Se añadieron etiquetas localizadas y nombres/ayudas explícitos de UI Automation para el indicador de ciclo, el filtro de favoritos y el botón Ping, sin cambiar comandos ni AutomationIds.

No cambiaron la API pública, el esquema IPC, las rutas, permisos, dependencias, versión del producto, versión de API ni los frameworks objetivo.

## Código, recursos y pruebas

- `src/CalradiaForge.Mod/PipeServer.cs`, `Runtime.cs`, `SubModule.cs` y `CampaignBehaviors/ClanCharacterProgressionBehavior.cs` implementan el transporte acotado y la limpieza independiente/fallo cerrado.
- `src/CalradiaForge.Sdk/Contracts.cs` detiene notificaciones de generaciones obsoletas.
- Los recursos de presentación WPF y los 13 catálogos localizados aportan etiquetas accesibles.
- Las regresiones cubren límites IPC, terminadores, cancelación, vencimiento/reconexión, limpieza posterior a excepciones, reloj de campaña inaccesible, reentrada con generaciones obsoletas, paridad de localización y propiedades de UI Automation.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Tests.bat --no-pause`: compilación de solución correcta con 0 advertencias y 0 errores; Core 436/436, ForgeWeave 74/74, Desktop 75/75 y 296 casos WPF con 320 pases de layout/render aprobados.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat`: 4/4 verificaciones aprobadas.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause`: auditoría estructural aprobada, 0 errores y 0 advertencias; esto valida fuentes/fixtures, no el juego en vivo.
- `tools/Test-CalradiaForge-Desktop-Uia.bat`: inspección de solo lectura aprobada para 23/23 registros observados en su proceso de pruebas dedicado.
- Los 9.962,7 ms de render/layout (22.060 ms de suite de render total) corresponden al arnés WPF, no a latencia observada de la aplicación. No se inició Bannerlord; el comportamiento en el juego queda sin verificar.

Este anexo añade evidencia sin sustituir revisiones anteriores. Todo el contenido DOCX previo y sus registros de integridad siguen siendo append-only.
