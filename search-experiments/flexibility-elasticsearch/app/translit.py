"""Aksharamukha-based transliteration between VRI script codes."""
from __future__ import annotations

from functools import lru_cache
from aksharamukha import transliterate

from roman import fold, iast_variants, literal_reading, long_reading

# VRI folder name -> Aksharamukha script name.
# RussianCyrillic is Aksharamukha's Pali Cyrillic mapping (matches VRI cyrl).
SCRIPTS: dict[str, str] = {
    "deva": "Devanagari",
    "romn": "IAST",
    "beng": "Bengali",
    "mymr": "Burmese",
    "sinh": "Sinhala",
    "thai": "Thai",
    "cyrl": "RussianCyrillic",
    "gujr": "Gujarati",
    "guru": "Gurmukhi",
    "khmr": "Khmer",
    "knda": "Kannada",
    "mlym": "Malayalam",
    "taml": "Tamil",
    "telu": "Telugu",
    "tibt": "Tibetan",
}

ALL_SCRIPTS = tuple(SCRIPTS.keys())

# Unicode block ranges used to guess the script of a raw query string.
# Order matters: more specific blocks first.
_RANGES: list[tuple[str, int, int]] = [
    ("deva", 0x0900, 0x097F),
    ("beng", 0x0980, 0x09FF),
    ("guru", 0x0A00, 0x0A7F),
    ("gujr", 0x0A80, 0x0AFF),
    ("taml", 0x0B80, 0x0BFF),
    ("telu", 0x0C00, 0x0C7F),
    ("knda", 0x0C80, 0x0CFF),
    ("mlym", 0x0D00, 0x0D7F),
    ("sinh", 0x0D80, 0x0DFF),
    ("thai", 0x0E00, 0x0E7F),
    ("tibt", 0x0F00, 0x0FFF),
    ("mymr", 0x1000, 0x109F),
    ("khmr", 0x1780, 0x17FF),
    ("cyrl", 0x0400, 0x04FF),
]


def detect_script(query: str) -> str:
    """Best-effort script detection by counting characters per Unicode block."""
    counts: dict[str, int] = {}
    for ch in query:
        cp = ord(ch)
        for code, lo, hi in _RANGES:
            if lo <= cp <= hi:
                counts[code] = counts.get(code, 0) + 1
                break
    if not counts:
        return "romn"
    return max(counts.items(), key=lambda kv: kv[1])[0]


@lru_cache(maxsize=4096)
def translit(text: str, src: str, dst: str) -> str:
    """Transliterate `text` from VRI script code `src` to `dst`. Cached."""
    if src == dst:
        return text
    return transliterate.process(SCRIPTS[src], SCRIPTS[dst], text)


def fan_out(query: str, src: str | None = None, expand: bool = True) -> dict[str, list[str]]:
    """Return {script_code: [query_strings]} for all 15 scripts.

    Each script maps to a *list* of clauses to OR together. Roman is handled
    specially (see roman.py): a typed `aa` is ambiguous (long `ā` vs. genuine
    vowel hiatus).

    With ``expand=True`` (default) the romn clauses cover **both** folded
    readings — `sotaapatti` finds `sotāpatti` AND a real `...aa...` hiatus word
    stays findable. With ``expand=False`` (strict/literal toggle) doubled vowels
    are matched literally only: `aa` means two short `a`s, never `ā`.

    The other 14 scripts get a single transliteration (the conventional all-long
    reading, or the literal reading in strict mode), to bound fan-out.
    """
    if src is None:
        src = detect_script(query)

    if src == "romn":
        if expand:
            primary = long_reading(query)
            out: dict[str, list[str]] = {
                dst: [translit(primary, "romn", dst)] for dst in ALL_SCRIPTS
            }
            # The romn field is diacritic-folded; OR every candidate spelling.
            out["romn"] = list(dict.fromkeys(fold(v) for v in iast_variants(query)))
        else:
            literal = literal_reading(query)
            out = {dst: [translit(literal, "romn", dst)] for dst in ALL_SCRIPTS}
            out["romn"] = [fold(literal)]
        return out

    out = {dst: [translit(query, src, dst)] for dst in ALL_SCRIPTS}
    # The romn field is stored diacritic-folded, so fold the transliterated
    # Roman form too (e.g. a Devanagari query → IAST → folded ASCII).
    out["romn"] = [fold(out["romn"][0])]
    return out
