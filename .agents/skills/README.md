# Skills del proyecto Calradia Forge

Esta carpeta conserva las skills locales del proyecto. Las carpetas permanecen planas para facilitar su descubrimiento; la agrupación conceptual y las rutas recomendadas están en [la taxonomía](../../docs/SKILLS_TAXONOMY.md).

## Punto de entrada rápido

- Desarrollo general del mod y arquitectura: [calradia-forge-modding](./calradia-forge-modding/SKILL.md).
- Implementación y verificación: [calradia-forge-dev-workflow](./calradia-forge-dev-workflow/SKILL.md).
- C# y MSBuild: [calradia-forge-dotnet](./calradia-forge-dotnet/SKILL.md), seguido de las skills del dominio.
- Documentación del proyecto: [calradia-forge-docs](./calradia-forge-docs/SKILL.md).
- Gauntlet y recursos: [bannerlord-gauntlet-ui](./bannerlord-gauntlet-ui/SKILL.md), [bannerlord-resource-browser](./bannerlord-resource-browser/SKILL.md) y [bannerlord-fbx-importer](./bannerlord-fbx-importer/SKILL.md).
- Aplicación WPF: [calradia-forge-desktop](./calradia-forge-desktop/SKILL.md).
- Mecánicas del juego: elige la skill específica del subsistema en la taxonomía.
- Compatibilidad con Codex y multi-agente: [AGENTS.md](../../AGENTS.md), [CODEX.md](../../CODEX.md) y [docs/CODEX_COMPATIBILITY.es.md](../../docs/CODEX_COMPATIBILITY.es.md).

## Copias locales protegidas

Las skills locales [using-dotnet](./using-dotnet/SKILL.md), [docs-generator](./docs-generator/SKILL.md) y [superpowers](./superpowers/SKILL.md) son copias de respaldo deliberadas. Resguardan el contenido original y las adaptaciones para que una actualización de la skill de origen no borre el conocimiento del proyecto.

Las skills aisladas [calradia-forge-dotnet](./calradia-forge-dotnet/SKILL.md), [calradia-forge-docs](./calradia-forge-docs/SKILL.md) y [calradia-forge-dev-workflow](./calradia-forge-dev-workflow/SKILL.md) contienen las reglas y rutas activas del proyecto. Las copias protegidas se conservan intactas como respaldo; no son dependencias para ejecutar esos flujos.

Esta reorganización conserva todas las skills. Antes de retirar alguna en el futuro, revisa sus enlaces, activadores, referencias desde reglas y agentes, conocimiento local y uso en scripts o empaquetado.
