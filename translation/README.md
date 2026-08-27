# Daily translation archive

Populated by the daily translation job (see `.github/workflows/daily-translation.yml`
and `scripts/daily_translate.py`). Each successful run writes three files, named
by UTC date and a per-day run counter:

- `pali-YYYY-MM-DD-runN.txt` — the original Pali text of that day's chunk.
- `eng-YYYY-MM-DD-runN.txt` — the English translation only (matches the emailed content).
- `full-YYYY-MM-DD-runN.txt` — heading, English translation, and original Pali together.

`runN` starts at `run1` for the first run on a given UTC date and increments for
any additional runs that day (e.g. a manual re-run).
