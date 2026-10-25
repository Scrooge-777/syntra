# Contributing to SYNTRA

Thank you for your interest in contributing to **SYNTRA** ("Focus. Learn. Prove it.").

---

## 1. Project Phase Notice

> [!IMPORTANT]
> SYNTRA is currently in **Milestone 0: Planning, Research, Documentation, and Repository Setup**.
> Application implementation code will not be accepted until Milestone 0 is formally closed by the project maintainer.

---

## 2. Architectural Principles

All contributions must preserve the core architectural pillars:
1. **Clean Architecture Isolation:** `Syntra.Core` is pure .NET 8 BCL with zero dependencies on WinUI, Win32, or AI packages.
2. **Deterministic Completion Authority:** The application core is the sole completion authority. Timer expiration alone never completes a task.
3. **Windows 11 Home Safety:** No code may require administrator privileges, damage Safe Mode, or alter Windows security boundaries.
4. **Local-First Privacy:** All user data stays on the local device in SQLite. No tracking telemetry.
5. **Verified Evidence:** Never claim a test or benchmark passed without running verification commands.

---

## 3. Contribution Workflow

1. **Check Existing Milestones:** Review [docs/roadmap.md](docs/roadmap.md) before proposing new features.
2. **Create an Issue:** Use the appropriate issue template (`bug_report.md`, `feature_request.md`, or `technical_research.md`).
3. **Branch Naming:**
   - Features: `feature/milestone-<id>-<description>`
   - Bugfixes: `fix/<issue-id>-<description>`
   - Docs: `docs/<description>`
4. **Submit a Pull Request:** Fill in `pull_request_template.md` completely. Ensure all architectural checks pass.

---

## 4. Code & Documentation Style

- Follow standard Microsoft C# coding conventions and .NET design guidelines.
- Markdown documents must use standard GitHub Flavored Markdown (GFM).
- Every architectural modification must be accompanied by an Architectural Decision Record in [docs/decisions.md](docs/decisions.md).
