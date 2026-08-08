# Money Tracker

A single-page, self-contained money/expense tracker. No build step, no npm
packages, no backend — everything (HTML, CSS, JS) lives in one static file.

> **Known quirk:** the entry file in this folder is currently named
> `index_backup.html` (there is no `index.html`). `scripts/start.ps1` auto-detects
> whichever one exists, so this works either way — but if `index_backup.html` was
> meant to be renamed to `index.html`, do that manually (`Rename-Item`) whenever convenient.

---

## Prerequisites

Just a modern web browser. `scripts/start.ps1` uses Windows PowerShell (built in
on Windows) — no Node, Python, or other runtime needed.

---

## Quick start

```powershell
.\scripts\start.ps1
```

This serves the app at `http://localhost:8080` (via a tiny dependency-free
PowerShell HTTP server) and opens it in your default browser. Press `Ctrl+C`
in the terminal to stop.

Custom port:

```powershell
.\scripts\start.ps1 -Port 9000
```

### Alternative: open directly, no server

Since the app has no `fetch`/module dependencies, you can also just double-click
`index_backup.html` (or `index.html`) to open it directly in a browser via a
`file://` URL — no script needed.

---

## Project structure

```
money-tracker/
  index_backup.html   # the entire app (HTML/CSS/JS inline)
  docs/                # (currently empty)
  scripts/start.ps1    # local dev server helper
```
