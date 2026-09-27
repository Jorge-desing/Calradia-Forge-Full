# Rev014 — calibración del Resource Browser con iconos locales

## Estado

La calibración del perfil `texture-only` sigue pendiente. Se identificaron nueve PNG de iconos en `modules/CalradiaForge/GUI/SpriteParts/ui_calradiaforge`; `calradiaforge_compass.png` quedó elegido como muestra para una futura revisión del filtro y la pantalla de ajustes. No se confirmó que PNG sea una extensión aceptada por el selector de importación.

## Selección segura de ventana

La instalación expone las ventanas `Resource Browser` y `Edit Mode` bajo el proceso configurado `TaleWorlds.MountAndBlade.Launcher`. La configuración ahora incluye `resourceBrowserWindowTitle` con el título exacto `Resource Browser`. El inspector enumera ventanas superiores desde los hilos del proceso configurado, filtra por PID, título exacto y visibilidad, y falla de forma segura si no encuentra una coincidencia única. No busca ventanas de otros procesos ni usa heurísticas por coordenadas para seleccionar el destino.

La inspección detectó un fallo de interoperabilidad: el nombre administrado del método de visibilidad no correspondía al punto de entrada Win32. Se corrigió el punto de entrada explícito de `IsWindowVisible`; las pruebas de selección cubren `Resource Browser` frente a `Edit Mode`, títulos ausentes, ventanas invisibles y títulos duplicados.

## Resultado de la inspección real

El intento más reciente de `--inspect` se ejecutó contra el proceso configurado. El proceso auxiliar agotó los 45 segundos y devolvió `UNKNOWN`. No se volvió a intentar ni se cerró ninguna ventana. No se obtuvieron los controles del selector, el filtro de extensiones ni los ajustes de textura. No se hizo clic, no se eligió un archivo, no se pulsó Import y no se guardó ni reemplazó ningún recurso.

Por ello, `profiles.texture-only.supportedExtensions` continúa vacío; `calibrationReviewed`, `resourceInventoryComplete` y la aprobación global siguen desactivados. `--submit` permanece bloqueado. El icono es solo una muestra candidata; las carpetas `Assets` y `AssetSources` y todos los recursos permanecen bajo revisión manual.

## Verificación

`Test-BannerlordFbxImporter.bat`: compilación correcta con cero advertencias y errores; 36 pruebas pasaron, 0 fallaron y 1 se omitió porque Windows no permitió crear el enlace simbólico de prueba. Se añadió cobertura de selección exacta de ventana y de rechazo de acciones de escritura por el worker. No se hizo una importación, no se verificó salida TPAC y no se regeneraron los paquetes 22.0.0.
