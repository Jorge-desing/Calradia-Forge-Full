# Rev102 — Revisión post-push y codificación del inventario

**Versión:** Calradia Forge 25.2.0; sin cambios.

La regla de sincronización Git y la skill de desarrollo requieren ahora comprobar el SHA remoto exacto, las ejecuciones Actions, los check runs y los estados del commit después de cada push autorizado. La sincronización y la validación remota se informan por separado. Los fallos relacionados con la tarea requieren revisar logs, ejecutar pruebas BAT relevantes, subir la corrección y revisar el nuevo SHA. Los checks pendientes no aprueban; los fallos históricos se conservan. La evidencia remota exitosa se informa sin crear un ciclo infinito de commits exclusivos de evidencia.

CI remota en d59db82 aprobó ForgeWeave 73/73, Desktop 65/65 y WPF 293/293. Su paso agregado siguió fallando porque Windows PowerShell decodificó un guion Unicode UTF-8 sin marca en TpacInventory.psm1 usando la página de códigos ANSI del runner, provocando un error de sintaxis. El rango del diagnóstico usa ahora un guion ASCII. El BAT de inventario incluye una regresión del parser mediante decodificación Windows-1252 y aprobó localmente. La skill de desarrollo aprobó su validador rápido BAT. El workflow del registro para d59db82 aprobó.

La validación remota de esta corrección queda pendiente hasta la subida. No hubo parche de runtime, cambio de pantalla, sesión de juego, ZIP ni cambio de versión. Las revisiones anteriores permanecen intactas.
