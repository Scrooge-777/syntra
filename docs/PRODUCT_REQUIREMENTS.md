# SYNTRA — Product Requirements

**Target Platform:** Windows 11 Home (64-bit x64 / ARM64)  

---

## 1. Overview

SYNTRA is a Windows desktop application that combines automated study planning with desktop distraction blocking. Study sessions remain locked until the user passes an active recall test on the assigned material.

---

## 2. Requirements

### 1. Profile & Study Preferences
- Configure subjects (e.g. Distributed Systems, Organic Chemistry), target completion dates, and daily study hours.
- Study blocks: 15 to 120 minutes (default: 45 min). Break periods: 5 to 30 minutes (default: 10 min).
- Enforcement modes:
  - *Standard:* Fullscreen topmost overlay with `WinKey` and `Alt+Tab` suppressed.
  - *Strict:* Standard mode plus background process scanning for blacklisted entertainment apps.
- Onboarding disclosure: clearly explains Windows 11 Home boundaries (`Ctrl+Alt+Del` cannot be intercepted) and emergency exit procedures.

### 2. Document Ingestion
- Supported formats: text PDF (`.pdf`), Markdown (`.md`), and plain text (`.txt`).
- Pure C# PDF parsing via `UglyToad.PdfPig` (Apache 2.0). No native C++ DLLs or command-line wrappers.
- Preserves headings, lists, code snippets, and section hierarchy.
- Chunks text into 1,500–2,500 token blocks with 200-token overlap.
- Rejects corrupt files and records extraction errors without inventing missing content.

### 3. Study Plan Generation
- Generates structured, dependency-ordered study schedules from ingested materials and user availability.
- Groups tasks by prerequisites so foundational concepts are studied first.
- Inserts spaced repetition review checkpoints at 1-day, 3-day, and 7-day intervals.
- Emits structured JSON validated against C# DTOs before saving to SQLite.
- Users can review and reorder tasks before starting a session. Tasks cannot be edited during an active session.

### 4. Desktop Focus Enforcement (Windows 11 Home)
- Fullscreen borderless window (`WS_EX_TOPMOST`) covering all connected displays.
- Low-level keyboard hook (`WH_KEYBOARD_LL`) blocking `VK_LWIN`, `VK_RWIN`, `Alt+Tab`, `Alt+Esc`, and `Ctrl+Esc`.
- Foreground monitor (`SetWinEventHook`) that detects focus changes and re-asserts SYNTRA.
- Background process watcher checking for blacklisted games and media apps every 2.5 seconds.
- Emergency exit hatch (`Ctrl+Shift+F12`): requires typing a 30-word phrase or waiting 3 minutes to cleanly abort a session.

### 5. Comprehension Verification & Scoring
- Each task ends with a quiz: 3–5 multiple-choice questions and 1–2 conceptual short-answer questions.
- MCQs are evaluated locally in C# against the pre-stored answer key.
- Short-answer questions are evaluated by Cloud AI against a rubric (0–100 score).
- Composite score formula: 70% MCQ + 30% Short Answer.
- Passing threshold: **80.0%**.
- Timer expiration alone never completes a task.
- Passing writes an HMAC evidence hash to SQLite and unlocks the desktop.
- Failing locks retry for 5 minutes, displays targeted feedback, and allows up to 3 attempts.

### 6. Crash & Reboot Resilience
- State transitions are saved to SQLite in WAL mode before updating the UI.
- Application registers with Windows Restart Manager (`RegisterApplicationRestart`).
- If killed via Task Manager or interrupted by a reboot, SYNTRA detects the active session on startup, re-engages restrictions, and resumes progress.

---

## 3. Performance & System Limits

- Cold start to dashboard: under 1.5 seconds.
- Memory: under 120 MB RAM during active study.
- CPU overhead: under 1.0% during keyboard/window hook operation.
- Privilege: runs as standard user (`asInvoker`). No admin rights required.
- Accessibility: WCAG 2.1 Level AA contrast, full keyboard accessibility, Windows Narrator UI Automation properties.
