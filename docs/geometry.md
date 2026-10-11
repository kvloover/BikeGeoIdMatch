# How the solver works

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

