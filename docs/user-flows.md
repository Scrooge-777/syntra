# SYNTRA — User Flows & State Journeys

---

## 1. Onboarding & First-Run Setup Flow

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Welcome as Welcome Screen
    participant Profile as Profile Wizard
    participant Security as Windows Security Disclosure
    participant Core as Syntra.Core

    User->>Welcome: Launch SYNTRA
    Welcome-->>User: Display Brand Vision ("Focus. Learn. Prove it.")
    User->>Profile: Enter Name, Target Subjects, Daily Hour Budget
    Profile-->>User: Prompt Study Cadence (Default: 45m Study / 10m Break)
    User->>Security: Review Windows 11 Home Enforcement Disclosure
    Note over User,Security: User explicitly acknowledges user-mode boundaries & emergency exit rules
    Security->>Core: SaveProfileAsync(profileDto)
    Core-->>User: Navigate to Main Dashboard
```

---

## 2. Learning Material Ingestion Flow

```mermaid
flowchart TD
    Start["Drag & Drop Document (PDF / MD / TXT)"] --> Validation{"Format Validated?"}
    Validation -- No --> Error["Display 'Unsupported Format' Dialog"]
    Validation -- Yes --> Parse["Local Parser: UglyToad.PdfPig / TextReader"]
    Parse --> Chunking["Semantic Chunking (1,500-2,500 tokens)"]
    Chunking --> StoreLocal["Persist Chunks to SQLite"]
    StoreLocal --> SendAI["Send Chunk Summaries to Cloud AI"]
    SendAI --> ExtractTopics["AI Extracts Topics, Prerequisites & Objectives"]
    ExtractTopics --> Dashboard["Populate Ingested Materials in Library"]
```

---

## 3. Active Study Session & Enforcement Flow

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Dash as Dashboard
    participant Core as Syntra.Core (State Machine)
    participant Win as Syntra.Windows (Hooks & Overlay)
    participant DB as SQLite WAL

    User->>Dash: Select Task -> Click "Start Session"
    Dash->>Core: StartSession(taskId)
    Core->>DB: INSERT session(state: 'Active', started_at: NOW)
    Core->>Win: EngageRestrictions(allowlist)
    Win->>Win: Show Topmost Borderless Window (WS_EX_TOPMOST)
    Win->>Win: Install WH_KEYBOARD_LL (Block WinKey, Alt+Tab)
    Win->>Win: Register SetWinEventHook (Foreground Monitor)
    Core-->>Dash: Transition UI to "Active Study Mode"

    loop Active Study Reading
        User->>Win: Press Alt+Tab (Habitual Distraction)
        Win-->>User: Keystroke Intercepted & Discarded (No Action)
        User->>Win: Background App Notification Steals Focus
        Win->>Win: Foreground Hook Detects Focus Change
        Win-->>User: Immediately Bring SYNTRA to Foreground
    end

    User->>Dash: Finish Reading -> Click "Start Verification Quiz"
    Dash->>Core: CompleteReading()
    Core->>Core: Transition to 'TaskAssessment'
```

---

## 4. Assessment, Deterministic Scoring & Unlock Flow

```mermaid
flowchart TD
    A["Enter 'TaskAssessment' State"] --> B["AI Generates 3-5 MCQs + 1 Short Answer"]
    B --> C["User Submits Answers"]
    C --> D["Local Core Scores MCQs Deterministically (70% weight)"]
    D --> E["AI Evaluates Short Answer via Rubric (30% weight)"]
    E --> F["Core Calculates Composite Score"]
    F --> G{"Composite Score >= 80%?"}
    
    G -- "YES (Pass)" --> H["Generate HMAC Evidence Hash"]
    H --> I["Record 'Passed' in assessment_attempts"]
    I --> J["Syntra.Windows Releases All Hooks & Overlay"]
    J --> K["State Machine Transitions to 'TaskCompleted'"]
    K --> L["Congratulatory Screen & Unlock Desktop"]
    
    G -- "NO (Fail)" --> M["Record 'Failed' in assessment_attempts"]
    M --> N["AI Synthesizes Targeted Remediation Notes"]
    N --> O["Start 5-Minute Study Cool-Down"]
    O --> P{"Attempts < 3?"}
    P -- "Yes" --> Q["Return to Study Reading for Retry"]
    P -- "No" --> R["Flag Task for Curriculum Adjustment"]
```

---

## 5. Emergency Exit Flow

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Overlay as Active Study Overlay
    participant Dialog as Emergency Exit Modal
    participant Win as Syntra.Windows
    participant Core as Syntra.Core
    participant DB as SQLite WAL

    User->>Overlay: Press Hotkey (Ctrl + Shift + F12)
    Overlay->>Dialog: Display Emergency Prompt
    Dialog-->>User: Prompt: Type 30-word emergency phrase OR Wait 3m Cool-Down
    User->>Dialog: Types Emergency Phrase Correctly -> Click "Abort Session"
    Dialog->>Win: ReleaseRestrictionsAsync()
    Win->>Win: Unhook WH_KEYBOARD_LL & WinEvent
    Win->>Win: Close Topmost Overlay Window
    Dialog->>Core: TriggerEmergencyExit()
    Core->>DB: INSERT session_event('EmergencyExitTriggered', reason)
    Core->>DB: UPDATE session SET state = 'EmergencyExit'
    Core-->>User: Desktop Fully Restored (Session Incomplete)
```

---

## 6. Crash & Unexpected Reboot Recovery Flow

```mermaid
sequenceDiagram
    autonumber
    participant OS as Windows Boot / User Login
    participant RestartMgr as Windows Restart Manager
    participant App as Syntra.App Bootstrap
    participant DB as SQLite WAL
    participant Win as Syntra.Windows
    participant Core as Syntra.Core

    OS->>RestartMgr: User logs in / WER restart triggered
    RestartMgr->>App: Launch syntra.exe --recovery
    App->>DB: Query Active Unfinished Sessions
    DB-->>App: Found Session ID #42 (State: 'Active', Task: 5)
    App->>Core: RestoreSession(sessionId: 42)
    Core->>Core: Transition to 'RecoveryRequired'
    Core->>Win: Immediately Re-Engage Topmost Overlay & Hooks
    Core->>DB: INSERT session_event('SessionCrashRecovered')
    Core->>Core: Resume 'Active' State with Remaining Time
    Core-->>App: Render Active Study Surface
    Note over App: Distraction environment restored without unearned completion
```
