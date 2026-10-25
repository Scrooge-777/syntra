# SYNTRA

> **Focus. Learn. Prove it.**

An AI-driven Windows desktop study environment that builds adaptive study schedules, blocks desktop distractions, and unlocks only when you prove retention through tested recall.

---

### Project Status: Planning & Architecture (Milestone 0)
SYNTRA is currently in active planning and architectural specification. **There is no executable build or binary release yet.** Source code and test implementation will begin once architectural and security specs are fully reviewed.

---

## Why SYNTRA?

Most study and productivity tools on Windows have two glaring problems:
1. **Passive reading creates false confidence:** Highlighting notes or skimming textbooks gives the feeling of learning without retention.
2. **Honor-system timers are easy to game:** Pomodoro timers and simple app blockers count down whether you actually studied or spent the hour staring at tabs.

SYNTRA fixes both:
- **Structured study planning:** You provide textbooks, slides, or notes (PDF, Markdown, text). Cloud AI breaks them into sequential topics with prerequisite ordering, time estimates, and scheduled reviews.
- **Enforced focus on Windows 11 Home:** When a session starts, SYNTRA locks the desktop surface using native Win32 window management, a topmost borderless overlay, and low-level keyboard hooks (`WinKey`, `Alt+Tab`).
- **Comprehension unlocks the app, not the clock:** When reading finishes, you take an active-recall quiz (multiple-choice and short-answer). Pass with 80% or higher, and the desktop unlocks. Fail, and you get targeted review notes with a mandatory cool-down before retrying.

---

## System Architecture

```mermaid
graph TD
    UI[WinUI 3 Desktop App / MVVM] --> Core[Syntra.Core: Domain & State Machine]
    UI --> Windows[Syntra.Windows: Restriction Engine]
    Core --> AI[Syntra.AI: Cloud AI Abstraction]
    Core --> Data[Syntra.Data: SQLite WAL Persistence]
    Windows -.-> Core
```

- **`Syntra.Core`**: Pure .NET 8 domain layer with zero UI or OS dependencies. Holds the session state machine, scoring rules, and completion logic. Fully unit-tested in isolation.
- **`Syntra.Windows`**: Win32 integration adapter (`SetWinEventHook`, `WH_KEYBOARD_LL`, `RegisterApplicationRestart`, `WS_EX_TOPMOST`). Manages overlays and blocks distraction shortcuts.
- **`Syntra.AI`**: Built on `Microsoft.Extensions.AI` (`IChatClient`). Connects to OpenAI, Anthropic, Gemini, or Azure with strict JSON schemas and delimiter-fenced prompt injection defenses.
- **`Syntra.Data`**: Local SQLite database running in Write-Ahead Logging (`WAL`) mode for crash-proof local state.
- **`Syntra.App`**: Windows 11 desktop UI built with WinUI 3 and Windows App SDK.

---

## Windows 11 Home & Security Boundaries

SYNTRA runs as a standard user desktop application without requiring administrative privileges:

- **No Enterprise Kiosk Features:** Windows Assigned Access and Shell Launcher are not available on Windows 11 Home (they require Pro or Enterprise editions). SYNTRA relies on user-mode window overlays and keyboard hooks.
- **No Kernel Rootkits or Drivers:** SYNTRA does not install kernel drivers, tamper with BIOS/UEFI, or intercept `Ctrl + Alt + Delete` (which is reserved by Windows Winlogon).
- **Restart & Crash Recovery:** If the app is killed or Windows reboots, SQLite WAL records ensure the session resumes on login without granting unearned completion.
- **Emergency Exit:** An emergency exit hatch (`Ctrl + Shift + F12`) allows immediate unlock via a 30-word confirmation challenge or a 3-minute cool-down, ensuring you never lock yourself out of your machine.

---

## Technical Specifications

| Parameter | Specification |
|---|---|
| **OS Target** | Windows 11 Home (64-bit x64 / ARM64), 22H2 or newer |
| **Runtime** | .NET 8 LTS |
| **UI Platform** | WinUI 3 (Windows App SDK 1.5+) |
| **Local Storage** | SQLite 3 (WAL mode, parameterized SQL) |
| **AI Abstraction** | `Microsoft.Extensions.AI` (`IChatClient`) |
| **PDF Ingestion** | `UglyToad.PdfPig` (Pure C#, Apache 2.0) |
| **Memory Target** | < 120 MB RAM during active study |
| **Hook Latency** | < 1.0 ms callback execution |

---

## Documentation Index

All technical specifications, design documents, and architecture records are consolidated under [`docs/`](docs/):

### Core Architecture & Strategy
- [Product Specification](docs/spec.md) — Product requirements, user stories, acceptance criteria, and non-goals.
- [System Architecture](docs/architecture.md) — Component boundaries, Clean Architecture layers, data flow, and state machine.
- [Windows Security & Enforcement](docs/security.md) — Windows 11 Home restriction model, Win32 hooks, and STRIDE threat model.
- [AI & Verification Design](docs/ai.md) — `Microsoft.Extensions.AI` abstraction, prompt injection defenses, JSON schemas, and deterministic scoring.
- [Implementation Roadmap](docs/roadmap.md) — Milestone breakdown (M0 through M8), packaging strategy, code signing, and release criteria.
- [Architectural Decisions](docs/decisions.md) — Verified Architectural Decision Records (ADR-001 through ADR-009).
- [Open-Source Research](docs/research.md) — Forensic audit of candidate repositories, license terms, and architectural lessons.
- [Milestone 0 Audit Report](docs/audit-milestone-0.md) — Comprehensive technical audit, verified sources, and findings matrix.

### Engineering & Domain Specifications
- [Database Schema](docs/database.md) — Relational SQLite schema, WAL pragmas, audit event logging, and migrations.
- [UI/UX Guidelines](docs/ui-ux.md) — WinUI 3 Fluent Design, dark palette, typography scale, spacing, and motion rules.
- [User Flows](docs/user-flows.md) — Visual sequence diagrams for onboarding, ingestion, study sessions, assessments, and recovery.
- [Testing Strategy](docs/testing.md) — Unit testing pyramid, xUnit fixtures, contract testing, and hook latency benchmarks.
- [Error Handling](docs/error-handling.md) — Error taxonomy, exponential backoff with jitter, and schema recovery policies.
- [Accessibility](docs/accessibility.md) — WCAG 2.1 Level AA compliance, Microsoft UI Automation (UIA), and keyboard navigation.
- [Privacy Policy](docs/privacy.md) — Local-first privacy guarantees, credential protection, and AI data boundary disclosures.
- [Windows Compatibility](docs/windows-compatibility.md) — Multi-monitor topology, per-monitor DPI v2 scaling, and Windows build support.

### Repository Guidelines
- [Contributing Guidelines](.github/CONTRIBUTING.md) — Development workflow, commit conventions, and branch management.

---

## Roadmap Overview

```mermaid
flowchart LR
    M0["Milestone 0<br/>Planning & Setup<br/>(CURRENT)"] --> M1["Milestone 1<br/>Core Domain &<br/>State Machine"]
    M1 --> M2["Milestone 2<br/>SQLite Storage &<br/>Recovery Engine"]
    M2 --> M3["Milestone 3<br/>AI Abstraction &<br/>Schema Validation"]
    M3 --> M4["Milestone 4<br/>Windows Hooks &<br/>Overlay Engine"]
    M4 --> M5["Milestone 5<br/>WinUI 3 Desktop<br/>Interface"]
    M5 --> M6["Milestone 6<br/>PDF & Markdown<br/>Ingestion"]
    M6 --> M7["Milestone 7<br/>System Integration<br/>& Hardening"]
    M7 --> M8["Milestone 8<br/>Private MVP"]
```
