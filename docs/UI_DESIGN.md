# Calradia Forge UI design

Calradia Forge uses a developer's forge workbench: durable materials, dense evidence and one restrained ornamental mark. The interface must make diagnostic data easier to read, never compete with it.

## Tokens

| Role | Desktop | Native panel | Use |
|---|---|---|---|
| Coal | `#0D1714` | `#0C1411` | Application and result ground |
| Deep pine | `#13231E` | `#14231D` | Cards and structural surfaces |
| Tempered green | `#29463A` | `#1C332A` | Secondary actions and raised panels |
| Brass | `#C7A45A` | `#C7A45A` | Selection, boundaries and one active marker |
| Quiet brass | `#8F7748` | `#8F7748` | Dividers and subdued borders |
| Parchment | `#F1E6C8` | — | Desktop evidence ledger |
| Ink | `#28231B` | — | Desktop result text |
| Ember | `#BC6542` | — | Failure and caution state |

Georgia identifies the product and section titles. Segoe UI carries controls and descriptions. Consolas is reserved for payloads, reports and diagnostic data.

## Components and behavior

Both interfaces keep a section rail visible. Connection and report-file actions are global; scanning, inspection, tests and commands appear only in their relevant section. The native Summary has three status tiles and a session ledger, with brief tool guidance. Other native sections share a search field, contextual actions and a framed result area. Modules and Dependencies expose the primary ForgeWeave Framework action, while the historical Harmony query remains outside the active interface workflow. Search starts empty.

Buttons use three visual roles: primary brass-framed actions, tempered secondary actions and disabled actions. Keyboard focus receives a light outline that does not rely on color alone; the native selected state also retains a dark background under its parchment label. Native controls are 44 layout pixels high. Touch and controller navigation have not been validated.

The result area is a ledger, not a decoration. It has a readable title, a page indicator where applicable, left-aligned technical text and a fixed visible boundary. Empty results state what to do next. Known operational messages are localized; IDs, paths, JSON and arbitrary mod diagnostics remain intact as evidence.

The native result filter is a presentation-only query, separate from the tool argument and command history. Its live input and clear action share the evidence heading row so the filter does not consume the result viewport; the short localized “Evidence” heading stays fixed while the active route title remains in the workspace header, preventing long SDK names from clipping against the filter. The layout audit checks that the field remains usable at each reference size. While the panel is open, its query survives area navigation and filters newly produced output. Matching is case-insensitive and applies to output lines before the existing wrapping and pagination; an empty query shows the unfiltered result, and a localized empty-match state appears when no lines match. Filtering never rewrites the original output or retained evidence. All 13 generated game-language resources must carry the translations from the source catalogs.

Output comparison is separate from the existing object-snapshot comparison. Pinning stores a reference to the current raw output and its source route for the life of the panel. Compare outputs presents the baseline and current output in two columns, retaining identical lines and aligning insertions and deletions. Both sides wrap and paginate together in groups of 16 visual rows. The existing case-insensitive output filter searches both original sides and keeps a row pair when either side matches. An active comparison refreshes when new output arrives and its baseline survives area navigation. Clearing current output exits comparison view but keeps the baseline; Clear output baseline releases it explicitly. The comparison view never rewrites raw output, tool arguments, command history, clipboard content, exports or retained evidence. Inputs are limited to 256 Ki characters and 4,096 lines per side, with bounded alignment work; exceeding a configured bound yields a localized unavailable state without partial comparison, while ordinary output remains available. An oversized baseline pin is rejected without changing the current baseline.

The Tests area provides a structured, panel-lifetime inspector for the latest structurally valid `run` or `run-batch` response. Its scrollable list shows each result's technical ID, status and duration; selecting a row shows seed, context, `StartedAt`, steps, error and cleanup error in a separate detail surface. A batch is bounded to 51 displayed result records. Status values, IDs, timestamps and diagnostic text remain verbatim; only interface labels and empty-state copy are localized. Failed test cases remain visible as failures. Opening the inspector and selecting a case are presentation-only actions and must never execute or repeat a test. The result collection survives area navigation while the overlay closes on route changes; results are released when the panel closes. The inspector does not replace or mutate the raw ledger output, argument, command history, filter or output comparison.

The native maker's seal uses the attributed knight-banner mark. Module-owned flat brushes define hover, pressed, selected and disabled states; technical text uses the game's Fira Sans Extra Condensed font without heavy outlines. The same nine attributed Game-icons.net marks are assigned to native navigation and the Desktop workbench: compass, gears, scroll, magnifier, crossed swords, stopwatch, gear-hammer, banner and target. Each symbol is mapped by section purpose, recolored by the interface palette and kept inside a passive bounded slot; none crosses labels or result text. The Desktop header pairs three marks with localized Build, Inspect and Verify badges; area icons repeat in each tool's work-order seal, while the evidence ledger carries the scroll mark. Lucide-derived geometries remain limited to compact control glyphs. No decorative animation runs in diagnostic views. The native sprite definitions need Bannerlord's Resource Browser/TPAC import before in-game rendering can be confirmed.

Desktop's War Table, Parchment Light and High Contrast themes each own the full palette and brush set in one resource dictionary. Switching themes replaces the active dictionary, so surfaces, text, focus and status colors change together without walking the visual tree. Pinned and Recent keep their individual bounded viewports; Recent retains at most twelve entries and scrolls independently from the work-area list.

## Accessibility and localization

The desktop names inputs, navigation, results and the busy indicator for Windows automation. Body text scales from 100% to 200%; headings stop at 150% and compact labels retain their size. The heraldic seal uses one vector canvas, so its command diamond, crosshair and brass focal point share the same tactical axis. The ten-unit command disc is centred exactly at the 34/34 intersection of the diamond's horizontal and vertical lines, preserving the same geometry at every scale. The identity header grows with its content instead of laying rules across the title. The status region declares a polite live setting; screen-reader announcements have not been verified. The game panel follows Bannerlord's text language automatically through translation resources. English is the source and fallback; the desktop picker exposes all 13 installed Bannerlord languages, and the Build/Inspect/Verify badges are present in every language catalog.

Diagnostic color is always paired with text, icon or state wording. The application must not make a connection, test permission or destructive action look enabled when it is unavailable. Decorative images are excluded from the accessibility tree.
