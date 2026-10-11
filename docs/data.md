# Geometry data

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

