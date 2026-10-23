# Court Piece Rung — Landing Page Design System

## Product and audience

Court Piece Rung is an original mobile card-game landing page for players who enjoy familiar South Asian trick-taking games, quick matches, and friendly competition. The page should make the rules feel approachable, show the social energy of the table, and move visitors toward installing the Android game.

## Design direction

Use the supplied Google Play game listing as inspiration for clear hierarchy, practical trust signals, and a focused install action—not as a visual clone. Create a distinctive card-table identity: a deep midnight-navy hero, rich emerald felt, warm gold highlights, and clean ivory content surfaces. It should feel polished, strategic, friendly, and mobile-first.

## Page architecture

1. Sticky navigation: compact Court Piece Rung wordmark, links for How to Play, Features, and Community, plus a gold Download button.
2. Hero: short eyebrow, bold headline "Own every hand.", supporting copy, primary Google Play-style download CTA, secondary "Watch gameplay" action, trust row, and a large illustrated phone mockup showing an original four-player card table.
3. Feature strip: three concise benefits—Play with friends, Master the rung, and Join a lively table—using simple line icons.
4. How it works: three numbered steps with card-inspired illustrations.
5. Game modes: casual and competitive match cards, each with clear labels and small stats.
6. Social proof: ratings, player count placeholder, and short testimonial cards.
7. Closing CTA: repeat the install invitation on a dark navy panel.
8. Minimal footer: product links, privacy, support, and social placeholders.

## Visual tokens

- `--ink`: #09162B (midnight navy)
- `--felt`: #0D6B55 (emerald card-table green)
- `--felt-deep`: #08483B
- `--gold`: #F4BE49
- `--gold-light`: #FFE1A0
- `--ivory`: #FFF9EF
- `--paper`: #FFFFFF
- `--text`: #152238
- `--muted`: #687386
- `--line`: #E6E2D9
- `--danger`: #C85050

## Type, spacing, and shape

- Font pairing: `Manrope` for headings and interface; `Inter` for body copy. Fall back to system sans-serif.
- Headline: 64–72px desktop / 42–48px mobile, 800 weight, tight letter spacing.
- Body: 16–18px, 1.55 line-height.
- Use an 8px spacing grid; section padding 88px desktop and 56px mobile.
- Max content width: 1180px.
- Radii: 14px for cards, 12px for controls, 20px for large media panels, 999px only for chips.

## Components and interaction

- Primary button: gold fill, navy text, 52px tall, 12px radius, subtle dark shadow. Hover raises 2px and brightens slightly.
- Secondary button: transparent with ivory/white border in dark areas; navy outline on light surfaces.
- Use small gold suit symbols as restrained decorative accents. Do not use gambling language, casino imagery, or real-money claims.
- Feature and testimonial cards use white/ivory surfaces with thin borders and soft shadows; avoid glassmorphism, neon, gradients, or oversized blobs.
- The hero should use layered CSS playing cards and a phone mockup rather than copied store screenshots or third-party brand marks.
- Motion should be restrained: 180–240ms ease-out hover transitions, an occasional floating-card drift, and no auto-playing video.

## Responsive behavior and accessibility

- Collapse navigation to a menu on mobile; keep the primary download action visible.
- Stack hero content with the mockup after the text on narrow screens.
- Maintain 4.5:1 text contrast, obvious keyboard focus, descriptive labels, and touch controls at least 44px tall.

## Hard constraints

This is an original landing page. Do not use Google Play logos, screenshots, app icons, copied copy, ratings, or visual identity from the reference listing. Use only the defined fonts, colors, spacing, and component styles. Do not introduce purple, pink, neon, heavy gradients, generic gaming mascots, or casino motifs.
