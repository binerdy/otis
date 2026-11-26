# letter-finding Specification

## Purpose
Let Otis do the searching, so that the user mainly fills gaps and makes small corrections: guide lines
are found automatically, every example of a letter on every page can be found from one example, and
each candidate gets a quality score so the best one is easy to choose.

## Requirements

### Requirement: Automatic guide lines
When the user draws a new box, Otis SHALL detect the baseline and x-line from the text line the box
sits on: it looks at the ink in a band around the box, two box widths to each side, builds the row
profile of ink and takes the densest run of rows (above half the maximum) as the lowercase body. The
lines SHALL be offered in place of fixed defaults and stay movable. "Detect lines" SHALL run the same
detection for an existing letter. If no plausible run is found (shorter than 8 pixels), the old
defaults SHALL be used.

#### Scenario: Lines for a new box
- **WHEN** the user draws a box around an `a` in a line of lowercase text
- **THEN** the green and blue lines sit within 3 pixels of the bottom and top of that line's lowercase letters

#### Scenario: Capital takes the line of its neighbours
- **WHEN** the box surrounds a flourished capital followed by lowercase letters
- **THEN** the lines match the lowercase letters, not the capital

### Requirement: Find more like this
For a letter with a cutout, "Find more like this" SHALL search all pages of the project for similar
shapes and list the best matches. Pages are searched line by line: text lines are found from the row
profile of the page's ink, the example is scaled to each line's x-height, and every position along the
line is compared with the example by normalised cross-correlation of the blurred ink. Matches that
overlap each other or the example itself SHALL be merged, and at most 12 matches with a similarity of
at least 0.5 SHALL be listed, best first, each with page, similarity and quality. The search SHALL run
in the background with progress and can be cancelled.

#### Scenario: Other examples found
- **WHEN** a page holds the word "banana" twice and the user searches from one `a`
- **THEN** the other five `a` are listed before any `n`

#### Scenario: Different sizes
- **WHEN** another line is written 1.5 times larger
- **THEN** its `a` are found as well

### Requirement: Use a match
Choosing a match SHALL show its ink and outline in the preview. "Use this" SHALL replace the letter's
box with the match's box and lines as an unsaved change; erasures and adjustments of the old example
are not carried over, because they belonged to the old position.

#### Scenario: Swap to a better example
- **WHEN** the user picks the second match and clicks "Use this"
- **THEN** the letter's box moves to that page and position, marked as changed

### Requirement: Quality score
Every cutout and every match SHALL get a quality score from 0 to 100, made of four parts that are
shown in its tooltip:
- **isolation** (35 %): little ink along the box edge, i.e. no neighbour cut through;
- **smoothness** (25 %): few pixels change under a small opening and closing, i.e. calm edges;
- **completeness** (20 %): few separate pieces, i.e. no broken strokes;
- **contrast** (20 %): dark, even ink rather than faded ink.

#### Scenario: Clean beats touched
- **WHEN** one `a` stands alone and another touches the next letter
- **THEN** the alone one scores higher

#### Scenario: Whole beats broken
- **WHEN** one stroke is complete and another is broken in two
- **THEN** the complete one scores higher

### Requirement: Initial pick
"Pick letters automatically" SHALL read every page with Tesseract 5.5 (bundled through the TesseractOCR
package with the `deu_latf` Fraktur model from tessdata_best) and fill every target character that is
neither cut out nor derived. Pages are read at half the project resolution. Each recognised letter
becomes a candidate: its box is widened by 15 % of its height but stops halfway to the neighbouring
recognised letters, its guide lines are detected as for a new box, and its score is half the OCR
confidence and half the quality score. The best candidate per character becomes an unsaved change for
the user to review; nothing is saved until the user saves. A recognised long s (ſ) fills the `s.alt`
building block. Reading runs in the background with progress and can be cancelled. A model placed in
the `tessdata` folder of the Otis data folder is used instead of the bundled one.

#### Scenario: Missing letters filled
- **WHEN** a project has scans but only a few letters cut out, and the user starts the initial pick
- **THEN** missing characters that Tesseract recognised with a confidence of at least 60 % appear as changed letters with boxes and guide lines

#### Scenario: Existing letters stay
- **WHEN** the letter `a` is already cut out
- **THEN** the initial pick leaves it unchanged
