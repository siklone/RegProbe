# Settings Expansion Report

Date: 2026-03-09

## Current Direction

`Settings` should stay small and app-wide.

Removed from visible Settings because they were low-value or noisy:

- compact layout
- card depth / shadow toggle
- integrations and utility actions

It should answer only these questions:

- How should the app look?
- How should the app behave at startup?
- Which lightweight UX preferences should apply everywhere?

It should **not** become a dumping ground for feature actions, repair tools, imports, exports, integrations, driver tools, or monitoring utilities.

## What Belongs In Settings

These are the right long-term buckets for this page:

- Appearance
  - theme
  - dark/light base
  - compact density
  - shadow depth
  - future text scaling / reduced motion
- General behavior
  - startup scan
  - remember last page
  - preview or onboarding hints
  - confirm before higher-impact actions
- Accessibility
  - larger text preset
  - high contrast preset
  - reduced motion
  - stronger focus outlines
- Privacy and local data
  - clear local cache
  - clear exported temp reports
  - crash log consent / diagnostics level
- Notifications
  - in-app notifications on/off
  - quiet mode
  - completion toast behavior

## What Should Not Live In Settings

These items should stay out of Settings and live in their actual workspaces:

- cleanup and repair scripts
- update checking buttons
- config import/export flows
- DNS actions
- monitor layout reset
- webhook or service integrations
- anything that changes Windows configuration directly

Reason: users should discover actions where they use them. `Settings` should feel calm, predictable, and global.

## Best Next Additions

### 1. Accessibility Pack

Highest-value addition for this page:

- `Text size`
- `Reduced motion`
- `High contrast surfaces`
- `More visible focus states`

This improves usability without turning Settings into a tools page.

### 2. Startup Experience

Good medium-priority additions:

- `Open on last page`
- `Open on Dashboard`
- `Show startup summary`

These are classic app-level preferences and fit naturally here.

### 3. Safety and Confirmation

Useful for a tool that can affect Windows behavior:

- `Confirm before apply`
- `Confirm before maintenance actions`
- `Always open preview before apply`
- `Show rollback reminder after changes`

This matches the SAFE / reversible design philosophy.

### 4. Notification Preferences

Keep it app-scoped:

- `Show in-app success toasts`
- `Show maintenance completion notices`
- `Quiet mode while app is focused`

If external integrations return later, they should live in a separate `Integrations` page, not in Settings.

### 5. Local Data Controls

Useful but still general:

- `Clear local cache`
- `Clear old logs`
- `Open logs folder`
- `Limit retained report history`

This gives users control without mixing operational tools into Settings.

## Suggested Information Architecture

Recommended page structure:

1. `Appearance`
2. `Behavior`
3. `Accessibility`
4. `Privacy & Local Data`
5. `About`

Each setting card should keep the same micro-structure:

- `Title`
- `One-line explanation`
- `Current control`

Optional:

- `Requires restart`
- `Applies immediately`

## UX Rules For This Page

- keep the page compact
- avoid long paragraphs
- avoid feature-specific buttons
- prefer toggles, small pickers, and lightweight read-only info
- every item should feel safe to change
- do not place destructive or system-altering actions here

## Product Recommendation

The best next step is:

1. add `Accessibility`
2. add `Startup experience`
3. add `Safety confirmations`

That keeps `Settings` clean while still making it more useful.
