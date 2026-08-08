# ATS Resume Builder

A single-page, self-contained ATS-friendly resume builder. No build step, no
npm packages, no backend — everything (HTML, CSS, JS) lives in one static file.

---

## Prerequisites

Just a modern web browser. `scripts/start.ps1` uses Windows PowerShell (built in
on Windows) — no Node, Python, or other runtime needed.

---

## Quick start

```powershell
.\scripts\start.ps1
```

This serves the app at `http://localhost:8081` (via a tiny dependency-free
PowerShell HTTP server) and opens it in your default browser. Press `Ctrl+C`
in the terminal to stop.

Custom port:

```powershell
.\scripts\start.ps1 -Port 9001
```

### Alternative: open directly, no server

Since the app has no `fetch`/module dependencies, you can also just double-click
`index.html` to open it directly in a browser via a `file://` URL — no script needed.

---

## Project structure

```
resume-builder/
  index.html          # the entire app (HTML/CSS/JS inline)
  scripts/start.ps1    # local dev server helper
```
