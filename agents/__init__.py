"""
Calradia Forge - Autonomous AI Agents System.

Powered by Google Antigravity SDK.
Provides specialized autonomous agents for Mount & Blade II: Bannerlord modding,
WPF workbench development, stateless behavior auditing, and bilingual documentation.
"""

__version__ = "25.2.0"
__author__ = "Calradia Forge Team"

from agents.config import (
    COMPACTION_PRESET_BALANCED,
    COMPACTION_PRESET_DEEP,
    COMPACTION_PRESET_ULTRA,
    COMPACTION_PRESETS,
    ForgeAgentConfig,
    create_forge_agent_config,
)
from agents.compactor import (
    CompactionStats,
    ForgeTokenCompactor,
    estimate_tokens,
    save_raw_log,
)
from agents.tools import (
    run_dotnet_build,
    verify_stateless_behavior,
    run_solution_tests,
    run_ui_automation_smoke,
    audit_documentation_parity,
    audit_ledger_integrity,
    inspect_csharp_source,
    audit_desktop_contracts,
    run_package_workflow,
)
from agents.subagents import (
    MASTER_AGENT_NAME,
    ARCHITECT_AGENT_NAME,
    STATELESS_AUDITOR_NAME,
    DESKTOP_WPF_NAME,
    DOC_LEDGER_NAME,
    get_subagent_configs,
)
from agents.orchestrator import ForgeAgentOrchestrator

__all__ = [
    "__version__",
    "ForgeAgentConfig",
    "create_forge_agent_config",
    "ForgeTokenCompactor",
    "CompactionStats",
    "COMPACTION_PRESET_ULTRA",
    "COMPACTION_PRESET_BALANCED",
    "COMPACTION_PRESET_DEEP",
    "COMPACTION_PRESETS",
    "estimate_tokens",
    "save_raw_log",
    "ForgeAgentOrchestrator",
    "MASTER_AGENT_NAME",
    "ARCHITECT_AGENT_NAME",
    "STATELESS_AUDITOR_NAME",
    "DESKTOP_WPF_NAME",
    "DOC_LEDGER_NAME",
    "get_subagent_configs",
    "run_dotnet_build",
    "verify_stateless_behavior",
    "run_solution_tests",
    "run_ui_automation_smoke",
    "audit_documentation_parity",
    "audit_ledger_integrity",
    "inspect_csharp_source",
    "audit_desktop_contracts",
    "run_package_workflow",
]

