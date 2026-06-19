# Design

## Overview

iLearn uses a restrained product interface for a desktop learning tool. The design is light, calm, and task-focused: muted blue-slate navigation, porcelain surfaces, clear type hierarchy, compact controls, and a single teal-blue accent reserved for selection and primary actions. The visual system should make course browsing, playback, downloads, and settings feel like one coherent app.

## Color

Use tinted neutrals rather than pure gray or pure white. The base palette is cool and low-chroma, with accent color used sparingly.

- App background: `#F4F7F8`
- Main surface: `#FFFFFF`
- Raised surface: `#FBFDFD`
- Sidebar: `#13202A`
- Sidebar hover: `#1B2B36`
- Ink: `#17212B`
- Muted text: `#5C6B78`
- Subtle text: `#7B8794`
- Border: `#DDE5EA`
- Soft border: `#E8EEF2`
- Accent: `#0F766E`
- Accent hover: `#0B625C`
- Accent soft: `#E5F4F1`
- Danger: `#B42318`
- Warning: `#B7791F`
- Info: `#2563EB`

## Typography

Use the platform/Semi sans stack already present in Avalonia. Product UI should not use display type. Headings are compact and stable:

- Page title: 28px, semi-bold
- Section title: 18px, semi-bold
- Item title: 15-17px, semi-bold
- Body: 13-14px
- Metadata: 12-13px

Keep body line length readable and avoid oversized headings inside panels, lists, or cards.

## Layout

The app shell uses a left navigation rail plus a content workspace. The sidebar is narrow and quiet, with a clear selected state. The content area has a page header band and a scrollable work surface. Pages use a shared structure: header row, toolbar row, progress/state area, and list/grid content.

Spacing scale: 6, 8, 10, 12, 16, 20, 24, 28, 32. Cards and panels use 8px radius. Avoid nested cards; use panels for page sections and row containers for repeated items.

## Components

- Primary buttons use accent fill and white text.
- Secondary buttons use white/tinted surfaces with soft borders.
- Navigation items are full-width rows with selected background and accent text.
- Course tiles are simple scan cards with course name, teacher, and status, not decorative blocks.
- Video/download/local rows are dense horizontal rows with actions aligned right.
- Toasts appear bottom-right, auto-dismiss, and use semantic tinted backgrounds.
- Empty states explain what to do next and occupy the content area without feeling like a modal.

## Motion

Keep motion functional and short, 150-200ms. Toast arrival, hover feedback, progress, and selection transitions are acceptable. Avoid page-load choreography and decorative animation.

## Implementation Notes

Keep shared brushes and styles in `App.axaml`. Avoid per-page color literals unless they represent semantic state not yet captured globally. Use Avalonia/Semi controls already in the project and avoid adding new UI libraries for this redesign.
