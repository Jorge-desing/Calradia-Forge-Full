# Calradia Forge 14.4.1 — short validation

## Reproduced and fixed

The real WPF window failed during Show() with the same XamlParseException as the user screenshot: a TwoWay binding cannot write RawResult on ToolPageViewModel. IsReadOnly on TextBox does not change the default binding mode. The output binding now explicitly uses OneWay. The ViewModel setter remains private.

The new Windows-only rendering executable references the real Desktop project and instantiates its compiled App resources, MainWindow and DataTemplates. It checks 190 routes, source-to-output notifications and the absence of writeback, then loads all 13 languages at four layout scales: 242 passing cases. The initial failing result is retained in artifacts/desktop-render-before.json. The successful run is in artifacts/desktop-render-tests.json, with a WPF-rendered preview at artifacts/desktop-render-tests.png. This is real layout validation, not an assertion that every label is fully translated or that no control clips at every scale.

## Short suite

- Release build: zero warnings and errors.
- Core and legacy suite: 222 passed, zero failed, with installed module scan skipped.
- ForgeWeave: 24 passed, zero failed.
- Desktop protocol and structural suite: 34 passed, zero failed.
- Actual WPF rendering: 242 cases passed, approximately 2.4 seconds in the observed run.
- Native resources: 394 keys with UTF-8 BOM and key parity across 13 languages. Missing retained translations fall back to English; key parity alone is not proof of complete translation. The two new focus labels have explicit translations in all 13 languages.
- Three native tests resolve prefab bindings against compiled properties/commands, verify decoration event flags and confirm focus preserves page, argument and result without a runtime session.

The build script regenerates resources before compilation. Both the short build and packaging invoke the real WPF regression executable. ForgeWeave and WPF render tests are now included in the solution, avoiding execution of stale test binaries.

## Game interface

The native panel adds four briefing cards, evidence heading and page count, and a Focus evidence toggle. The viewport grows from 372 to 552 logical pixels and the evidence font grows from 18 to 24. Both states end above the lower action rows. No game state, replay gate or save data is changed by focus. Framework and Extensions have direct navigation, and typing updates the argument immediately.

These native changes are compiled and contract-tested. Their appearance and interaction inside Bannerlord remain pending a short main-menu inspection; no campaign, battle or endurance test is claimed.

## Distribution

The three 14.4.1 archives remain separate. Desktop includes Run-CalradiaForge-Desktop.bat and has no app-host EXE. Archive auditing checks paths, XML, versions, hashes, required content and excluded game/save files. Read package-audit-1441.json for archive audit results.
