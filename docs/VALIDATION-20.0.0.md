# Calradia Forge 20.0.0 validation

## Automated checks

- Release solution build: passed with 0 warnings and 0 errors.
- ForgeWeave suite: 24 passed, 0 failed.
- Desktop transport and MVVM suite: 38 passed, 0 failed.
- WPF render suite: 261 cases passed, including the declared workbench routes, language/scale coverage, theme samples, and assembly inspection/edit safety fixtures.
- Localization audit: 13 native languages with 394 keys each; 13 Desktop dictionaries with matching resource keys.
- Desktop structural visual matrix: 55 short cases passed.
- DocFX: local static documentation build passed with no warnings or errors.
- Archive audit: recorded by `artifacts/package-audit-2000.json` after package generation.

## Assembly workbench coverage

The render test fixtures inspect a managed assembly and reject malformed or unsupported native input. They verify preview writes no output, patching keeps the input hash unchanged, creates a source backup, and verifies output metadata. The write path uses a temporary file and removes partial output on failure. DLLs are inspected as metadata and are not loaded or executed by the Desktop analyzer.

## Limits

The legacy combined test executable was stopped after remaining silent for more than one minute; its result is therefore **not run to completion**. The focused suites above were run separately and passed. No Bannerlord process, campaign, battle, or long-duration test was run. Live game integration remains unverified in this release validation.
