# SYNTRA — Architectural Decision Records (ADRs)

Last updated: October 24, 2026  

---

## Index of Decisions

- [ADR-001: Selection of .NET 8 LTS and WinUI 3 (Windows App SDK)](#adr-001-selection-of-net-8-lts-and-winui-3-windows-app-sdk)
- [ADR-002: Adoption of `Microsoft.Extensions.AI` as Cloud AI Abstraction](#adr-002-adoption-of-microsoftextensionsai-as-cloud-ai-abstraction)
- [ADR-003: Local Persistence via SQLite with Write-Ahead Logging (WAL)](#adr-003-local-persistence-via-sqlite-with-write-ahead-logging-wal)
- [ADR-004: Windows 11 Home Restriction Model (WinEventHook + Topmost Overlay + Low-Level Keyboard Hook)](#adr-004-windows-11-home-restriction-model)
- [ADR-005: Pure C# PDF Parsing via `UglyToad.PdfPig` (Apache 2.0)](#adr-005-pure-c-pdf-parsing-via-uglytoadpdfpig-apache-20)
- [ADR-006: Deterministic Core Completion Authority vs. Advisory AI Scoring](#adr-006-deterministic-core-completion-authority-vs-advisory-ai-scoring)
- [ADR-007: Single-User Local-First Architecture without Cloud Backend](#adr-007-single-user-local-first-architecture-without-cloud-backend)
- [ADR-008: Deferred Project Licensing Decision](#adr-008-deferred-project-licensing-decision)
- [ADR-009: In-App Emergency Passphrase Exit Hatch](#adr-009-in-app-emergency-passphrase-exit-hatch)

---

### ADR-001: Selection of .NET 8 LTS and WinUI 3 (Windows App SDK)
- **Status:** **Confirmed**
- **Date:** 2026-10-24
- **Context:** SYNTRA requires a high-performance, native Windows desktop framework capable of rendering a fluid modern UI while interfacing directly with low-level Win32 windowing and input APIs.
- **Options Considered:**
  1. *C# / WinUI 3 (Windows App SDK):* Microsoft's current native Windows desktop UI platform with modern Fluent design controls.
  2. *WPF (.NET 8):* Mature, stable, but older UI styling; requires third-party themes to match Windows 11 Fluent aesthetic.
  3. *Electron / Web Technologies:* High memory consumption (200MB+ idle), non-native Win32 integration requiring native Node C++ addons, poor performance during low-level keyboard hooking.
  4. *Rust + Slint / Tauri:* Strong performance and memory safety, but less mature ecosystem for complex document presentation and enterprise Windows integration.
- **Decision:** Adopt **.NET 8 LTS** with **WinUI 3 (Windows App SDK 1.5+)** using unpackaged or MSIX deployment.
- **Reasoning:** .NET 8 LTS provides long-term stability and unmatched integration with native Windows APIs via direct P/Invoke or CsWin32. WinUI 3 delivers the official Windows 11 design language natively.
- **Trade-offs:** WinUI 3 has a slightly larger initial installation footprint than WPF, but delivers superior graphics acceleration, dark mode fidelity, and accessibility.
- **Verification Status:** Verified against Microsoft Learn Windows App SDK documentation.
- **Revisit Conditions:** If Windows App SDK encounters unresolvable packaging bugs on Windows 11 Home clean installs, evaluate WPF as a fallback.

---

### ADR-002: Adoption of `Microsoft.Extensions.AI` as Cloud AI Abstraction
- **Status:** **Confirmed**
- **Date:** 2026-10-24
- **Context:** SYNTRA requires Cloud AI to synthesize study plans and generate assessment quizzes. Hardcoding a specific vendor SDK (e.g. raw OpenAI or Anthropic SDK) creates vendor lock-in and impedes automated offline testing.
- **Options Considered:**
  1. *Direct OpenAI / Anthropic .NET SDKs:* Tightly couples domain logic to vendor-specific classes.
  2. *Semantic Kernel:* Powerful, but introduces heavy cognitive overhead, agent orchestration concepts, and complex plugin architectures unnecessary for SYNTRA's structured generation needs.
  3. *`Microsoft.Extensions.AI` (`IChatClient`):* Official Microsoft provider-neutral standard library for .NET AI integration with built-in middleware (telemetry, caching, retries) and structured JSON schema output support.
- **Decision:** Adopt **`Microsoft.Extensions.AI`** as the sole AI transport abstraction.
- **Reasoning:** Standardizes AI interactions across OpenAI, Azure OpenAI, Anthropic Claude, and Google Gemini under the `IChatClient` interface. Allows immediate injection of a `MockChatClient` for fast, zero-cost, offline unit testing.
- **Trade-offs:** Package is modern and evolving, but backed by Microsoft's core .NET runtime team as the official standard.
- **Verification Status:** Verified on Microsoft Learn (`dotnet/ai/microsoft-extensions-ai`).
- **Revisit Conditions:** If `Microsoft.Extensions.AI` deprecates standard interfaces before .NET 9 LTS.

---

### ADR-003: Local Persistence via SQLite with Write-Ahead Logging (WAL)
- **Status:** **Confirmed**
- **Date:** 2026-10-24
- **Context:** Study state, ingested materials, and assessment records must be saved locally with zero data loss during sudden crashes or power cuts.
- **Options Considered:**
  1. *Plain JSON files:* High risk of corruption during sudden power loss; inefficient querying for large schedules.
  2. *SQLite with Rollback Journal:* Reliable, but concurrent reads and writes lock the entire database file.
  3. *SQLite with Write-Ahead Logging (`WAL`):* High concurrency, atomic transaction commits, microsecond read latencies, resilience against system crashes.
  4. *LiteDB (NoSQL):* Pure C# document database, but less battle-tested against abrupt OS power termination than SQLite.
- **Decision:** Adopt **SQLite 3** via `Microsoft.Data.Sqlite` configured in **WAL mode** (`PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL`).
- **Reasoning:** SQLite is the industry benchmark for zero-configuration, robust local storage. WAL mode provides instantaneous atomic commits before session state transitions.
- **Trade-offs:** Requires distributing the native `e_sqlite3.dll` binary (bundled automatically by `Microsoft.Data.Sqlite`).
- **Verification Status:** Verified against SQLite official documentation.
- **Revisit Conditions:** None anticipated.

---

### ADR-004: Windows 11 Home Restriction Model
- **Status:** **Confirmed**
- **Date:** 2026-10-24
- **Context:** SYNTRA must restrict distractions during study sessions on Windows 11 Home without violating OS security rules or damaging user systems.
- **Options Considered:**
  1. *Windows Assigned Access (Kiosk Mode):* **Unsupported on Windows 11 Home** (confirmed via Microsoft Learn; requires Pro/Enterprise).
  2. *Shell Launcher CSP:* **Unsupported on Windows 11 Home** (requires Enterprise/Education/IoT).
  3. *Kernel Driver (`.sys`) SSDT Hooking:* Violates Windows Driver Signing, trips anti-cheat/antivirus, creates blue-screen hazards. Explicitly rejected.
  4. *User-Mode Native Supervision (`WS_EX_TOPMOST` + `SetWinEventHook` + `WH_KEYBOARD_LL`):* Works universally on Windows 11 Home, requires zero administrative elevation, safely suppresses distraction hotkeys, and reclaims focus immediately.
- **Decision:** Implement **User-Mode Native Supervision** in `Syntra.Windows`.
- **Reasoning:** Technically feasible, legally compliant, non-destructive, and enforces high behavioral friction against unconscious multitasking while preserving system safety.
- **Trade-offs:** Cannot prevent `Ctrl+Alt+Del` or administrative process termination via Task Manager. This is addressed transparently in the product specification and mitigated via persistent restart recovery.
- **Verification Status:** Verified against Microsoft Win32 API documentation and Windows 11 Home edition matrix.
- **Revisit Conditions:** If Microsoft introduces consumer kiosk capabilities to Windows 11 Home.

---

### ADR-005: Pure C# PDF Parsing via `UglyToad.PdfPig` (Apache 2.0)
- **Status:** **Confirmed**
- **Date:** 2026-10-24
- **Context:** Users study from textbooks and course materials distributed as PDF documents. Ingestion must extract text reliably without native DLL dependencies or restrictive licensing.
- **Options Considered:**
  1. *iText 7 / iTextSharp:* High fidelity, but licensed under **AGPL v3** (viral copyleft) or expensive commercial license. Rejected.
  2. *PDFium / PdfiumViewer:* Fast C++ engine (BSD-3), but requires packaging native architecture-specific binaries (`pdfium.dll` x64/ARM64).
  3. *UglyToad.PdfPig:* 100% pure C#, zero native binary dependencies, active maintenance, licensed under permissive **Apache 2.0**.
- **Decision:** Adopt **`UglyToad.PdfPig`**.
- **Reasoning:** Pure .NET implementation ensures cross-architecture compatibility (x64 and ARM64 Snapdragon devices) with zero P/Invoke overhead and a completely safe Apache 2.0 license.
- **Trade-offs:** Slightly slower on massive 1,000-page scanned documents than native C++ engines, but academic study materials are parsed in chunks within seconds.
- **Verification Status:** Verified on GitHub (`UglyToad/PdfPig`).
- **Revisit Conditions:** If support for complex scanned OCR becomes an MVP requirement.

---

### ADR-006: Deterministic Core Completion Authority vs. Advisory AI Scoring
- **Status:** **Confirmed**
- **Date:** 2026-10-24
- **Context:** Deciding which component holds the authority to mark a study task complete and unlock the desktop.
- **Options Considered:**
  1. *AI Decides Completion:* Prompt instructs AI: "Output 'COMPLETE' if the student answered well." Rejected due to prompt injection vulnerabilities and stochastic hallucination.
  2. *Timer Decides Completion:* Task completes when elapsed time reaches target duration. Rejected because it incentivizes passive idling and fake studying.
  3. *Deterministic Core Authority:* AI provides objective questions, answer keys, and rubric evaluations. The local `Syntra.Core` state machine computes the final grade, verifies it against the hardcoded 80% threshold, writes an HMAC evidence hash to SQLite, and unlocks the session.
- **Decision:** Adopt **Deterministic Core Authority**.
- **Reasoning:** Guarantees absolute integrity. No hallucinated response or timer expiration can bypass the comprehension requirement.
- **Trade-offs:** Requires building domain-level scoring and validation infrastructure.
- **Verification Status:** Verified through state machine domain specification.
- **Revisit Conditions:** None. This is a core requirement of SYNTRA.

---

### ADR-007: Single-User Local-First Architecture without Cloud Backend
- **Status:** **Confirmed**
- **Date:** 2026-10-24
- **Context:** Architectural scope for the initial personal prototype.
- **Options Considered:**
  1. *Microservices / Web Backend (FastAPI / ASP.NET Core Web API):* High operational complexity, requires cloud hosting costs, authentication infrastructure, and cloud databases. Unnecessary for a personal Windows application.
  2. *Single-User Local-First Desktop App:* All data stored in local SQLite; application talks directly to Cloud AI endpoints over TLS.
- **Decision:** Adopt **Single-User Local-First Desktop Architecture**.
- **Reasoning:** Minimizes attack surface, eliminates cloud infrastructure costs, guarantees user data privacy, and aligns with the user's personal workstation requirements.
- **Trade-offs:** Multi-device synchronization is deferred to future releases.
- **Verification Status:** Verified.
- **Revisit Conditions:** When multi-device sync or team features are formally planned.

---

### ADR-008: Deferred Project Licensing Decision
- **Status:** **Confirmed (Deferred)**
- **Date:** 2026-10-24
- **Context:** Establishing the repository's public/private licensing terms (`LICENSE` file).
- **Options Considered:**
  1. *MIT / Apache 2.0:* Permissive open source.
  2. *GPL v3 / AGPL v3:* Copyleft open source.
  3. *Proprietary / All Rights Reserved:* Commercial personal software.
- **Decision:** **Do NOT create a `LICENSE` file during Milestone 0.** Mark availability and licensing as pending explicit user authorization.
- **Reasoning:** In accordance with user directives, licensing decisions must not be silently made on behalf of the project owner.
- **Trade-offs:** Repository remains private and unlicensed during planning.
- **Verification Status:** Verified compliance with user rules.
- **Revisit Conditions:** When the project owner specifies the intended distribution model.

---

### ADR-009: In-App Emergency Passphrase Exit Hatch
- **Status:** **Confirmed**
- **Date:** 2026-10-24
- **Context:** Users must be protected against accidental lockouts, emergency personal situations, or software deadlocks during an active restricted session.
- **Options Considered:**
  1. *No Exit Mechanism:* Highly dangerous; turns application into ransomware-like software if an unforeseen bug occurs.
  2. *Simple "Cancel" Button:* Trivially clicked on impulse; defeats the psychological friction of focus enforcement.
  3. *Friction-Gated Emergency Exit (30-word phrase or 3-minute cool-down):* Accessible via hotkey (`Ctrl+Shift+F12`), requires deliberate friction, immediately unhooks all restrictions, and permanently records an audit penalty event.
- **Decision:** Adopt **Friction-Gated Emergency Exit Hatch**.
- **Reasoning:** Ensures user safety and computing sovereignty while filtering out momentary impulses to abandon study.
- **Trade-offs:** High friction during genuine non-critical interruptions, but safeguards system usability.
- **Verification Status:** Verified in security architecture.
- **Revisit Conditions:** User feedback during Milestone 8 alpha testing.
