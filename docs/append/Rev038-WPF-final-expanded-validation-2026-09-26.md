# Rev038 — Resultado final de validación WPF con cobertura ampliada

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Desktop. Registro final de las correcciones WPF documentadas en Rev037, tras ampliar la comprobación de recursos localizados.

## Problema observado y justificación técnica

Después de crear Rev037, la última corrida amplió la comprobación de los fallbacks del Split Deck a los 13 idiomas. Para que el registro protegido refleje la validación final, se repitió la matriz de render de cinco ejecuciones en serie. Este anexo conserva Rev037 intacta y agrega el resultado final de mayor cobertura.

## Solución técnica y decisiones arquitectónicas

Se confirmó la paridad de 101 claves en los 13 catálogos. Los cinco informes finales registran 275 casos y 152 pases de layout por ejecución; los dos pases adicionales respecto a la línea base corresponden a la comprobación del Split Deck activo con la ventana mínima. La relación cartográfica y la composición mínima se prueban en el harness, pero la matriz de escalas rasteriza capturas: no emula el layout WPF bajo DPI real de Windows.

## Cambios en activos, código y dependencias

Este seguimiento no añade cambios de código, recursos, dependencias, API ni funciones. El rail artwork decorativo permanece colapsado intencionalmente porque su relación de aspecto no cabe en el marco disponible. La revisión en vivo del aspecto WPF y la UI Automation siguen pendientes; el runner disponible modifica pestañas y el Split Deck, por lo que no se usa como inspección de solo lectura. La versión permanece en 25.2.0, y los ZIP no se generaron ni modificaron.

## Validación y límites de la evidencia

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause <nul"`: build sin advertencias ni errores; Desktop MVVM 55/55; un recorrido del render con 275 casos y 152 pases de layout.
- Cinco ejecuciones seriales de `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause --output <report.json> <nul>` guardaron informes `artifacts/desktop-render-wpf-final-20260926-01.json` a `-05.json`. Cada una pasó 275 casos y registró 152 pases de layout.
- La mediana de tiempo total fue 11.335 ms frente a 13.652 ms de baseline (-17,0 %). La mediana de llamadas de layout fue 2.074,7 ms frente a 2.397,2 ms (-13,5 %). Las medianas de inicio, navegación/filtro, localización y tema fueron 2.581,8 ms, 2.041,5 ms, 2.007,3 ms y 3.108,5 ms, cada una menor que la correspondiente línea base de 3.133,4 ms, 2.841,9 ms, 2.368,1 ms y 3.547,9 ms. Son mediciones del harness, no latencia de interacción de la aplicación abierta.
- No se comprobó el layout en DPI real del sistema ni la apariencia de una ventana WPF en vivo. No se inició Bannerlord ni se cargó campaña o batalla; no se afirma validación dentro del juego.

Este anexo complementa Rev037 sin alterar sus párrafos, estilos ni bytes. Los dos documentos permanecen en la cadena protegida de integridad.
