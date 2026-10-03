# Calradia Forge in-game hook fixture

This test-only Bannerlord module provides one harmless, Forge-owned static method so a live menu smoke test can exercise registration, explicit Apply, Verify, and Revert without patching TaleWorlds or gameplay code.

Run `tools\Build-CalradiaForge-HookGameFixture.bat` to build and stage the module under an ignored `artifacts\hook-game-fixture-*` directory. The launcher does not install the module or start Bannerlord. When live validation is resumed, copy the staged `Modules\CalradiaForgeHookFixture` folder into the game's `Modules` directory, enable it, and test only from the main menu. The target reports `17` while registered or reverted and `29` while applied in the Calradia Forge session log. Revert the hook before disabling the fixture. Do not load a campaign, mission, or multiplayer session for this check.
