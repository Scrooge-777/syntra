# SYNTRA — UI/UX Design System

**Platform:** Windows 11 Fluent Design (WinUI 3 / XAML)  
**Theme:** Pure Dark Theme (High Contrast / Accessible)  

---

## 1. Design Philosophy: Quiet Intelligence & Deep Focus

SYNTRA's user experience is designed for sustained cognitive absorption. Unlike gamified productivity apps with noisy badges, confetti explosions, and intrusive notifications, SYNTRA adopts an editorial, calm, and purposeful design language.

- **Content Over Chrome:** Reading text, conceptual diagrams, and assessment prompts dominate the visual hierarchy. Navigation chrome remains subtle and unobtrusive.
- **Zero Ambiguity State Indicators:** The current session state (`Active`, `TaskAssessment`, `Paused`) is always distinct and unmistakable.
- **Intentional Friction:** High-stakes actions (such as initiating an emergency exit or resetting a study plan) incorporate deliberate physical confirmation steps.

---

## 2. Color System (Dark Theme Palette)

The default color system is calibrated for OLED and modern IPS desktop displays, avoiding harsh 100% pure blacks while maintaining high contrast.

| Token Name | Hex Code | Visual Swatch | Usage Description |
|---|---|:---:|---|
| **Background / Mica Base** | `#0D1117` | Dark Slate Black | Root window backdrop with Windows 11 Mica Alt effect. |
| **Surface Level 1 (Card Base)** | `#161B22` | Deep Charcoal | Elevated cards, sidebar navigation, and material panels. |
| **Surface Level 2 (Elevated)** | `#21262D` | Charcoal Gray | Active reading containers, quiz option cards, input boxes. |
| **Border & Divider** | `#30363D` | Subtle Border Gray | 1px border lines defining cards and separator rules. |
| **Accent Primary (Focus Blue)** | `#388BFD` | Electric Blue | Primary action buttons, active tab indicators, progress fills. |
| **Accent Secondary (Subdued)** | `#58A6FF` | Soft Cerulean | Hyperlinks, subtopic tags, selected option borders. |
| **Success / Verified** | `#2EA043` | Emerald Green | Passing quiz scores (>= 80%), completed task checkmarks. |
| **Warning / Cooldown** | `#D29922` | Warm Amber | In-progress assessments, cool-down timers, revision alerts. |
| **Error / Failed** | `#F85149` | Crimson Red | Failed assessment attempts (< 80%), restriction alerts. |
| **Text Primary** | `#F0F6FC` | Crisp Off-White | Headings, reading body text, quiz questions. |
| **Text Secondary** | `#8B949E` | Muted Gray | Metadata, timestamps, prerequisite labels, footnotes. |

---

## 3. Typography System (`Segoe UI Variable`)

SYNTRA uses Windows 11's native dynamic font **Segoe UI Variable**, leveraging optical sizing (`Display`, `Text`, `Small`) for maximum legibility.

| Style Name | Font Size | Weight | Line Height | Usage |
|---|---|---|---|---|
| **Header Hero** | 28 px | SemiBold (600) | 36 px | Dashboard greeting, milestone completion screens. |
| **Title Large** | 20 px | SemiBold (600) | 28 px | Section titles, active task name. |
| **Title Medium** | 16 px | Medium (500) | 24 px | Card headings, topic groups. |
| **Body Large (Study Reading)** | 15 px | Regular (400) | 24 px | Primary document reading surface (optimized for reading). |
| **Body Regular** | 13 px | Regular (400) | 18 px | Dashboard metadata, form labels. |
| **Caption / Meta** | 11 px | Regular (400) | 14 px | Timestamps, token counters, prerequisite badges. |
| **Monospace (Code / Citations)** | 13 px | Regular (Consolas / Cascadia Code) | 20 px | Technical text chunks, mathematical formulas, code. |

---

## 4. Spacing & Grid System

Layouts conform to a strict **4px / 8px baseline grid**:
- **Padding Micro:** 4 px / 8 px (pill tags, compact badges).
- **Padding Standard:** 16 px (internal card padding, input fields).
- **Padding Macro:** 24 px / 32 px (main container gutters, dashboard sections).
- **Corner Radii:**
  - Standard cards and buttons: **8 px** (Fluent standard).
  - Modal dialogs and study overlay: **12 px**.
  - Small pill tags: **16 px** (fully rounded).

---

## 5. Key View Layouts

### 1. Main Dashboard
- **Left Rail (64px collapsed / 200px expanded):** Navigation items (Today, Materials Library, Study Plans, Analytics, Settings).
- **Central Canvas (Flexible):**
  - Top Hero Card: Current active subject, target completion deadline, overall mastery progress bar.
  - Middle Section: "Today's Study Schedule" with chronological task cards showing estimated durations and statuses.
  - Right Rail (320px): Upcoming spaced repetition reviews and quick performance statistics.

### 2. Active Study Surface (Fullscreen Mode)
- **Top Header Bar (48px height):**
  - Minimalist progress bar showing elapsed vs. target reading time.
  - Active topic and subtopic title.
  - Subtle Emergency Exit indicator (`Ctrl+Shift+F12`).
- **Main Reading Pane (Centered, Max Width 840px):**
  - Distraction-free, single-column reading typography with generous line height (1.6) and comfortable margins.
  - Formatted Markdown renderer supporting headers, bulleted lists, and syntax-highlighted code.
- **Bottom Action Bar:**
  - Primary button: "Proceed to Active Recall Assessment" (enabled once reading criteria are met).

### 3. Assessment & Quiz View
- **Question Navigator:** Clean breadcrumb indicators (`Question 1 of 4`).
- **Multiple Choice Card:**
  - Clearly articulated question prompt.
  - 4 distinct option cards with generous hit targets.
  - Instant tactile feedback upon option selection.
- **Short Answer Card:**
  - Multi-line text editor with character counter and key-concept reminder.
- **Submission Barrier:** Explicit confirmation modal preventing accidental submittals.

---

## 6. Motion & Animation Standards

Animations follow the Fluent Motion guidance:
- **Duration:** Standard transitions execute within **150 ms to 250 ms** using the `Exponential` or `CubicEaseOut` curve.
- **Entrance Effects:** Subtle 8px vertical slide-and-fade for newly loaded study cards.
- **State Changes:** Smooth color cross-fade when transitioning from `Active` (Focus Blue) to `TaskAssessment` (Warm Amber) or `TaskCompleted` (Emerald Green).
- **Respect User Settings:** All animations strictly respect Windows system settings: if `Windows Settings > Accessibility > Visual Effects > Animation effects` is turned OFF, transitions snap instantly without delay.
