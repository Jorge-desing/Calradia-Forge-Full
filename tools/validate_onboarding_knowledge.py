"""Check repository onboarding links and mirrored root instructions without UI access."""

import re
from pathlib import Path
from urllib.parse import unquote, urlparse


ROOT = Path(__file__).resolve().parents[1]


def main():
    errors = []
    guides = [(ROOT / name).read_text(encoding="utf-8-sig")
              for name in ("AGENTS.md", "CODEX.md", "GEMINI.md")]
    start = "For SDK packages, .NET templates and static content, follow"
    end = "Preserve upstream snapshots and unrelated staged work."
    shared = []
    for guide in guides:
        if start not in guide or end not in guide:
            errors.append("Missing shared SDK onboarding instructions in a root guide")
            continue
        shared.append(" ".join(guide.split(start, 1)[1].split(end, 1)[0].split()))
    if len(shared) == 3 and len(set(shared)) != 1:
        errors.append("SDK onboarding instructions differ between root guides")

    files = [ROOT / ".agents/rules/developer_onboarding.md",
             ROOT / ".agents/skills/calradia-forge-dotnet/references/sdk-onboarding.md",
             ROOT / "docs/SDK_EVOLUTION.md", ROOT / "docs/SDK_EVOLUTION.es.md"]
    for path in files:
        content = path.read_text(encoding="utf-8-sig")
        for target in re.findall(r"\[[^\]]*\]\(([^)]+)\)", content):
            target = target.strip("<>").split("#", 1)[0]
            if not target or urlparse(target).scheme:
                continue
            if not (path.parent / unquote(target)).resolve().exists():
                errors.append(f"Broken local link in {path.relative_to(ROOT)}: {target}")

    english = (ROOT / "docs/SDK_EVOLUTION.md").read_text(encoding="utf-8")
    spanish = (ROOT / "docs/SDK_EVOLUTION.es.md").read_text(encoding="utf-8")
    # Technical literals stay identical even when explanatory prose is localized.
    literals_en = set(re.findall(r"`([^`\n]+)`", english))
    literals_es = set(re.findall(r"`([^`\n]+)`", spanish))
    if literals_en != literals_es:
        errors.append("Technical literals differ between the English and Spanish onboarding guides")

    for error in errors:
        print("FAIL:", error)
    print(f"Onboarding knowledge: {len(errors)} issue(s)")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
