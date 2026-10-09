> **Disclaimer.** This project is vibe coded: it was written with an AI assistant and has not had a
> formal code review or test pass. All geometry data it contains or imports (bike frames, cockpit
> presets, saddle figures) is **not validated** — it may be wrong, outdated or mis-transcribed.
> Check every figure against the manufacturer and your fitter before ordering parts or changing
> your bike setup, and use the results at your own risk.

# BikeGeo — cockpit calculator

An ASP.NET Core (.NET 10) Blazor Server app that turns a bike-fit result into a build sheet:
which frame size, which stem, and how many spacers put the handlebar exactly where the fit says
it belongs — plus a scaled drawing of the bike and a detail drawing of the cockpit.

```bash
dotnet run --project BikeFit.Web/BikeFit.Web.csproj
```

## Inputs

**Fit target** — the two coordinates an idmatch (or comparable) session produces, both measured
from the bottom bracket centre, using idmatch's own axis names (**X** horizontal, **Z** up):

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

Saddle-to-bar is reported as the **direct** straight line from BRP to HB, which is what the idmatch
fit converter shows, with the horizontal component and the drop alongside it.

A fit sheet can be **saved by name**, so the coordinates and the HB reference are kept for the session and,
if you export them, between visits. Saved fits are personal data and are not kept on the server; see
[Your data](#your-data).

> Note on the HB reference: a fit sheet quoting "centre top of the handlebar" means **Top of the bar
> tube**, not the centre. Getting this wrong shifts the target by half a bar diameter — about 16 mm,
> or three spacers — and is the single easiest way to disagree with your fitter.

> Note on BRP: it is idmatch's **Biomechanical Reference Point** — the point on the saddle where the
> width reaches 70 mm — not the saddle's midpoint. Defining it by width is what makes it portable
> between saddles of different lengths, so a fit sheet's coordinates transfer to a new saddle
> unchanged. To set a saddle up, find its 70 mm-width point and put *that* at the BRP coordinates.

**Cockpit** — headset stack, stem clamp height, where the extension centreline sits within that
clamp, cockpit angle, bar tube diameter, handlebar reach and drop, and any offset between the
extension axis and the bar centre (integrated cockpits are rarely exactly on axis). Spacer
increment and maximum stack are configurable; the available stem lengths and angles are just lists
you can edit. An **unsigned** angle in the list is tried both ways up; a **signed** one (e.g. `-16`)
only in that orientation.

Catalogue cockpits can be loaded from a dropdown — see below.

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

## Running in a container

A prebuilt image (linux/amd64 and linux/arm64) is published to Docker Hub on every push to `master`
and on version tags:

```bash
docker run --rm -p 8080:8080 kvloover/bikegeo:latest
```

or build it yourself from the `Dockerfile`:

```bash
docker build -t bikegeo .
docker run --rm -p 8080:8080 bikegeo
```

then open <http://localhost:8080>. The image listens on plain HTTP on port 8080 and expects TLS to
be handled by whatever sits in front of it (an App Service, Container Apps, a reverse proxy). It
writes nothing to disk, so it runs happily with `--read-only` and without a volume.

One thing to know before hosting it publicly: **Add geometry → fetch from a URL** makes the server
request whatever address a visitor types in. Restrict or disable it on a public deployment.

## The geometry

Origin is the bottom bracket, Y up, millimetres throughout.

```
HTT   = (Reach, Stack)                        top of the head tube
 + headset stack       along the steerer axis
 + spacer stack        along the steerer axis
 + stem axis offset    along the steerer axis   -> P, the stem pivot
 + stem length         along the stem axis      -> bar clamp centre
 + bar centre offset   perpendicular to the stem axis
```

The steerer axis unit vector pointing up is `(-cos HTA, sin HTA)` — going up the steerer moves you
backwards, because the head tube leans back. That is what makes spacers move the bar up *and* back,
and it is why the calculator needs the head tube angle rather than treating spacers as pure height.

Stem angle uses the industry convention: measured from the perpendicular to the steerer, so the
installed angle from horizontal is `(90 − HTA) + stem angle`. On a 73° head angle a −17° stem is
horizontal and a +6° stem rises 23°.

### Level tops

A one-piece aero cockpit quotes the angle of its **extension body** — steerer clamp to notional bar
clamp, −6° for a Roval Rapide. But the HB point sits on the **aero tops**, a different feature of the
same part, and those are built to sit level. So the angle that matters is not a property of the
cockpit at all: it is `HTA − 90`, about −16.5° on a 73.5° frame, and it changes from bike to bike.

Setting `FlatTops` makes the solver derive it that way. It is on for every one-piece preset, because
the evidence is strong — tested against three bikes and three cockpits from one fit sheet:

| | implied stem lengths vs stock 90 / 90 / 100 | spread in stack-above-head-tube |
|---|---|---|
| quoted −6° | 87.1 / 87.2 / 97.2 — all ~2.9 mm short | 2.1 mm |
| level tops | 90.5 / 90.5 / 100.7 | **0.7 mm** |

A consistent bias across three unrelated frames is a modelling error, not noise. Under level tops it
vanishes, the implied lengths land on catalogue values, and three different cockpits agree on the
stack to within 0.7 mm. Misses against the fit sheet fell from 2.8–4.2 mm to 0.5–0.7 mm.

One consequence worth knowing: with level tops, **stem length changes reach but not bar height**. On
a +10.5° extension a 10 mm longer stem also lifts the bar ~1.8 mm.

For one size and one stem angle there are two unknowns (spacer stack, stem length) and two
equations, so there is exactly one *exact* solution — the app solves that in closed form and reports
it, then snaps it to real parts: spacers in 5 mm steps and stem lengths from the catalog list. The
`Exact stem` / `Exact spacers` columns are the un-rounded answer; `Miss X` / `Miss Y` are what the
rounding costs you.

Results are ranked either by **most practical build** or by **smallest miss**. The practical ranking
treats everything inside the tolerance as on target, then prefers a stem near horizontal, a normal
length, and a spacer stack near a neutral 15 mm — deviation in *either* direction costs, because a
tall stack is evidence the frame is too small and a slammed one leaves nowhere to go. That matters
more than it sounds: without it, a frame one size down wins on a fraction of a millimetre of bar
position while running 30 mm of spacers, since frame size otherwise carries no weight of its own.

## Importing geometry

Pasting is the reliable path: copy the geometry table from the page and drop it in the box. The
parser handles tab, comma, semicolon and multi-space separated text, both table orientations,
cm/inch values, and the label variants sites actually use (`Head Angle` vs `Head Tube Angle`,
`Head Tube` meaning length, `Top Tube (effective)`, `Seat Tube C-T`, …). Parsed numbers land in an
editable preview so you can fix anything before saving, and obvious nonsense — non-monotonic stack
across sizes, out-of-range angles, a missing head tube angle row — is flagged.

**geometrygeeks.bike cannot be fetched by URL.** It renders the table in the browser and serves
*randomised placeholder numbers* to plain HTTP clients — repeat the same request and you get
different values. Rather than importing garbage, the fetcher detects that markup and refuses. Copy
the rendered table from your browser instead. URL import still works on ordinary dealer and brand
pages whose tables are plain HTML.

## Bundled real geometry

These four bikes are not part of the repository: they were the author's own entries, kept as an
export file. All are taken from the manufacturer's own geometry table:

| Bike | Sizes | Source |
|---|---|---|
| Specialized Tarmac SL8 | 56, 58 | [specialized.com](https://www.specialized.com/us/en/s-works-tarmac-sl8-frameset-fact-12r-carbon/p/216956) |
| Specialized Tarmac SL9 | 56, 58 | [specialized.com](https://www.specialized.com/us/en/s-works-tarmac-sl9-frameset-fact-12r-carbon/p/4293537) |
| Specialized Aethos 2 Pro | 56, 58 | [specialized.com](https://www.specialized.com/us/en/aethos-2-pro-shimano-ultegra-di2/p/4279155) |
| Trek Madone SLR 7 Gen 8 | L (58) | [trekbikes.com](https://www.trekbikes.com/us/en_US/bikes/road-bikes/performance-road-bikes/madone/madone-slr/madone-slr-7-gen-8/p/46687/) |

Two things to know about those numbers:

- Specialized publishes **frame stack/reach** (to the top of the head tube) *and* **stack-to-stem**
  (to the bottom of the stem). The calculator wants the frame figures, because it adds the headset
  stack itself. On the SL8/SL9 the two differ by 8 mm vertically and 2 mm horizontally, which is
  **8.3 mm along the 73.5° steerer** — so with the stock transition spacer, set headset stack to
  **8.5 mm**. That is the one headset figure in this app derived from published data; every other
  default is a placeholder.
- Trek's table is in centimetres and was converted to millimetres. Trek does not publish a headset
  cover stack for Gen 8, so measure it on the bike.

In 56 and 58 the SL9 is dimensionally identical to the SL8; they differ only in size 54 and in
front centre.

## Cockpit presets

`Data/cockpits.json`. Only values the manufacturer actually publishes are filled in — anything they
do not publish is flagged in the preset's note rather than guessed at.

| Preset | Reach | Drop | Stem angle | Stem lengths |
|---|---|---|---|---|
| Roval Rapide Cockpit | 75 mm | 127 mm | −6° | 75–135 mm |
| Roval Alpinist Cockpit II | 75 mm | 125 mm | −6° (not published separately) | 75–130 mm |
| Trek Aero RSL (Madone Gen 8) | 80 mm | 124 mm | not published — 0° placeholder | 70–130 mm |
| Bontrager Aeolus RSL VR-C | 100 mm | 124 mm | not published — 0° placeholder | 80–140 mm |

Roval publishes a 40 mm steerer clamp height for the Rapide; the others are assumed to match and
should be measured.

Corrections override the shipped preset of the same id for your session (export to keep them),
and *reset to catalogue values* drops the override.

**Each bike is assigned its own cockpit**, so stock builds compare directly against one another — an
SL8 with its Roval Rapide next to a Madone with its Bontrager and an Aethos with its Alpinist, all
in one ranked table with a Cockpit column. Pick per bike in the Bikes panel; *Custom* falls back to
the cockpit panel's values. Search settings (spacer step, maximum stack, stem lengths and angles)
stay global, so every bike is judged on the same terms.

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
  above are real, from the manufacturer's own tables.
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

## Layout

```
BikeFit.Web/
  Models/          BikeGeometry.cs, FitModels.cs, CockpitPreset.cs, FitProfile.cs, UserData.cs
  Services/        CockpitMath.cs, FitSolver.cs, BikeDrawing.cs,
                   Catalogue.cs, BikeLibrary.cs, FitLibrary.cs, UserDataExchange.cs,
                   GeometryImporter.cs
  Components/
    Pages/Home.razor
    Diagrams/BikeDiagram.razor, CockpitDiagram.razor
  Components/Shared/DataTransfer.razor   export / upload
  Data/bikes.json          sample archetypes (read-only)
  Data/cockpits.json       catalogue cockpit presets (read-only)
```

## Releases

Releases are cut by pushing a version tag:

```bash
git tag v1.2.0
git push origin v1.2.0
```

That one tag does two things: the **Release** workflow creates a GitHub Release whose notes are
generated from the pull requests merged since the previous tag, and the **Docker image** workflow
publishes `1.2.0`, `1.2` and `latest` images. Label pull requests (`feature`, `fix`, `data`,
`docs`) to have them grouped in the notes, or `skip-changelog` to leave one out; the grouping lives
in [`.github/release.yml`](.github/release.yml). A tag with a suffix, such as `v1.2.0-rc.1`, is
marked as a pre-release.

## License

[MIT](LICENSE) — free to use, copy, modify, merge, publish and distribute, with no warranty.
