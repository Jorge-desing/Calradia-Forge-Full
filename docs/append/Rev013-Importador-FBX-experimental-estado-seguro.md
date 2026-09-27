# Rev013 — importador FBX experimental y estado seguro

## Resumen

Se amplió `BannerlordFbxImporter` como experimento independiente para planificar lotes de recursos y preparar una futura calibración visible con Resource Browser. El helper permanece fuera del módulo, de los paquetes de distribución 22.0.0 y del importador FlaUI mantenido. La principal condición de seguridad es que el modo `--submit` está bloqueado hasta que haya evidencia suficiente para ejecutar y verificar un ejemplo real.

## Perfiles y límites de los lotes

El planificador distingue `static-mesh`, `rigged-mesh`, `texture-only` y `texture-assign`. Los perfiles de malla procesan FBX. Los perfiles de textura aceptan únicamente extensiones declaradas por configuración como observadas en los filtros del Editor; por defecto no se inventa una lista de formatos. `texture-assign` requiere un mapa explícito que cubra cada entrada del lote y vincule archivo, textura, material y slot.

El recorrido admite subdirectorios con límites de cantidad, profundidad, entradas, tamaño individual y volumen agregado; omite puntos de reanálisis y rechaza nombres base repetidos antes de cualquier acceso a UI. El preflight FBX lee solo texto ASCII dentro de límites. Materiales declarados son evidencia orientativa; archivos binarios, malformados, sobredimensionados y manifiestos ausentes o inválidos quedan como no disponibles, nunca como aprobados.

## Inspección del Editor y envío bloqueado

`--inspect` adjunta solo al proceso configurado, corre en un proceso auxiliar acotado y no envía clics, teclas ni selecciones. Una inspección de solo lectura previa encontró una ventana titulada `Resource Browser` con 112 controles, pero no permitió probar la cadena de padres `Modules > módulo > Assets`; una inspección posterior expiró. La superficie de control de escritorio de esta sesión no expuso ventanas nativas, así que no se manipuló el Editor ni se hicieron nuevos intentos de interacción.

La pantalla de ajustes, el inventario de materiales, el selector de archivos, el botón final Import y un perfil aprobado tras revisar un ejemplo siguen sin calibrarse. En consecuencia, `--submit` se detiene antes de pedir `IMPORT` y antes de realizar acciones. El modelo de estados reserva `SUBMITTED` para una activación final confirmada; requiere observar el nombre y tipo del recurso en Resource Browser para alcanzar `VERIFIED`. Model Viewer se documenta como revisión separada. Ninguno de esos pasos ocurrió en esta entrega.

## Respaldo y reemplazos

Se añadió un servicio independiente que crea una copia acotada de `Assets`, verifica los archivos copiados mediante SHA-256 y compara de nuevo la fuente durante el proceso. Se niega a tratar una carpeta `Assets` no directa como destino. El servicio no está conectado al flujo de importación porque este no puede establecer todavía el inventario de destino ni detectar colisiones. Los reemplazos siguen bloqueados hasta identificar el recurso afectado, comprobar una copia de respaldo y solicitar confirmación individual.

## Evidencia de fuentes y límites

Las guías de TaleWorlds describen las funciones de `Assets`, `AssetSources` y `AssetPackages`, el uso manual de Resource Browser para importar recursos y las convenciones de nombres de materiales/esqueletos. No confirman la supuesta precedencia fatal de carpetas de autoría vacías, el protocolo de Enter redirigido del generador de sprites, una regla universal de Add Leaf Bones ni transferencia automática de pesos por `_notused`. El helper conserva intactas las carpetas de autoría, no abre el Editor ni ejecuta SpriteSheetGenerator, y no descarta modales.

## Pruebas y entrega

`Test-BannerlordFbxImporter.bat` compiló helper y arnés con cero advertencias y errores. Pasaron 32 pruebas breves. Una prueba de omisión de puntos de reanálisis se saltó porque Windows no permitió crear un enlace simbólico con la cuenta actual. Las pruebas abarcan perfiles y CLI, límites de lotes, rutas recursivas, nombres Unicode/con espacios, duplicados, extensiones configuradas, mapas explícitos, evidencia FBX disponible/no disponible, resolución de destino, calibración desactivada, copias de respaldo verificadas, entradas obsoletas, timeouts y detención ante estados `UNKNOWN`.

No se efectuó una importación real, no se observó ni verificó salida TPAC, no se inspeccionó el modelo con Model Viewer y no se reconstruyeron los ZIP 22.0.0. `SUBMITTED` no se informó como resultado.
