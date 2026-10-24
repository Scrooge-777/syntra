# SYNTRA — Privacy & Data Handling Notes (Draft)

*Note: Technical draft outlining data handling practices. Subject to legal review prior to public release.*

**Applies to:** SYNTRA Desktop on Windows  

---

## 1. Core Commitment: Local-First Privacy

SYNTRA is architected from the ground up as a **local-first application**. Unlike cloud-hosted SaaS educational platforms, SYNTRA does not operate a centralized user database, tracking server, or remote user profile repository.

- **Your Device, Your Data:** All personal study materials, ingested textbooks, generated study plans, assessment scores, and session logs reside strictly in an encrypted or user-owned SQLite database on your local personal computer.
- **Zero Third-Party Telemetry:** SYNTRA contains no Google Analytics, Mixpanel, Sentry, or telemetry trackers. We do not track your clicks, application launch counts, study duration, or hardware identifiers.

---

## 2. Cloud AI Data Transmission Disclosures

To synthesize adaptive study plans and generate comprehension verification quizzes, SYNTRA communicates with Cloud AI services configured by the user (e.g. OpenAI, Azure OpenAI, Anthropic, or Google Gemini).

### Exactly What Is Sent to the AI Provider:
1. **Educational Text Chunks:** Selected text segments extracted from the user's uploaded materials (typically 1,500–2,500 tokens) needed to formulate curriculum topics and questions.
2. **Context Prompts:** Objective prompt instructions instructing the model to generate multiple-choice questions, answer keys, or rubrics.
3. **User Quiz Responses (Short-Answer Only):** Free-text answers submitted by the user for short-answer questions so the model can evaluate comprehension against a rubric.

### Exactly What Is NEVER Sent:
- **No Personal Identifiers:** Your Windows username, machine name, IP address (beyond standard TLS routing), hardware MAC addresses, or personal contact information are never transmitted.
- **No File Paths or System Metadata:** Local folder paths, drive letters, and operating system configurations are stripped prior to transmission.
- **No Keystroke or Activity Logs:** Distraction monitoring events, blocked application attempts, and background process names remain strictly local.

---

## 3. Storage and Protection of Credentials

- **API Keys:** Cloud AI API keys provided by the user are stored directly within the **Windows Credential Locker** (`Windows.Security.Credentials.PasswordVault`).
- **Operating System Protection:** Credentials are encrypted using the Windows Data Protection API (DPAPI) tied to the user's Windows login profile. Keys are never written to plain-text configuration files, SQLite tables, or debug logs.

---

## 4. User Rights: Data Export, Deletion & Portability

Because all data is stored locally in SQLite (`%LOCALAPPDATA%\Syntra\syntra.db`):
- **Full Portability:** Users can copy, back up, or inspect their database using any standard SQLite inspection tool.
- **Immediate Deletion:** Deleting a subject or material from the SYNTRA library permanently purges all associated text chunks, study plans, and assessment logs from the local database.
- **Clean Uninstall:** Uninstalling the application deletes the local database and releases registered restart entries from Windows.
