# Prevalidación de blueprints de parche

La Prevalidación de blueprints de parche permite que una extensión describa la intención de un parche antes de elegir o usar un framework de parches. Es una herramienta original e independiente de Forge: no requiere Harmony, MCM, ButterLib ni otro mod para compilarse o distribuirse.

La comprobación es solo de lectura. Nunca aplica, elimina, recarga, deshace, reordena ni invoca un parche. Que un destino se resuelva significa únicamente que la firma declarada coincide con un ensamblado ya cargado en la sesión actual. No garantiza que un framework pueda aplicar el parche ni que exista compatibilidad durante la ejecución.

Registra un `IPatchBlueprintProvider` mediante `ForgeApi.PatchBlueprints?.Register(...)` cuando Forge esté disponible. Su `Descriptor` debe usar `ChangesState=false`; Forge rechaza proveedores que declaren cambios de estado. Consulta el ejemplo completo en [PATCH_BLUEPRINTS.md](PATCH_BLUEPRINTS.md) y la extensión declarativa incluida en `examples/CalradiaForge.Examples/Examples.cs`.

Cada blueprint usa `PatchBlueprint`, `MethodReference` y `TypeReference`. `MethodReference.From(MethodBase)` y `TypeReference.From(Type)` ayudan a capturar firmas exactas. Declara el nombre simple del ensamblado, el nombre completo del tipo, el miembro y cada parámetro en orden. Para constructores usa `PatchMemberKind.Constructor`.

Forge consulta solamente ensamblados ya cargados: no usa `Assembly.Load`, no escanea tipos arbitrarios y no recibe destinos de reflexión desde el pipe. Informa destino resuelto, declaración inválida, ensamblado o tipo ausente, miembro no encontrado, ID duplicado o destino ambiguo. También registra como elementos de revisión las declaraciones Before/After que se refieren al mismo blueprint. No selecciona una sobrecarga parecida por aproximación ni deduce un orden de ejecución final.

El panel del juego ofrece **Prevalidación de parches** junto a Módulos y Dependencias. La aplicación de escritorio tiene una sección separada y puede mostrar una captura incluida en un informe sin conexión. Las acciones de protocolo `patch-blueprints` y `patch-preflight` son solo de lectura. Exportar un informe reutiliza la última captura explícita y no vuelve a ejecutar proveedores ni repite una captura de diagnóstico.
