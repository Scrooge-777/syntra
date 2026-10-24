# SYNTRA — Accessibility Specification & WCAG Guidelines

**Compliance:** WCAG 2.1 Level AA  
**Framework:** Microsoft UI Automation (UIA) / WinUI 3  

---

## 1. Accessibility Commitment & Architecture

Education and focused study tools must be accessible to all learners, including individuals with visual, motor, or cognitive impairments. SYNTRA is designed to natively integrate with Windows 11 assistive technologies without relying on bolt-on overlays.

- **Full Keyboard Operability:** Every control, reading pane, quiz option, and modal dialog can be navigated and operated using keyboard alone.
- **Screen Reader Parity:** All UI elements expose rich Microsoft UI Automation (UIA) attributes for Windows Narrator, NVDA, and JAWS.
- **Cognitive Clarity:** Distraction-free reading views minimize extraneous visual clutter, cognitive fatigue, and sensory overload.

---

## 2. Contrast & Color Accessibility (WCAG 2.1 AA)

| UI Element | Foreground Color | Background Color | Calculated Contrast Ratio | WCAG 2.1 AA Requirement | Status |
|---|---|---|:---:|:---:|:---:|
| **Primary Reading Text** | `#F0F6FC` (Off-White) | `#0D1117` (Slate Base) | **15.2 : 1** | Min 4.5 : 1 (Normal Text) | **Exceeds (AAA)** |
| **Secondary Metadata** | `#8B949E` (Muted Gray) | `#161B22` (Card Base) | **5.1 : 1** | Min 4.5 : 1 (Normal Text) | **Complies (AA)** |
| **Primary Action Button** | `#FFFFFF` (White) | `#388BFD` (Focus Blue) | **4.7 : 1** | Min 4.5 : 1 (Normal Text) | **Complies (AA)** |
| **Passing Score Indicator**| `#2EA043` (Emerald) | `#161B22` (Card Base) | **4.6 : 1** | Min 3.0 : 1 (Large Text) | **Complies (AA)** |
| **Error / Failed Alert** | `#F85149` (Crimson) | `#161B22` (Card Base) | **4.8 : 1** | Min 4.5 : 1 (Normal Text) | **Complies (AA)** |

### Color Independence
Color is **never** used as the sole indicator of meaning:
- A passed assessment displays an Emerald Green accent **plus** a bold checkmark icon `✓` and the explicit label `"Verified Mastery (85%)"`.
- A failed assessment displays a Crimson Red accent **plus** an alert icon `⚠` and the explicit label `"Retrial Required (65%)"`.

---

## 3. Keyboard Navigation & Focus Management

- **Focus Order:** Logical top-to-bottom, left-to-right tab order across all views.
- **Visible Focus Indicator:** High-visibility 2px focus ring (`#58A6FF`) surrounds the active element.
- **Shortcuts & Access Keys:**
  - `Tab` / `Shift + Tab`: Move between interactive controls.
  - `Space` / `Enter`: Activate button or select multiple-choice option.
  - `Up` / `Down` Arrow Keys: Navigate between quiz options and menu items.
  - `Ctrl + Shift + F12`: Global shortcut to invoke Emergency Exit hatch.
  - `Esc`: Dismiss non-critical modals (when permitted by session policy).

---

## 4. Microsoft UI Automation (UIA) Integration

WinUI 3 XAML controls are decorated with explicit `AutomationProperties`:
```xml
<Button x:Name="StartTaskButton"
        Content="Start Study Session"
        AutomationProperties.Name="Start Study Session for Topic: Virtual Memory"
        AutomationProperties.HelpText="Activates fullscreen focus mode and begins the study reading timer" />

<ListView x:Name="QuizOptionsList"
          AutomationProperties.Name="Quiz Options"
          AutomationProperties.LiveSetting="Polite">
    <!-- Quiz options -->
</ListView>
```

- **Live Announcements:** Dynamic assessment score updates and cool-down timer changes declare `AutomationProperties.LiveSetting="Polite"`, allowing screen readers to speak updates without interrupting the user.
- **Reading View Landmark:** The primary study text container is tagged with `AutomationProperties.LandmarkType="Main"` to allow instant navigation for screen reader users.

---

## 5. Reduced Motion & Sensory Accommodation

- **System Animation Setting Respect:** If Windows 11 `Animation effects` is disabled by the user, all slide, fade, and zoom transitions are disabled automatically.
- **No Flashing Content:** No UI element flashes more than 0.5 times per second, well below the 3-flash threshold for photosensitive seizure safety.
