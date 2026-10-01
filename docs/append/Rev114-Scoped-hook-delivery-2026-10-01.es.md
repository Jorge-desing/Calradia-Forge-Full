# Rev114 — Validación de entrega acotada de hooks — 2026-10-01

El objetivo Finalizer/ILHook se validó desde el árbol del índice Git en una copia local aislada, excluyendo los cambios concurrentes de SDK/onboarding. La compilación pasó sin advertencias ni errores. La validación BAT pasó Core 405/405, ForgeWeave 73/73, Desktop 65/65, 294 casos WPF con 182 pases de layout, Asset Pipeline 20/20, aceptación stateless 4/4 y auditoría estructural Gauntlet. Hook Utility pasó 29 comprobaciones de argumentos y 22 casos de transporte aislado. El harness WPF tardó 14.026 ms; no mide la latencia de interacción de la aplicación.

Un benchmark BAT serial nuevo incluyó cinco muestras por escenario: mediana Finalizer de 183,0 ns por llamada y 136,31 B por llamada; mediana IL-only de 17,8 ns por llamada y cero bytes de asignación medidos. Estas mediciones del fixture no certifican seguridad de modificación concurrente ni comportamiento real en el juego.

El pipeline canónico generó y auditó los tres archivos de distribución 25.2.0 desde esa copia acotada. La verificación SHA-256 independiente coincidió con su manifiesto y el archivo Source-SDK excluyó los añadidos concurrentes ContentShowcase, proyecto de pruebas HarmonyDiagnostics y evolución SDK. Las guías procedimentales finales distinguen la aplicación inicial autorizada de la reconstrucción de una activación IL propia ya verificada; un Undo incierto nunca autoriza reconstrucción.

La aplicación real de hooks en el menú principal sigue sin verificar porque la API actual de Computer Use no permite controlar ventanas nativas. No se usó campaña, batalla ni destino TaleWorlds. Se conservan producto 25.2.0, API SDK 13 y MonoMod 25.3.6. La entrega requiere un commit local acotado sin push; el trabajo ajeno permanece sin staging.
