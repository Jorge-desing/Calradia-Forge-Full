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
