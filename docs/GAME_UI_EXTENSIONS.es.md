# Extensiones de interfaz Gauntlet

Calradia Forge ofrece un catálogo SDK optativo para páginas Gauntlet independientes. No inserta controles en el prefab compartido de Forge ni parchea la interfaz de otros módulos. Cada página registrada usa su propio prefab XML dentro de `GUI/Prefabs` del módulo propietario, abierto como una capa Gauntlet separada.

## Declarar y registrar una página

Referencia el SDK de Forge y el ensamblado `TaleWorlds.Library` de Bannerlord. Coloca el ensamblado del ViewModel en `bin/Win64_Shipping_Client` del módulo y el prefab en `GUI/Prefabs`.

```csharp
using CalradiaForge.Sdk;
using TaleWorlds.Library;

[ForgeUiPage("my_mod.tools", "ForgeToolsPage", "MyMod.Tools.Title", Context = Context.Any)]
public sealed class ForgeToolsPageViewModel : ViewModel
{
    [DataSourceProperty] public string Title => "My tools";

    [ForgeUiCommand("close", nameof(ExecuteClose), Context = Context.Any, ChangesState = false)]
    public void ExecuteClose() => ForgeUI.ClosePage();
}
```

El módulo propietario registra únicamente su ensamblado cuando Forge ya está disponible:

```csharp
ForgeApi.AutoRegister(typeof(ForgeToolsPageViewModel).Assembly, "MyMod");
```

El catálogo valida IDs únicos de página/comando, carpeta propietaria, contención y existencia del prefab, constructor público sin parámetros del ViewModel, firmas de métodos, contextos válidos y coincidencia de nombres `Command.Click` en el prefab. Los errores se registran por página y no impiden abrir Forge. `ForgeUI.OpenPage("my_mod.tools")` solicita la página cuando el panel de Forge está abierto; Escape o `ForgeUI.ClosePage()` cierra la capa. Las llamadas desde otro hilo se ponen en cola para el hilo del juego.

El host comprueba el contexto de la página y sus comandos antes de abrirla. Los comandos declarados como modificadores de estado requieren modo de pruebas y, en campaña, confirmación de una copia. Los métodos Gauntlet ejecutan directamente el código de la extensión en el hilo del juego. Los atributos describen y protegen la apertura; no aíslan una extensión, no revocan permisos posteriormente ni convierten en seguro un mod no confiable. Instala solo extensiones de confianza.

## Prefab de ejemplo

```xml
<Prefab>
  <Window>
    <Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent">
      <Children>
        <TextWidget Text="@Title" />
        <ButtonWidget Command.Click="ExecuteClose">
          <Children><TextWidget Text="Close" /></Children>
        </ButtonWidget>
      </Children>
    </Widget>
  </Window>
</Prefab>
```

Consulta `CalradiaForgeExamples/GUI/Prefabs/ForgeExamplesPage.xml` y `ForgeExamplesPageViewModel` para ver un ejemplo completo y mínimo. Al descargar el módulo, el propietario elimina sus páginas con `ForgeApi.UnregisterUiPages("MyMod")`.

## Ayuda contextual e iconos

La acción Help presenta resúmenes breves sin conexión derivados de los comentarios XML del SDK y traducidos al idioma activo de Bannerlord. DocFX solo se utiliza al compilar. Modules incluye el SVG original de Game-icons.net y PNG transparentes atribuidos para sprites; SpriteSheetGenerator y Resource Browser de Bannerlord deben empaquetarlos en los recursos gráficos del juego antes de que un prefab Gauntlet los referencie.

## Arte y disposición de la mesa táctica integrada

El overlay integrado de la mesa táctica de Calradia Forge es distinto del catálogo de páginas de terceros descrito arriba. Su composición Gauntlet de primera parte se genera desde `tools/generate_assets.py`, se enlaza mediante `src/CalradiaForge.Mod/PanelViewModel.cs` y se muestra con `modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml`. Mantén el arte y el registro de sprites dentro del flujo Gauntlet; los recursos WPF son independientes y no deben reutilizarse como sprites Gauntlet.

Los masters originales creados con ImageGen se conservan en `assets/gauntlet-imagegen/` con sus dimensiones y procedencia SHA-256. Los masters de Rev082 son:

| Master | Dimensiones y modo | SHA-256 |
| --- | --- | --- |
| `forge_war_table_cloth_v3_master.png` | 2172×724 RGB | `32206FCD23A74379412DAC373CB26984553A6404CB9A732495D10700C2ADF552` |
| `forge_heraldic_rail_v4_master.png` | 887×1774 RGB | `9EA66B7F9F91828539D637EC2EE939E61BD7A8B66238CF7426C8420EE7F80FCF` |
| `forge_heraldic_header_v3_master.png` | 2048×768 RGBA, alfa 0–254 | `FA854C634D051EBD9D98909DD9F332963FD09FA78A628B69FFE6DFF78CAC0083` |

Ejecuta `tools/Prepare-CalradiaForge-ImageGenTextures.bat --prepare --no-pause` para derivar los sprites de tamaño fijo y alfa limitado; después usa `tools/generate_assets.py` y el launcher SpriteSheetGenerator del proyecto para generar prefab, `SpriteData` y atlas fuente. `--check` comprueba el flujo de fuentes preparadas; no demuestra la importación del atlas ni el render en el juego. Mantén los adornos pasivos y fuera del ledger de evidencia, campos editables, botones y texto de resultados.

Los ocho adornos fuente heredados `forge_header_*_v1` se habían creado a 128×64 y se mostraban a 24×12, donde sus detalles se aliasaban. Se retiran de la generación activa de sprites y se conservan, junto con sus copias preparadas y de SpriteParts, en `assets/gauntlet-imagegen/archive/2026-09-28/`; `SHA256SUMS.txt` registra sus hashes archivados. Los iconos semánticos de navegación y las señales de foco existentes siguen indicando el estado de navegación.

El ledger normal de evidencia debe conservar al menos 160 DIP en el viewport de auditoría 1280×720. En el explorador de resultados de pruebas, estado y duración usan celdas separadas con recorte para impedir que los valores largos invadan la columna vecina. El auditor visual Gauntlet comprueba estos contratos geométricos en 1220×880, 1280×720, 1600×900 y 1920×1080; sigue siendo evidencia de fuente/disposición, no una garantía del render en vivo.

Se mantienen como ruta de diagnóstico el flanco ascendente exclusivo de F10 y la telemetría de apertura/cierre de `GauntletLayer` en `SubModule.cs`. La regresión Core valida estructura del código y ciclo de vida, no la entrada de teclado en vivo ni la aserción nativa. La importación por Resource Browser y el render de F10 dentro del juego deben quedar pendientes hasta observarlos directamente; que pase `--check` de texturas o una auditoría de fuente no demuestra importación ni ejecución.
