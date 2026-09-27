#!/usr/bin/env python3
"""
Calradia Forge - Autonomous Agents Unified CLI Runner.

Executes specialized multi-agent workflows powered by Google Antigravity SDK:
  python tools/run_forge_agents.py audit
  python tools/run_forge_agents.py verify
  python tools/run_forge_agents.py docs
  python tools/run_forge_agents.py architect
  python tools/run_forge_agents.py run "Audit persistence safety and verify docs parity"

Compaction options:
  --compaction-preset [ultra|balanced|deep]  Set context window compaction threshold (default: balanced = 16k)
  --raw-tools                                Bypass semantic distillation and stream raw uncompacted tool output
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

# Ensure repo root is on sys.path
REPO_ROOT = Path(__file__).resolve().parent.parent
if str(REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(REPO_ROOT))

from agents.config import (
    COMPACTION_PRESET_BALANCED,
    COMPACTION_PRESET_DEEP,
    COMPACTION_PRESET_ULTRA,
    COMPACTION_PRESETS,
    ForgeAgentConfig,
)
from agents.compactor import ForgeTokenCompactor
from agents.memory import CoALAAgentMemory
from agents.orchestrator import ForgeAgentOrchestrator
from agents.tools import (
    audit_code_smells,
    audit_concurrency_hazards,
    audit_desktop_contracts,
    audit_documentation_parity,
    audit_ledger_integrity,
    audit_section_playbooks,
    inspect_csharp_source,
    run_dotnet_build,
    run_package_workflow,
    run_solution_tests,
    run_ui_automation_smoke,
    verify_stateless_behavior,
)


def _apply_compactor_settings(args: argparse.Namespace) -> ForgeAgentConfig:
    """Configures ForgeAgentConfig and ForgeTokenCompactor according to CLI flags."""
    if getattr(args, "raw_tools", False):
        ForgeTokenCompactor.raw_mode = True
    else:
        ForgeTokenCompactor.raw_mode = False

    preset = getattr(args, "compaction_preset", COMPACTION_PRESET_DEEP)
    threshold = COMPACTION_PRESETS.get(preset, 32_000)

    return ForgeAgentConfig(
        model=getattr(args, "model", "gemini-3.8-flash"),
        offline_mode=getattr(args, "offline", False),
        verbose=getattr(args, "verbose", False),
        compaction_preset=preset,
        token_threshold=threshold,
        raw_tools=getattr(args, "raw_tools", False),
    )


def _print_compaction_telemetry() -> None:
    """Prints cumulative token savings telemetry if tools were executed."""
    if ForgeTokenCompactor.history:
        print("\n" + ForgeTokenCompactor.get_cumulative_summary())


def cmd_audit(args: argparse.Namespace) -> int:
    """Executes full repository architectural and safety audits."""
    print("=== [ForgeAgent CLI] Executing Repository Audit ===")
    config = _apply_compactor_settings(args)
    orch = ForgeAgentOrchestrator(config)
    report = orch.run_task_sync("Audit C# anti-shadowing rules, stateless campaign behaviors, and desktop static contracts")
    print(report)
    _print_compaction_telemetry()
    return 0


def cmd_verify(args: argparse.Namespace) -> int:
    """Executes the test suite and UI Automation verification."""
    print("=== [ForgeAgent CLI] Executing Solution Verification ===")
    config = _apply_compactor_settings(args)
    orch = ForgeAgentOrchestrator(config)
    report = orch.run_task_sync("Execute full solution tests and Windows UI Automation smoke check")
    print(report)
    _print_compaction_telemetry()
    return 0


def cmd_docs(args: argparse.Namespace) -> int:
    """Audits bilingual documentation parity and ledger cryptographic integrity."""
    print("=== [ForgeAgent CLI] Executing Documentation & Ledger Audit ===")
    _apply_compactor_settings(args)
    parity_report = audit_documentation_parity(raw=args.raw_tools)
    ledger_report = audit_ledger_integrity(raw=args.raw_tools)
    print("\n" + parity_report)
    print("\n" + ledger_report)
    _print_compaction_telemetry()
    return 0


def cmd_architect(args: argparse.Namespace) -> int:
    """Builds the solution and validates C# architecture and target frameworks."""
    print("=== [ForgeAgent CLI] Executing C# Architecture Validation ===")
    _apply_compactor_settings(args)
    build_report = run_dotnet_build(configuration=args.configuration, raw=args.raw_tools)
    inspect_report = inspect_csharp_source(raw=args.raw_tools)
    print("\n" + build_report)
    print("\n" + inspect_report)
    _print_compaction_telemetry()
    return 0


def cmd_playbooks(args: argparse.Namespace) -> int:
    """Audits section playbooks, troubleshooting trees, and personalization macros."""
    print("=== [ForgeAgent CLI] Auditing Section Playbooks & Personalization ===")
    _apply_compactor_settings(args)
    report = audit_section_playbooks(raw=args.raw_tools)
    print("\n" + report)
    _print_compaction_telemetry()
    return 0


def cmd_bughunt(args: argparse.Namespace) -> int:
    """Audits code smells, logic bugs, concurrency hazards, and persistence safety."""
    print("=== [ForgeAgent CLI] Executing Bug Hunter & Concurrency Audit ===")
    _apply_compactor_settings(args)
    smells_report = audit_code_smells(raw=args.raw_tools)
    concurrency_report = audit_concurrency_hazards(raw=args.raw_tools)
    print("\n" + smells_report)
    print("\n" + concurrency_report)
    _print_compaction_telemetry()
    return 0


def cmd_compact(args: argparse.Namespace) -> int:
    """Benchmarks and reports semantic token compaction across all 12 repository tools."""
    print("=== [ForgeAgent CLI] Executing Comprehensive Token Compaction Benchmark ===")
    config = _apply_compactor_settings(args)
    memory = CoALAAgentMemory(
        objective="Comprehensive Token Compaction Benchmark across all 12 Tools",
        context_token_budget=config.token_threshold,
    )
    live = getattr(args, "live", False)
    results = []

    # 1-8: Fast tools that run directly in < 0.1s
    tool_executors = [
        ("inspect_csharp_source", lambda: inspect_csharp_source(raw=False)),
        ("verify_stateless_behavior", lambda: verify_stateless_behavior(raw=False)),
        ("audit_desktop_contracts", lambda: audit_desktop_contracts(raw=False)),
        ("audit_documentation_parity", lambda: audit_documentation_parity(raw=False)),
        ("audit_ledger_integrity", lambda: audit_ledger_integrity(raw=False)),
        ("audit_section_playbooks", lambda: audit_section_playbooks(raw=False)),
        ("audit_code_smells", lambda: audit_code_smells(raw=False)),
        ("audit_concurrency_hazards", lambda: audit_concurrency_hazards(raw=False)),
    ]

    for tool_name, func in tool_executors:
        out = func()
        stats = ForgeTokenCompactor.last_stats
        if stats:
            memory.record_step(
                agent_name="BenchmarkRunner",
                action=tool_name,
                summary=out.splitlines()[0] if out else "OK",
                stats=stats,
            )
            results.append(stats)

    # 9-12: Heavy tools: run live if requested, or test representative real-world output
    heavy_tools = [
        ("run_dotnet_build", lambda: run_dotnet_build(raw=False), (
            "Build SUCCESS for CalradiaForge.sln [Release]:\n"
            "Microsoft (R) Build Engine version 17.11.4+5622a130a for .NET\n"
            "Copyright (C) Microsoft Corporation. All rights reserved.\n\n"
            "  Determining projects to restore...\n"
            "  All projects are up-to-date for restore.\n"
            "  CalradiaForge.Core -> C:\\repo\\bin\\Release\\net472\\CalradiaForge.Core.dll\n"
            "  CalradiaForge.Mod -> C:\\repo\\bin\\Release\\net472\\CalradiaForge.Mod.dll\n"
            "  CalradiaForge.Desktop -> C:\\repo\\bin\\Release\\net8.0-windows\\CalradiaForge.Desktop.dll\n"
            "Build succeeded.\n"
            "    0 Warning(s)\n"
            "    0 Error(s)\n"
            "Time Elapsed 00:00:03.42"
        )),
        ("run_solution_tests", lambda: run_solution_tests(raw=False), (
            "Ran 8 tests in 0.066s\nOK\n"
            "CalradiaForge.Tests: 239 passed\n"
            "RESULT: 71 passed, 0 failed\n"
            "RESULT: 54 passed, 0 failed\n"
            "PASS 282 WPF render cases; 17200 ms. No game session or tool execution.\n"
            "PERF 150 render/layout passes, 2311.7 ms in those calls; visual-tree snapshots 89 builds / 1282 hits / 119410 visited nodes.\n"
            "All selected Calradia Forge test suites passed."
        )),
        ("run_ui_automation_smoke", lambda: run_ui_automation_smoke(raw=False), (
            "=== Calradia Forge Windows UI Automation Smoke Verification ===\n"
            "[1/3] Searching for active Calradia Forge Desktop window (Process: CalradiaForge.Desktop)...\n"
            "  Window handle resolved: 0x002104BC ('Calradia Forge - Standalone Assembly Workbench')\n"
            "[2/3] Querying UI Automation element tree and accessible controls...\n"
            "  Found 48 interactive automation elements (Buttons, Tabs, TextBoxes).\n"
            "[3/3] Testing tab navigation and accessibility patterns...\n"
            "  Tab 'Assembly Inspector': InvokePattern OK\n"
            "  Tab 'Telemetry & Profiler': InvokePattern OK\n"
            "All UI Automation smoke tests passed successfully."
        )),
        ("run_package_workflow", lambda: run_package_workflow(raw=False), (
            "=== Calradia Forge Distribution Packaging Workflow ===\n"
            "[1/5] Building Release configuration for all targets... [OK]\n"
            "[2/5] Verifying zero Zone.Identifier NTFS streams... [OK]\n"
            "[3/5] Compiling DocFX static site and generating in-game help... [OK]\n"
            "[4/5] Creating distribution archives:\n"
            "  - artifacts/CalradiaForge-Release-1.2.0.zip (SHA-256: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855)\n"
            "  - artifacts/CalradiaForge-Desktop-1.2.0.zip (SHA-256: 7852b855e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b)\n"
            "  - artifacts/CalradiaForge-Source-SDK-1.2.0.zip (SHA-256: 991b7852b855e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495)\n"
            "[5/5] Packaging completed successfully in 18.4s."
        )),
    ]

    for tool_name, live_func, repr_text in heavy_tools:
        if live:
            out = live_func()
            stats = ForgeTokenCompactor.last_stats
        else:
            out, stats = ForgeTokenCompactor.distill(tool_name, repr_text, save_raw=True)
        if stats:
            memory.record_step(
                agent_name="BenchmarkRunner",
                action=tool_name,
                summary=out.splitlines()[0] if out else "OK",
                stats=stats,
            )
            results.append(stats)

    # Format Markdown Table
    table_lines = [
        "",
        "| Tool Identifier | Status | Raw Tokens | Compacted | Saved | Reduction | Forensic Log |",
        "| :--- | :---: | :---: | :---: | :---: | :---: | :--- |",
    ]
    for s in results:
        status_badge = "FAIL" if s.has_errors else "PASS"
        ratio_pct = f"{s.compression_ratio * 100:.1f}%"
        log_name = s.log_file if s.log_file else "memory-only"
        table_lines.append(
            f"| `{s.tool_name}` | **{status_badge}** | {s.raw_tokens} | {s.compacted_tokens} | {s.tokens_saved} | **-{ratio_pct}** | `{log_name}` |"
        )

    print("\n".join(table_lines))
    print("\n" + memory.render_context())
    _print_compaction_telemetry()
    return 0


def cmd_run(args: argparse.Namespace) -> int:
    """Executes an arbitrary autonomous task using the multi-agent orchestrator."""
    prompt = " ".join(args.prompt).strip()
    if not prompt:
        print("Error: No prompt provided for 'run' command.")
        return 1

    print(f"=== [ForgeAgent CLI] Executing Autonomous Task: '{prompt}' ===")
    config = _apply_compactor_settings(args)
    orch = ForgeAgentOrchestrator(config)
    report = orch.run_task_sync(prompt)
    print(report)
    _print_compaction_telemetry()
    return 0


def main() -> int:
    common_parser = argparse.ArgumentParser(add_help=False)
    common_parser.add_argument(
        "--offline",
        action="store_true",
        help="Force offline deterministic simulation mode (does not require GEMINI_API_KEY).",
    )
    common_parser.add_argument(
        "--verbose",
        "-v",
        action="store_true",
        help="Enable verbose output streaming.",
    )
    common_parser.add_argument(
        "--model",
        default="gemini-3.8-flash",
        help="Gemini model identifier (default: gemini-3.8-flash).",
    )
    common_parser.add_argument(
        "--compaction-preset",
        default=COMPACTION_PRESET_DEEP,
        choices=[COMPACTION_PRESET_ULTRA, COMPACTION_PRESET_BALANCED, COMPACTION_PRESET_DEEP],
        help="Context window compaction threshold preset: ultra (8k), balanced (16k), deep (32k).",
    )
    common_parser.add_argument(
        "--raw-tools",
        action="store_true",
        help="Bypass ForgeTokenCompactor and output uncompressed raw console logs.",
    )

    parser = argparse.ArgumentParser(
        prog="run_forge_agents",
        description="Calradia Forge Autonomous Agents CLI Runner (Google Antigravity SDK)",
        parents=[common_parser],
    )

    subparsers = parser.add_subparsers(dest="command", help="Available subcommands")

    # audit subcommand
    subparsers.add_parser("audit", parents=[common_parser], help="Audit C# rules, stateless behaviors, and contracts.")

    # verify subcommand
    subparsers.add_parser("verify", parents=[common_parser], help="Run solution test suites and UI Automation checks.")

    # docs subcommand
    subparsers.add_parser("docs", parents=[common_parser], help="Verify English/Spanish docs parity and SHA-256 ledger integrity.")

    # architect subcommand
    arch_parser = subparsers.add_parser("architect", parents=[common_parser], help="Compile and validate C# architecture.")
    arch_parser.add_argument(
        "--configuration",
        "-c",
        default="Release",
        choices=["Release", "Debug"],
        help="Build configuration (default: Release).",
    )

    # playbooks subcommand
    subparsers.add_parser("playbooks", parents=[common_parser], help="Audit section playbooks, troubleshooting trees, and procedural macros.")

    # bughunt subcommand
    subparsers.add_parser("bughunt", parents=[common_parser], help="Audit code smells, antipatterns, and concurrency hazards.")

    # compact subcommand
    compact_parser = subparsers.add_parser("compact", parents=[common_parser], help="Benchmark and report token compaction across all 12 tools.")
    compact_parser.add_argument(
        "--live",
        action="store_true",
        help="Execute all tools live, including heavy compiler, test, and packaging runs.",
    )

    # run subcommand
    run_parser = subparsers.add_parser("run", parents=[common_parser], help="Run arbitrary autonomous multi-agent task.")
    run_parser.add_argument("prompt", nargs="+", help="Task prompt for ForgeMasterAgent.")

    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        return 0

    commands = {
        "audit": cmd_audit,
        "verify": cmd_verify,
        "docs": cmd_docs,
        "architect": cmd_architect,
        "playbooks": cmd_playbooks,
        "bughunt": cmd_bughunt,
        "compact": cmd_compact,
        "run": cmd_run,
    }

    cmd_fn = commands.get(args.command)
    if cmd_fn:
        return cmd_fn(args)

    return 1


if __name__ == "__main__":
    sys.exit(main())
