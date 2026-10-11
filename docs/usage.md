# Using the calculator

## Inputs

**Fit target** — the two coordinates an [IdMatch](https://www.idmatch.cc/) (or comparable) session produces, both measured
from the bottom bracket centre, using IdMatch's own axis names (**X** horizontal, **Z** up):

| | meaning | sign |
|---|---|---|
| BRP X / Z | Biomechanical Reference Point: where the saddle is 70 mm wide | X is **setback**, entered positive for a saddle behind the BB |
| HB X / Z | the bar tops at the stem clamp — *not* the hoods | X positive towards the front wheel |

Internally the solver and the drawings work in one consistent frame where X is positive forward, so
`FitTarget.BrpSetback` is the entered value and `FitTarget.BrpX` is its negation. Everything that
faces the user — the input, the derived metrics, the diagram label — uses the positive setback.

HB can be referenced to the bar tube centre (the usual convention), to the top of the bar tube, or
to the front of the bar tops — one bar reach ahead of the clamp. The calculator converts whichever
you pick into the bar clamp centre it implies.

Saddle-to-bar is reported as the **direct** straight line from BRP to HB, which is what the IdMatch
fit converter shows, with the horizontal component and the drop alongside it.

A fit sheet can be **saved by name**, so the coordinates and the HB reference are kept for the session and,
if you export them, between visits. Saved fits are personal data and are not kept on the server; see
[Your data](#your-data).

> Note on the HB reference: a fit sheet quoting "centre top of the handlebar" means **Top of the bar
> tube**, not the centre. Getting this wrong shifts the target by half a bar diameter — about 16 mm,
> or three spacers — and is the single easiest way to disagree with your fitter.

> Note on BRP: it is IdMatch's **Biomechanical Reference Point** — the point on the saddle where the
> width reaches 70 mm — not the saddle's midpoint. Defining it by width is what makes it portable
> between saddles of different lengths, so a fit sheet's coordinates transfer to a new saddle
> unchanged. To set a saddle up, find its 70 mm-width point and put *that* at the BRP coordinates.

**Cockpit** — headset stack, stem clamp height, where the extension centreline sits within that
clamp, cockpit angle, bar tube diameter, handlebar reach and drop, and any offset between the
extension axis and the bar centre (integrated cockpits are rarely exactly on axis). Spacer
increment and maximum stack are configurable; the available stem lengths and angles are just lists
you can edit. An **unsigned** angle in the list is tried both ways up; a **signed** one (e.g. `-16`)
only in that orientation.

Catalogue cockpits can be loaded from a dropdown — see [Cockpit presets](data.md#cockpit-presets).

**Bikes** — three sample archetypes ship with the app. Add more by URL, by pasting a geometry
table, or by typing sizes in by hand, and edit any of them from the `edit` link next to the bike
(built-in bikes are copied rather than modified). Everything you add or edit lasts for your session; export it to keep it
— see [Your data](#your-data).


## Your data

The server stores nothing about you. Your bikes, saved fits, cockpit corrections and where you left
off (target, saddle, cockpit, selected bikes) live in memory for the length of your browser session
and are gone when you close or reload the page. To keep them, use **Export my data** at the top of
the page, which downloads one JSON file, and **Upload** it next time to pick up where you left off.

An upload merges into what you have: an entry with the same id replaces yours, new ones are added,
and entries that are incomplete or share an id with a built-in bike are skipped. Single-purpose
lists exported by earlier versions (a bikes list, a fits list) can still be uploaded.

Because each visitor's data is separate and nothing is written to disk, the app is safe to host for
more than one person and needs no persistent storage.

## Drawings

- **Bike diagram** — scaled side view: wheels, frame, seatpost and saddle at the BRP, the cockpit
  chain (headset, spacers, stem, bar), stack/reach dimensions, both fit points, and the resulting
  saddle-to-bar reach and drop. The px-per-mm scale is printed on the drawing.
- **Cockpit detail** — the stack from head tube top to bar centre drawn part by part: headset cover,
  each individual spacer, the stem clamp with its axis offset, the extension, the bar tube at its
  real diameter and the drop bend built from the bar's own reach and drop. A **one-piece** cockpit
  (`CockpitSpec.OnePiece`, set by any preset marked `integrated`) is drawn instead as the single
  swept body it actually is — no stem block and no bar clamp, because the part has neither, and a top
  surface that runs flat when `FlatTops` is on. Two-piece builds keep the stem-and-bar rendering. Every dimension is offset
  *perpendicular to the thing it measures*, so the steerer dimensions stay parallel to the steerer
  and the stem length stays parallel to the stem whatever the angle. The angle figures live in a
  fixed corner block rather than floating on the arcs — when the stem sits near horizontal the two
  reference rays nearly coincide and any label placed on them collides with the bar.

Both are plain inline SVG, theme-aware, and scale to the container.

## Caveats

- The three **Archetype** bikes are samples, not real manufacturer geometry. The four named bikes
  in [Geometry data](data.md#bundled-real-geometry) are real, from the manufacturer's own tables.
- Handlebar reach and drop are quoted at neutral bar roll, so the drawing does not rotate them with
  the stem. Rolling the bar moves the hoods and drops but not the clamp centre, which is where the
  HB coordinate lives.
- Stack and reach are taken to the top of the head tube, as published. If a frame quotes them to the
  top of an integrated headset cover, set the headset stack to 0.
- **Headset stack belongs to the frame, not the cockpit.** It lives on each `SizeGeometry` and is
  edited through *edit* on the bike. Null means nobody measured it, in which case 12 mm is assumed
  and the solution carries a warning — it feeds the solve exactly like a spacer, so a 5 mm error
  costs 5 mm of spacers. Where a brand publishes both a frame stack and a stack-to-stem, the
  difference gives it (divided by sin of the head angle, since the published figure is vertical).
- Nothing here checks saddle position against the seatpost's actual rail range; the "saddle vs seat
  tube axis" figure tells you how much offset the post has to provide, not whether one exists.

