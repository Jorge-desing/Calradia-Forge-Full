# Rev024 — Corrección de validación de servicios compartidos

**Fecha:** 25 de septiembre de 2026  
**Versión:** Calradia Forge 23.0.0; API del SDK versión 7  
**Alcance:** SDK. Aclaración de semántica de `Watch<T>` y validación final de los servicios compartidos agregados en Rev023.  
**Distribución:** seguimiento solo del código fuente; no se regeneraron ZIP ni paquetes del producto.

## Problema observado y justificación técnica

Rev023 se compiló antes de terminar la regresión de integración y de ejecutar la validación final. Por eso dejó la suite como pendiente y describió de forma imprecisa la reentrancia. La revisión del código aclaró que una mutación realizada dentro de `Changed` se aplica inmediatamente; el registro encola solamente la entrega de los callbacks posteriores. Como consecuencia, los argumentos del evento siguen siendo snapshots inmutables de su transición y pueden quedar obsoletos antes de que los reciban manejadores posteriores.

## Solución técnica y decisiones arquitectónicas

Se añadieron comentarios XML a `SharedServiceMonitor<T>` y a `Changed` para documentar la semántica exacta: las mutaciones reentrantes se aplican en el hilo del registro, la cola evita el despacho recursivo, `Current` y `Resolve<T>` exponen el estado más reciente y un handle puede quedar invalidado por una baja. Se amplió la integración de los ensamblados reales del proveedor y consumidor para probar `Resolve<T>`, `Watch<T>` y `SharedServiceBatch` con el contrato CLR compartido, conservando `Require<T>` en el ejemplo existente.

## Cambios en activos, código y dependencias

La ampliación se limita a comentarios XML y pruebas SDK. No cambia firmas públicas, ciclo de vida de `ForgeApi.Connect`/`Disconnect`, contrato IPC, dependencias, destinos `net472` / `net8.0`, ni el comportamiento del módulo. `ForgeApi.Version` sigue en 7 y la versión del producto permanece en 23.0.0.

## Validación y límites de la evidencia

- `cmd /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"`: compilación con 0 advertencias y 0 errores; Core pasó 263/263 y ForgeWeave pasó 43/43.
- La integración cruzada entre ensamblados verificó la resolución opcional, el snapshot del monitor y la publicación agrupada del servicio; la prueba reentrante confirmó el orden de snapshots, que los callbacks no se anidan y que los handles retirados se invalidan.
- Todas las pruebas se ejecutaron mediante el launcher `.bat`; no se inició directamente ningún `.exe` ni `.dll` de pruebas. No se inició Bannerlord, campaña ni batalla. No se generaron ZIP.

Este anexo corrige y completa la evidencia técnica de Rev023 sin editar ni reemplazar sus párrafos ni el DOCX anterior.
