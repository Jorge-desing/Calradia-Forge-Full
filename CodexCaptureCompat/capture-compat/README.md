# Codex Capture Compat · 构建与使用

此目录包含 Windows 10 Computer Use 截图兼容层的源码、构建脚本和测试工具。项目功能介绍见[项目 README](../README.md)，内部机制见[实现说明](docs/implementation.md)。

下文的命令均在本目录执行，helper 路径使用占位示例。

## 构建要求

| 依赖 | 要求 |
| --- | --- |
| Windows | x64 构建环境；完整实机回归面向 Windows 10 |
| PowerShell | Requerido por la lógica protegida de instalación, validación y empaquetado; los puntos de entrada principales de compilación y pruebas son `.bat` |
| Visual Studio 或 Build Tools | 安装 MSVC x64/x86 C++ 工具，支持 C++17 |
| MASM | 提供 `ml64.exe`，用于 x64 导出转发入口 |
| Windows SDK | 包含 C++/WinRT 头文件、`IGraphicsCaptureSession3` 和 Direct3D 11 开发库 |

构建脚本通过 Visual Studio Installer 的 `vswhere.exe` 查找带有 `Microsoft.VisualStudio.Component.VC.Tools.x86.x64` 组件的最新安装，随后进入 x64 host / x64 target 开发环境。通常不需要预先在 Developer Command Prompt 中设置环境变量。

项目直接使用 MSVC 编译和链接，不依赖 CMake、Cargo 或 npm。

## 构建

### 普通构建

```cmd
build.bat
```

En el checkout del proyecto, el BAT inicializa MSVC x64, compila el DLL, las pruebas y el probe, ejecuta las pruebas unitarias mediante `Run-CodexCaptureCompatUnitTests.bat` y copia los artefactos aprobados a `dist/`. Requiere Visual C++ x64/x86 Build Tools y MASM. Los ZIP de distribución no incluyen BAT; si solo tienes el ZIP, su `build.ps1` sigue siendo el fallback.

主要选项是 C++17、`/O2`、静态 CRT `/MT`、`/guard:cf`，并在链接时启用 `/dynamicbase` 和 `/nxcompat`。

### 诊断构建

```cmd
build.bat -Trace
```

`-Trace` 定义 `CAPTURE_COMPAT_TRACE`，在调用路径上增加诊断钩子和文本日志。它仍使用优化编译，且会覆盖同一组 `build/`、`dist/` 产物；不是一个独立的 Debug 输出目录。

普通构建不写 Trace 文件。切换回普通构建后，如需更新正在使用的 DLL，还要按升级流程重新安装并重启 helper。

### 构建产物

| 路径 | 说明 |
| --- | --- |
| `dist/version.dll` | 部署用代理 DLL |
| `dist/compat_probe.exe` | 可单独运行的 WGC 探针 |
| `dist/capture_test_window.exe` | Computer Use 隔离 fixture，提供无副作用按钮 |
| `build/core_tests.exe` | COM 边框兼容测试 |
| `build/dispatch_tests.exe` | 回调派发测试 |
| `build/*.obj`、`build/proxy.lib` 等 | 编译和链接中间产物 |

再次构建前关闭正在运行的探针和测试窗口，以免覆盖 EXE 时出现文件占用错误。构建脚本不会自动安装 DLL。

## 测试

```cmd
# 文件安装逻辑测试；使用 validation/ 下的临时文件
tests\Test-CodexCaptureCompatInstall.bat

# 真实 WGC 捕获测试；需要正常交互桌面
.\validate.ps1
```

实机验证会显示探针自己的窗口，并生成 `validation/report.json` 和 `validation/capture.bmp`。基线测试明确预期 Windows 10 缺少边框接口，不能将它直接当作跨 Windows 版本的通用通过标准。

`tools\Build-Test-CodexCaptureCompat.bat` ejecuta compilación, pruebas de instalación y benchmark. `tools\Test-CodexCaptureCompatLatency.bat --self-test` verifica el cálculo de medianas sin abrir una ventana de captura. Los `.exe` de pruebas solo se invocan desde BAT. Para el smoke test interactivo, usa `tests\Start-CodexCaptureCompatFixture.bat`; la ventana ofrece un botón sin efectos externos y se cierra automáticamente tras diez minutos.

探针参数、输出解释和各测试的覆盖范围见[验证与排错指南](docs/validation.md)。

## 安装与使用

使用预编译 Release 时，保留 ZIP 中的完整目录结构，从解压后的 `capture-compat` 目录执行安装命令即可。发布包已包含 `dist/` 中的三个程序文件，不需要先运行构建脚本。下载与校验说明见 [CI 与 Release 指南](docs/ci.md)。

### 确认目标路径

安装对象是实际运行的 `codex-computer-use.exe`。可以在它运行时只读查看路径：

```powershell
Get-Process -Name codex-computer-use -ErrorAction SilentlyContinue |
    Select-Object Id, Path
```

如果有多个结果，确认当前 Computer Use 使用的是哪一个 runtime；不要根据某个旧安装目录批量部署。进程未运行或权限不足时，列表可能为空或不显示路径。

### 首次安装

```cmd
Install-CodexCaptureCompat.bat "C:\path\to\codex-computer-use.exe" Install -WhatIf

rem Cierra el helper que usa el DLL antes de instalar o desinstalar.
Install-CodexCaptureCompat.bat "C:\path\to\codex-computer-use.exe" Install
```

`-Action Install` 是默认值，可以省略。安装后重新启动 Computer Use，继续使用其原有接口。此 DLL 不需要作为独立程序启动。

脚本只向目标目录写入：

```text
codex-computer-use.exe            已有的目标 helper
version.dll                      本项目代理
codex-capture-compat.install.json 安装记录
```

代理必须放在 helper 同目录。安装脚本不查找所有 runtime、不修改系统目录、不修改注册表，也不会终止或重启进程。
En el checkout del proyecto, el BAT sirve como punto de entrada y delega la validación y escritura a `install.ps1`. Los ZIP de distribución no incluyen BAT y usan `install.ps1`. Un BAT puro no conserva las comprobaciones de ruta, propiedad SHA-256 y registro JSON. Si Avast bloquea la operación, usa una excepción dirigida al script local en vez de retirar esas comprobaciones.

### 安装参数

| 参数 | 含义 |
| --- | --- |
| `-HelperPath <路径>` | 必填；必须指向已存在、文件名为 `codex-computer-use.exe` 的文件 |
| `-Action Install` | 安装 `dist/version.dll` |
| `-Action Uninstall` | 按安装记录卸载本项目代理 |
| `-WhatIf` | 预览操作，不复制或删除文件；路径与已有文件校验仍会执行 |

安装记录包含 helper 路径、helper 哈希、DLL 哈希和安装时间。helper 哈希用于记录目标构建；卸载时实际核验的是目标路径和 DLL 哈希。

若已有 DLL 与待安装文件不同，脚本会拒绝覆盖。若 DLL 哈希相同且安装记录存在，则报告已安装。不要通过直接覆盖未知 DLL 来处理此错误。

### 升级

先构建新版本，再退出使用该 DLL 的 helper：

```cmd
Install-CodexCaptureCompat.bat "C:\path\to\codex-computer-use.exe" Uninstall
Install-CodexCaptureCompat.bat "C:\path\to\codex-computer-use.exe" Install
```

随后重新启动 Computer Use。卸载根据旧安装记录核验已安装文件，不要求它与刚构建出的新 DLL 相同。

### 卸载

```cmd
# 先退出使用该 DLL 的 helper
Install-CodexCaptureCompat.bat "C:\path\to\codex-computer-use.exe" Uninstall
```

脚本要求安装记录存在、记录路径匹配，并确认待删除 DLL 的哈希未改变。只删除本项目代理和安装记录，保留 helper。卸载后重新启动 Computer Use，恢复原有加载行为。

如果记录缺失或哈希不符，应先确认文件来源；脚本不会猜测哪个 DLL 可以删除。

## Experimental CUA keyboard alias patch / Parche experimental de aliases de teclado CUA

This separate, opt-in manager changes only key-name aliases in the installed `@oai/sky` JavaScript adapter. It is allowlisted for runtime `b35a688736d56912`, `@oai/sky` 0.7.4, the exact package path below, package manifest SHA-256 `14F88DA4A41E71B878B8045C3FD77D74CE6E9BA212007A407DE98A7D92B2B246`, and adapter input SHA-256 `617D8E6E18FDDE25F06D4CBA2C84C994E076E05F30A8C55D09E918401C48B171`. It rejects unknown paths, versions, hashes, reparse-point paths, or a non-unique patch anchor. The expected patched adapter SHA-256 is `F40CEF88BF48349EA769FE6A10D290D2B25F1623D504C37EE11BA30A10CD4517`.

The patch normalizes only tokens handled by the `press_key` chord parser: `Control`, `Ctrl`, `Control_L`, and `Ctrl_L` (case-insensitive) become `Ctrl_L`; `Control_R` and `Ctrl_R` become `Ctrl_R`. Other key tokens pass through unchanged. It does not alter `type_text`, the native helper, screenshots, public APIs, IPC, the MSIX, or `WindowsApps`; it does not add held-key or modifier-click support. It stores a verified original backup and ownership manifest under `%LOCALAPPDATA%\OpenAI\Codex\CodexCaptureCompat\SkyKeyAliases`, serializes `Apply` and `Rollback` with a three-second named-mutex wait, and preserves the replacement sidecar if replacement or hash verification fails. Close Codex and its Computer Use helper before Apply or Rollback; this manager never terminates processes.

Run from Command Prompt. Both paths are required and must match this exact runtime profile:

```cmd
set "RUNTIME=%LOCALAPPDATA%\OpenAI\Codex\runtimes\cua_node\b35a688736d56912"
set "SKY=%RUNTIME%\bin\node_modules\@oai\sky"
Manage-CodexCaptureCompatKeyAliases.bat Test "%RUNTIME%" "%SKY%"
Manage-CodexCaptureCompatKeyAliases.bat Status "%RUNTIME%" "%SKY%"
Manage-CodexCaptureCompatKeyAliases.bat Apply "%RUNTIME%" "%SKY%"
Manage-CodexCaptureCompatKeyAliases.bat Rollback "%RUNTIME%" "%SKY%"
```

`Test` computes and checks the prospective output hash without writing. `Status` is read-only and reports an absent manifest, a verified interrupted `Prepared` transaction, an applied patch, or an inconsistent/corrupt state. `Apply` requires the exact original hashes, creates and verifies the original backup before replacing the adapter, then verifies the new hash. `Rollback` restores a target with the known patched hash from that verified backup. If the original hash is still present with a valid `Prepared` manifest, Rollback verifies the backup and clears only that owned manifest; it does not replace the target. Unknown or failed states are preserved for manual review; do not edit the installed package by hand. Invoke the manager through its `.bat`, not a test executable.

Este administrador optativo modifica únicamente los aliases de nombres de tecla del adaptador JavaScript instalado de `@oai/sky`. Está permitido solo para el runtime `b35a688736d56912`, `@oai/sky` 0.7.4, la ruta exacta del paquete indicada arriba, el SHA-256 del manifiesto `14F88DA4A41E71B878B8045C3FD77D74CE6E9BA212007A407DE98A7D92B2B246` y el SHA-256 original del adaptador `617D8E6E18FDDE25F06D4CBA2C84C994E076E05F30A8C55D09E918401C48B171`. Rechaza rutas, versiones, hashes o anclas desconocidas y rutas con puntos de reanálisis. El SHA-256 esperado del adaptador modificado es `F40CEF88BF48349EA769FE6A10D290D2B25F1623D504C37EE11BA30A10CD4517`.

El parche normaliza solo los tokens procesados por el chord parser de `press_key`: `Control`, `Ctrl`, `Control_L` y `Ctrl_L` (sin distinguir mayúsculas) pasan a `Ctrl_L`; `Control_R` y `Ctrl_R` pasan a `Ctrl_R`. Los demás nombres se conservan. No modifica `type_text`, el helper nativo, capturas, APIs públicas, IPC, el MSIX ni `WindowsApps`, y no añade teclas sostenidas ni modificadores para clics. Guarda un respaldo original verificado y un manifiesto de propiedad en `%LOCALAPPDATA%\OpenAI\Codex\CodexCaptureCompat\SkyKeyAliases`, serializa `Apply` y `Rollback` con un mutex de tres segundos y conserva el sidecar de reemplazo si falla el reemplazo o la verificación del hash. Cierra Codex y su helper Computer Use antes de aplicar o revertir; el administrador no termina procesos.

Ejecuta desde el Símbolo del sistema. Ambos paths son obligatorios y deben coincidir con este perfil exacto:

```cmd
set "RUNTIME=%LOCALAPPDATA%\OpenAI\Codex\runtimes\cua_node\b35a688736d56912"
set "SKY=%RUNTIME%\bin\node_modules\@oai\sky"
Manage-CodexCaptureCompatKeyAliases.bat Test "%RUNTIME%" "%SKY%"
Manage-CodexCaptureCompatKeyAliases.bat Status "%RUNTIME%" "%SKY%"
Manage-CodexCaptureCompatKeyAliases.bat Apply "%RUNTIME%" "%SKY%"
Manage-CodexCaptureCompatKeyAliases.bat Rollback "%RUNTIME%" "%SKY%"
```

`Test` calcula y comprueba el hash previsto sin escribir. `Status` es de solo lectura e informa si no existe manifiesto, si hay una transacción `Prepared` interrumpida con respaldo verificado, si el parche está aplicado o si el estado es inconsistente/corrupto. `Apply` exige los hashes originales exactos, crea y verifica el respaldo antes de reemplazar el adaptador y comprueba el hash nuevo. `Rollback` restaura un destino con el hash conocido del parche usando el respaldo verificado. Si el hash original sigue presente junto a un manifiesto `Prepared` válido, Rollback verifica el respaldo y elimina únicamente ese manifiesto de propiedad; no reemplaza el adaptador. Los estados desconocidos o fallidos se conservan para revisión manual; no edites el paquete instalado a mano. Ejecuta el administrador mediante el `.bat`, no mediante un ejecutable de pruebas.

## 诊断日志

安装 `-Trace` 构建后，新启动的目标进程可写入：

```text
%TEMP%\codex-capture-compat-<PID>.log
```

这里的 `%TEMP%` 是 helper 进程自己的临时目录。日志记录系统启动后的毫秒数、线程 ID、操作、对象地址和 HRESULT，不保存截图或窗口文本。日志写入失败不会中断原调用。

也可通过只读导出 `CodexCaptureCompatGetStatus` 获取当前进程的统计，接口及字段说明见[实现说明](docs/implementation.md)。

## 部署限制

- 原生支持的边框接口继续由系统处理；缺失接口时的兼容路径保留系统默认捕获边框。
- 更新宿主程序后，应重新确认 helper 路径、导入项及 DLL 加载方式。
- 已加载模块不能靠替换磁盘文件完成热更新，必须重启相应 helper。
- 卸载失败时优先检查文件占用与安装记录，具体错误处理见[验证与排错指南](docs/validation.md)。
