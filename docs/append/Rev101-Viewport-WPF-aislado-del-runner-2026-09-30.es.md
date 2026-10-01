# Rev101 — Viewport WPF aislado del runner

**Fecha:** 30 de septiembre de 2026
**Versión:** Calradia Forge 25.2.0; sin cambios
**Alcance:** Fixture de render Desktop y recuperación de CI alojada.

## Problema observado

GitHub CI en el commit 118fd2c compiló correctamente y aprobó Desktop 65/65, pero falló la aserción del viewport fijo de 1360×820 DIP. El workflow de documentación y registro aprobó. WPF conserva límites nativos de seguimiento de ventana; una pantalla del runner menor que el viewport del fixture puede limitar la ventana de pruebas.

## Corrección

El fixture de render aislado amplía únicamente los límites máximos de seguimiento de su propio HWND mediante WM_GETMINMAXINFO antes de que WPF procese el mensaje. El código de la ventana de la aplicación, las dimensiones mínimas, la configuración nativa de pantalla y las ventanas interactivas permanecen intactos. Una regresión envía límites menores de 1024×768 al HWND exacto del fixture y comprueba los límites resultantes antes de ejercitar el viewport lógico fijo. Los diagnósticos muestran ahora las dimensiones reales en caso de fallo.

Referencia: [Implementación de Window en WPF](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Window.cs) y [Microsoft WM_GETMINMAXINFO](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-getminmaxinfo).

## Validación y límites

El BAT Desktop recompiló correctamente sin advertencias ni errores, aprobó Desktop 65/65 y render WPF 293/293, conservando 182 pases de render/layout. El caso adicional verifica límites nativos pequeños sin cambiar la pantalla. CI remota de esta corrección queda pendiente hasta la subida. Las mediciones de render alojado son tiempos del fixture, no latencia de la aplicación. No hubo juego, campaña, batalla, paquete de distribución ni cambio de versión del producto. Las revisiones previas del registro permanecen intactas.
