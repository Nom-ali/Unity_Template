# Rolling Ball Shop — Mobile UI Design System

## Product context

An original portrait mobile-game shop screen for a rolling-ball arcade game. It should feel joyful, tactile, and instantly scannable, while remaining clearly distinct from the supplied reference mockup.

## Mandatory structure

- One top-left back button.
- Three section title bars, in this order: `Boost Packs`, `Coin Vault`, and `Ad-Free`.
- Exactly five prominent in-app purchase buttons: two boost-pack cards, two coin-purchase cards, and one full-width ad-free purchase card.
- Design at a 1080 × 1920 portrait mobile-game viewport. All five purchase buttons must be visibly complete without relying on scrolling in the preview.

## Visual direction

Use an original arcade-sports look: midnight-blue background with a subtle radial spotlight, cobalt and aqua panels, lime highlights, orange price bars, and white display text. Use large, rounded, dark-outlined cards with soft drop shadows, but do not copy the reference artwork, labels, layout proportions, palette, icons, pricing, or typography. Use original stylised vector-like art: a glowing rolling ball, lightning bolts, boost trails, magnet, shield, and abstract coin tokens. Do not use logos, screenshots, ads badges, or copied art from any game.

## Tokens

- Background: `#061B3A`
- Panel navy: `#0A3A75`
- Panel cobalt: `#126AD1`
- Panel aqua: `#19C5D8`
- Accent lime: `#C8F53D`
- Accent orange: `#FF9F1C`
- Accent coral: `#FF5B6E`
- Surface white: `#F9FCFF`
- Ink: `#08162B`
- Muted ink: `#38516B`
- Outline: `#07111E`

## Typography and components

- Use `Baloo 2` or a similarly friendly rounded display sans for headings, with `Nunito Sans` for small labels. Never use a serif, condensed sports typeface, or handwritten font.
- Headings are white with a 3–4px near-black outline and a shallow navy shadow.
- Section bars are 64px tall, cobalt, 16px radius, and hold centered display labels.
- Purchase cards are rounded rectangles with a 4px dark outline and low dark shadow. Each card has an upper art area, title, one benefit label, and a high-contrast orange price button attached at the bottom.
- Back control: 72px square, navy-blue surface, white left chevron, dark outline; no text.
- Currency display at top-right: a small lime orb icon plus an illustrative `820` balance; it is display-only, not one of the five purchase buttons.

## Layout

- Header: back button left, `Shop` centered, currency display right.
- Use 24px outer margins and 18–22px gaps.
- Two equal cards under `Boost Packs`: `Turbo Bundle` and `Shield Bundle`.
- Two equal cards under `Coin Vault`: `Coin Stack` and `Coin Cache`.
- One full-width card below `Ad-Free`: `Remove Interruptions`, with an original crossed-out play icon, a concise benefit, and a purchase button.
- Make price labels illustrative (`$2.49`, `$4.99`, etc.), not real offers.

## Motion and accessibility

- Add only subtle 180ms press-in states and a light sparkle/roll visual cue; no neon, glossy casino styling, or excessive gradients.
- Maintain high contrast; touch targets are at least 44px; labels remain readable at mobile scale.

## Fidelity rule

Use ONLY the fonts, colors, spacing, and component styles defined here. Do not introduce any fonts, colors, or visual styles not in this design system.
