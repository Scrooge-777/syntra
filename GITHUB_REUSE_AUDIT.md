# SYNTRA — Open-Source Research & Audit Notes

**Date:** October 24, 2026  
**Purpose:** Technical evaluation of existing open-source productivity, lock-screen, and study tools. Determine what patterns to adopt, what to avoid, and verify license terms.

---

## 1. Summary of Candidates Checked

| Project | Stack | License | Verdict | Notes |
|---|---|---|:---:|---|
| **`Yasinjahantigh/FocusStudy`** | N/A | None (404) | **Reject** | Repo does not exist or is private. Checked Oct 24, 2026. |
| **`tellihatem/FocusLock`** | TypeScript, React, Zustand, IndexedDB | **No license** (All Rights Reserved) | **Learn concepts only** | Progressive Web App. Task-based unlock and dual persistence are good design patterns. No open-source license, so zero code copied. |
| **`jiah0231/stopvibe`** | Python / Windows scripts | **MIT** | **Adapt pattern** | Focus utility that terminates AI processes during work blocks. Good reference for process matching on Windows, but SYNTRA avoids sudden process killing. |
| **`sandeep749/StudyGuard-AI`** | Python, OpenCV, MediaPipe, YOLO11 | **No license** | **Reject** | Webcam gaze and drowsiness detection. Rejected because webcam tracking is invasive and inaccurate for natural reading habits. |

---

## 2. Detailed Candidate Notes

### `Yasinjahantigh/FocusStudy`
- **URL:** `https://github.com/Yasinjahantigh/FocusStudy`
- **Checked:** October 24, 2026
- **Status:** Inaccessible / 404.
- **Notes:** Repository could not be found on GitHub or public search. Dropped from consideration.

---

### `tellihatem/FocusLock`
- **URL:** `https://github.com/tellihatem/FocusLock`
- **Checked:** October 24, 2026
- **Stack:** TypeScript, React, Zustand, Vite, IndexedDB.
- **How it works:** PWA that locks the browser view and unlocks only after user completes a list of assigned tasks. Features tab-visibility tracking and shortcut prevention (`Cmd+W`, `Cmd+R`).
- **License:** No `LICENSE` file found in repository. By default under copyright law, this is All Rights Reserved. Code cannot be copied or redistributed.
- **Takeaways for SYNTRA:**
  - The principle of "earned unlocking" via task completion rather than elapsed time matches SYNTRA's philosophy.
  - Browser PWAs are trivially bypassed on desktop by opening another window or using OS task switchers. Confirms our choice of a native Windows desktop app with Win32 hooks.
- **Verdict:** Do not copy any code. Adopt the state machine concept of linking unlock privileges to verified task results.

---

### `jiah0231/stopvibe`
- **URL:** `https://github.com/jiah0231/stopvibe`
- **Checked:** October 24, 2026
- **Stack:** Python, Win32 process enumeration.
- **How it works:** Monitors running processes and terminates AI coding tools (Copilot, Claude desktop, IDE sidecars) during focus sprints to encourage manual problem-solving.
- **License:** MIT License verified.
- **Takeaways for SYNTRA:**
  - Validates that targeted process inspection is useful for developers prone to distraction.
  - Abruptly terminating running processes (`TerminateProcess`) risks data loss in unsaved documents. SYNTRA will use fullscreen overlays and foreground focus reclamation rather than killing user apps.
- **Verdict:** Clean MIT license. Re-implement process inspection in pure C# using `CreateToolhelp32Snapshot` with gentle warning dialogs instead of process termination.

---

### `sandeep749/StudyGuard-AI`
- **URL:** `https://github.com/Sandeep749/StudyGuard-AI`
- **Checked:** October 24, 2026
- **Stack:** Python 3, OpenCV, MediaPipe, YOLO11.
- **How it works:** Runs a continuous webcam feed to detect gaze direction, phone usage, and drowsiness, calculating a "Focus Score."
- **License:** No license file present.
- **Takeaways for SYNTRA:**
  - Webcam surveillance causes anxiety, high CPU/GPU load, and battery drain.
  - Looking away to read handwritten notes or look at a textbook triggers false positive "distraction" flags.
- **Verdict:** Fully rejected. SYNTRA tests comprehension through active quiz responses, never through webcam tracking.

---

## 3. Libraries Selected for SYNTRA

Based on this audit, third-party libraries incorporated into SYNTRA must be pure .NET and permissively licensed:

1. **`UglyToad.PdfPig` (Apache 2.0):** Pure C# PDF parsing. Avoids AGPL copyleft hazards of iText and eliminates native C++ DLL bindings (`pdfium.dll`).
2. **`Microsoft.Extensions.AI` (MIT):** Official Microsoft provider-neutral abstraction layer for cloud models (`IChatClient`).
3. **`Microsoft.Data.Sqlite` (MIT):** Official lightweight SQLite driver for local persistence.
