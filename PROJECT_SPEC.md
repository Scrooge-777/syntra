# SYNTRA — Product Specification

**Platform:** Windows 11 Home (64-bit x64 / ARM64)  
**Status:** Approved for Architecture  

---

## 1. Scope & Core Objectives

SYNTRA is a native Windows desktop study application. It addresses two primary failure points in digital self-study:
1. **Unearned completion:** Timers complete while the user browses social media or games.
2. **Passive reading without retention:** Skimming material creates an illusion of competence without comprehension.

Core technical objectives:
- **Deterministic verification:** Study tasks complete only when the user passes an application-validated assessment (>= 80% score). Time elapsed alone never completes a task.
- **Distraction mitigation on Windows 11 Home:** Provide the strongest practical distraction barrier achievable within Windows 11 Home user-mode limits, without breaking OS recovery, requiring elevation, or modifying system files.
- **Adaptive scheduling:** Parse local study files (PDF, Markdown, text) and generate dependency-ordered study plans via Cloud AI.
- **Crash & reboot resilience:** Transactional state persistence in SQLite WAL mode. Interrupted sessions resume on login via Windows Restart Manager.
- **Local-first privacy:** Study files, database records, and logs stay on the local device. Only necessary study text chunks are sent to the user's configured AI provider.

---

## 2. Target Use Cases

### Use Case A: Dense Technical Self-Study
- **Scenario:** An engineer studying distributed systems or cloud architecture on a personal gaming PC.
- **Challenge:** Switching between technical whitepapers and high-dopamine desktop distractions (browser, Steam, Discord).
- **SYNTRA Workflow:** Locks the screen to the active document chunk, blocks `Alt+Tab` and `WinKey`, and requires passing a 5-question comprehension quiz before returning to the desktop.

### Use Case B: High-Volume Exam Preparation
- **Scenario:** A student studying multi-chapter textbooks (100+ pages) for an upcoming examination.
- **Challenge:** Cognitive overload, arbitrary scheduling, and lack of active recall testing.
- **SYNTRA Workflow:** Ingests the textbook PDF, breaks it into sequential modules with prerequisite ordering, and tests retention with spaced repetition checkpoints.

---

## 3. Functional Requirements

### 3.1 Profile & Setup
- Configure study subjects, target mastery dates, and daily study hour budgets.
- Set study duration (15–120 min, default: 45 min) and break length (5–30 min, default: 10 min).
- Review and acknowledge the Windows 11 Home enforcement disclosure and emergency exit instructions.

### 3.2 Material Ingestion
- Supported formats: text-based PDF (`.pdf`), Markdown (`.md`), and plain text (`.txt`).
- Pure C# local PDF extraction via `UglyToad.PdfPig` (Apache 2.0). Zero native C++ DLL bindings.
- Extract headings, chapter divisions, lists, and code blocks while preserving hierarchy.
- Chunk text into semantic segments (1,500–2,500 tokens) with 200-token overlap.
- Treat all files as untrusted data. Reject malformed streams and record errors explicitly.

### 3.3 AI Study Planning
- Generate structured multi-day schedules based on materials, deadlines, and daily hours.
- Order topics by prerequisite dependencies. Foundational concepts must precede advanced topics.
- Integrate spaced repetition reviews at 1-day, 3-day, and 7-day intervals.
- Validate generated plans against C# JSON schemas before saving to SQLite.
- Allow users to review and reorder tasks prior to launching a study session. Mid-session editing is blocked.

### 3.4 Active Study & Enforcement
- Display a borderless fullscreen topmost window (`WS_EX_TOPMOST`) covering desktop surfaces.
- Install a low-level keyboard hook (`WH_KEYBOARD_LL`) suppressing `WinKey`, `Alt+Tab`, `Alt+Esc`, and `Ctrl+Esc`.
- Register a WinEvent hook (`EVENT_SYSTEM_FOREGROUND`) to detect focus loss and reassert SYNTRA.
- Run low-overhead process scanning to detect blacklisted gaming/entertainment applications.
- Provide an emergency exit hatch (`Ctrl+Shift+F12`) requiring a 30-word challenge phrase or a 3-minute cool-down.

### 3.5 Verification & Scoring
- When reading finishes, generate 3–5 multiple-choice questions and 1–2 short-answer questions.
- Grade multiple-choice answers locally against the pre-stored answer key.
- Submit short-answer text to Cloud AI for rubric evaluation (0–100 scale).
- Calculate final composite score: 70% MCQ + 30% Short Answer.
- Enforce strict passing threshold: **80.0%**.
- If passed: record HMAC evidence hash in SQLite, release hooks, and transition to `TaskCompleted`.
- If failed: provide targeted AI feedback, enforce a 5-minute study cool-down, and permit up to 3 retries before requiring plan review.

### 3.6 Session State Machine & Completion Authority
- State machine in `Syntra.Core` is the sole completion authority.
- Valid states: `NotStarted`, `Ready`, `Active`, `Paused`, `TaskAssessment`, `TaskCompleted`, `SessionCompleted`, `EmergencyExit`, `RecoveryRequired`.
- The following conditions **never** complete a task:
  - Timer expiration.
  - Process termination (`taskkill`).
  - System reboot or shutdown.
  - Network loss or AI provider error.
  - An unvalidated AI response.

### 3.7 Persistence & Crash Recovery
- Commit state changes to SQLite synchronously in WAL mode (`journal_mode = WAL`, `synchronous = NORMAL`).
- Register with Windows Restart Manager (`RegisterApplicationRestart`).
- On launch, check for incomplete active sessions. If found, boot into `RecoveryRequired`, re-engage hooks, and resume without granting free completion.

---

## 4. Non-Functional Requirements

- **Cold Start:** Under 1.5 seconds to interactive dashboard on modern NVMe SSD.
- **Resource Footprint:** Under 120 MB RAM at idle during active study; hook CPU usage under 1.0%.
- **Hook Performance:** Low-level keyboard hook callback executes in under 0.5 ms to avoid Windows hook timeouts.
- **Security:** Runs with standard user privileges (`asInvoker`). API keys stored in Windows `PasswordVault`. Zero network telemetry.
- **Accessibility:** WCAG 2.1 Level AA contrast compliance; full keyboard navigation; Microsoft UI Automation (UIA) support for screen readers.

---

## 5. MVP Scope vs. Future Scope

| Area | In Scope for MVP (v1.0) | Deferred to Future Releases |
|---|---|---|
| **OS Support** | Windows 11 Home (x64 / ARM64) | Windows 10, macOS, Linux |
| **File Formats** | Text PDF, Markdown, Plain Text | Scanned OCR PDFs, DOCX, EPUB, YouTube transcripts |
| **AI Providers** | Cloud AI via `Microsoft.Extensions.AI` (OpenAI, Gemini, Anthropic, Azure) | Local ONNX / Ollama offline inference |
| **Enforcement** | Fullscreen Topmost overlay, WinEvent foreground monitor, `WH_KEYBOARD_LL` hook | Dedicated standard user provisioning scripts, router DNS integration |
| **Assessments** | Multiple-choice + Rubric short-answer | Code execution sandbox, math LaTeX evaluation |
| **Data Storage** | Local-first SQLite database | Encrypted cloud sync, Anki flashcard export |

---

## 6. Explicit Non-Goals

1. **No Kernel Drivers:** SYNTRA will never install `.sys` drivers or hook SSDT.
2. **No Secure Attention Sequence Bypass:** SYNTRA cannot and will not attempt to block `Ctrl + Alt + Delete` or the Windows Security screen.
3. **No Invasive Surveillance:** No webcam tracking, gaze monitoring, microphone listening, or background keylogging.
4. **No Destructive System Modifications:** SYNTRA will never edit Windows Safe Mode settings, disable Windows Update, or break administrative recovery.
5. **Not Parental Control Software:** Built for self-directed personal study, not adversarial surveillance of other users.
