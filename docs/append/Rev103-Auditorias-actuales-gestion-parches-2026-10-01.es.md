# Rev103 — Auditorías actuales de gestión de parches

**Versión:** Calradia Forge 25.2.0; sin cambios.

CI remota en 97e238c aprobó las suites portables y el gate stateless, y después falló la auditoría opcional de agentes porque buscaba el obsoleto _syncLock y validación directa de punteros en ForgePatcher. El auditor comprueba ahora la gestión de recibos con syncLock, la delegación a ForgeDetour.IsTrackedReceipt y ForgeDetour.Verify, los registros protegidos por Gate y el rechazo de punteros nulos antes de instalar. Su informe limita explícitamente la evidencia a comprobaciones estáticas de gestión, sin afirmar seguridad de ejecución concurrente del destino.

Cinco regresiones independientes del SDK cubren el código actual y la eliminación de sincronización de recibos, delegación de integridad, sincronización del registro y protecciones de punteros nulos. Se ejecutan desde el BAT Python junto con las suites existentes. La herramienta stateless de agentes usa también el gateway BAT y selecciona el grafo portable en GitHub Actions, en lugar de requerir ensamblados propietarios del motor.

El BAT Python CI local aprobó Ruff, 12 fixtures de assets, 5 pruebas de imágenes, 5 regresiones del auditor y comprobaciones de sprites e iconos. Las pruebas remotas del SDK opcional siguen pendientes para esta corrección. El ZIP de logs suministrado contiene el fallo previo de codificación del inventario, resuelto por 97e238c. No cambiaron comportamiento de la aplicación, API pública, runtime del juego, paquetes ni versión; los registros anteriores permanecen intactos.
