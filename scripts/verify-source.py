#!/usr/bin/env python3
"""Static PH-07 through PH-10 source checks that do not require Windows or .NET.

This script deliberately makes no runtime claims. It verifies repository structure,
XML/XAML integrity, event-handler presence, version consistency, exclusion rules,
and a small set of known regression tokens.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Iterable

XAML_NS = "http://schemas.microsoft.com/winfx/2006/xaml"
EVENT_ATTRIBUTES = {
    "Click",
    "Closing",
    "DragEnter",
    "DragLeave",
    "DragOver",
    "Drop",
    "Loaded",
    "MediaEnded",
    "MediaFailed",
    "MediaOpened",
    "MouseDoubleClick",
    "MouseLeftButtonDown",
    "MouseLeftButtonUp",
    "MouseMove",
    "PreviewKeyDown",
    "SelectionChanged",
    "SizeChanged",
    "TextChanged",
    "ValueChanged",
}
FORBIDDEN_SOURCE_TOKENS = (
    "System.Windows.WpfDragEventArgs",
    "System.Windows.Controls.WpfComboBox",
    "System.Windows.Wpf",
    "System.Windows.Controls.Wpf",
)
EXCLUDED_TOP_LEVEL = {"bin", "obj", "artifacts", ".git", ".vs", "__pycache__"}
BINARY_SUFFIXES = {
    ".exe", ".dll", ".pdb", ".so", ".dylib", ".mp4", ".mkv", ".mov",
    ".avi", ".mp3", ".wav", ".flac", ".png", ".jpg", ".jpeg", ".webp", ".pyc",
}


@dataclass
class Check:
    name: str
    passed: bool
    detail: str


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def local_name(name: str) -> str:
    return name.rsplit("}", 1)[-1]


def iter_source_files(root: Path) -> Iterable[Path]:
    for path in root.rglob("*"):
        if not path.is_file():
            continue
        if any(part in EXCLUDED_TOP_LEVEL for part in path.relative_to(root).parts):
            continue
        yield path


def method_exists(code: str, handler: str) -> bool:
    pattern = re.compile(
        rf"\b(?:private|protected|public|internal)?\s*(?:async\s+)?(?:void|Task|ValueTask)\s+{re.escape(handler)}\s*\(",
        re.MULTILINE,
    )
    return bool(pattern.search(code))


def read_version(project_file: Path) -> str:
    root = ET.parse(project_file).getroot()
    values = [node.text.strip() for node in root.iter() if local_name(node.tag) == "Version" and node.text]
    if len(values) != 1:
        raise ValueError(f"Expected exactly one <Version>; found {len(values)}")
    return values[0]


def parse_installer_version(installer_file: Path) -> str:
    text = installer_file.read_text(encoding="utf-8")
    match = re.search(r"^#define\s+MyAppVersion\s+\"([^\"]+)\"", text, re.MULTILINE)
    if not match:
        raise ValueError("MyAppVersion definition was not found")
    return match.group(1)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--json", dest="json_path", type=Path)
    args = parser.parse_args()

    root = args.root.resolve()
    checks: list[Check] = []

    required = [
        "MediaForge.csproj",
        "MediaForge.sln",
        "App.xaml",
        "App.xaml.cs",
        "MainWindow.xaml",
        "MainWindow.xaml.cs",
        "MainWindow.Projects.cs",
        "MainWindow.Workflow.cs",
        "RecoveryDialog.xaml",
        "PresetEditorDialog.xaml",
        "RecoveryDialog.xaml.cs",
        "PresetEditorDialog.xaml",
        "PresetEditorDialog.xaml.cs",
        "MediaEditorWindow.xaml",
        "MediaEditorWindow.xaml.cs",
        "app.manifest",
        "installer/MediaForge.iss",
        "scripts/build-release.ps1",
        "scripts/build-source-zip.ps1",
        "AGENTS.md",
        "VERIFICATION.md",
        "project_docs/PROJECT_INDEX.md",
        "project_docs/NEXT_BUILD_PLAN.md",
        "Services/Runtime/IProcessRunner.cs",
        "Services/Runtime/ProcessRunner.cs",
        "Services/Runtime/ProcessRunRequest.cs",
        "Services/Runtime/ProcessRunResult.cs",
        "Services/Runtime/StartupDiagnostics.cs",
        "Services/Runtime/ApplicationDataPaths.cs",
        "Models/ConversionOptionInput.cs",
        "Models/ConversionOptionOverrides.cs",
        "Models/Presets/PresetDocument.cs",
        "Models/Queue/QueueDocument.cs",
        "Services/IMediaConversionService.cs",
        "Services/Options/IEffectiveOptionsResolver.cs",
        "Services/Options/EffectiveOptionsResolver.cs",
        "Services/Options/EffectiveOptionsRequest.cs",
        "Services/Options/EffectiveOptionsResult.cs",
        "Services/Options/EffectiveOptionSource.cs",
        "Services/Presets/PresetService.cs",
        "Services/Session/IProjectSession.cs",
        "Services/Session/ProjectSession.cs",
        "Models/Projects/ProjectDocument.cs",
        "Models/Projects/ProjectLoadResult.cs",
        "Models/Projects/RecoverySnapshot.cs",
        "Models/Projects/RecentProjectEntry.cs",
        "Services/Projects/ProjectService.cs",
        "Services/Projects/ProjectDocumentMapper.cs",
        "Services/Projects/ProjectRecoveryStore.cs",
        "Services/Projects/ProjectAutosaveCoordinator.cs",
        "Services/Projects/RecentProjectStore.cs",
        "Services/Projects/ProjectRelinkService.cs",
        "Services/Projects/ProjectPathPolicy.cs",
        "Services/Queue/IQueueCoordinator.cs",
        "Services/Queue/QueueCoordinator.cs",
        "Services/Queue/QueueRunItem.cs",
        "Services/Queue/QueueEstimateService.cs",
        "Services/Queue/QueueService.cs",
        "ViewModels/MainViewModel.cs",
        "Services/Images/ImageHeaderProbe.cs",
        "Services/Images/ImageSourcePreflight.cs",
        "tests/MediaForge.Tests/MediaForge.Tests.csproj",
        "tests/MediaForge.Tests/Program.cs",
        "scripts/run-characterization-tests.ps1",
    ]
    missing = [item for item in required if not (root / item).is_file()]
    checks.append(Check("required-files", not missing, "all present" if not missing else f"missing: {', '.join(missing)}"))

    xml_files = [
        "App.xaml",
        "MainWindow.xaml",
        "RecoveryDialog.xaml",
        "MediaEditorWindow.xaml",
        "MediaForge.csproj",
        "tests/MediaForge.Tests/MediaForge.Tests.csproj",
        "app.manifest",
    ]
    xml_errors: list[str] = []
    for relative in xml_files:
        try:
            ET.parse(root / relative)
        except Exception as exc:  # noqa: BLE001 - evidence should retain exact parser failure
            xml_errors.append(f"{relative}: {exc}")
    checks.append(Check("xml-xaml-parse", not xml_errors, f"parsed {len(xml_files)} files" if not xml_errors else "; ".join(xml_errors)))

    for xaml_relative, code_relative in (
        ("MainWindow.xaml", "MainWindow.xaml.cs"),
        ("RecoveryDialog.xaml", "RecoveryDialog.xaml.cs"),
        ("PresetEditorDialog.xaml", "PresetEditorDialog.xaml.cs"),
        ("MediaEditorWindow.xaml", "MediaEditorWindow.xaml.cs"),
    ):
        names: list[str] = []
        handlers: set[str] = set()
        try:
            tree = ET.parse(root / xaml_relative)
            for element in tree.iter():
                for attribute, value in element.attrib.items():
                    attr_name = local_name(attribute)
                    if attribute == f"{{{XAML_NS}}}Name" or attr_name == "Name":
                        names.append(value)
                    if attr_name in EVENT_ATTRIBUTES and re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", value or ""):
                        handlers.add(value)
            duplicates = sorted({name for name in names if names.count(name) > 1})
            checks.append(Check(
                f"{xaml_relative}-unique-names",
                not duplicates,
                f"{len(names)} names" if not duplicates else f"duplicates: {', '.join(duplicates)}",
            ))
            code = (root / code_relative).read_text(encoding="utf-8")
            if xaml_relative == "MainWindow.xaml":
                code += "\n" + (root / "MainWindow.Projects.cs").read_text(encoding="utf-8")
                code += "\n" + (root / "MainWindow.Workflow.cs").read_text(encoding="utf-8")
            missing_handlers = sorted(handler for handler in handlers if not method_exists(code, handler))
            checks.append(Check(
                f"{xaml_relative}-handlers",
                not missing_handlers,
                f"{len(handlers)} handlers found" if not missing_handlers else f"missing: {', '.join(missing_handlers)}",
            ))
        except Exception as exc:  # noqa: BLE001
            checks.append(Check(f"{xaml_relative}-surface", False, str(exc)))

    source_text = "\n".join(
        path.read_text(encoding="utf-8", errors="replace")
        for path in iter_source_files(root)
        if path.suffix.lower() in {".cs", ".xaml", ".ps1"}
    )
    forbidden = [token for token in FORBIDDEN_SOURCE_TOKENS if token in source_text]
    checks.append(Check("forbidden-regression-tokens", not forbidden, "none" if not forbidden else ", ".join(forbidden)))

    production_process_calls: list[str] = []
    allowed_process_owner = Path("Services/Runtime/ProcessRunner.cs")
    for path in iter_source_files(root):
        relative = path.relative_to(root)
        if path.suffix.lower() != ".cs" or relative.parts[0] == "tests" or relative == allowed_process_owner:
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        if any(token in text for token in ("Process.Start(", "new ProcessStartInfo", ".Kill(entireProcessTree", ".WaitForExitAsync(")):
            production_process_calls.append(str(relative).replace(os.sep, "/"))
    checks.append(Check(
        "central-process-runner",
        not production_process_calls,
        "all production child processes use Services/Runtime/ProcessRunner.cs"
        if not production_process_calls else f"direct process lifecycle calls: {', '.join(sorted(production_process_calls))}",
    ))

    process_runner_text = (root / "Services/Runtime/ProcessRunner.cs").read_text(encoding="utf-8", errors="replace")
    explicit_system_io = bool(re.search(r"^using\s+System\.IO\s*;", process_runner_text, re.MULTILINE))
    checks.append(Check(
        "process-runner-system-io-import",
        explicit_system_io,
        "explicit System.IO import present" if explicit_system_io else "ProcessRunner.cs must explicitly import System.IO",
    ))

    ph09_io_files = (
        "MainWindow.Projects.cs",
        "RecoveryDialog.xaml.cs",
        "Models/Projects/RecoverySnapshot.cs",
        "Services/Projects/AtomicFile.cs",
        "Services/Projects/ProjectDocumentMapper.cs",
        "Services/Projects/ProjectPathPolicy.cs",
        "Services/Projects/ProjectRecoveryStore.cs",
        "Services/Projects/ProjectRelinkService.cs",
        "Services/Projects/ProjectService.cs",
        "Services/Projects/RecentProjectStore.cs",
        "tests/MediaForge.Tests/Program.cs",
    )
    ph09_missing_system_io = []
    for relative in ph09_io_files:
        text = (root / relative).read_text(encoding="utf-8", errors="replace")
        if not re.search(r"^using\s+System\.IO\s*;", text, re.MULTILINE):
            ph09_missing_system_io.append(relative)
    checks.append(Check(
        "ph09-system-io-imports",
        not ph09_missing_system_io,
        "explicit System.IO imports present across PH-09 persistence, recovery, relink, UI and tests"
        if not ph09_missing_system_io else f"missing explicit System.IO import: {', '.join(ph09_missing_system_io)}",
    ))

    ph10_io_files = (
        "MainWindow.Workflow.cs",
        "Services/Presets/PresetService.cs",
        "Services/Queue/QueueService.cs",
    )
    ph10_missing_system_io = []
    for relative in ph10_io_files:
        text = (root / relative).read_text(encoding="utf-8", errors="replace")
        if not re.search(r"^using\s+System\.IO\s*;", text, re.MULTILINE):
            ph10_missing_system_io.append(relative)
    checks.append(Check(
        "ph10-system-io-imports",
        not ph10_missing_system_io,
        "explicit System.IO imports present across PH-10 preset, queue and UI file operations"
        if not ph10_missing_system_io else f"missing explicit System.IO import: {', '.join(ph10_missing_system_io)}",
    ))

    main_window_text = (root / "MainWindow.xaml.cs").read_text(encoding="utf-8", errors="replace")
    project_window_text = (root / "MainWindow.Projects.cs").read_text(encoding="utf-8", errors="replace")
    workflow_window_text = (root / "MainWindow.Workflow.cs").read_text(encoding="utf-8", errors="replace")
    selected_project_id_nullable = bool(re.search(
        r"Guid\?\s+selectedId\s*=\s*JobsGrid\.SelectedItem\s+is\s+MediaJob",
        project_window_text,
    ))
    checks.append(Check(
        "ph09-selected-project-id-nullable",
        selected_project_id_nullable,
        "selected project item ID is explicitly target-typed as Guid?"
        if selected_project_id_nullable else "MainWindow.Projects.cs must target-type selectedId as Guid?",
    ))
    current_settings_ok = all((
        "_settingsService.Save(CaptureCurrentSettings())" in main_window_text,
        "VideoAudioBitrate = Math.Clamp(ParseInt(VideoAudioBitrateText.Text, 192), 1, 512)" in project_window_text,
        "AudioBitrate = Math.Clamp(ParseInt(AudioBitrateText.Text, 192), 1, 512)" in project_window_text,
    ))
    checks.append(Check(
        "save-settings-bitrate-scope",
        current_settings_ok,
        "global settings and project defaults share bounded typed control capture"
        if current_settings_ok else "typed settings capture or bounded bitrate parsing is incomplete",
    ))

    main_xaml_text = (root / "MainWindow.xaml").read_text(encoding="utf-8", errors="replace")
    architecture_tokens = (
        "IProjectSession _session" in main_window_text,
        "IEffectiveOptionsResolver _effectiveOptionsResolver" in main_window_text,
        "IQueueCoordinator _queueCoordinator" in main_window_text,
        "MainViewModel _viewModel" in main_window_text,
        "new EffectiveOptionsRequest(" in workflow_window_text,
        "_queueCoordinator.RunAsync(" in main_window_text,
        "DataContext = _viewModel" in main_window_text,
        'ItemsSource="{Binding Jobs}"' in main_xaml_text,
        'Value="{Binding OverallProgress, Mode=OneWay}"' in main_xaml_text,
        'Text="{Binding SummaryText}"' in main_xaml_text,
        'Text="{Binding TotalsText}"' in main_xaml_text,
    )
    forbidden_main_ownership = [
        token for token in ("ObservableCollection<MediaJob> _jobs", "SemaphoreSlim", "ProcessJobAsync(", "ValidateAndBuildOptions(")
        if token in main_window_text
    ]
    checks.append(Check(
        "ph08-architecture-seams",
        all(architecture_tokens) and not forbidden_main_ownership,
        "session, options, queue and presentation state are explicitly owned outside code-behind"
        if all(architecture_tokens) and not forbidden_main_ownership
        else f"missing seam token or retained owner: {', '.join(forbidden_main_ownership) or 'binding/interface token'}",
    ))

    overall_progress_binding_ok = all((
        'Value="{Binding OverallProgress, Mode=OneWay}"' in main_xaml_text,
        'Value="{Binding OverallProgress}"' not in main_xaml_text,
    ))
    checks.append(Check(
        "main-progress-readonly-binding",
        overall_progress_binding_ok,
        "read-only MainViewModel.OverallProgress is bound OneWay"
        if overall_progress_binding_ok else "OverallProgress must use an explicit OneWay binding",
    ))

    project_session_text = (root / "Services/Session/ProjectSession.cs").read_text(encoding="utf-8", errors="replace")
    queue_text = (root / "Services/Queue/QueueCoordinator.cs").read_text(encoding="utf-8", errors="replace")
    resolver_text = (root / "Services/Options/EffectiveOptionsResolver.cs").read_text(encoding="utf-8", errors="replace")
    seam_contracts_ok = all((
        "ReadOnlyObservableCollection<MediaJob>" in project_session_text,
        "job.PropertyChanged += Job_PropertyChanged" in project_session_text,
        "workerCount" in queue_text,
        "WaitForDispatchAsync" in queue_text,
        "IsDispatchPaused" in queue_text,
        "CancellationTokenSource.CreateLinkedTokenSource" in queue_text,
        "IMediaConversionService" in queue_text,
        "FileSuffix = SanitizeSuffix" in resolver_text,
        "WebM output requires VP9" in resolver_text,
    ))
    checks.append(Check(
        "ph08-seam-contracts",
        seam_contracts_ok,
        "session dirty ownership, bounded queue execution and deterministic option compatibility are present"
        if seam_contracts_ok else "one or more PH-08 seam contracts are missing",
    ))

    project_document_text = (root / "Models/Projects/ProjectDocument.cs").read_text(encoding="utf-8", errors="replace")
    project_service_text = (root / "Services/Projects/ProjectService.cs").read_text(encoding="utf-8", errors="replace")
    recovery_text = (root / "Services/Projects/ProjectRecoveryStore.cs").read_text(encoding="utf-8", errors="replace")
    relink_text = (root / "Services/Projects/ProjectRelinkService.cs").read_text(encoding="utf-8", errors="replace")
    path_policy_text = (root / "Services/Projects/ProjectPathPolicy.cs").read_text(encoding="utf-8", errors="replace")
    ph09_persistence_ok = all((
        "CurrentSchemaVersion = 1" in project_document_text,
        "JsonExtensionData" in project_document_text,
        "AtomicFile.WriteUtf8" in project_service_text,
        "schemaVersion" in project_service_text,
        "opened read-only" in project_service_text,
        ".mediaforge-recovery" in recovery_text,
        "MaximumSnapshotsPerProject = 5" in recovery_text,
        "UniqueExact" in relink_text,
        "UniqueChanged" in relink_text,
        "TryResolveRelativeInsideRoot" in path_policy_text,
        "IsReparsePoint" in path_policy_text,
        "var pathRoot = relativeRoot is null ? null" in (root / "Services/Projects/ProjectDocumentMapper.cs").read_text(encoding="utf-8", errors="replace"),
        "../" not in path_policy_text,
        all(token in project_window_text for token in (
            "NewProject_Click", "OpenProject_Click", "SaveProject_Click", "SaveProjectAs_Click",
            "RelinkMissing_Click", "RecoveryDialog", "ScheduleAutosave", "ConfirmProjectTransition"
        )),
    ))
    checks.append(Check(
        "ph09-project-persistence-contracts",
        ph09_persistence_ok,
        "versioned projects, atomic canonical save, separate recovery, recent/relink UI and contained portable paths are present"
        if ph09_persistence_ok else "one or more PH-09 project persistence contracts are incomplete",
    ))

    preset_document_text = (root / "Models/Presets/PresetDocument.cs").read_text(encoding="utf-8", errors="replace")
    preset_service_text = (root / "Services/Presets/PresetService.cs").read_text(encoding="utf-8", errors="replace")
    overrides_text = (root / "Models/ConversionOptionOverrides.cs").read_text(encoding="utf-8", errors="replace")
    queue_document_text = (root / "Models/Queue/QueueDocument.cs").read_text(encoding="utf-8", errors="replace")
    queue_service_text = (root / "Services/Queue/QueueService.cs").read_text(encoding="utf-8", errors="replace")
    queue_run_item_text = (root / "Services/Queue/QueueRunItem.cs").read_text(encoding="utf-8", errors="replace")
    media_job_text = (root / "Models/MediaJob.cs").read_text(encoding="utf-8", errors="replace")
    ph10_workflow_ok = all((
        "CurrentSchemaVersion = 1" in preset_document_text,
        "JsonExtensionData" in preset_document_text,
        "LockedFields" in preset_document_text,
        "IsBuiltIn" in preset_document_text,
        ".mediaforge-preset" in preset_service_text,
        "AtomicFile.WriteUtf8" in preset_service_text,
        "RejectUnsafePropertyNames" in preset_service_text,
        "CreateBuiltIns" in preset_service_text,
        "ApplyTo(ConversionOptionInput source)" in overrides_text,
        "request.Preset.Values.ApplyTo" in resolver_text,
        "request.JobOverrides.ApplyTo" in resolver_text,
        "preset locks these fields" in resolver_text.lower(),
        "CurrentSchemaVersion = 1" in queue_document_text,
        ".mediaforge-queue" in queue_service_text,
        "AtomicFile.WriteUtf8" in queue_service_text,
        "public sealed class QueueRunItem" in queue_run_item_text,
        "Snapshot = job.CreateRunSnapshot()" in queue_run_item_text,
        "IsDispatchPaused" in queue_text,
        "PauseAfterCurrent" in queue_text,
        "OrderByDescending(entry => entry.Item.Snapshot.Priority)" in queue_text,
        "SelectedPresetSnapshot" in media_job_text,
        "PerJobOverrides" in media_job_text,
        "SelectedPresetSnapshot" in project_document_text,
        "EstimatedProcessingTime" in (root / "Services/Queue/QueueEstimateService.cs").read_text(encoding="utf-8", errors="replace"),
        all(token in workflow_window_text for token in (
            "ApplyPresetToSelected_Click", "PresetManager_Click", "SaveQueue_Click", "LoadQueue_Click",
            "MoveSelectedUp_Click", "PriorityUp_Click", "ToggleEnabled_Click", "DuplicateSelected_Click",
            "RetrySelected_Click", "PauseResume_Click", "TryCreateRunItems"
        )),
    ))
    checks.append(Check(
        "ph10-presets-professional-queue-contracts",
        ph10_workflow_ok,
        "versioned safe presets, typed precedence, immutable run items and persistent priority/pause-aware queue controls are present"
        if ph10_workflow_ok else "one or more PH-10 preset or professional queue contracts are incomplete",
    ))

    solution_text = (root / "MediaForge.sln").read_text(encoding="utf-8", errors="replace")
    app_project_text = (root / "MediaForge.csproj").read_text(encoding="utf-8", errors="replace")
    test_program_text = (root / "tests/MediaForge.Tests/Program.cs").read_text(encoding="utf-8", errors="replace")
    test_script_text = (root / "scripts/run-characterization-tests.ps1").read_text(encoding="utf-8", errors="replace")
    test_wiring_ok = all((
        '"MediaForge.Tests", "tests\\MediaForge.Tests\\MediaForge.Tests.csproj"' in solution_text,
        'Compile Remove="tests\\**\\*.cs"' in app_project_text,
        "ProcessRunner cancellation kills the process tree" in test_program_text,
        "EffectiveOptionsResolver resolves a valid snapshot" in test_program_text,
        "ProjectSession owns jobs and dirty revision" in test_program_text,
        "Project projects round-trip typed queue state" in test_program_text,
        "Project replacement preserves duplicate queue items" in test_program_text,
        "Project output rules override legacy defaults" in test_program_text,
        "Project newer schema opens read-only" in test_program_text,
        "Project non-portable saves absolute references" in test_program_text,
        "Project recovery remains separate and bounded" in test_program_text,
        "Project relink distinguishes exact changed and ambiguous" in test_program_text,
        "Project autosave debounces recovery snapshots" in test_program_text,
        "Preset catalogue enforces ownership and CRUD" in test_program_text,
        "Preset catalogue falls back when user storage is unavailable" in test_program_text,
        "Application data overrides isolate persistent stores" in test_program_text,
        "Preset import rejects command injection fields" in test_program_text,
        "Effective options apply preset and job precedence" in test_program_text,
        "Project preserves preset snapshots and queue metadata" in test_program_text,
        "ProjectSession supports professional queue mutations" in test_program_text,
        "Queue document round-trips independent workflow state" in test_program_text,
        "Queue run item freezes source and edit plan" in test_program_text,
        "Queue estimates label size and time" in test_program_text,
        "QueueCoordinator pause stops new dispatch" in test_program_text,
        "QueueCoordinator owns parallel execution" in test_program_text,
        "Conversion preserves source and commits atomically" in test_program_text,
        "Image preflight rejects oversized decoded frames" in test_program_text,
        all(token in test_script_text for token in ("dotnet", "restore", "build", "run", "$LASTEXITCODE")),
    ))
    checks.append(Check(
        "characterisation-test-wiring",
        test_wiring_ok,
        "solution, app exclusion, architecture/safety tests and guarded runner present" if test_wiring_ok else "test project wiring is incomplete",
    ))

    test_project_text = (root / "tests/MediaForge.Tests/MediaForge.Tests.csproj").read_text(encoding="utf-8", errors="replace")
    global_json = json.loads((root / "global.json").read_text(encoding="utf-8"))
    framework_ok = all((
        "<TargetFramework>net10.0-windows</TargetFramework>" in app_project_text,
        "<TargetFramework>net10.0-windows</TargetFramework>" in test_project_text,
        global_json.get("sdk", {}).get("version") == "10.0.100",
        global_json.get("sdk", {}).get("rollForward") == "latestFeature",
        ".NET 10 SDK" in (root / "scripts/build-release.ps1").read_text(encoding="utf-8", errors="replace"),
    ))
    checks.append(Check(
        "dotnet10-migration-contract",
        framework_ok,
        "application, tests, SDK pin and build guidance target .NET 10"
        if framework_ok else "framework, SDK pin or build guidance is inconsistent",
    ))

    conversion_text = (root / "Services/MediaConversionService.cs").read_text(encoding="utf-8", errors="replace")
    preflight_text = (root / "Services/Images/ImageSourcePreflight.cs").read_text(encoding="utf-8", errors="replace")
    large_image_ok = all((
        "ImageSourcePreflight.ValidateForFfmpeg(job.SourcePath)" in conversion_text,
        "DescribeFfmpegFailure" in conversion_text,
        "ConservativeMaximumEstimatedFrameBytes = int.MaxValue" in preflight_text,
        "decode the full source" in preflight_text,
    ))
    checks.append(Check(
        "large-image-preflight",
        large_image_ok,
        "oversized decoded frames fail before FFmpeg with an actionable limitation"
        if large_image_ok else "large-image preflight or FFmpeg error translation is incomplete",
    ))

    image_probe_text = (root / "Services/Images/ImageHeaderProbe.cs").read_text(encoding="utf-8", errors="replace")
    read_jpeg_start = image_probe_text.find("private static ImageSourceInfo? ReadJpeg")
    read_jpeg_end = image_probe_text.find("private static bool IsStartOfFrame", read_jpeg_start)
    read_jpeg_body = image_probe_text[read_jpeg_start:read_jpeg_end] if read_jpeg_start >= 0 and read_jpeg_end > read_jpeg_start else ""
    jpeg_loop_index = read_jpeg_body.find("while (stream.Position < stream.Length)")
    jpeg_pre_loop = read_jpeg_body[:jpeg_loop_index] if jpeg_loop_index >= 0 else ""
    jpeg_loop_and_after = read_jpeg_body[jpeg_loop_index:] if jpeg_loop_index >= 0 else read_jpeg_body
    jpeg_stackalloc_ok = all((
        "stackalloc byte[2]" in jpeg_pre_loop,
        "stackalloc byte[6]" in jpeg_pre_loop,
        "stackalloc" not in jpeg_loop_and_after,
    ))
    checks.append(Check(
        "image-header-stackalloc-scope",
        jpeg_stackalloc_ok,
        "JPEG probe stack buffers are allocated once outside the marker loop"
        if jpeg_stackalloc_ok else "JPEG probe contains a stackalloc inside its marker loop",
    ))

    try:
        project_version = read_version(root / "MediaForge.csproj")
        installer_version = parse_installer_version(root / "installer/MediaForge.iss")
        changelog = (root / "CHANGELOG.md").read_text(encoding="utf-8")
        changelog_has_version = bool(re.search(rf"^##\s+\[{re.escape(project_version)}\]", changelog, re.MULTILINE))
        version_ok = project_version == installer_version and changelog_has_version
        detail = f"project={project_version}; installer={installer_version}; changelog={changelog_has_version}"
        checks.append(Check("version-consistency", version_ok, detail))
    except Exception as exc:  # noqa: BLE001
        project_version = "unknown"
        checks.append(Check("version-consistency", False, str(exc)))

    ignore_text = (root / ".gitignore").read_text(encoding="utf-8", errors="replace") if (root / ".gitignore").is_file() else ""
    missing_ignores = [entry for entry in ("bin/", "obj/", "artifacts/", ".vs/") if entry not in ignore_text]
    checks.append(Check("generated-path-ignore", not missing_ignores, "required entries present" if not missing_ignores else f"missing: {', '.join(missing_ignores)}"))

    unexpected_binary = []
    for path in iter_source_files(root):
        if path.suffix.lower() in BINARY_SUFFIXES:
            unexpected_binary.append(str(path.relative_to(root)))
    checks.append(Check(
        "source-tree-binaries-media",
        not unexpected_binary,
        "none" if not unexpected_binary else f"unexpected: {', '.join(sorted(unexpected_binary))}",
    ))

    app_xaml_text = (root / "App.xaml").read_text(encoding="utf-8", errors="replace")
    app_code_text = (root / "App.xaml.cs").read_text(encoding="utf-8", errors="replace")
    startup_diagnostics_text = (root / "Services/Runtime/StartupDiagnostics.cs").read_text(encoding="utf-8", errors="replace")
    startup_resilience_ok = all((
        "StartupUri=" not in app_xaml_text,
        "protected override void OnStartup" in app_code_text,
        "ShowStartupFailure" in app_code_text,
        "StartupDiagnostics.TryWrite" in app_code_text,
        "InitializeWorkflowFeaturesSafe" in (root / "MainWindow.xaml.cs").read_text(encoding="utf-8", errors="replace"),
        "ContentRendered += MainWindow_ContentRendered" in project_window_text,
        "Dispatcher.BeginInvoke(new Action(ShowRecoveryPromptSafe))" in project_window_text,
        'StartupDiagnostics.TryWrite("recovery-prompt"' in project_window_text,
        "_workflowFeaturesAvailable = false" in workflow_window_text,
        "Built-ins remain available" in preset_service_text,
        "ApplicationDataPaths.TryGetLocalProductPath" in startup_diagnostics_text,
        "Path.GetTempPath()" in startup_diagnostics_text,
    ))
    checks.append(Check(
        "startup-failure-containment",
        startup_resilience_ok,
        "startup is explicit, diagnostic, and PH-10 catalogue failure degrades to a usable window"
        if startup_resilience_ok else "startup exception reporting or PH-10 fail-soft containment is incomplete",
    ))
    fresh_start_project_tracking_ok = all((
        "Keep dirty tracking suppressed until" in project_window_text,
        "RegisterProjectOptionTracking();\n        _suppressProjectDirty = false;\n        UpdateProjectStatus();\n        UpdateProjectControlState();\n\n        Dispatcher.BeginInvoke(new Action(ShowRecoveryPromptSafe));" in project_window_text,
        "private void CompleteProjectInitialization()\n    {\n        // WPF can raise initial TextChanged/SelectionChanged/toggle events" in project_window_text,
    ))
    checks.append(Check(
        "fresh-start-project-remains-clean",
        fresh_start_project_tracking_ok,
        "project dirty tracking starts after first render, avoiding startup-only save prompts"
        if fresh_start_project_tracking_ok else "project dirty tracking can still activate during initial WPF render",
    ))

    startup_diagnostics_stage_ok = all((
        "BuildReport(safeStage, exception)" in startup_diagnostics_text,
        "BuildReport(stage, exception)" not in startup_diagnostics_text,
    ))
    checks.append(Check(
        "startup-diagnostics-normalised-stage",
        startup_diagnostics_stage_ok,
        "startup diagnostics pass the non-null normalised stage to report generation"
        if startup_diagnostics_stage_ok else "StartupDiagnostics must report with safeStage",
    ))

    build_script = (root / "scripts/build-release.ps1").read_text(encoding="utf-8", errors="replace")
    build_guards = all(token in build_script for token in ("dotnet restore", "dotnet build", "dotnet publish", "$LASTEXITCODE"))
    checks.append(Check("build-wrapper-native-failure-guards", build_guards, "restore/build/publish guarded" if build_guards else "missing required command or exit-code guard"))

    application_data_paths_text = (root / "Services/Runtime/ApplicationDataPaths.cs").read_text(encoding="utf-8", errors="replace")
    smoke_script_text = (root / "scripts/smoke-launch.ps1").read_text(encoding="utf-8", errors="replace")
    persistent_path_files = (
        "Services/SettingsService.cs",
        "Services/Presets/PresetService.cs",
        "Services/Projects/RecentProjectStore.cs",
        "Services/Projects/ProjectRecoveryStore.cs",
        "Services/Runtime/StartupDiagnostics.cs",
        "Services/FfmpegLocator.cs",
    )
    direct_special_folder_users = [
        relative for relative in persistent_path_files
        if "Environment.GetFolderPath(Environment.SpecialFolder" in (root / relative).read_text(encoding="utf-8", errors="replace")
    ]
    launch_isolation_ok = all((
        "MEDIAFORGE_APPDATA_ROOT" in application_data_paths_text,
        "MEDIAFORGE_LOCALAPPDATA_ROOT" in application_data_paths_text,
        'EnvironmentVariables["MEDIAFORGE_APPDATA_ROOT"]' in smoke_script_text,
        'EnvironmentVariables["MEDIAFORGE_LOCALAPPDATA_ROOT"]' in smoke_script_text,
        "Read-StartupDiagnostics" in smoke_script_text,
        "mainWindowHandle" in smoke_script_text,
        not direct_special_folder_users,
    ))
    checks.append(Check(
        "launch-smoke-profile-isolation",
        launch_isolation_ok,
        "smoke cases use explicit MediaForge storage roots and capture isolated startup diagnostics"
        if launch_isolation_ok else f"profile isolation incomplete; direct special-folder users: {', '.join(direct_special_folder_users) or 'none'}",
    ))

    release_smoke_ok = all(token in build_script for token in (
        "smoke-launch.ps1",
        "Running isolated launch smoke test",
        "$launchSmokeEvidence",
        "Launch smoke test failed",
    )) and all(token in smoke_script_text for token in (
        "blocked-preset-storage",
        "No closeable top-level window appeared",
        "startupDiagnostics",
        "schema = 2",
    ))
    checks.append(Check(
        "release-launch-smoke-gate",
        release_smoke_ok,
        "release packaging is blocked unless the isolated executable launch/close smoke test passes"
        if release_smoke_ok else "build-release.ps1 does not enforce the launch smoke test",
    ))

    package_audit_text = (root / "scripts/verify-release-package.ps1").read_text(encoding="utf-8", errors="replace")
    package_audit_interpolation_ok = (
        '${version}: $resolvedZip' in package_audit_text
        and '$version: $resolvedZip' not in package_audit_text
    )
    checks.append(Check(
        "release-package-powershell-interpolation",
        package_audit_interpolation_ok,
        "release package audit delimits version interpolation before a colon"
        if package_audit_interpolation_ok else "release package audit contains an ambiguous PowerShell variable-colon interpolation",
    ))
    package_audit_root_resolution_ok = all(token in package_audit_text for token in (
        "[string]$ProjectRoot,",
        'if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {',
        "$ProjectRoot = Split-Path -Parent $PSScriptRoot",
    )) and "[string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)," not in package_audit_text
    checks.append(Check(
        "release-package-project-root-resolution",
        package_audit_root_resolution_ok,
        "release package audit resolves its default project root after parameter binding"
        if package_audit_root_resolution_ok else "release package audit still evaluates PSScriptRoot in a parameter default",
    ))
    isolated_publish_ok = all(token in build_script for token in (
        "[System.IO.Path]::GetTempPath()",
        "$maximumPublishAttempts = 3",
        "IntermediateOutputPath",
        "$attemptPublishDirectory",
        "Retrying with a fresh isolated intermediate directory",
        "-p:PublishSingleFile=false",
        "singleFile = $false",
    )) and "-p:PublishSingleFile=true" not in build_script and all(token in package_audit_text for token in (
        '"MediaForge.dll"',
        '"MediaForge.deps.json"',
        '"MediaForge.runtimeconfig.json"',
        "singleFile=true",
    ))
    checks.append(Check(
        "isolated-retrying-multifile-publish",
        isolated_publish_ok,
        "multi-file publish is explicit and retains fresh temporary intermediates with bounded retry"
        if isolated_publish_ok else "multi-file publish or isolated retry contract is incomplete",
    ))

    markdown_files = [path for path in iter_source_files(root) if path.suffix.lower() == ".md"]
    broken_links: list[str] = []
    link_pattern = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
    for path in markdown_files:
        text = path.read_text(encoding="utf-8", errors="replace")
        for target in link_pattern.findall(text):
            if target.startswith(("http://", "https://", "mailto:", "#")):
                continue
            target_without_anchor = target.split("#", 1)[0]
            if not target_without_anchor:
                continue
            resolved = (path.parent / target_without_anchor).resolve()
            try:
                resolved.relative_to(root)
            except ValueError:
                broken_links.append(f"{path.relative_to(root)} -> {target}")
                continue
            if not resolved.exists():
                broken_links.append(f"{path.relative_to(root)} -> {target}")
    checks.append(Check("relative-markdown-links", not broken_links, f"checked {len(markdown_files)} files" if not broken_links else "; ".join(broken_links[:20])))

    files = sorted(iter_source_files(root))
    result = {
        "root": str(root),
        "version": project_version,
        "file_count": len(files),
        "checks": [asdict(check) for check in checks],
        "passed": all(check.passed for check in checks),
        "source_manifest": [
            {
                "path": str(path.relative_to(root)).replace(os.sep, "/"),
                "size": path.stat().st_size,
                "sha256": sha256(path),
            }
            for path in files
        ],
    }

    for check in checks:
        print(f"[{'PASS' if check.passed else 'FAIL'}] {check.name}: {check.detail}")
    print(f"Static source verification {'passed' if result['passed'] else 'failed'} ({len(checks)} checks, {len(files)} files).")

    if args.json_path:
        args.json_path.parent.mkdir(parents=True, exist_ok=True)
        args.json_path.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
        print(f"Evidence: {args.json_path}")

    return 0 if result["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
