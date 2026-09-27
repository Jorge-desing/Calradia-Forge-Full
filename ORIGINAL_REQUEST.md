# Original User Request

## 2026-09-20T21:07:11Z

# Teamwork Project Prompt

> Status: Launched

Use a very large team of agents.

Develop a massive experimental CampaignBehavior that explores all clan and character development hooks (dynastic succession, companion spawning, progression). The system must run statelessly using vanilla game states without requiring custom save data serialization.

Working directory: c:\Users\Alex\Documents\Mod Desarrolladores
Integrity mode: demo

## Requirements

### R1. Implement a Stateless Clan & Character CampaignBehavior
Create a new C# class inheriting from `CampaignBehaviorBase` in the `CalradiaForge.Mod` project. Hook into as many relevant `CampaignEvents` as possible related to heroes, clans, and character progression (e.g., birth, coming of age, death, marriage, clan leader changes). 

### R2. Adhere to Architecture Rules
Ensure the behavior avoids the "Engine Initialization Crash Constraint" by deferring complex logic, and strictly follows the anti-shadowing rule (do not use `Campaign` in namespaces or class names). The behavior must be registered in the `MBSubModuleBase.OnGameStart` pipeline.

## Acceptance Criteria

### Verification & Compliance
- [ ] The `CalradiaForge.sln` compiles successfully in Release mode without errors.
- [ ] A programmatic script (e.g., PowerShell or Python) verifies that no custom classes inherit from `SaveableTypeDefiner` and no data is synced in `SyncData`.
- [ ] A test script confirms the SubModule properly registers the new CampaignBehavior via `AddBehavior()`.

## 2026-09-20T21:15:12Z

User Request (via parent): "Diles a los agentes que terminen de implementar los cambios" (Tell the agents to finish implementing the changes). Please proceed with the implementation and finalize the deliverables as soon as the quality checks are passed.

## 2026-09-21T01:02:05Z

User Request (via parent): Execute verification and audit task for the upcoming interface integration of the Underworld & Crime Rackets system (sim-crime / UnderworldCrimeSimulator). Monitor workspace files, verify zero-shadowing and statelessness rules, and prepare for test suite execution.

## 2026-09-21T01:07:00Z

# Teamwork Project Prompt

> Status: Launched

Use a very large team of agents.

Develop a massive experimental CampaignBehavior that explores all clan and character development hooks (dynastic succession, companion spawning, progression) and integrate the Underworld & Crime Rackets system (sim-crime / UnderworldCrimeSimulator) across the mod and desktop interfaces. The system must run statelessly using vanilla game states without requiring custom save data serialization.

Working directory: c:\Users\Alex\Documents\Mod Desarrolladores
Integrity mode: demo

## Requirements

### R1. Implement a Stateless Clan & Character CampaignBehavior
Maintain and verify the C# class inheriting from `CampaignBehaviorBase` in the `CalradiaForge.Mod` project. Hook into as many relevant `CampaignEvents` as possible related to heroes, clans, and character progression.

### R2. Adhere to Architecture Rules
Ensure the behavior avoids the 'Engine Initialization Crash Constraint' by deferring complex logic, and strictly follows the anti-shadowing rule (do not use `Campaign` in namespaces or class names). The behavior must be registered in the `MBSubModuleBase.OnGameStart` pipeline.

### R3. Interface Integration Parity
Integrate and audit the Underworld & Crime Rackets system (`sim-crime` in Gauntlet UI `CalradiaForge.xml` / `PanelViewModel.cs`, and `UnderworldCrimeSimulator` in WPF Desktop `MainWindow.xaml` / `MainWindow.xaml.cs`) with complete multi-language support.

## Acceptance Criteria

### Verification & Compliance
- [ ] The `CalradiaForge.sln` compiles successfully in Release mode without errors or warnings.
- [ ] A programmatic script (PowerShell and Python) verifies that no custom classes inherit from `SaveableTypeDefiner` and no data is synced in `SyncData`.
- [ ] Automated tests verify SubModule properly registers the new CampaignBehavior via `AddBehavior()`.
- [ ] Automated test suites pass 100% via the repository's `.bat` test launchers.
- [ ] Mod packages are generated in `artifacts/` (`CalradiaForge-13.3.0.zip`, `CalradiaForge-Modules-13.3.0.zip`, `CalradiaForge-Desktop-13.3.0.zip`).
