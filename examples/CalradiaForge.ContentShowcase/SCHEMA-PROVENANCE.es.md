# Procedencia de esquemas

El XML del ejemplo se comprobó contra los XSD de la instalación de Bannerlord el 2026-10-01. La ruta local fue `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord`; el verificador acepta otra raíz mediante `--game-root` y lee los archivos directamente, sin copiar datos propietarios del juego al repositorio.

| Fuente instalada | Propósito | SHA-256 capturado el 2026-10-01 |
| --- | --- | --- |
| `XmlSchemas/Items.xsd` | Estructura del elemento Item y `ItemComponent` | `E57D50EA6B0C6FD8356F4A26AAB7593B410242C6F4C457E4F1A4226961B9E266` |
| `XmlSchemas/NPCCharacters.xsd` | `EquipmentRoster` en línea y estructura del equipo | `912DDCB8BECB1315D9A579C81C109C420FD0391FFCF0D8AB53B49DEFE0B2A687` |
| `Modules/SandBoxCore/ModuleData/items/weapons.xml` | Malla, cuerpo, clase de arma y uso del `horse_whip` de Native | `9C1BF93DB03C47895C010595B76BCADD7D9C398E5CFE8BBA42BDF0C8DAA0F4BC` |

El verificador compila los XSD instalados actuales y valida los archivos generados en cada ejecución. También compara el ítem de muestra con el registro `horse_whip` instalado y comprueba que la tropa referencia `Item.cfcs_practice_whip`, declarado en este módulo. El informe BAT imprime los hashes SHA-256 actuales de ambos XSD y de `weapons.xml`. Los hashes de la tabla identifican las fuentes observadas al crear el ejemplo; si el hash local difiere, se debe revisar como un cambio de datos del juego, no tratar como corrupción.

La validación de esquema solo establece la estructura XML. No demuestra que el juego acepte todos los valores de juego, cargue correctamente el módulo, incorpore la tropa estática a una campaña o renderice la página Gauntlet. Esas comprobaciones requieren validación por separado dentro del motor.
