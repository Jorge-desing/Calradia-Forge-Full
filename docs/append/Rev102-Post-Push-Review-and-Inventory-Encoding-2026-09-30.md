# Rev102 — Post-push review and inventory encoding

**Version:** Calradia Forge 25.2.0; unchanged.

The Git synchronization rule and development skill now require checking the exact remote SHA, Actions runs, check runs and commit statuses after every authorized push. Synchronization and remote validation must be reported separately. Task-related failures require log inspection, relevant BAT tests, a corrective push and review of the new SHA. Pending checks do not pass; historical failures are preserved. Successful remote evidence is reported without creating an endless evidence-only commit cycle.

Remote CI on d59db82 passed ForgeWeave 73/73, Desktop 65/65 and WPF 293/293. Its aggregate step still failed because Windows PowerShell decoded an unmarked UTF-8 en dash in TpacInventory.psm1 using the runner ANSI code page, producing a parser error. The diagnostic range now uses an ASCII hyphen. The inventory BAT includes a parser regression using Windows-1252 decoding and passed locally. The development skill passed its BAT quick validator. The ledger workflow for d59db82 passed.

Remote validation of this correction remains pending until push. No runtime patch, display setting, game session, ZIP or product-version change was made. Previous revisions remain unchanged.
