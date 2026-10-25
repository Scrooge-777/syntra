# SYNTRA — Milestone 0 Architecture & Repository Audit Report

**Audit Date:** October 25, 2026  
**Auditor:** Senior Software Architect, Security Engineer & Windows Desktop Specialist  
**Repository:** `https://github.com/Scrooge-777/syntra`  
**Target Milestone:** Milestone 0 (Planning, Architecture & Setup)  
**Status:** **AUDIT COMPLETE — REMEDIATED IN PLACE**

---

## 1. Executive Summary

A forensic architectural and technical audit was conducted across the entire SYNTRA repository, covering `README.md`, all 15 technical specifications under `docs/`, issue and PR templates under `.github/`, and repository configurations.

All identified documentation bugs, broken internal references, character encoding corruptions, and inaccurate open-source research notes were remediated in place. No application implementation code (`src/` or `tests/`) was introduced, and zero credentials or secrets exist in the repository.

---

## 2. Audit Findings & Remediation Matrix

| Finding ID | Domain | Severity | Finding Description | Evidence | Remediation Applied |
|---|---|:---:|---|---|---|
| **AUD-001** | Content Integrity | **Critical** | Automated string substitution script corrupted 12 markdown files in `docs/` with em-dashes (`—`) between individual characters and replaced the letter 's' with warning icons (`⚠`). | Regex `re.sub(r'\??\"?', '—', text)` inserted dashes between zero-width matches across `docs/*.md`. | Restored clean UTF-8 source content from git commit `7107f6f` for all affected documents and verified syntax. |
| **AUD-002** | Upstream Research | **High** | `docs/research.md` contained four inaccurate claims regarding candidate open-source repositories and licenses. | GitHub API audit revealed `Yasinjahantigh/FocusStudy` exists and is MIT; `tellihatem/FocusLock` has an MIT license and is an Electron/React/SQLite3 desktop app (not a PWA); `sandeep749/StudyGuard-AI` has an MIT license. | Completely rewrote `docs/research.md` with verified GitHub API data, SPDX identifiers, repository URLs, and architectural takeaways checked October 25, 2026. |
| **AUD-003** | Template Integrity | **Medium** | `.github/CONTRIBUTING.md`, `pull_request_template.md`, and `technical_research.md` referenced outdated uppercase root filenames (`IMPLEMENTATION_PLAN.md`, `DECISIONS.md`, `ARCHITECTURE.md`). | Text references in `.github/` did not reflect the consolidation of docs under `docs/`. | Updated all `.github/` references to point to `docs/roadmap.md`, `docs/decisions.md`, and `docs/architecture.md`. |
| **AUD-004** | Security Model | **Medium** | `docs/windows-compatibility.md` stated low-level keyboard hooks prevent switching to clean virtual desktops, omitting touch/taskbar gesture bypasses. | `WH_KEYBOARD_LL` suppresses keystrokes (`Ctrl+Win+Left/Right`), but touchpad 4-finger swipes and Task View icons cannot be intercepted by keyboard hooks. | Clarified in `docs/windows-compatibility.md` and `docs/security.md` that user-mode hooks provide behavioral friction, with `SetWinEventHook` acting as a focus reclamation fallback. |
| **AUD-005** | State Recovery | **Low** | Crash recovery protocol did not explicitly define how partial quiz submissions interrupted by system crashes or forced kills are handled in `TaskAssessment`. | Incomplete quiz attempts could leave indeterminate scores or unfairly penalize retry quotas. | Updated `docs/architecture.md`, `docs/spec.md`, and `docs/database.md` to define the `Interrupted` attempt status and mandate that a fresh quiz variant is generated upon reboot without quota penalty. |
| **AUD-006** | Licensing Status | **Low** | ADR-008 stated repository remains "private and unlicensed" during planning, contradicting the current public repository visibility. | Repository was made public per user authorization. | Updated ADR-008 in `docs/decisions.md` to clarify that the repository is public on GitHub, with formal licensing deferred per owner direction. |

---

## 3. Detailed Audit Verifications

### 3.1 Link Integrity & Documentation Index (Task 1)
- **Check Performed:** Automated crawler extracted all Markdown relative links in `README.md`, `docs/*.md`, and `.github/**/*.md` and validated their targets against disk paths.
- **Result:** **100% Passed.** All 17 links in `README.md` and all cross-document links across `docs/` resolve cleanly. Zero broken relative links exist.

### 3.2 Framework & Dependency Verification against Primary Sources (Task 3)
All core technology stack choices were verified against official Microsoft Learn and upstream project documentation on October 25, 2026:

1. **.NET 8 LTS:**
   - *Status:* Confirmed Active LTS (Long Term Support through November 10, 2026).
   - *Primary Source:* Microsoft .NET Support Policy (`https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core`, checked Oct 25, 2026).
2. **WinUI 3 & Windows App SDK 1.5+:**
   - *Status:* Supported native UI framework for Windows desktop apps. Fully supported on Windows 11 (22H2, 23H2, 24H2) and Windows 10 build 17763+.
   - *Primary Source:* Windows App SDK Documentation (`https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/`, checked Oct 25, 2026).
3. **`Microsoft.Extensions.AI` (`IChatClient`):**
   - *Status:* Official unified Microsoft .NET abstraction library for LLMs, SLMs, and chat pipelines with pluggable middleware for caching, retries, and telemetry.
   - *Primary Source:* Microsoft DevBlogs & .NET AI docs (`https://learn.microsoft.com/en-us/dotnet/ai/`, checked Oct 25, 2026).
4. **SQLite (`Microsoft.Data.Sqlite`):**
   - *Status:* Lightweight ADO.NET SQLite driver natively supporting Write-Ahead Logging (`WAL`), transactional commits, and in-memory test databases.
   - *Primary Source:* Microsoft Data Sqlite Overview (`https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/`, checked Oct 25, 2026).
5. **PDF Ingestion (`UglyToad.PdfPig`):**
   - *Status:* Pure C# Apache 2.0 PDF document parser. Eliminates architecture-specific native C++ DLLs (`pdfium.dll`) and copyleft risks of AGPL alternatives.
   - *Primary Source:* Upstream Repository (`https://github.com/UglyToad/PdfPig`, checked Oct 25, 2026).

### 3.3 Windows 11 Home Security & Enforcement Model (Task 4)
- **Edition Boundaries:**
  - *Assigned Access (Single/Multi-App Kiosk):* Confirmed **unsupported on Windows 11 Home** via Microsoft Learn (`https://learn.microsoft.com/windows/configuration/assigned-access/`, checked Oct 25, 2026). Requires Windows Pro, Enterprise, or Education.
  - *Shell Launcher:* Confirmed **unsupported on Windows 11 Home and Pro** via Microsoft Learn (`https://learn.microsoft.com/windows/configuration/kiosk-shelllauncher`, checked Oct 25, 2026). Requires Enterprise or Education.
  - *Group Policy (`gpedit.msc`):* Absent on Windows 11 Home.
- **Ordinary Desktop Restrictions vs. OS Boundaries:**
  - `Syntra.Windows` uses standard user-mode Win32 capabilities (`WS_EX_TOPMOST`, `SetWinEventHook`, `WH_KEYBOARD_LL`, `RegisterApplicationRestart`).
  - **Explicit Non-Claims:** SYNTRA does **not** claim to prevent Secure Attention Sequence (`Ctrl + Alt + Delete`), administrative process termination via Task Manager (`taskkill /F`), physical power off, Windows Safe Mode boot, or Windows Recovery Environment (WinRE) access.
  - **Watchdog Behavior:** Low-level keyboard hooks are subject to `LowLevelHooksTimeout` (default 200 ms); SYNTRA requires callbacks under 0.5 ms to avoid silent unhooking by the OS.

### 3.4 Session State Machine & Completion Authority (Task 5)
- **Sole Authority:** `SessionStateMachine` in `Syntra.Core` is the single source of truth.
- **Completion Invariants:**
  - Timer expiration alone **never** marks a task complete.
  - Process termination (`taskkill`) or OS restart **never** marks a task complete.
  - Network loss or AI provider failure **never** marks a task complete.
  - Unvalidated AI responses **never** unlock the desktop.
- **Deterministic Validation:** Multiple-choice answers are graded strictly by local C# code against pre-stored answer keys. Short answers are evaluated against AI rubrics with schema bounds. Passing requires a composite score $\ge 80.0\%$ and an HMAC-SHA256 evidence record written to SQLite WAL before unlocking.

### 3.5 AI Assessment, Prompt Injection & Privacy Audit (Task 6)
- **Delimiter Fencing:** Untrusted document text is isolated within `<<<UNTRUSTED_CONTENT_START>>>` and `<<<UNTRUSTED_CONTENT_END>>>` blocks. System prompts explicitly instruct models to treat enclosed content as passive information.
- **Schema Gate:** Responses must deserialize strictly into `QuizAssessmentDto` and `StudyPlanDto`. Malformed outputs trigger structured retry requests before tripping circuit breakers.
- **Credential Storage:** User API keys are stored exclusively in the Windows Credential Locker (`PasswordVault`) protected by DPAPI. Zero plaintext keys in configs or databases.
- **Privacy:** Zero third-party telemetry. Ingested materials and session databases reside entirely on the local device.

### 3.6 Upstream Open-Source Candidate Verification (Task 7)
All research claims were verified directly via GitHub API calls on October 25, 2026:
1. `Yasinjahantigh/FocusStudy`: Active public repo, MIT License (Copyright 2026 Yasin Jahantigh). Electron + React + TypeScript desktop focus app.
2. `tellihatem/FocusLock`: Active public repo, MIT License (Copyright 2026 Focus-Lock Contributors). Electron + React + SQLite3 desktop app.
3. `jiah0231/stopvibe`: Active public repo, MIT License (Copyright 2026 jiah0231). Python Win32 focus app blocking AI tools.
4. `Sandeep749/StudyGuard-AI`: Active public repo, MIT License (Copyright 2026 DecoderSandeep). Python computer vision study monitor. Rejected due to webcam privacy concerns.

### 3.7 Emergency Exit & Lockout Prevention (Task 8)
- In-app exit hatch (`Ctrl + Shift + F12`) presents a 30-word phrase challenge or a 3-minute cool-down, immediately releasing hooks and recording an audit penalty in `session_events`.
- In an unrecoverable software deadlock, `Ctrl + Alt + Delete` is guaranteed by the Windows NT kernel to reach the Windows Security Screen to sign out, switch users, or open Task Manager.
- SYNTRA makes zero modifications to Windows Safe Mode or BCD startup configurations.

### 3.8 Roadmap Consistency & Milestone Hierarchy (Task 9)
- Clean milestone sequence:
  - **M0:** Research, Architecture & Setup (CURRENT)
  - **M1:** Domain Core & Session State Machine
  - **M2:** SQLite Persistence & Recovery
  - **M3:** AI Abstraction & Validation Gate
  - **M4:** Windows 11 Home Restriction Engine
  - **M5:** WinUI 3 Desktop App & MVVM
  - **M6:** Learning Material Ingestion Engine
  - **M7:** End-to-End System Integration & Hardening
  - **M8:** Private MVP Release
- Milestone dependencies, deliverables, and acceptance criteria are aligned across `docs/roadmap.md`, `docs/spec.md`, and `docs/architecture.md`.

### 3.9 Secret Hygiene & Code Boundary Verification (Task 10)
- **Source Code Scan:** Zero `.cs`, `.csproj`, `.sln`, `.dll`, or `.exe` files exist in the repository.
- **Secret Scan:** Regular expression audits across all files detected zero API keys (`ghp_`, `sk-`, `AIzaSy`), passwords, or private data.
- **False Claim Audit:** The repository contains no claims of completed builds, passed test suites, or binary releases.

---

## 4. Unresolved Decisions & Unverified Claims Register

The following items are intentionally open and tracked for future milestone spikes:

| Item ID | Category | Description | Target Milestone | Current Assumption / Status |
|---|---|---|:---:|---|
| **UND-001** | Packaging | MSIX Packaging vs. Standalone Unpackaged deployment (`--self-contained true`). MSIX requires certificate trust installation on clean Windows 11 Home machines; unpackaged runs directly but lacks package isolation. | Milestone 8 | Both packaging targets are documented; final default to be chosen during Milestone 8 installer spike. |
| **UND-002** | Licensing | Final open-source or proprietary software license selection for SYNTRA (`LICENSE` file). | Explicit Owner Action | Deferred per ADR-008. Repository is currently public without an open-source license (default All Rights Reserved). |
| **UND-003** | Display Hardware | Multi-monitor dynamic geometry changes (`WM_DISPLAYCHANGE`) when secondary monitors are hot-plugged during active fullscreen overlay. | Milestone 4 | Handled programmatically via `EnumDisplayMonitors`, but requires physical multi-monitor hardware verification in Milestone 4. |
| **UND-004** | Offline AI Fallback | Local Small Language Model (SLM) inference via ONNX Runtime / Phi-3-mini when internet connectivity is completely lost during assessment. | Post-v1.0 / Future | Deferred to post-v1.0. MVP relies on cloud AI via `Microsoft.Extensions.AI` with pre-cached offline review questions as fallback. |

---

## 5. Exact Next Step to Close Milestone 0

1. **Review & Approval:** The repository maintainer reviews this audit report (`docs/audit-milestone-0.md`) and the verified documentation suite under `docs/`.
2. **Milestone 0 Closure:** Upon maintainer confirmation, Milestone 0 is formally closed.
3. **Milestone 1 Authorization:** Explicit authorization is granted to begin Milestone 1 (implementing pure .NET 8 domain entities, state machine, and xUnit test suite in `src/Syntra.Core` and `tests/Syntra.Core.Tests`).
