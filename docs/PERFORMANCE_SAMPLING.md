# Sampling a game session

Use `tools/sample_session.ps1` from the developer source download to retain read-only metrics from a running Forge session. The optional desktop application is not required. Close its connection before sampling because the current pipe server accepts one client at a time.

```powershell
pwsh -File tools/sample_session.ps1 -ProcessId 12345 -Context Campaign `
  -Label panel-closed-paused-map -Samples 12 -IntervalSeconds 10 `
  -OutputDirectory artifacts/my-campaign-closed
```

Replace the process ID with the running Bannerlord process and choose a new output directory. The script checks process start time, Forge session identity and the requested game context between samples. It stops on a disconnected or changed session, preserving completed samples. It does not start the game, open the panel, move the camera or alter campaign state.

Observe the scene before each phase. Keep the same camera, pause state, graphics settings and module selection when comparing panel-open and panel-closed periods. Record a screenshot and the phase label. Each sample retains the raw session summary, metrics, process memory and cumulative CPU time. ForegroundAtEveryCheck records whether the game was foreground at each one-second check in the preceding interval; it does not prove uninterrupted foreground status between checks. Exclude intervals with false when comparing foreground rendering. Labelled scene conditions are operator observations; the protocol cannot independently verify camera or panel visibility.

Frame metrics describe the last 600 observed application ticks, not all frames since sampling began. Sampling every ten seconds leaves gaps at high frame rates. A percentile of those window means is not a percentile of individual frames. Process memory and CPU include the whole game. Comparing sequential phases without a mod-disabled baseline cannot isolate Forge's total overhead, and short runs cannot rule out slow leaks. Report the duration, scenario, limitations and raw evidence with any performance claim.

The `forge.callback.*` metrics describe elapsed wall-clock time inside Forge's `OnApplicationTick` callback, measured with `Stopwatch` timestamps. The last 600 completed callbacks provide sample count, mean, p95 and maximum milliseconds. They include synchronous Forge command handling, keyboard/panel actions and extension work executed within that callback. They exclude the rest of the engine frame, Gauntlet rendering outside the callback, background scans/persistence, other module callbacks and the recording operation itself. Thread scheduling can affect wall-clock duration. A metrics request reports completed callbacks, so the request's own callback enters the next window. These values are scoped timings, not total CPU time or a complete measurement of mod overhead.

## Español

La herramienta toma muestras de una sesión activa sin necesitar la aplicación de escritorio. Sustituye el ID del proceso, indica Campaign, Mission o Any y utiliza una carpeta de salida nueva. Conserva las muestras completadas si se pierde la conexión o cambia el contexto. No abre el juego ni modifica la campaña.

Compara periodos con la misma cámara, estado de pausa, configuración gráfica y módulos. Los tiempos corresponden a ventanas móviles de 600 actualizaciones; no representan todos los fotogramas del periodo. La memoria y CPU pertenecen al proceso completo del juego. Conserva las capturas y datos originales, y explica estas limitaciones al presentar los resultados.
Las métricas forge.callback.* miden únicamente el tiempo transcurrido dentro de OnApplicationTick de Forge: cantidad de muestras, promedio, percentil 95 y máximo de las últimas 600 llamadas completadas. Excluyen renderizado externo, tareas en segundo plano y otras llamadas del motor; no equivalen al consumo total del mod.
