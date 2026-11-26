# cutout-editing Specification

## Purpose
Let someone who is not a type designer pick the best example of each letter, correct it and see
right away what the font will get.

## Requirements

### Requirement: Character table
The left pane SHALL list every target character in groups (capitals, lowercase, digits and
punctuation, building blocks such as `ſ` and the umlaut dot). Each cell SHALL show the character,
a thumbnail of its current ink and its coverage: cut out (green), derived (blue, with the rule
as tooltip) or missing (grey). Cells with unsaved changes SHALL be marked.

#### Scenario: Derived umlaut
- **WHEN** `a` and the dot are cut out but `ä` is not
- **THEN** the `ä` cell is blue with the tooltip "a + dots"

### Requirement: Selecting jumps to the source
Selecting a character with a cutout SHALL open its page and zoom to the outline polygon. Selecting
a missing character SHALL start drawing a new box on the current page and say so.

#### Scenario: Missing capital
- **WHEN** the user selects the missing `O`
- **THEN** the canvas is in drawing mode and the status reads "O is missing. Draw a box around a good example."

### Requirement: Moving and shaping the outline
On the page canvas the user SHALL be able to:
- drag inside the outline to move it, together with its erasures, baseline and x-line, also onto
  another letter or page region;
- drag a corner to reshape it, double-click an edge to add a corner, right-click a corner to remove
  it (at least three remain);
- draw a new box (key B), which keeps the current baseline and x-line if they cross the new box;
- draw erasures (key E), move them, and remove them with a right-click;
- drag the baseline (green) and x-line (blue), or type their rows;
- nudge the selection with the arrow keys by 1 pixel, with Shift by 10.

Cutouts of other characters on the same page SHALL be shown as dashed outlines with their
character; clicking one selects it.

#### Scenario: Choose a better a
- **WHEN** the user drags the `a` outline from one word onto a cleaner `a` two lines lower
- **THEN** the erasures and both lines move with it and the preview shows the new `a`

### Requirement: Pointer precision
Pointer positions SHALL be mapped through the canvas's actual on-screen size and the display scale,
so that a click lands on the page pixel under the pointer at every zoom level and DPI setting.

#### Scenario: High-DPI display
- **WHEN** Windows display scaling is 125 %
- **THEN** grabbing a corner moves that corner and not the view

### Requirement: Navigation
The mouse wheel SHALL zoom around the pointer and dragging empty space SHALL pan. "Whole page"
and "Zoom to cutout" SHALL be available. The view SHALL fit again when the window is resized,
unless the user has zoomed or panned.

#### Scenario: First view
- **WHEN** a page is opened in a window that is still being laid out
- **THEN** the page is fitted once the canvas has its final size

### Requirement: Live ink preview
The right pane SHALL show the ink exactly as the build will extract it, with baseline and x-line,
updated within half a second after each change and without saving.

#### Scenario: Erasure takes effect
- **WHEN** the user draws an erasure over a piece of the next letter
- **THEN** that piece disappears from the preview

### Requirement: Explicit saving
Changes SHALL stay in memory until the user saves (button or Ctrl+S). Revert SHALL restore the saved
cutout; Remove SHALL delete the cutout after confirmation so the character becomes derived or
missing. Closing with unsaved changes SHALL ask first.

#### Scenario: Revert a mistake
- **WHEN** the user moved a box and chooses Revert
- **THEN** the saved cutout is shown again and the project file is unchanged

### Requirement: Build from the editor
The build pane SHALL show the version field, prefilled with the proposed next version, and the
build history (see font-building and project-files). "Build font" SHALL build that version from the
saved project, show the coverage summary and any problems, show the specimen with a sample text the
user can edit, and offer "Install in Windows" (see font-installation), "Open output folder" and
"Save font as…". When the latest version may still be rebuilt, the button SHALL read
"Rebuild 1.004" next to "Build 1.005".

#### Scenario: Unsaved changes at build time
- **WHEN** the user builds while cutouts have unsaved changes
- **THEN** Otis asks whether to save them first

#### Scenario: Build a new version
- **WHEN** the last build was 1.003 and the user clicks "Build font" without changing the field
- **THEN** `KanzleiFraktur-Regular-1.004.ttf` is written and listed first in the history
