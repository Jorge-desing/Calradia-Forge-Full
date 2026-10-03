# Bannerlord CampaignBehavior & Event Architecture

In Mount & Blade II: Bannerlord, `CampaignBehaviorBase` is the fundamental base class for reactive simulation, periodic logic, and persistent state. It operates with zero external dependencies (no Harmony, no ButterLib).

---

## 1. Architectural Lifecycle
The execution lifecycle of a `CampaignBehaviorBase` follows a deterministic sequence:
1. **Instantiation**: Inside `MBSubModuleBase.OnGameStart(Game game, IGameStarter gameStarterObject)`:
   ```csharp
   if (gameStarterObject is CampaignGameStarter campaignStarter)
   {
       campaignStarter.AddBehavior(new CustomCampaignBehavior());
   }
   ```
2. **Registration (`RegisterEvents`)**: Before simulation begins, `CampaignBehaviorManager.InitializeBehaviors()` invokes `RegisterEvents()` on every registered behavior. This hooks delegates into `CampaignEvents.*.AddNonSerializedListener(this, ...)`.
3. **Save/Load Hydration (`SyncData`)**:
   - **On New Game**: `SyncData` initializes baseline values.
   - **On Load Game**: `SyncData` runs before tick events start, restoring primitive values, object keys, and container collections from the binary save stream.
4. **Session Activation (`OnSessionLaunchedEvent`)**: Fired once the world map, settlements, heroes, and parties are completely initialized. Dialogues, game menus, and dynamic quest triggers are injected here.
5. **Teardown**: When quitting to the main menu or switching save files, the behavior instance is discarded alongside `Campaign.Current`. Non-serialized listeners are dropped cleanly.

---

## 2. Event Registration: `AddNonSerializedListener` vs `AddSerializedListener`

| Feature | `AddNonSerializedListener` (MANDATORY) | `AddSerializedListener` (PROHIBITED) |
| :--- | :--- | :--- |
| **Save File Impact** | **Zero.** The event registration is transient in memory. | Serializes delegate references and method tokens into the save file binary stream. |
| **Save Safety** | Safe. Renaming methods or updating the mod will not corrupt saves. | Risky. Renaming a method causes immediate save game crashes. |
| **Rehydration** | The engine calls `RegisterEvents()` every time a save is loaded. | Conflicting with engine rehydration. |

### Memory Leak Prevention Across Sessions:
- Never store static references to behaviors (`public static MyBehavior Instance`).
- Subscribe exclusively with `AddNonSerializedListener(this, ...)`.
- When `Campaign.Current` is destroyed on session exit, all `MbEvent` instances owned by `CampaignEvents` are garbage collected together with the behaviors.

---

## 3. Two-Way State Synchronization (`SyncData`)
`IDataStore` acts as a bidirectional serialization abstraction:
- When `dataStore.IsSaving == true`, `dataStore.SyncData()` serializes the referenced variable.
- When `dataStore.IsLoading == true`, `dataStore.SyncData()` deserializes the variable from the save file.

```csharp
public override void SyncData(IDataStore dataStore)
{
    // 1. Primitive Synchronization
    dataStore.SyncData("_internalCounter", ref _internalCounter);

    // 2. Collection Synchronization
    dataStore.SyncData("_heroScores", ref _heroScores);
    dataStore.SyncData("_quarantinedSettlementIds", ref _quarantinedSettlementIds);

    // 3. Defensive Post-Load Null Guards & Backward Compatibility
    if (dataStore.IsLoading)
    {
        _heroScores ??= new Dictionary<string, int>();
        _quarantinedSettlementIds ??= new List<string>();
    }
}
```

### SaveableTypeDefiner for Custom Collections:
When saving `Dictionary<string, int>` or custom structs:
- Inherit `SaveableTypeDefiner` with a base ID $\ge 2{,}500{,}000$.
- Override `DefineContainerDefinitions()` and call `ConstructContainerDefinition(typeof(...))`.

---

## 4. Tick Performance & Time Slicing
Choose scheduling and performance changes from the behavior's timing requirements and measurements. Do not infer a frame-time regression or a universal performance threshold from source syntax alone.

### Measured Strategies:
1. **Stable Time Slicing for Deferrable Work**:
   Use the SDK scheduler when an operation may safely wait for its assigned hour. It hashes a stable `StringId` and normalizes the supplied hour; it does not make the collection scan itself O(N/24).
   ```csharp
   using CalradiaForge.Sdk;

   int currentHour = (int)CampaignTime.Now.ToHours;
   foreach (Hero hero in Hero.AllAliveHeroes)
   {
       if (hero == null || !hero.IsActive) continue;
       if (ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour))
       {
           ProcessHeroDailyLogic(hero);
       }
   }
   ```
   This example still traverses `Hero.AllAliveHeroes` on every call. Use a maintained bucket/index only if justified by a measured traversal cost and correct invalidation semantics.
2. **Allocation and Duration Budgets**: Measure the complete callback, including invoked helpers and engine calls, before deciding whether LINQ, delegates, or temporary collections need changes. Avoid claiming zero allocations or GC pauses without such evidence.
3. **Squared Distance Calculations**: Always compare squared distances (`Vec2.DistanceSquared`) instead of `Math.Sqrt`.

---

## 5. Safe Entity Lookups & Object References
- **Never store hard references** to `Hero`, `Settlement`, or `MobileParty` in saved fields. Mobile parties can disband, heroes can die, and hard references will leak or crash.
- **Store `StringId`**: Store unique string identifiers and resolve them on demand:
  ```csharp
  Hero hero = MBObjectManager.Instance.GetObject<Hero>(heroStringId);
  Settlement settlement = MBObjectManager.Instance.GetObject<Settlement>(settlementStringId);
  MobileParty party = MBObjectManager.Instance.GetObject<MobileParty>(partyStringId);
  ```

---

## 6. Critical Project Rules
1. **GEMINI.md Rule**: NEVER name a namespace, folder, or class `Campaign` (`TaleWorlds.CampaignSystem.Campaign` shadowing). Use `CampaignBehaviors` or `Behaviors`.
2. **Main Thread Only**: Never invoke Campaign APIs or modify campaign state from background threads (`Task.Run`). TaleWorlds Campaign System is strictly single-threaded.

---

## 7. Memoria Cognitiva CoALA y Actualización Precisa de Relaciones
Al integrar sistemas de memoria cognitiva (como `ForgeAgentMemory` o agentes basados en la arquitectura CoALA) dentro de un `CampaignBehavior`:
- **Separación entre Relación Absoluta y Variación (Delta):**
  - **Relación Absoluta (`Relation_<HeroId>`):** Debe reflejar siempre el valor real absoluto del motor (`hero1.GetRelation(hero2)` o `CharacterRelationManager.GetHeroRelation(hero1, hero2)`), en el rango estándar $[-100, 100]$.
  - **Delta o Variación (`LastRelationDelta_<HeroId>`):** Registrar por separado el cambio puntual derivado de una acción (ej. $+5$, $-10$) para auditoría episódica o explicabilidad.
  - *Anti-patrón Crítico:* Guardar el delta directamente en el hecho semántico de la relación (ej. asignar `5` a `Relation_hero_1` cuando la relación real con el héroe es `-40`) desvirtúa la memoria semántica y corrompe las decisiones de IA y los diálogos dinámicos.
- **Seguridad Sin Estado en Modding In-Game:**
  - Si la memoria de trabajo reside en un `CampaignBehavior` en `CalradiaForge.Mod`, respetar la Regla B de `AGENTS.md`: nunca heredar de `SaveableTypeDefiner` ni serializar grafos de memoria pesados en `SyncData`. La memoria transitoria de agentes debe reconstruirse reactivamente a partir de los hechos del mundo o gestionarse mediante el SDK desacoplado.

