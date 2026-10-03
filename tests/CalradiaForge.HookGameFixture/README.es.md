# Fixture de hooks de Calradia Forge dentro del juego

Este módulo de Bannerlord, exclusivo para pruebas, aporta un método estático inocuo y propiedad de Forge para que un smoke test en vivo pueda comprobar registro, Apply explícito, Verify y Revert sin modificar TaleWorlds ni código de juego.

Ejecuta `tools\Build-CalradiaForge-HookGameFixture.bat` para compilar y preparar el módulo en una carpeta ignorada `artifacts\hook-game-fixture-*`. El launcher no instala el módulo ni inicia Bannerlord. Cuando se reanude la validación en vivo, copia la carpeta preparada `Modules\CalradiaForgeHookFixture` a `Modules` del juego, habilítala y prueba solo desde el menú principal. El objetivo informa `17` mientras está registrado o revertido y `29` cuando está aplicado, en el registro de sesión de Calradia Forge. Revierte el hook antes de deshabilitar la fixture. No cargues campaña, misión ni multijugador para esta comprobación.
