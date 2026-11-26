# outline-tracing Specification

## Purpose
Turn ink into smooth, correct outlines that keep the character of the hand lettering.

## Requirements

### Requirement: Potrace port
Tracing SHALL be a C# port of Potrace 1.16 (path decomposition, polygon optimisation, corner
detection, curve optimisation) in `Otis.Core/Tracing`, with Potrace's copyright and license
notice. Its output SHALL match Potrace's reference output for the same bitmap and parameters within
0.01 pixel per control point.

#### Scenario: Reference bitmaps
- **WHEN** the test bitmaps are traced with default parameters
- **THEN** every curve matches the stored Potrace reference output

### Requirement: Smoothing before tracing
The ink SHALL be blurred (sigma 1.3 pixels), upsampled three times with bicubic interpolation and
thresholded at 50 % before tracing, so that scan stair-steps do not become jagged outlines. Specks
smaller than 40 source pixels SHALL be ignored (`turdsize` scaled accordingly). Defaults are
`alphamax` 1.0 and `opttolerance` 0.25.

#### Scenario: Smooth bowl
- **WHEN** the `o` from a 400 dpi scan is traced
- **THEN** its outer contour consists of curves without small zig-zag segments

### Requirement: Correct contour direction
Traced outlines SHALL follow the TrueType convention: outer contours clockwise, holes
counter-clockwise, with no self-intersections.

#### Scenario: Counter of an e
- **WHEN** an `e` with an enclosed counter is traced
- **THEN** the counter is a separate contour in the opposite direction and renders as a hole

### Requirement: Ink, not paper
The tracer SHALL trace ink pixels as filled areas; tracing the inverted bitmap is a defect.

#### Scenario: Letter is filled
- **WHEN** a traced `a` is rendered
- **THEN** the strokes are black and the background is empty, not the other way round
