"""Print the shared Calradia Forge release value used by build and package tooling."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
props = (root / "Directory.Build.props").read_text(encoding="utf-8")
match = re.search(r"<CalradiaForgeVersion>([^<]+)</CalradiaForgeVersion>", props)
if not match:
    raise SystemExit("CalradiaForgeVersion is missing from Directory.Build.props")
print(match.group(1))
