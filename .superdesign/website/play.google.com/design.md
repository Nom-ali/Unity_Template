---
version: "superdesign-alpha"
name: "Store shelf grid"
description: "Light-mode Material system with a single dark cinematic hero band, edge-to-edge sticky navbar, and a rationed emerald accent across pill buttons, ratings, and stat rows."
colors:
  background: "#FFFFFF"
  surface-dark: "#202124"
  surface-muted: "#EEEEEE"
  text-primary: "#202124"
  text-secondary: "#5F6368"
  text-on-dark: "#FFFFFF"
  accent: "#00A173"
  accent-hover: "#00B380"
  accent-deep: "#01875F"
  link: "#12229C"
  border: "#DADCE0"
  border-focus: "#185ABC"
typography:
  display-lg:
    fontFamily: "Google Sans Display"
    fontSize: "64px"
    fontWeight: 500
    lineHeight: "1.19"
  headline-md:
    fontFamily: "Google Sans"
    fontSize: "22px"
    fontWeight: 500
    lineHeight: "1.27"
  body-md:
    fontFamily: "Google Sans"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: "2.29"
  label-icon:
    fontFamily: "Google Material Icons"
    fontWeight: 400
  label-icon-extended:
    fontFamily: "Material Icons Extended"
    fontWeight: 400
  body-alt:
    fontFamily: "Roboto"
    fontWeight: 400
spacing:
  base: "8px"
  gap: "24px"
  section-padding: "64px"
rounded:
  control: "8px"
  card: "16px"
  chip: "9999px"
  small: "4px"
  medium: "12px"
  large: "28px"
components:
  button-primary:
    background: "#00A173"
    text-color: "#202124"
    radius: "8px"
    height: "44px"
    padding: "10px 16px"
    hover-background: "#00B380"
    hover-shadow: "rgba(60, 64, 67, 0.3) 0px 1px 2px 0px, rgba(60, 64, 67, 0.15) 0px 1px 3px 1px"
  button-navbar-text:
    background: "transparent"
    text-color: "#000000"
    radius: "0px"
    height: "64px"
    padding: "0px 16px"
  button-navbar-tab:
    background: "transparent"
    text-color: "#000000"
    radius: "0px"
    height: "48px"
    padding: "12px"
  button-chip-outline:
    background: "#FFFFFF"
    text-color: "#5F6368"
    radius: "9999px"
    height: "32px"
    padding: "0px"
    border: "1px solid #DADCE0"
  card-carousel-tile:
    background: "transparent"
    radius: "0px"
    padding: "0px"
  card-event-banner:
    background: "#202124"
    radius: "16px"
    padding: "0px"
  card-list-row:
    background: "transparent"
    radius: "0px"
    padding: "0px"
---
# Store shelf grid
Source: https://play.google.com/store/apps/details?id=com.king.candycrushsaga

## Overview
This is a Material-design storefront: a near-white, card-and-list surface built on an 8px grid, ruled by a strict 64px header and one saturated emerald accent used only for the primary action and rating fills. The single expressive gesture is the hero — a full-bleed near-black cinematic band bearing an in-context screenshot — everything below it reverts to flat #FFFFFF panels, hairline borders, and small Google Sans text. It reads as restrained, information-dense, and utilitarian rather than expressive: Material Design's elevation-and-motion doctrine applied to a dense retail listing, not a marketing page.

## Composition
The first screen is a dark hero band (roughly the top 590px of the page) holding a left-aligned title block — an oversized display word-mark, a byline, a rating/downloads/badge cluster, and a two-button row — beside a right-weighted illustrative screenshot with a soft radial vignette. Below the fold the page snaps to white and becomes a strict two-column layout: a ~68%-width main column (description, tag chips, an event banner, data-safety card, ratings module, individual reviews, "what's new") beside a ~28%-width right rail of similar-app list rows, repeating the same row pattern (icon-thumbnail + title + rating) five to six times per block. The deliberate choice is a single hero color-drop against an otherwise colorless body — rejecting the alternative of color-coding each section — so emphasis lands only on the one primary button and the emerald rating stars/fills, keeping the long scroll calm and scannable.

## Colors
Background is flat #FFFFFF (dominant, ~79% of pixels) carrying nearly all body content; the hero and its illustration panel are the only large dark passage (#202124, ~72% of the *declared* hero area but a small minority of total page pixels — it is one band, not the page mood). Text ink is #202124 for primary copy and #5F6368 for secondary/meta text (byline, timestamps, helper copy) — both sit on white in the body and as white-on-dark only inside the hero. The single accent, #00A173 (with a lighter hover state #00B380 and a deeper variant #01875F), is rationed to: the primary Install button, the star-rating fill bars, and small in-context marks — never spread into backgrounds or borders. Structural lines use #DADCE0 hairlines; a link/utility blue #12229C and #185ABC appear only in the small "Learn more"/link and focus-ring positions. Surfaces stay uncolored by design — cards, rows, and the right rail are all transparent-on-white — so the accent's rarity is what gives it weight.

## Typography
Display headline (the hero name) uses Google Sans Display at 64px/500, line-height 1.19 — the only oversized type on the page. Section and card headings drop to Google Sans 22px/500 (headline-md). Nearly all running copy — descriptions, review bodies, list rows, byline — sits in Google Sans 14px/400 with a notably open line-height of 2.29, which is what gives the dense list sections their breathing room despite small type. Secondary/meta text reuses the same 14px face at #5F6368 rather than a distinct size, so hierarchy comes from color and weight, not scale. Iconography is carried by Google Material Icons and Material Icons Extended glyphs inline with text (rating stars, chip icons, safety-row icons); Roboto appears as a body fallback face alongside Google Sans.

## Layout
Content is capped at a 1296px max-width, centered, with 64px section padding between major blocks. The navbar is the single full-viewport exception. Below it, the dominant pattern is a fixed two-column split (grid: 2 columns, gap 24px, rows [21/78]) — a narrow top strip and a tall lower block — carrying the main-column/right-rail relationship. The main column itself is a single-column stack (grid: 1 column, 6 items, uniform ~107px rows) for description, banner, safety, ratings, and reviews; the right rail is a matching single-column list (5 uniform ~100px rows) of similar-app entries. No card grid widens past one column — this is a list-and-rail composition, not a bento or masonry system; density is high (rows tight, 24px/16px/12px gaps) with generous internal line-height compensating for narrow row heights.

## Components
- **Navbar**: edge-to-edge square bar, 100% viewport width (0px inset either side), 64px tall, 0px radius on all four corners, sticky, background #FFFFFF. Carries 18 total interactive items: a logo mark at left, a row of top-level text tabs (transparent fill, #000000 text, 0px radius, 64px height, 0px 16px padding), and utility icon buttons (search, help, avatar) at right; a second tier of transparent 48px-tall tab buttons (0px radius) sits just under it for in-page section anchors.
- **Hero primary button**: an observed solid near-white/cream pill... — correction: the measured prominent button is #00A173 fill with #202124 text, 8px radius (slightly-rounded, not a pill), 44px height, 10px 16px padding; hover shifts fill to #00B380 with shadow `rgba(60, 64, 67, 0.3) 0px 1px 2px 0px, rgba(60, 64, 67, 0.15) 0px 1px 3px 1px`. This is the single primary action in the hero, sitting left-aligned under the byline/rating cluster, beside two lower-emphasis siblings (a text-icon share link and an outlined add-to-list button) and a dark-glass trailer chip at the hero's right edge.
- **Chip/filter button**: #FFFFFF fill, #5F6368 text, 9999px radius (full pill), 32px height, 0px padding, 1px solid #DADCE0 border — used mid-page in a horizontal wrapping cluster of tag chips beneath the description.
- **Media carousel tile**: transparent background, 0px radius, 0px padding; appears directly under the hero as a horizontally scrolling rail of portrait screenshot tiles, each nearly full-bleed image with a short caption baked into the artwork near the tile's bottom edge; a circular next-arrow control floats at the rail's right edge.
- **Event/offer banner**: full-width dark panel (#202124-family) with 16px radius, split into a left text block (eyebrow tag, two-line heading, no CTA text) and a right image-bleed panel showing character/product art; sits as one continuous row beneath the tag-chip cluster.
- **Info/safety card**: white surface, hairline #DADCE0 border, 12–16px radius, internal padding ~20–24px; stacked icon-label-caption rows (share icon, cloud icon, lock icon, trash icon) each pairing a small heading with a muted #5F6368 caption line, closed by a text link.
- **Rating summary module**: a large numeral (4.6-scale) beside five horizontal bar rows (one per star level) filled in accent green against a light-gray track, with a starred-average glyph row beneath.
- **Review row**: circular avatar (photo or initial-on-color), name, star row, timestamp, body copy in 14px/2.29, a "N people found this helpful" caption, and a Yes/No pill-button pair — repeated as one card family per reviewer, stacked with generous vertical spacing rather than boxed borders.
- **Similar-app list row**: square-rounded thumbnail icon (~8px radius), title, publisher-style subline, star rating — repeated 5–6× in the right rail and again in a "similar games" block, uniform row height (~100–107px).
- **Footer**: transparent background, no border, four-column link list (12 total links) in #5F6368/#000000 text on white, plain text with no card treatment, region selector at far right.

## Graphics & Effects
The hero's illustration panel carries three directional scrims used to vignette the product screenshot into the dark hero field: `linear-gradient(90deg, rgb(32, 33, 36) 0px, rgba(0, 0, 0, 0) 56%)`, `linear-gradient(0deg, rgb(32, 33, 36) 0px, rgba(0, 0, 0, 0) 56%)`, and `linear-gradient(270deg, rgb(32, 33, 36) 0px, rgba(0, 0, 0, 0) 56%)` — each fades from the hero's dark surface color to transparent, feathering the screenshot's edges into the surrounding black rather than covering the full hero; together they occupy only the illustration's own bounding box, roughly the right two-thirds of the hero band, not the page. Elevation is expressed through two shadow tiers: a tight two-layer button shadow (`rgba(60, 64, 67, 0.3) 0px 1px 2px 0px, rgba(60, 64, 67, 0.15) 0px 1px 3px 1px`) on the primary button's hover state, and a heavier three-layer shadow (`rgba(0, 0, 0, 0.2) 0px 3px 5px -1px, rgba(0, 0, 0, 0.14) 0px 6px 10px 0px, rgba(0, 0, 0, 0.12) 0px 1px 18px 0px`) reserved for floating/overlay surfaces such as menus or the trailer control. A soft ambient glow (`rgb(128, 128, 128) 0px 0px 5px 0px`) rings small focal icons. No noise, grain, or pattern texture is present — surfaces are flat.

## Motion
Interactions are fast and linear rather than springy: color and background transitions run at `background 0.2s ease` and `color 0.15s linear`, while structural state changes (borders, shadows, opacity) use `cubic-bezier(0.4, 0, 0.2, 1)` at 0.28s — a standard Material deceleration curve, not an overshoot. Ripple feedback on buttons is driven by keyframes (`mdc-ripple-fg-radius-in`, `mdc-ripple-fg-opacity-in/out`), and floating form labels animate via `mdc-floating-label-shake-float-above-standard`/`-outlined`. A page-load progress bar uses `boqChromeapiPageProgressAnimation`. Scroll-driven CSS animations are present but subtle, likely reserved for the carousel rail and sticky header shadow reveal.

## Guardrails
- Keep the accent green confined to the primary button, rating fills, and small marks — never tint card backgrounds or the navbar with it.
- Do not extend the hero's directional scrim gradients beyond the illustration panel into a full-bleed background wash.
- Preserve the navbar as a literal edge-to-edge, 0px-radius, 64px bar — never convert it to an inset or rounded floating bar.
- Keep body type small (14px) with the open 2.29 line-height; do not enlarge list/review text to compensate for density.
- Reserve 8px "slightly-rounded" corners for the primary button; do not give it a full pill or heavy 16–28px radius.
- Leave all list rows and rail cards borderless/transparent — hierarchy comes from spacing and hairline dividers, not card fills.