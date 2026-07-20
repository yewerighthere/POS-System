name: design-taste-frontend
description: Anti-slop frontend skill for landing pages, portfolios, and redesigns. The agent reads the brief, infers the right design direction, and ships interfaces that do not look templated. Real design systems when applicable, audit-first on redesigns, strict pre-flight check.
---

# tasteskill: Anti-Slop Frontend Skill

> Landing pages, portfolios, and redesigns. Not dashboards, not data tables, not multi-step product UI.
> Every rule below is **contextual**. None of it fires automatically. First read the brief, then pull only what fits.

---

## 0. BRIEF INFERENCE (Read the Room Before Anything Else)

Before touching code or tweaking dials, **infer what the user actually wants**. Most LLM design output is bad because the model jumps to a default aesthetic instead of reading the room.

### 0.A Read these signals first
1. **Page kind** - landing (SaaS / consumer / agency / event), portfolio (dev / designer / creative studio), redesign (preserve vs overhaul), editorial / blog.
2. **Vibe words** the user used - "minimalist", "calm", "Linear-style", "Awwwards", "brutalist", "premium consumer", "Apple-y", "playful", "serious B2B", "editorial", "agency-y", "glassy", "dark tech".
3. **Reference signals** - URLs they linked, screenshots they pasted, products they named, brands they're competing with.
4. **Audience** - B2B procurement panel vs. design-conscious consumer vs. recruiter scanning a portfolio. The audience picks the aesthetic, not your taste.
5. **Brand assets that already exist** - logo, color, type, photography. For redesigns, these are starting material, not optional input (see Section 11).
6. **Quiet constraints** - accessibility-first audiences, public-sector, regulated industries, trust-first commerce, kids' products. These constraints OVERRIDE aesthetic preference.

### 0.B Output a one-line "Design Read" before generating
Before any code, state in one line: **"Reading this as: <page kind> for <audience>, with a <vibe> language, leaning toward <design system or aesthetic family>."**

### 0.C If the brief is ambiguous, ask one question, do not guess
Ask exactly **one** clarifying question - never a multi-question dump - and only when the design read genuinely diverges. Example: *"Should this feel closer to Linear-clean or Awwwards-experimental?"*

If you can confidently infer from context, **do not ask**. Just declare the design read and proceed.

### 0.D Anti-Default Discipline
Do not default to: AI-purple gradients, centered hero over dark mesh, three equal feature cards, generic glassmorphism on everything, infinite-loop micro-animations everywhere, Inter + slate-900. These are the LLM defaults. Reach past them deliberately based on the design read.

---

## 1. THE THREE DIALS (Core Configuration)

After the design read, set three dials. Every layout, motion, and density decision below is gated by these.

* **`DESIGN_VARIANCE: 8`** - 1 = Perfect Symmetry, 10 = Artsy Chaos
* **`MOTION_INTENSITY: 6`** - 1 = Static, 10 = Cinematic / Physics
* **`VISUAL_DENSITY: 4`** - 1 = Art Gallery / Airy, 10 = Cockpit / Packed Data

**Baseline:** `8 / 6 / 4`. Use these unless the design read overrides them. Do not ask the user to edit this file - overrides happen conversationally.

---

## 2. BRIEF -> DESIGN SYSTEM MAP

Once you have the design read (Section 0) and dials (Section 1), pick the right foundation. Do not invent CSS for things that have an official package. Do not pretend an aesthetic trend is an official system.

---

## 3. DEFAULT ARCHITECTURE & CONVENTIONS

Unless the design read picks a real design system, these are the defaults:

* **Animation:** **Motion** (subtle hover transitions, focus rings, no distracting animations).
* **Fonts:** Use clean, premium typography with strict hierarchy.
* **Icons:** Standardize stroke width and design language globally.
* **Shape Consistency Lock:** Pick one corner-radius scale for the page and stick to it (e.g., all-soft `8px`, or all-sharp `0px`). Mixed shapes are prohibited unless following a strict logical mapping.

---

## 4. DESIGN ENGINEERING DIRECTIVES (Bias Correction)

LLMs default to clichés. Override these defaults proactively.

### 4.1 Typography
* **Display / Headlines:** Default `tracking-tighter leading-none`.
* **Body / Paragraphs:** Default `leading-relaxed`.
* **Sans font choice:** Use high-quality sans-serif display fonts.
* **Emphasis Rule:** For emphasis in display type, use bold or italic of the same font family. Mixed families are prohibited.

### 4.2 Color Calibration
* Max 1 accent color. Saturation < 80% by default.
* Neutral bases with high-contrast singular accents (e.g. Electric Blue, Emerald).
* **Color Consistency Lock:** Once an accent color is chosen for a page, it is used on the whole page consistently.

### 4.3 Layout Diversification
* Avoid centered hero elements unless explicitly requested. Use asymmetric, split, or left-aligned layouts to feel more premium and modern.

### 4.4 Materiality, Shadows, Cards
* Use cards only when elevation communicates real hierarchy.
* Tint shadows to the background hue (no pure-black drop shadows on light backgrounds).

### 4.5 Interactive UI States
* **Tactile Feedback:** On hover/active states, use scale-down (`0.98`) or translation to simulate physical touch.
* **Contrast Checks:** Ensure form elements, placeholders, error alerts, and buttons pass WCAG AA contrast standards.
* **CTA Button Wrap Ban:** Primary buttons must fit text on a single line at desktop viewports.
