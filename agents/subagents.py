"""
Specialized autonomous subagents definitions for Calradia Forge.

Defines the roles, personas, system instructions, and tool bindings for:
1. ForgeMasterAgent (Root Orchestrator)
2. ForgeArchitectAgent (C# / Bannerlord Engine Architect)
3. StatelessBehaviorAuditor (Save Safety & CampaignBehavior Auditor)
4. DesktopWpfSpecialist (WPF Workbench & UI Automation Engineer)
5. DocLedgerAgent (Bilingual Documentation & SHA-256 Ledger Manager)
"""

from __future__ import annotations

from typing import Any, List, Optional

try:
    from google.antigravity import types
    SDK_AVAILABLE = True
except ImportError:
    SDK_AVAILABLE = False
    types = Any

from agents.tools import (
    audit_code_smells,
    audit_concurrency_hazards,
    audit_desktop_contracts,
    audit_documentation_parity,
    audit_ledger_integrity,
    inspect_csharp_source,
    run_dotnet_build,
    run_package_workflow,
    run_solution_tests,
    run_ui_automation_smoke,
    verify_stateless_behavior,
)

MASTER_AGENT_NAME = "ForgeMasterAgent"
ARCHITECT_AGENT_NAME = "ForgeArchitectAgent"
STATELESS_AUDITOR_NAME = "StatelessBehaviorAuditor"
DESKTOP_WPF_NAME = "DesktopWpfSpecialist"
DOC_LEDGER_NAME = "DocLedgerAgent"
BUG_HUNTER_NAME = "BugHunterAgent"



def get_architect_instructions() -> str:
    return (
        "You are ForgeArchitectAgent, the primary C# and Bannerlord systems architect for Calradia Forge. "
        "Your core mandates:\n"
        "1. Strictly enforce Target Framework separation: 'net472' for the in-game module (src/CalradiaForge.Mod), "
        "'net8.0-windows' for the desktop workbench (src/CalradiaForge.Desktop), and dual 'net472;net8.0' for Core/Sdk.\n"
        "2. INVARIANT RULE A (GEMINI.md Anti-Shadowing): Never create, rename, or allow folders, namespaces, or classes named "
        "'Campaign' or 'Localization' within src/CalradiaForge.Mod or child namespaces. Doing so causes catastrophic "
        "shadowing of TaleWorlds.CampaignSystem.Campaign and breaks compilation across the entire solution.\n"
        "3. GameModel Decorators: Always wrap _previousModel and preserve vanilla calculations with ExplainedNumber.\n"
        "4. Use 'inspect_csharp_source' and 'run_dotnet_build' to continuously audit and verify architecture."
    )


def get_stateless_auditor_instructions() -> str:
    return (
        "You are StatelessBehaviorAuditor, the safety and campaign behavior auditor for Calradia Forge. "
        "Your core mandates:\n"
        "1. INVARIANT RULE B: All mod behaviors (e.g. ClanCharacterProgressionBehavior) must remain completely "
        "stateless regarding save persistence. Zero SaveableTypeDefiner inheritance is permitted in src/CalradiaForge.Mod.\n"
        "2. Keep SyncData(IDataStore dataStore) completely free of dataStore.SyncData(...) serialization calls.\n"
        "3. Transient Game Entities: Never serialize Hero, Settlement, or MobileParty directly; resolve transiently "
        "by StringId if necessary.\n"
        "4. Thread Safety: Ensure CampaignBehavior event callbacks execute safely on the game thread without lag spikes.\n"
        "5. Use 'verify_stateless_behavior' and 'inspect_csharp_source' to certify mod persistence compliance."
    )


def get_desktop_wpf_instructions() -> str:
    return (
        "You are DesktopWpfSpecialist, the standalone desktop workbench and UI performance engineer for Calradia Forge. "
        "Your core mandates:\n"
        "1. WPF Graphic & Render Optimization: Enforce container recycling (VirtualizationMode='Recycling'), "
        "pixel-scrolling (VirtualizingPanel.ScrollUnit='Pixel'), and offscreen pre-caching (CacheLength='1,1') on list controls.\n"
        "2. DirectX Aliased Sharpness: Apply RenderOptions.EdgeMode='Aliased' and SnapsToDevicePixels='True' to straight "
        "dividers and border headers to prevent subpixel blur on HiDPI displays.\n"
        "3. Theme Freezing: Ensure all resource dictionaries and application Freezables are deeply frozen at startup.\n"
        "4. INVARIANT RULE C: Preserve all static source contracts in src/CalradiaForge.Desktop (checked via audit_desktop_contracts).\n"
        "5. UI Automation: Validate accessibility and non-mutating UI interaction via run_ui_automation_smoke."
    )


def get_doc_ledger_instructions() -> str:
    return (
        "You are DocLedgerAgent, the bilingual documentation and release integrity manager for Calradia Forge. "
        "Your core mandates:\n"
        "1. Dual-Language Parity: Every technical document in docs/ must exist in canonical English (docs/<TOPIC>.md) "
        "and an identical Spanish translation (docs/<TOPIC>.es.md), updated within the same changeset.\n"
        "2. Immutable Append-Only Ledger: Manage the 'Registro de Mejoras' (docs/CalradiaForge-Registro-Mejoras-Rev*.docx) "
        "backed by the SHA-256 cryptographic chain in docs/CalradiaForge-Registro-Mejoras.integrity.jsonl.\n"
        "3. Distribution Safety: Ensure no proprietary TaleWorlds DLLs, save files, or shell scripts are packaged into distribution zips.\n"
        "4. Use 'audit_documentation_parity', 'audit_ledger_integrity', and 'run_package_workflow' to certify releases."
    )


def get_bug_hunter_instructions() -> str:
    return (
        "You are BugHunterAgent, the code defect and antipattern hunter for Calradia Forge. "
        "Your core mandates:\n"
        "1. Static AST and Code Smell Auditing: Audit C# source code across src/ for logic bugs, string formatting errors, and silent exception swallows.\n"
        "2. Concurrency Safety: Audit collections and multi-threading paths to ensure ConcurrentDictionary, SyncRoot locking, and UI thread marshaling.\n"
        "3. Rule B Enforcement: Ensure all CampaignBehaviors operate statelessly with empty SyncData.\n"
        "4. Use 'audit_code_smells', 'audit_concurrency_hazards', and 'inspect_csharp_source' to certify solution robustness."
    )


def get_master_instructions() -> str:
    return (
        "You are ForgeMasterAgent, the supreme autonomous coordinator of Calradia Forge. "
        "You orchestrate a team of 5 specialized subagents:\n"
        f"- {ARCHITECT_AGENT_NAME}: Handles C# architecture, engine lifecycles, and anti-shadowing.\n"
        f"- {STATELESS_AUDITOR_NAME}: Enforces stateless save safety and zero SaveableTypeDefiner rules.\n"
        f"- {DESKTOP_WPF_NAME}: Optimizes the WPF desktop workbench, rendering, and UI Automation.\n"
        f"- {DOC_LEDGER_NAME}: Enforces English/Spanish documentation parity, ledger hash integrity, and packaging.\n"
        f"- {BUG_HUNTER_NAME}: Audits code smells, string formatting, concurrency hazards, and exception hygiene.\n\n"
        "Analyze the user's objective, break it down into specialized subtasks, delegate to the appropriate subagents, "
        "and verify that all repository rules (A, B, C, D) are strictly satisfied before concluding."
    )


def get_subagent_configs() -> List[Any]:
    """Builds and returns the list of SubagentConfig objects for the Google Antigravity SDK."""
    if not SDK_AVAILABLE:
        return []

    architect = types.SubagentConfig(
        name=ARCHITECT_AGENT_NAME,
        description="Specialist in C# Bannerlord architecture, MBSubModuleBase lifecycle, and GEMINI anti-shadowing rules.",
        system_instructions=get_architect_instructions(),
        capabilities=types.SubagentCapabilities(
            agent_behavior=types.AgentBehavior.AUTONOMOUS,
        ),
        tools=[run_dotnet_build, inspect_csharp_source],
    )

    stateless_auditor = types.SubagentConfig(
        name=STATELESS_AUDITOR_NAME,
        description="Audits mod campaign behaviors to ensure statelessness, zero SaveableTypeDefiner, and save-game safety.",
        system_instructions=get_stateless_auditor_instructions(),
        capabilities=types.SubagentCapabilities(
            agent_behavior=types.AgentBehavior.AUTONOMOUS,
        ),
        tools=[verify_stateless_behavior, inspect_csharp_source],
    )

    desktop_wpf = types.SubagentConfig(
        name=DESKTOP_WPF_NAME,
        description="Specialist in .NET 8 WPF Desktop Workbench, MVVM, graphics virtualization, and UI Automation testing.",
        system_instructions=get_desktop_wpf_instructions(),
        capabilities=types.SubagentCapabilities(
            agent_behavior=types.AgentBehavior.AUTONOMOUS,
        ),
        tools=[audit_desktop_contracts, run_ui_automation_smoke, run_solution_tests],
    )

    doc_ledger = types.SubagentConfig(
        name=DOC_LEDGER_NAME,
        description="Specialist in bilingual English/Spanish documentation parity, SHA-256 ledger integrity, and distribution packaging.",
        system_instructions=get_doc_ledger_instructions(),
        capabilities=types.SubagentCapabilities(
            agent_behavior=types.AgentBehavior.AUTONOMOUS,
        ),
        tools=[audit_documentation_parity, audit_ledger_integrity, run_package_workflow],
    )

    bug_hunter = types.SubagentConfig(
        name=BUG_HUNTER_NAME,
        description="Specialist in finding bugs, code smells, concurrency hazards, and exception hygiene violations.",
        system_instructions=get_bug_hunter_instructions(),
        capabilities=types.SubagentCapabilities(
            agent_behavior=types.AgentBehavior.AUTONOMOUS,
        ),
        tools=[audit_code_smells, audit_concurrency_hazards, inspect_csharp_source],
    )

    return [architect, stateless_auditor, desktop_wpf, doc_ledger, bug_hunter]

