# Rev039 — Aclaración del alcance de pruebas WPF

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Aclaración append-only de Rev038; no altera Rev037 ni Rev038.

## Aclaración

Después de Rev037 se ampliaron únicamente las regresiones de prueba: la comprobación de fallbacks vacíos/fijados de Split Deck ahora cubre los 13 idiomas y el render harness verifica valores de recursos en los 13 diccionarios XAML. Por tanto, la frase de Rev038 que indica que no hubo cambios de código debe leerse como “sin cambios en código de runtime”: sí hubo cambios en fuentes de pruebas. No se modificaron la aplicación en ejecución, los recursos del producto, la API ni el comportamiento. Esta nota precisa Rev038 sin editarla ni alterar sus párrafos o huella.

## Evidencia

- La ejecución final de `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause <nul"` completó la compilación sin advertencias ni errores y pasó Desktop MVVM 55/55.
- Las cinco ejecuciones finales de `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause --output <report.json> <nul>` cubrieron 275 casos y 152 pases de layout cada una. Sus métricas y límites se conservan en Rev038; son observaciones del harness, no una inspección en vivo ni una simulación de DPI de Windows.
- La revisión de la ventana WPF/UI Automation en vivo continúa pendiente. La versión permanece en 25.2.0 y no se generaron ZIPs.
