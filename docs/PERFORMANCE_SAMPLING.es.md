# Muestreo de una sesión de juego

Usa `tools/sample_session.ps1` del código fuente para desarrolladores para conservar métricas de solo lectura de una sesión de Forge en ejecución. La aplicación de escritorio opcional no es necesaria. Cierra su conexión antes del muestreo, porque el servidor de canalizaciones actual acepta un cliente a la vez.

```powershell
pwsh -File tools/sample_session.ps1 -ProcessId 12345 -Context Campaign `
  -Label panel-closed-paused-map -Samples 12 -IntervalSeconds 10 `
  -OutputDirectory artifacts/my-campaign-closed
```

Sustituye el ID del proceso por el del proceso de Bannerlord en ejecución y elige un directorio de salida nuevo. El script comprueba la hora de inicio del proceso, la identidad de la sesión de Forge y el contexto de juego solicitado entre muestras. Se detiene si la sesión se desconecta o cambia, y conserva las muestras completadas. No inicia el juego, abre el panel, mueve la cámara ni modifica el estado de la campaña.

Observa la escena antes de cada fase. Mantén la misma cámara, el mismo estado de pausa, la misma configuración gráfica y la misma selección de módulos al comparar periodos con el panel abierto y cerrado. Registra una captura de pantalla y la etiqueta de la fase. Cada muestra conserva el resumen sin procesar de la sesión, las métricas, la memoria del proceso y el tiempo de CPU acumulado. `ForegroundAtEveryCheck` registra si el juego estuvo en primer plano en cada comprobación de un segundo durante el intervalo anterior; no demuestra que permaneciera continuamente en primer plano entre comprobaciones. Excluye los intervalos con el valor `false` al comparar el renderizado en primer plano. Las condiciones de escena etiquetadas son observaciones del operador; el protocolo no puede verificar de forma independiente la visibilidad de la cámara o del panel.

Las métricas de fotogramas describen los últimos 600 ticks observados de la aplicación, no todos los fotogramas desde que comenzó el muestreo. Muestrear cada diez segundos deja huecos cuando la frecuencia de fotogramas es alta. Un percentil de esas medias de ventana no es un percentil de fotogramas individuales. La memoria y la CPU del proceso incluyen todo el juego. Comparar fases secuenciales sin una línea base con el mod desactivado no permite aislar la sobrecarga total de Forge, y las ejecuciones breves no descartan fugas lentas. Incluye la duración, el escenario, las limitaciones y la evidencia sin procesar en cualquier afirmación de rendimiento.

Las métricas `forge.callback.*` describen el tiempo de reloj de pared transcurrido dentro de la devolución de llamada `OnApplicationTick` de Forge, medido con marcas de tiempo de `Stopwatch`. Las últimas 600 devoluciones de llamada completadas proporcionan el recuento de muestras, la media, el p95 y el máximo en milisegundos. Incluyen el manejo síncrono de comandos de Forge, las acciones del teclado y del panel, y el trabajo de extensiones ejecutado dentro de esa devolución de llamada. Excluyen el resto del fotograma del motor, el renderizado de Gauntlet fuera de la devolución de llamada, los análisis o la persistencia en segundo plano, otras devoluciones de llamada de módulos y la propia operación de registro. La planificación de hilos puede afectar la duración de reloj de pared. Una solicitud de métricas informa sobre las devoluciones de llamada completadas; por eso, la devolución de llamada de la propia solicitud entra en la siguiente ventana. Estos valores son tiempos con alcance definido, no tiempo total de CPU ni una medición completa de la sobrecarga del mod.

