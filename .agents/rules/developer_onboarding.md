---
name: developer-onboarding
description: Keep SDK packages, .NET templates and generated Bannerlord content reproducible and grounded in the actual public contract.
trigger: model_decision
---

# Developer onboarding and capability evidence

- Route C# and packages through `calradia-forge-dotnet`; use its
  [SDK onboarding reference](../skills/calradia-forge-dotnet/references/sdk-onboarding.md).
- Package the SDK for its actual `net472;net8.0` targets. Generated game modules
  target `net472` and resolve licensed engine references from an explicit local path.
  Never distribute TaleWorlds binaries or a second runtime SDK with a consumer module.
- Beginner content builders generate native ModuleData files before loading the
  game. Valid XML alone is insufficient: validate referenced IDs, manifest entries
  and the schema against the target game's data, then separately test engine loading.
- A `.NET new` template must consume a versioned SDK package and declare the Forge
  module dependency. Test it with an isolated template hive and package cache;
  avoid changing the user's installed templates or using stale global SDK packages.
- Keep ForgeWeave cooperative events, explicit hooks and experimental method
  replacement distinct. Shared targets are review signals, never proof of a conflict.
  Observations need bounded provenance, capture time, freshness and partial/failure state.
- A generated Gauntlet prefab/ViewModel/localization bundle is a static artifact.
  Source binding checks do not establish renderer support or hot reload.
- Retain local uncommitted work as pending evidence even when a compatible build
  succeeds. Avoid claiming a universal zero-allocation tick or safe concurrent
  native patch from a serial fixture. Profile actual callbacks before optimization.
- At objective completion, teach only supported lessons through project specialists,
  link from gateways, and mirror shared invariants in AGENTS.md, CODEX.md and GEMINI.md.
  Preserve upstream snapshots and maintain English/Spanish documentation parity.
