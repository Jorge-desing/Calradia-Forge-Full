# Rev054 — Disposición adaptable y desplazable del Playbook detallado

**Fecha:** 27 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Ubicación del Playbook detallado de Gauntlet, ajuste de la fila de acciones, paneles contextuales y auditoría estructural de barras.

## Defecto observado y justificación técnica

El Playbook detallado ocupaba la misma zona derecha que los controles y las filas de acciones, de modo que el panel cubría o competía con contenido activo. El contenido largo del Playbook tampoco tenía una superficie de desplazamiento acotada y adaptable. El panel de ayuda de teclas podía aparecer al mismo tiempo y agravar el solapamiento.

## Solución técnica y decisiones arquitectónicas

El Playbook detallado ahora aparece debajo de las tarjetas de contexto y reserva una columna a la derecha para no cubrir los controles ni las filas activas. En modo detallado, los botones de acción afectados se reducen a 100 DIP para conservar el espacio de trabajo utilizable. El contenido del Playbook ahora usa una superficie desplazable de altura adaptable. La visibilidad de Ayuda de teclas y Playbook es mutuamente excluyente, evitando que ambos paneles contextuales ocupen la misma columna. La auditoría Gauntlet ahora revisa el contrato de barras en seis superficies desplazables, incluida la nueva superficie del Playbook.

## Cambios en activos, código y dependencias

- Se actualizaron el generador y el prefab Gauntlet para reservar la columna del Playbook, ajustar el ancho de botones en modo detallado, habilitar el desplazamiento adaptable del Playbook y excluir mutuamente Ayuda de teclas y Playbook.
- Se amplió la auditoría estructural para incluir las seis superficies con barras; no se requirieron nuevos sprites de atlas ni dependencias.
- No cambiaron API pública, rutas, comandos, permisos, versión del producto ni ZIPs.

## Evidencia de validación y límites

- Pasó la ejecución completa de `tools/Run-CalradiaForge-Tests.bat`; Desktop pasó 63/63 y el render WPF pasó 289 casos.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat` pasó con cero errores y cero advertencias de auditoría visual Gauntlet, incluidos los seis contratos de barras.
- La validación de este ajuste adicional dentro del juego queda pendiente de confirmación; las comprobaciones de fuente y compilación no demuestran su ubicación ni interacción renderizadas en Bannerlord. No se cargó campaña ni batalla para esta comprobación.
- Este añadido da seguimiento a Rev053 y agrega evidencia sin reemplazar sus párrafos ni sus afirmaciones.

La versión del producto sigue en 25.2.0. Los párrafos y revisiones anteriores del ledger no se modifican.
