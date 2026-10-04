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

## Plano de página Gauntlet dentro del juego

Abre la paleta de navegación con `Ctrl+P` y selecciona **Plano de página Gauntlet** en el grupo de principiantes. La ruta enfoca el campo del título; seleccionarla no genera contenido. Escribe un título y usa la acción normal de generar. El texto completo se divide en cuatro secciones: XML del prefab `GUI/Prefabs`, ViewModel, entradas de localización en inglés e integración con SubModule.

El título se reduce a letras, números y espacios y se limita a 48 caracteres antes de usarse en identificadores o textos de localización generados. Una entrada vacía o compuesta solo por símbolos se convierte en `Tools`; se conservan letras y números Unicode. El plano solo devuelve texto y nunca crea ni sobrescribe archivos del módulo del usuario. Sustituye cada marcador `your_unique_module_page_prefix` por el mismo prefijo corto, único y en minúsculas. Debe coincidir en el atributo `ForgeUiPage`, la llamada `ForgeUI.OpenPage` y los IDs de localización; el ID completo de página debe respetar el límite de 96 caracteres del SDK. Sustituye `YOUR_EXACT_MODULE_FOLDER_ID` por el ID exacto de la carpeta que contiene el ensamblado y el directorio `GUI/Prefabs`. `YourMod.UI` es un ejemplo separado de namespace C# válido y se cambia independientemente cuando haga falta.

Integra las entradas inglesas en el recurso de cadenas en inglés que ya referencia el `SubModule.xml` del módulo destino. Fusiona el registro y la baja con los overrides existentes `OnSubModuleLoad` y `OnSubModuleUnloaded`. No añadas overrides, callbacks de disponibilidad ni llamadas `AutoRegister` duplicados para un ensamblado y propietario que ya estén registrados. **Refresh** y **Close** son comandos de demostración: Refresh solo cambia el texto de estado del ejemplo y Close solicita cerrar la página de extensión. Al registrar la página, Forge valida la carpeta propietaria, el prefab, el ViewModel y los enlaces `Command.Click`.

## Compositor de páginas Gauntlet dentro del juego

Abre la paleta de navegación con `Ctrl+P` y elige **Gauntlet Page Composer** en el grupo Novice. El ID de la ruta es `novice-gauntlet-composer`. Su espacio dedicado incluye el título de página, un catálogo y una lista ordenada de componentes, un editor de propiedades, una vista previa de muestra y las acciones **Save Draft**, **Generate** y **Copy Package**. Es una ruta independiente de **Gauntlet Page Blueprint**.

Puedes añadir hasta 12 componentes: encabezado, texto, campo editable, botón, estado/métrica, lista, alternador, barra de progreso y selector. Cada ID permanece asociado a su bloque al cambiar el orden. Las listas y los selectores admiten de 1 a 8 filas u opciones editables. El título de página tiene un máximo de 48 caracteres; las etiquetas y los textos, de 128 caracteres.

La vista previa usa una plantilla Gauntlet fija enlazada a `MBBindingList` y datos locales de muestra. Los botones, alternadores y selectores de la vista previa solo modifican ese modelo de muestra; no ejecutan acciones de campaña ni cargan XML generado.

El borrador se guarda de forma explícita. **Save Draft** escribe el esquema versión 1 en `%LOCALAPPDATA%\CalradiaForge\gauntlet-composer.json`; el archivo tiene un límite de 64 KiB. Al abrir la ruta se carga el último borrador válido. Si el archivo no existe, el lienzo queda vacío. Si está dañado, excede el límite o usa una versión de esquema desconocida, el compositor sigue disponible e informa el estado de carga; conserva el archivo existente hasta que el usuario guarde explícitamente un reemplazo.

**Generate** requiere al menos un componente y crea un paquete de texto completo con cuatro partes: el prefab XML de Gauntlet, un ViewModel de C#, las entradas inglesas de localización y las instrucciones de integración en SubModule. Antes de habilitar **Copy Package**, el generador comprueba que los bindings XML y comandos correspondan a miembros del ViewModel generado, que las plantillas de lista tengan su colección y contexto de fila, y que los IDs de localización concuerden en todo el paquete. Los errores de validación impiden copiar un paquete no válido. **Copy Package** copia toda la salida generada, incluidas todas sus secciones, no solo la página visible actualmente.

El compositor genera texto en memoria. No crea ni modifica archivos del módulo del usuario y no cambia la API pública del SDK.

## Constructor de Reglas de Campaña dentro del juego

Abre la paleta de navegación con `Ctrl+P` y selecciona **Constructor de Reglas de Campaña** en el grupo Novice. Su ID de ruta es `novice-campaign-rule-builder`. La columna izquierda contiene una lista ordenada de hasta ocho reglas. La derecha permite editar la regla elegida como **Cuando / Si / Entonces** y muestra una vista previa con datos de muestra fijos. Cada regla conserva su ID al cambiar de posición. Las áreas con desplazamiento interno mantienen disponibles las acciones inferiores **Guardar borrador**, **Validar**, **Generar** y **Copiar paquete** en ventanas pequeñas.

La primera visita sin un borrador guardado muestra la lista vacía; **Añadir regla** crea la primera. También puede aparecer una lista al cargar un borrador válido con reglas. Nota histórica: el commit `22359f4` creaba brevemente una regla inicial sin guardar en memoria, y cambiar su evento editaba esa misma fila sin pulsar Añadir ni Guardar. El commit `08f4eaa` eliminó ese comportamiento. Ver o seleccionar una fila por sí solo no demuestra quién la añadió o guardó.

El nombre del evento es un botón cíclico, no un catálogo ni un desplegable. Cada clic avanza al siguiente de siete eventos: `WeeklyTickEvent`, `DailyTickHeroEvent`, `DailyTickSettlementEvent`, `HeroComesOfAgeEvent`, `HeroGainedSkill`, `OnHeroJoinedPartyEvent` y `OnClanCreatedEvent`. Al cambiar de evento se vacían los dos grupos de condiciones, así que elige el evento antes de añadir condiciones. Una regla admite dos grupos de hasta tres condiciones cada uno. Dentro de un grupo se combinan con AND; el grupo B es una alternativa OR al grupo A. Las condiciones y los objetivos disponibles dependen de los datos del evento elegido. Las acciones permiten conceder oro, cambiar influencia, conceder renombre o cambiar la relación del jugador con el héroe del evento. Las combinaciones incompatibles de evento, acción y objetivo se rechazan antes de generar.

`DailyTickHeroEvent` y `DailyTickSettlementEvent` son eventos diarios por entidad. Cada regla se evalúa para el héroe o asentamiento entregado por el evento; las acciones solo se ejecutan cuando coinciden las condiciones de la regla. Por eso, una recompensa dirigida al jugador puede repetirse entre las ocurrencias coincidentes de varios héroes o asentamientos. `WeeklyTickEvent` ocurre una vez por semana de campaña. La vista previa y el informe de validación generado señalan los dos eventos diarios por entidad.

La vista previa solo evalúa datos de muestra. **Validar** comprueba el borrador sin ejecutar acciones de campaña. **Generar** exige al menos una regla y produce un paquete de texto completo: una clase `CampaignBehaviorBase` para `net472`, instrucciones para integrarla en el override existente de `OnGameStart` y un informe de validación. Las reglas que comparten evento usan una sola suscripción `AddNonSerializedListener`. El comportamiento generado mantiene `SyncData` vacío, no usa `SaveableTypeDefiner` y protege contra reentrada. **Copiar paquete** copia toda la salida aunque se presente por páginas. Revisa y adapta las reglas antes de integrarlas en otro módulo: una vez instaladas allí, sus acciones pueden alterar una partida guardada en cada evento coincidente.

El guardado es explícito. La primera vez que se abre sin un archivo guardado, el constructor muestra una lista vacía y ninguna regla seleccionada. Solo **Añadir regla** crea una regla; después, al pulsar el nombre del evento se edita la regla seleccionada. **Guardar borrador** escribe la lista actual —también si está vacía intencionalmente— de forma atómica con el esquema versión 1 en `%LOCALAPPDATA%\CalradiaForge\campaign-rule-builder.json`, con un límite de 64 KiB. No se puede generar un paquete con la lista vacía y **Generar** permanece deshabilitado hasta añadir una regla. Si el borrador está dañado, supera el límite o usa una versión futura, se informa del problema y se conserva hasta que el usuario guarde un reemplazo. El constructor no registra los comportamientos generados, no escribe archivos fuente del módulo ni modifica la campaña actual.

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

La cabecera heráldica usa un recorte de 512×100 de los píxeles del master por encima de alfa 8, con cuatro píxeles de margen de origen, para conservar las dos hojas de brújula y la regla inferior. El rail conserva todo el arte en 256×504 para su espacio de 128×256 DIP; así queda espacio para el margen del generador dentro del atlas fijo de 4096×512. Si cambia cualquiera de estas dimensiones, vuelve a generar el atlas y el `SpriteData` generado con SpriteSheetGenerator oficial. No edites a mano los rectángulos generados ni reutilices los recortes pequeños de Gauntlet en WPF.

Los ocho adornos fuente heredados `forge_header_*_v1` se habían creado a 128×64 y se mostraban a 24×12, donde sus detalles se aliasaban. Se retiran de la generación activa de sprites y se conservan, junto con sus copias preparadas y de SpriteParts, en `assets/gauntlet-imagegen/archive/2026-09-28/`; `SHA256SUMS.txt` registra sus hashes archivados. Los iconos semánticos de navegación y las señales de foco existentes siguen indicando el estado de navegación.

El ledger normal de evidencia debe conservar al menos 160 DIP en el viewport de auditoría 1280×720. En el explorador de resultados de pruebas, estado y duración usan celdas separadas con recorte y al menos 6 DIP de separación, para impedir que los valores largos invadan la columna vecina. El auditor visual Gauntlet comprueba estos contratos geométricos en 1220×880, 1280×720, 1600×900 y 1920×1080; sigue siendo evidencia de fuente/disposición, no una garantía del render en vivo.

Se mantienen como ruta de diagnóstico el flanco ascendente exclusivo de F10 y la telemetría de apertura/cierre de `GauntletLayer` en `SubModule.cs`. La regresión Core valida estructura del código y ciclo de vida, no la entrada de teclado en vivo ni la aserción nativa. La importación por Resource Browser y el render de F10 dentro del juego deben quedar pendientes hasta observarlos directamente; que pase `--check` de texturas o una auditoría de fuente no demuestra importación ni ejecución.
