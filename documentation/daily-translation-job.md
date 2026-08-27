# Daily Tipitaka translation job

A GitHub Actions job that works through the Pali canon a chunk at a time: each
day it translates the next chunk of verses from the current file to English,
emails the translation, archives the day's output, and advances its saved
position for next time.

## Flow

1. Load `state/translation-progress.json` — `{"file": "<name>.mul.xml", "next_verse": N}`.
2. Parse that file (`romn/<name>.mul.xml`) and select the next chunk of verses
   starting at `next_verse` (see "Verse and chunk model" below).
3. Translate the chunk's Pali text to English via the Anthropic API.
4. Email the English translation via the Resend API.
5. Write three archive files to `translation/` (Pali only, English only, and a
   combined record — see `translation/README.md`).
6. Update `state/translation-progress.json` to point at the verse after the
   last one translated (or the start of the next file, if the chunk reached
   the end of the current one).
7. The GitHub Actions workflow commits and pushes the updated state and any
   new archive files back to the repo.

Steps 3–6 are atomic: if anything fails partway, nothing from that run is
kept — no partial archive files, no state advance — so the same chunk is
simply retried on the next run. See "Error handling" below.

## Verse and chunk model

A **verse** is a `<p rend="bodytext" n="N">` element plus any immediately
following unnumbered `<p rend="bodytext">` continuation paragraphs. Verse
numbers increase monotonically through a file with no resets.

A **chunk** is up to `CHUNK_SIZE` consecutive verses (default 20) starting
from the saved position, but it never crosses a chapter/kanda boundary —
concretely, it stops as soon as the verse's immediate parent `<div>` element
changes, even if that means fewer than `CHUNK_SIZE` verses that day.

**File order**: all `romn/*.mul.xml` files, sorted alphabetically, treated as
a circular list. Progress is seeded to start at `vin01m.mul.xml`, verse 1.
When the alphabetically-last file is exhausted, the job wraps back around to
the alphabetically-first file.

## File and directory layout

- `scripts/daily_translate.py` — the job's entire logic (stdlib-only Python,
  no pip install required).
- `state/translation-progress.json` — current position (file + next verse).
- `translation/` — daily archive output (`pali-`, `eng-`, `full-` files per
  run; see `translation/README.md`).
- `.github/workflows/daily-translation.yml` — the scheduled workflow.

## Configuration

Set these in the repo's Settings → Secrets and variables → Actions:

**Secrets**
- `ANTHROPIC_API_KEY` — used to call the Anthropic API for translation.
- `RESEND_API_KEY` — used to send email via the Resend API.
- `EMAIL_TO` — destination address for both the daily reading and any error reports.

**Variables**
- `EMAIL_FROM` — sender address (must be on a domain verified in Resend).
- `CHUNK_SIZE` (optional, default `20`) — verses per day.
- `ANTHROPIC_MODEL` (optional, default `claude-sonnet-5`).

**Repo setting**: Settings → Actions → General → Workflow permissions must be
set to "Read and write permissions" so the job's `GITHUB_TOKEN` can push its
own commits.

The workflow can also be run manually (`workflow_dispatch`) with an optional
`chunk_size` override and a `dry_run` toggle that skips the Anthropic call,
the email, the archive files, and the state update — it only logs the
selected chunk, for safe testing.

## Error handling

If translation, emailing, file-writing, or the state update fails at any
point, the job sends a separate error-report email instead (subject
`Tipitaka job ERROR — <context>`, containing the exception and a truncated
traceback) and exits non-zero. If the error-report email itself can't be
sent (e.g. broken email secrets), the error is printed to the job log and the
run still exits non-zero, so the failure is visible as a red run in the
Actions tab even in that fallback case.

## Known assumptions

- Dates and run numbering (`run1`, `run2`, ...) use UTC, matching the cron
  schedule.
- The email body contains the English translation only; the Pali text and
  the combined record are archived on disk in `translation/`, not emailed.
- File ordering is alphabetical across all `romn/*.mul.xml` files, not a
  curated canonical reading order — it starts at `vin01m.mul.xml` and wraps
  around after the last file.
