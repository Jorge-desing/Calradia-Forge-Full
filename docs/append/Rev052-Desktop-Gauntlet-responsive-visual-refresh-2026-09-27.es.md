# Rev052 — Renovación visual adaptable de Desktop y Gauntlet

**Fecha:** 27 de septiembre de 2026<br>
**Versión:** Calradia Forge 25.2.0, sin cambios<br>
**Alcance:** Pie de la aplicación WPF Desktop y recursos de imagen locales; cabecera, navegación, disposición Playbook/evidencia y comprobaciones visuales estructurales de Gauntlet.

## Defectos confirmados y justificación

En el tamaño mínimo de ventana WPF de 980×680 DIP, el pie de atajos de teclado quedaba demasiado cerca del borde inferior. La auditoría Gauntlet también comparaba el prefab vigente con expectativas de iconos obsoletas y no modelaba cómo el estado de enfoque de evidencia cambia el ancho disponible del espacio de trabajo. Algunos solapamientos señalados estaban cubiertos por paneles opacos posteriores; las condiciones reales de disposición del Playbook y de evidencia requerían comprobaciones explícitas por estado.

## Implementación

El pie WPF ahora reserva un margen inferior y aplica una altura y margen mínimos al texto. Una regresión de render comprueba los límites reales del pie y el ajuste en los 13 idiomas en el tamaño mínimo de ventana. El recorte de cabecera empaquetado usa ahora un derivado determinista Rev072 generado a partir del original local de alta resolución; solo se integra el derivado optimizado.

La disposición Gauntlet ajusta el espaciado de la cabecera y vincula la reserva del espacio de trabajo a la visibilidad del Playbook. Al enfocar evidencia, el Playbook se oculta y la evidencia se expande, manteniendo fijo el borde inferior de las acciones. Los ocho botones de ruta usan sprites semánticos ya registrados por el proyecto. El catálogo de herramientas y sus 194 rutas no cambian, ni tampoco los 56 bindings de comando.

El auditor visual evalúa anchos de viewport de 1220, 1280, 1600 y 1920, estados normal y detallado del Playbook, y evidencia enfocada. El modelo de solapamientos solo permite una colisión si un panel opaco dibujado después cubre la decoración afectada. El validador de iconos ahora coincide con los ocho sprites de navegación vigentes y los demás iconos canónicos del prefab; se siguen validando los 17 iconos generados y las 30 entradas del atlas.

## Evidencia de validación y límites

- Cinco corridas WPF anteriores y cinco posteriores pasaron cada una 289 casos y 182 pases de layout. Las medianas totales del arnés fueron 10.953 ms antes y 11.474 ms después (+4,8 %). Las medianas de llamadas de layout fueron 3.220,1 ms antes y 3.095,3 ms después (-3,9 %). Son mediciones del arnés fuera de pantalla, no latencia de la aplicación abierta. La mediana total permanece dentro del límite de regresión del 10 % del plan; no se afirma una aceleración global.
- `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check` pasó la comparación determinista. El paquete optimizado ocupa 7.490.852 bytes comprimidos y 16.473.296 bytes RGBA decodificados, dentro de los presupuestos de recursos configurados.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` compiló con cero advertencias/errores y pasó Assets 8/8, Core 338/338, ForgeWeave 73/73, Desktop 63/63 y render WPF con 289 casos/182 pases de layout.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` pasó los gates de sprites decorativos, disposición/solapamientos y Core. UIA Desktop de solo lectura pasó 23/23 comprobaciones; valida el árbol accesible, no la aprobación visual.
- Los perfiles Client y Modding Kit se compilaron sin advertencias/errores y se desplegaron con respaldos verificados. El TPAC instalado existente se conservó y su hash fue verificado como `69513B5617F026E1F106F6CA6D47CF5C5CE0B5869FFA472E9166B037CA8EDEA6`; no se reemplazó. El inicio del Modding Kit llegó a un cuadro `RGL WARNING` con referencias a ensamblados de ejecución. El cuadro se dejó abierto sin interactuar, así que el render Gauntlet en vivo y el aspecto del recurso importado siguen sin verificarse. No se cargó campaña ni batalla.

La versión sigue en 25.2.0. No cambiaron API pública, IPC, rutas, comandos, permisos ni dependencias. No se regeneraron ZIPs.
