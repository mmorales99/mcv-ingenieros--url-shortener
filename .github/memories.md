# Project Memories

- Durable project memories/rules must be written in `.github/memories.md`.
- Implementations should be idempotent whenever it is feasible and reasonable for the feature.
- After the user approves a plan, proceed directly to implementation by default.
- Only involve the Software Architect agent for architecture-impacting work, not routine implementation reviews.
- Only use Frontend Quality Guardian for frontend changes that materially affect accessibility, SEO, or other high-impact frontend quality concerns.
- Only use UX Design Advisor when creating a new Blazor page or heavily reworking an existing one.
- After review feedback, batch fixes together and wait until a complete re-review can be made instead of re-running reviews after small fixes.
- During review-fix iterations, use compile/test checks for small changes and request re-review only when the full batch of fixes is ready.
- After a phase is completed, commit and push immediately.
