# 12.4.0 — detalle adicional conservado de la fuente inglesa

Estas notas amplían 12.4.0 a partir de los dos bloques de `docs/CHANGELOG.md`. Se preservan como afirmaciones históricas del registro, sin presentar los conteos de pruebas como una ejecución nueva.

- Se renombró el espacio de nombres `CalradiaForge.Core.SDK.Campaign` a `CampaignMechanics` para evitar colisión con `TaleWorlds.CampaignSystem.Campaign`.
- El bloque Core enumera simulación de misiones, clima, tiempo, lealtad, seguridad, heridas, plagas, recompensas, torneos y caza; además documenta fórmulas de combate, economía, política y UI. Entre los ejemplos concretos figuran penetración de armadura por tipo de daño, rutas de caravanas, convergencia de mercado, límites de séquito, decisiones de sucesión, proyección de HUD y clasificación de inventario.
- `ForgePartySpawner` declara plantillas de grupos, composición segura de tropas, presupuesto salarial y conductas AI `Patrol`, `DefendSettlement`, `Raid` y `Flee`.
- `ForgeHudProjector` proyecta coordenadas 3D a pantalla para overlays Gauntlet y calcula la atenuación de tracks del mapa.
- `ForgeApi` agrupa las superficies `Diplomacy`, `Settlements`, `Underworld`, `Progression`, `Combat`, `Trade`, `Parties` y `Hud`.
- La consola nativa añade comandos `cf.*` para puntuación de guerra, tributo de paz, auditoría de rebelión, rendimiento de callejón, aprendizaje de personaje, moral, brecha de asedio, precios, transferencias de inventario y auditoría de reglas.
- Bajo el bloque de 12.4.0 aparece una subsección de empaquetado etiquetada 12.3.0. Esa subsección atribuye 200 pruebas (176 Core y 24 ForgeWeave) y archivos de distribución 12.3.0; el mismo encabezado 12.4.0 informa por separado 203 pruebas (177 Core y 26 Desktop) y paquetes 12.4.0. Ambas cifras y etiquetas quedan atribuidas a sus respectivos pasajes fuente, sin reconciliarlas.

# 14.0.0 — cobertura de fixtures recuperada de la fuente inglesa

- El punto de validación omitido especifica fixtures positivos y negativos para entradas válidas, malformadas, ausentes, no compatibles e inseguras, además de capacidades, registro de analizadores y archivos ZIP. Es el alcance declarado por `docs/CHANGELOG.md`; este anexo no afirma que se hayan vuelto a ejecutar esas pruebas.
