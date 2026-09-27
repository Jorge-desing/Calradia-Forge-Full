# Rev049 — Ledger de evidencia en ventana mínima e identidad adaptable

**Fecha:** 27 de septiembre de 2026<br>
**Versión:** Calradia Forge 25.2.0, sin cambios<br>
**Alcance:** Disposición WPF mínima de Desktop independiente y regresiones de render.

## Problema observado y justificación técnica

En el tamaño mínimo de 980×680 DIP, el mensaje/acción del ledger de evidencia vacío y los textos largos de identidad de categoría necesitaban límites explícitos. Sin estas comprobaciones, un área de trabajo estrecha podía recortar la acción del estado vacío o permitir que el texto decorativo de categoría expandiera la tarjeta de resumen.

## Solución técnica y decisiones

La cobertura de render ahora verifica que el mensaje y la acción del ledger vacío permanezcan dentro del viewport visible del área de trabajo, y que la acción conserve su altura mínima de activación. El distintivo de identidad de herramienta tiene ancho acotado; los nombres de categoría y lemas largos permanecen en una sola línea, se recortan con puntos suspensivos, ofrecen el texto completo en tooltips y quedan dentro del marco. Otra aserción comprueba que la cabecera compacta permanezca en una sola fila con el ancho mínimo en los 13 idiomas admitidos. La suite conserva sus comprobaciones existentes de arte local y adornos pasivos.

No cambiaron API, rutas, comandos, permisos, contrato IPC, dependencias de ejecución ni comportamiento del producto. La versión sigue en 25.2.0 y no se regeneraron los ZIPs.

## Cambios en activos, código y dependencias

- Se actualizaron las comprobaciones de render para el tamaño mínimo, el ledger vacío, el distintivo de identidad, las etiquetas largas y la fila de cabecera en los idiomas admitidos.
- Este seguimiento no agregó ilustraciones ni dependencias de ejecución.
- Se actualizaron de forma append-only la documentación bilingüe de Desktop y los changelogs fuente.

## Validación y límites de la evidencia

- El artefacto BAT final proporcionado `artifacts/desktop-visual-rev069-final.json` registra cero advertencias/errores de compilación, Desktop 59/59, render WPF 286/286 y 172 pases de layout.
- Ese JSON registra 11.719 ms de tiempo total del arnés y 3.106,8 ms en llamadas de layout. Son mediciones del arnés, no latencia de la aplicación abierta ni una afirmación de rendimiento.
- El artefacto de render cubre presentación local y límites renderizados. No se realizó UI Automation en ejecución ni validación de la app en vivo. Esta tarea documental no volvió a ejecutar pruebas.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos. Rev069 del changelog fuente y Rev049 del Registro protegido son contadores independientes.
