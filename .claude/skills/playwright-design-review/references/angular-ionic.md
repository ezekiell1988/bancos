# Angular/Ionic UI Checks

Use these checks when a screen is built with Angular standalone and Ionic components.

## Setup

- Start the app with its normal command, usually `npm start -- --host 127.0.0.1 --port <port>` from `src/VoiceBot.Web`.
- If the route is protected, seed `localStorage` with the auth token/user before navigation.
- Mock API responses with `page.route()` when backend state is hard to reproduce.
- Use mobile-like context options: `isMobile: true`, `hasTouch: true`, and the target viewport.

## Overflow Fix Patterns

- Grid children that contain text should usually have `min-width: 0`.
- Use `grid-template-columns: repeat(n, minmax(0, 1fr))` instead of `minmax(220px, 1fr)` for cards that must fit the viewport.
- Use `overflow-wrap: anywhere` for addresses, IDs, model names, invoice codes, and long labels.
- Use ellipsis only where truncation is acceptable; otherwise allow wrapping.
- Avoid document-level horizontal scroll. If an inner carousel is intentional, confine `overflow-x: auto` to that element and verify no parent expands.
- For `ion-content`, `ion-content::part(scroll) { overflow-x: hidden; }` can be a guard, but still fix the offending child.

## Screenshot Targets

- Match the reference screenshot viewport first.
- Add at least one narrower phone viewport, for example `430x932`.
- For fixed footers, capture both first viewport and lower scroll positions if the footer may cover cards.

## Reporting

Include:

- screenshot path
- viewport dimensions
- whether `document.scrollWidth` exceeded `window.innerWidth`
- first offenders with selector/class/text
- exact files changed to fix the visual issue
