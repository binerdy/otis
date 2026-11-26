# outline-tracing Specification

## Purpose
Turn ink into smooth, correct outlines that keep the character of the hand lettering.

## Requirements

### Requirement: Potrace port
Tracing SHALL be a C# port of Potrace 1.16 (path decomposition, polygon optimisation, corner
detection, curve optimisation) in `Otis.Core/Tracing`, with Potrace's copyright and license
notice. It SHALL follow the C original where ports differ (outside pixels are background, C integer
division, the full `majority` radius). Its output SHALL match the stored reference traces for the same
bitmap and parameters within 0.01 pixel per control point. The references are synthetic shapes traced
with potracer (a Python port of Potrace 1.16) using the black turn policy, which does not depend on
`majority`.

#### Scenario: Reference bitmaps
- **WHEN** the reference bitmaps are traced with the black turn policy
- **THEN** every segment matches the stored reference trace

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
