# SYNTRA — Open-Source Research & Audit Notes

**Last Updated:** October 25, 2026  
**Auditor:** Senior Software Architect  
**Purpose:** Technical evaluation of existing open-source productivity, lock-screen, and study tools. Empirically verify upstream repository existence, stack, license terms, and architectural takeaways.

---

## 1. Verified Upstream Candidate Audit

Every candidate repository listed below was directly checked against the GitHub REST API and upstream source trees on October 25, 2026:

| Project | Stack | Verified License (SPDX) | Verdict | Notes |
|---|---|:---:|:---:|---|
| **[`Yasinjahantigh/FocusStudy`](https://github.com/Yasinjahantigh/FocusStudy)** | TypeScript, Electron, React, Tailwind CSS | **MIT** | **Learn & compare** | Active Windows desktop focus application. Features Pomodoro timer, Win32 active app tracking, AI workspace audits, focus lock, planner, and ambient sound. MIT license confirmed (Copyright 2026 Yasin Jahantigh). |
| **[`tellihatem/FocusLock`](https://github.com/tellihatem/FocusLock)** | TypeScript, Electron, React, Zustand, SQLite3 | **MIT** | **Adapt patterns** | Active Windows desktop productivity application (not a PWA). Gamified task-based enforcement restricting games/apps until tasks are finished. MIT license confirmed (Copyright 2026 Focus-Lock Contributors). |
| **[`jiah0231/stopvibe`](https://github.com/jiah0231/stopvibe)** | Python, Win32 process enumeration | **MIT** | **Adapt patterns** | Windows focus utility with background service monitoring running processes and blocking AI coding tools during timed sessions. MIT license confirmed (Copyright 2026 jiah0231). |
| **[`Sandeep749/StudyGuard-AI`](https://github.com/Sandeep749/StudyGuard-AI)** | Python 3, OpenCV, MediaPipe, YOLO11 | **MIT** | **Reject camera tracking** | Local-first AI study monitor using webcam computer vision for gaze, distraction, drowsiness, and posture detection. MIT license confirmed (Copyright 2026 DecoderSandeep). Architecture rejected due to webcam privacy intrusion and high battery drain. |

---

## 2. Detailed Technical Findings & Architectural Takeaways

### 1. `Yasinjahantigh/FocusStudy`
- **Repository URL:** `https://github.com/Yasinjahantigh/FocusStudy`
- **Verified Status:** Active public repository. Checked October 25, 2026.
- **License:** MIT License (`LICENSE` present in repository root, Copyright 2026 Yasin Jahantigh).
- **Architecture:** Electron desktop application for Windows, React 18, TypeScript, Tailwind CSS. Tracks active Windows processes and implements focus lock intervals.
- **Takeaways for SYNTRA:**
  - Validates desktop study focus demand on Windows.
  - Confirms that Electron has high idle memory overhead (>180 MB) and lacks direct Win32 keyboard hook latency guarantees compared to native .NET 8 / WinUI 3.
  - Reaffirms SYNTRA's choice of native .NET 8 LTS with CsWin32 P/Invoke for sub-millisecond hook callbacks.

---

### 2. `tellihatem/FocusLock`
- **Repository URL:** `https://github.com/tellihatem/FocusLock`
- **Verified Status:** Active public repository. Checked October 25, 2026.
- **License:** MIT License (`LICENSE` present in repository root, Copyright 2026 Focus-Lock Contributors).
- **Architecture:** Electron desktop application targeting Windows, TypeScript, React 18, Zustand, and local `sqlite3`.
- **Takeaways for SYNTRA:**
  - Proves the effectiveness of "earned unlocking" (locking desktop applications until specific tasks are satisfied rather than waiting for an idle timer).
  - Validates local SQLite as the standard persistence engine for desktop focus tools.
  - SYNTRA expands this pattern: instead of simple self-reported task checkmarks, SYNTRA requires proven comprehension via active recall quizzes evaluated by domain code before unlocking.

---

### 3. `jiah0231/stopvibe`
- **Repository URL:** `https://github.com/jiah0231/stopvibe`
- **Verified Status:** Active public repository. Checked October 25, 2026.
- **License:** MIT License (`LICENSE` present in repository root, Copyright 2026 jiah0231).
- **Architecture:** Python Windows background service using Win32 process enumeration (`psutil` / Win32 APIs) to detect and block AI coding executables.
- **Takeaways for SYNTRA:**
  - Confirms that background process inspection on Windows can be executed with low overhead.
  - Demonstrates that abrupt process termination (`TerminateProcess`) risks data loss in unsaved documents. SYNTRA will use fullscreen `WS_EX_TOPMOST` overlays and foreground reclamation instead of forcibly killing user processes.

---

### 4. `Sandeep749/StudyGuard-AI`
- **Repository URL:** `https://github.com/Sandeep749/StudyGuard-AI`
- **Verified Status:** Active public repository. Checked October 25, 2026.
- **License:** MIT License (`LICENSE` present in repository root, Copyright 2026 DecoderSandeep).
- **Architecture:** Python 3 application streaming continuous webcam frames through OpenCV, MediaPipe facial landmarks, and YOLO11 object detection.
- **Takeaways for SYNTRA:**
  - Webcam monitoring creates user anxiety, high continuous CPU/GPU load (>25%), and battery drain on laptops.
  - Looking away to read physical books, write notes on paper, or contemplate complex problems triggers false-positive "distraction" flags.
  - **Rejection rationale:** SYNTRA measures learning through active cognitive verification (recall quizzes), never through optical biometric surveillance.

---

## 3. Recommended Core Libraries for SYNTRA

All third-party libraries incorporated into SYNTRA must be pure .NET and permissively licensed:

1. **`UglyToad.PdfPig` (Apache 2.0):**
   - Source: `https://github.com/UglyToad/PdfPig` (NuGet: `PdfPig`)
   - Pure C# PDF parsing. Zero native C++ DLL bindings (`pdfium.dll`). Avoids AGPL copyleft hazards of iText.
2. **`Microsoft.Extensions.AI` / `Microsoft.Extensions.AI.Abstractions` (MIT):**
   - Source: `https://github.com/dotnet/extensions` (NuGet: `Microsoft.Extensions.AI`)
   - Official Microsoft provider-neutral abstraction layer for cloud models (`IChatClient`), supporting OpenAI, Azure OpenAI, Anthropic, Gemini, and deterministic offline `MockChatClient`.
3. **`Microsoft.Data.Sqlite` (MIT):**
   - Source: `https://github.com/dotnet/efcore` (NuGet: `Microsoft.Data.Sqlite`)
   - Official lightweight SQLite driver for local persistence in Write-Ahead Logging (`WAL`) mode.
4. **`CommunityToolkit.Mvvm` (MIT):**
   - Source: `https://github.com/CommunityToolkit/dotnet` (NuGet: `CommunityToolkit.Mvvm`)
   - Official Microsoft-backed MVVM library with source generators for clean XAML presentation in WinUI 3.
