# Taxonomía de skills de Calradia Forge

Esta taxonomía agrupa las skills locales por responsabilidad y ofrece rutas para tareas comunes. Las carpetas permanecen planas bajo .agents/skills/ para conservar su descubrimiento y los enlaces relativos existentes.

## Copias locales protegidas

Las carpetas locales using-dotnet, docs-generator y superpowers son copias de origen preservadas. Protegen los datos del proyecto ante actualizaciones de la fuente y deben permanecer intactas. Sus reglas específicas se consolidaron en skills aisladas de Calradia Forge; las copias son respaldo, no dependencias de ejecución.

| Copia de origen | Skill aislada del proyecto | Contenido consolidado |
|---|---|---|
| using-dotnet | [calradia-forge-dotnet](../.agents/skills/calradia-forge-dotnet/SKILL.md) | Límites Dual-TFM (`net472` y `net8.0-windows`), KISS y enrutamiento hacia dominios Bannerlord o Desktop. |
| docs-generator | [calradia-forge-docs](../.agents/skills/calradia-forge-docs/SKILL.md) | Orquestación documental, cuatro especialistas, paridad bilingüe, DOCX protegido append-only y SHA-256, DocFX y evidencia de entrega. |
| superpowers | [calradia-forge-dev-workflow](../.agents/skills/calradia-forge-dev-workflow/SKILL.md) | Ciclo de seis fases: planificación, pasarela TFM, cambios mínimos, pruebas, auditoría ModRuleAuditor, documentación y empaquetado solo cuando se solicite. |

Las tres skills del proyecto son autónomas para sus responsabilidades del repositorio. Los plugins externos pueden añadir orientación general, pero las skills del proyecto no dependen de ellos y sus reglas tienen prioridad.

## 1. Arquitectura y pasarelas de ingeniería
[calradia-forge-modding](../.agents/skills/calradia-forge-modding/SKILL.md) · [calradia-forge-dev-workflow](../.agents/skills/calradia-forge-dev-workflow/SKILL.md) · [calradia-forge-dotnet](../.agents/skills/calradia-forge-dotnet/SKILL.md) · [calradia-forge-gemini-conventions](../.agents/skills/calradia-forge-gemini-conventions/SKILL.md) · [calradia-forge-submodule-lifecycle](../.agents/skills/calradia-forge-submodule-lifecycle/SKILL.md) · [bannerlord-dotnet-artisan](../.agents/skills/bannerlord-dotnet-artisan/SKILL.md) · [bannerlord-shared-patterns](../.agents/skills/bannerlord-shared-patterns/SKILL.md) · [using-dotnet — copia protegida](../.agents/skills/using-dotnet/SKILL.md) · [superpowers — copia protegida](../.agents/skills/superpowers/SKILL.md)

## 2. Documentación y evidencia de entrega
[calradia-forge-docs](../.agents/skills/calradia-forge-docs/SKILL.md) · [calradia-forge-codemaps](../.agents/skills/calradia-forge-codemaps/SKILL.md) · [calradia-forge-registro-mejoras](../.agents/skills/calradia-forge-registro-mejoras/SKILL.md) · [calradia-forge-docfx-pipeline](../.agents/skills/calradia-forge-docfx-pipeline/SKILL.md) · [calradia-forge-release-validation](../.agents/skills/calradia-forge-release-validation/SKILL.md) · [docs-generator — copia protegida](../.agents/skills/docs-generator/SKILL.md)

## 3. Desktop, Gauntlet y recursos
[calradia-forge-desktop](../.agents/skills/calradia-forge-desktop/SKILL.md) · [calradia-forge-ui-automation](../.agents/skills/calradia-forge-ui-automation/SKILL.md) · [bannerlord-gauntlet-ui](../.agents/skills/bannerlord-gauntlet-ui/SKILL.md) · [bannerlord-resource-browser](../.agents/skills/bannerlord-resource-browser/SKILL.md) · [bannerlord-fbx-importer](../.agents/skills/bannerlord-fbx-importer/SKILL.md) · [bannerlord-localization](../.agents/skills/bannerlord-localization/SKILL.md) · [bannerlord-map-visuals](../.agents/skills/bannerlord-map-visuals/SKILL.md) · [bannerlord-audio-modding](../.agents/skills/bannerlord-audio-modding/SKILL.md) · [bannerlord-missionview-hud](../.agents/skills/bannerlord-missionview-hud/SKILL.md)

## 4. Campaña y simulación
[bannerlord-campaign-behavior](../.agents/skills/bannerlord-campaign-behavior/SKILL.md) · [bannerlord-gamemodels](../.agents/skills/bannerlord-gamemodels/SKILL.md) · [bannerlord-clan-succession](../.agents/skills/bannerlord-clan-succession/SKILL.md) · [bannerlord-character-development](../.agents/skills/bannerlord-character-development/SKILL.md) · [bannerlord-crime-underworld](../.agents/skills/bannerlord-crime-underworld/SKILL.md) · [bannerlord-economy-trade](../.agents/skills/bannerlord-economy-trade/SKILL.md) · [bannerlord-kingdom-diplomacy](../.agents/skills/bannerlord-kingdom-diplomacy/SKILL.md) · [bannerlord-party-spawner](../.agents/skills/bannerlord-party-spawner/SKILL.md) · [bannerlord-quest-system](../.agents/skills/bannerlord-quest-system/SKILL.md) · [bannerlord-settlement-rebellion](../.agents/skills/bannerlord-settlement-rebellion/SKILL.md) · [bannerlord-troop-character](../.agents/skills/bannerlord-troop-character/SKILL.md) · [bannerlord-inventory-barter](../.agents/skills/bannerlord-inventory-barter/SKILL.md)

## 5. Combate y misiones
[bannerlord-combat-ai](../.agents/skills/bannerlord-combat-ai/SKILL.md) · [bannerlord-siege-mechanics](../.agents/skills/bannerlord-siege-mechanics/SKILL.md)

## 6. Métodos y herramientas transversales
[agent-memory-systems](../.agents/skills/agent-memory-systems/SKILL.md) · [api-builder](../.agents/skills/api-builder/SKILL.md) · [browser-automation](../.agents/skills/browser-automation/SKILL.md) · [code-reviewer](../.agents/skills/code-reviewer/SKILL.md) · [frontend-expert](../.agents/skills/frontend-expert/SKILL.md) · [ponytail](../.agents/skills/ponytail/SKILL.md) · [uiux-designer](../.agents/skills/uiux-designer/SKILL.md)

## Enrutamiento
- C# del módulo: comienza con calradia-forge-dotnet, después bannerlord-dotnet-artisan y la skill del dominio.
- WPF de escritorio: comienza con calradia-forge-dotnet y después calradia-forge-desktop.
- Documentación o evidencia: comienza con calradia-forge-docs y agrega su especialista.
- Implementación general: comienza con calradia-forge-dev-workflow; sus seis fases contienen las compuertas de calidad del proyecto.
- Ejecución con Codex o multi-agente: consulta AGENTS.md, CODEX.md y docs/CODEX_COMPATIBILITY.es.md.

Antes de retirar una skill, audita su conocimiento propio, enlaces entrantes, activadores, scripts, referencias desde agentes y empaquetado. Esta reorganización conserva las copias locales protegidas.
