"""
Configuration factory and environment loader for Calradia Forge Autonomous Agents.

Integrates with Google Antigravity SDK LocalAgentConfig, discovering repo skills,
setting budget controls, and configuring multi-agent capability ceilings.
"""

from __future__ import annotations

import os
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, List, Optional

try:
    from google.antigravity import LocalAgentConfig, types
    SDK_AVAILABLE = True
except ImportError:
    SDK_AVAILABLE = False
    LocalAgentConfig = Any
    types = Any


def get_repo_root() -> Path:
    """Returns the absolute path to the repository root."""
    return Path(__file__).resolve().parent.parent


def get_skills_path() -> Path:
    """Returns the absolute path to the .agents/skills directory."""
    skills_dir = get_repo_root() / ".agents" / "skills"
    return skills_dir


COMPACTION_PRESET_ULTRA = "ultra"       # 8,000 tokens: hyper-compact for low-latency / resource limits
COMPACTION_PRESET_BALANCED = "balanced" # 16,000 tokens: standard optimal multi-agent context
COMPACTION_PRESET_DEEP = "deep"         # 32,000 tokens: deep reasoning context for complex tasks

COMPACTION_PRESETS: dict[str, int] = {
    COMPACTION_PRESET_ULTRA: 8_000,
    COMPACTION_PRESET_BALANCED: 16_000,
    COMPACTION_PRESET_DEEP: 32_000,
}


@dataclass
class ForgeAgentConfig:
    """Configuration container for Calradia Forge Autonomous Agents."""

    repo_root: Path = field(default_factory=get_repo_root)
    skills_path: Path = field(default_factory=get_skills_path)
    model: str = "gemini-3.8-flash"
    api_key: Optional[str] = None
    offline_mode: bool = False
    max_subagent_depth: int = 2
    max_model_calls: int = 30
    max_tool_calls: int = 60
    max_total_tokens: int = 300_000
    compaction_preset: str = COMPACTION_PRESET_DEEP
    token_threshold: int = 32_000
    raw_tools: bool = False
    verbose: bool = False

    def __post_init__(self) -> None:
        if self.compaction_preset in COMPACTION_PRESETS and self.token_threshold == 32_000:
            self.token_threshold = COMPACTION_PRESETS[self.compaction_preset]
        if not self.api_key:
            self.api_key = os.environ.get("GEMINI_API_KEY")
        if not self.api_key:
            # Check .env in repo root if present
            env_file = self.repo_root / ".env"
            if env_file.exists():
                try:
                    for line in env_file.read_text(encoding="utf-8").splitlines():
                        line = line.strip()
                        if line.startswith("GEMINI_API_KEY="):
                            self.api_key = line.split("=", 1)[1].strip().strip('"').strip("'")
                            break
                except Exception:
                    pass

    @property
    def has_api_credentials(self) -> bool:
        """Indicates whether API credentials are available for cloud execution."""
        return bool(self.api_key and self.api_key.strip())


def create_sdk_agent_config(
    forge_config: ForgeAgentConfig,
    tools: Optional[List[Any]] = None,
    subagents: Optional[List[Any]] = None,
    system_instructions: Optional[str] = None,
) -> Any:
    """
    Creates and returns a google.antigravity.LocalAgentConfig configured for Calradia Forge.
    """
    if not SDK_AVAILABLE:
        raise RuntimeError("google-antigravity SDK is not installed in the current environment.")

    # Validate skills path
    skills_paths: List[str] = []
    if forge_config.skills_path.exists():
        skills_paths.append(str(forge_config.skills_path.resolve()))

    capabilities = types.CapabilitiesConfig(
        enable_subagents=True,
        max_subagent_depth=forge_config.max_subagent_depth,
        allowed_subagents=[sa.name for sa in (subagents or [])],
        agent_behavior=types.AgentBehavior.AUTONOMOUS,
        run_command_config=types.RunCommandConfig(enable_sandbox=False),
    )

    budget_config = types.BudgetConfig(
        max_model_calls=forge_config.max_model_calls,
        max_tool_calls=forge_config.max_tool_calls,
        max_total_tokens=forge_config.max_total_tokens,
    )

    compaction_config = types.CompactionConfig(
        token_threshold=forge_config.token_threshold,
    )

    instructions = system_instructions or (
        "You are ForgeMasterAgent, the autonomous orchestrator for the Calradia Forge codebase. "
        "You supervise Bannerlord C# mod development, WPF workbench architecture, stateless behavior "
        "auditing, and dual-language documentation parity. Coordinate your specialized subagents to "
        "fulfill the user's objective thoroughly and safely."
    )

    config_kwargs: dict[str, Any] = {
        "model": forge_config.model,
        "capabilities": capabilities,
        "budget_config": budget_config,
        "compaction_config": compaction_config,
        "system_instructions": instructions,
    }

    if forge_config.api_key:
        config_kwargs["api_key"] = forge_config.api_key

    if skills_paths:
        config_kwargs["skills_paths"] = skills_paths

    if tools:
        config_kwargs["tools"] = tools

    if subagents:
        config_kwargs["subagents"] = subagents

    return LocalAgentConfig(**config_kwargs)


def create_forge_agent_config(
    model: str = "gemini-3.8-flash",
    offline_mode: bool = False,
    verbose: bool = False,
    compaction_preset: str = COMPACTION_PRESET_DEEP,
    raw_tools: bool = False,
    token_threshold: Optional[int] = None,
) -> ForgeAgentConfig:
    """Helper to instantiate a standard ForgeAgentConfig."""
    threshold = token_threshold if token_threshold is not None else COMPACTION_PRESETS.get(compaction_preset, 32_000)
    return ForgeAgentConfig(
        model=model,
        offline_mode=offline_mode,
        verbose=verbose,
        compaction_preset=compaction_preset,
        token_threshold=threshold,
        raw_tools=raw_tools,
    )

