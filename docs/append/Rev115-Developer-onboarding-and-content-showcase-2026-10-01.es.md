# Rev115 — Incorporación de desarrolladores y muestra instalable de contenido — 2026-10-01

Producto: Calradia Forge 25.2.0, sin cambios.
Alcance: incorporación al SDK, plantilla para desarrolladores, generación de contenido estático y guía para diagnósticos Harmony acotados.

## Cambios

- Se agregó una muestra instalable que genera de forma determinista un objeto, una definición de tropa, el manifiesto del módulo y una página Gauntlet localizada de solo lectura con su ViewModel. Reutiliza los recursos visuales Native horse_whip; no inserta la tropa en una plantilla de grupo ni en un árbol de tropas.
- Se alineó la generación de contenido para principiantes con los builders del SDK y se corrigieron los rosters generados para usar el elemento EquipmentRoster del esquema del juego. Se conservan las firmas de los builders y las opciones de objetos admitidas.
- Se añadió documentación de incorporación al SDK y a la plantilla, flujos de validación y empaquetado, y referencias a las skills específicas del proyecto. La guía explica la separación de frameworks objetivo y mantiene las restricciones del runtime de Bannerlord separadas del Workbench Desktop.
- Se documentó el inventario Harmony acotado y de solo lectura como señal de compatibilidad. Registra procedencia/frescura acotadas y consultas incompletas; su fixture aislada con consultas simuladas no demuestra coexistencia con Harmony en vivo.
- Este trabajo de incorporación y la muestra no agregan API pública del SDK. El informe del smoke aislado de paquetes registra el HEAD fuente d8c0def6ab3604ded8600bacd81db50cd581bd32, contrato SDK 13 y producto 25.2.0, con overlays explícitos de builders/proyecto. El smoke anterior con contrato 12 es evidencia histórica; el contrato 13 procede del trabajo separado de hooks, no de este objetivo.

## Evidencia de validación y límites

- `tools\Test-CalradiaForge-ContentShowcase.bat --no-pause` pasó la generación determinista, las comprobaciones contra Items.xsd y NPCCharacters.xsd del juego instalado, el manifiesto, las referencias Native y la página estática, y compiló el módulo de muestra net472 con cero advertencias y errores. La misma ejecución pasó Core 405/405 y ForgeWeave 73/73. SHA-256 de los XSD usados por el verificador: Items.xsd E57D50EA6B0C6FD8356F4A26AAB7593B410242C6F4C457E4F1A4226961B9E266; NPCCharacters.xsd 912DDCB8BECB1315D9A579C81C109C420FD0391FFCF0D8AB53B49DEFE0B2A687.
- El arnés sintético de división temporal, con cinco muestras, informó medianas de 1,945 ms (128 entidades × 128 repeticiones), 0,9962 ms (2.048 × 8) y 0,2476 ms (16.384 × 1), con cero asignaciones síncronas medidas por hilo en esa fixture. Los números de repeticiones difieren: no son mejoras comparables ni tiempos de frame en Bannerlord; no se aplicó ninguna optimización con base en ellos.
- La validación de incorporación pasó la paridad de guías raíz, enlaces de documentación local, Ruff y nueve comprobaciones de skills. El smoke aislado de la plantilla creó paquetes locales SDK/plantilla, generó y validó un módulo, y lo compiló contra las referencias instaladas del juego net472 con cero advertencias y errores. Informe: artifacts/sdk-evolution/onboarding/20261001T184300Z-5a8bfd73/source-package-report.json. Registra SHA-256 del paquete SDK 78c6362f07f9a6049a63b05febc375904f650530c0ac02c397c12f823e81307e y del paquete de plantilla 95d3f897362504fc01d63403f343d197ad827208e4a8d981487ac193c61b11f7. Esto verifica artefactos locales de smoke NuGet/plantilla, no los ZIP de distribución, la interfaz de IDE ni la carga/render en Bannerlord.
- El BAT integrado pasó Core 405/405, ForgeWeave 73/73, Desktop 65/65 y 294 casos de render WPF con 182 pases de layout. Sus 17.341 ms corresponden al arnés, no a latencia de interacción de la aplicación. La aceptación stateless pasó 4/4; Asset Pipeline pasó 23/23; la fixture aislada de diagnósticos Harmony pasó 11/11. Esa fixture no comprueba coexistencia Harmony en vivo.
- La plantilla fuente declara una versión de módulo independiente, `v1.0.0`; la excepción del auditor se limita a ese manifiesto exacto y los módulos Forge distribuibles siguen exigiendo `25.2.0`. Los hashes .nupkg locales de esta revisión no certifican los tres ZIP de distribución; el gate canónico de empaquetado valida esos archivos por separado.
- No se verificaron la interfaz de IDE, el render Gauntlet en vivo, el comportamiento dentro del juego, campañas ni batallas.

El DOCX protegido del registro de mejoras y su cadena de integridad se gestionan por separado; las revisiones anteriores permanecen intactas.
