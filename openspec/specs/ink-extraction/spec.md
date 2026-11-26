# ink-extraction Specification

## Purpose
Turn a cutout into a clean black-and-white bitmap of exactly one letter, deterministically, so the
preview and the build always agree.

## Requirements

### Requirement: Ink detection by darkness
A pixel SHALL be ink when, after a Gaussian blur with sigma 1 pixel, its mean channel value is
below 185, or when it is clearly coloured (red minus blue above 45) and its mean is below 225.
Isolated single pixels SHALL be removed by a 3×3 opening, except pixels darker than 120. Ink
detection SHALL NOT separate colours into letters: calligraphers shade one stroke from red into
dark blue or black.

#### Scenario: Shaded capital stays whole
- **WHEN** a red capital is shaded into dark blue at its lower end
- **THEN** the whole capital is ink

#### Scenario: Paper texture is not ink
- **WHEN** a cutout contains light grey paper fibres
- **THEN** they are not ink

### Requirement: Cutout area
Only ink inside the outline polygon SHALL count. Small gaps SHALL be closed first (two iterations
of 3×3 closing by default, overridable per cutout) and then cut by the polygon, so that a cut runs
exactly along the polygon edge.

#### Scenario: Notched capital
- **WHEN** a capital's polygon has a notch where the next lowercase letter starts
- **THEN** the lowercase letter's ink is not part of the capital

### Requirement: Erasures
Ink inside an erasure rectangle SHALL be removed before connected parts are evaluated.

#### Scenario: Fragment of the neighbour
- **WHEN** an erasure covers the stem of a neighbouring `i`
- **THEN** that stem is gone and the remaining parts are judged without it

### Requirement: Specks are dropped
After erasing, connected ink parts (8-neighbourhood) smaller than 6 % of the largest part SHALL be
dropped; the threshold is overridable per cutout. Separate parts above it, such as the dot of an
`i` or a detached flourish, SHALL be kept.

#### Scenario: i keeps its dot
- **WHEN** an `i` is cut out with its dot
- **THEN** the ink has two parts, stem and dot

### Requirement: Deterministic
The same page pixels and cutout SHALL always produce the same ink, bit for bit.

#### Scenario: Preview equals build
- **WHEN** the editor preview and the build extract the same cutout
- **THEN** both bitmaps are identical
