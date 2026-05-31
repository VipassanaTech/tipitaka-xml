"""Shared toy search UI (paginated). Identical file in both stacks."""
from __future__ import annotations

from translit import ALL_SCRIPTS


def _render_ui(engine: str, accent: str) -> str:
    options = "".join(f'<option value="{s}">{s}</option>' for s in ALL_SCRIPTS)
    # Default the UI script to deva, like the search API.
    options = options.replace('value="deva"', 'value="deva" selected')
    return f"""<!doctype html><html><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Tipiṭaka search · {engine}</title>
<style>
:root{{--accent:{accent}}}
*{{box-sizing:border-box}}
body{{font-family:system-ui,-apple-system,sans-serif;margin:0;color:#1a1a1a;background:#fafafa}}
header{{background:var(--accent);color:#fff;padding:1rem 1.5rem}}
header h1{{margin:0;font-size:1.15rem;font-weight:600}}
header .badge{{font-size:.75rem;opacity:.85}}
main{{max-width:920px;margin:0 auto;padding:1.25rem 1.5rem}}
form{{display:flex;flex-wrap:wrap;gap:.5rem;align-items:center}}
input,select,button{{font-size:15px;padding:8px 10px;border:1px solid #ccc;border-radius:6px;background:#fff}}
#q{{flex:1;min-width:220px}}
button{{background:var(--accent);color:#fff;border:none;cursor:pointer;font-weight:600}}
button:disabled{{opacity:.4;cursor:default}}
.tips{{color:#666;font-size:.8rem;margin:.6rem 0 0}}
.tips code{{background:#eee;padding:1px 5px;border-radius:4px}}
.bar{{display:flex;justify-content:space-between;align-items:center;margin:1.1rem 0 .4rem;flex-wrap:wrap;gap:.5rem}}
.bar .meta{{color:#666;font-size:.85rem}}
.pager{{display:flex;gap:.4rem;align-items:center}}
.hit{{background:#fff;border:1px solid #eee;border-radius:8px;padding:.75rem .9rem;margin:.55rem 0}}
.hit .meta{{color:#888;font-size:.78rem;margin-bottom:.3rem}}
.hit .line{{margin:.15rem 0;line-height:1.5}}
.hit .tag{{display:inline-block;min-width:42px;color:var(--accent);font-weight:600;font-size:.8rem}}
mark{{background:#ffe9a8;padding:0 1px}}
.empty{{color:#888;padding:2rem 0;text-align:center}}
</style></head><body>
<header>
  <h1>Tipiṭaka multi-script search <span class="badge">· {engine} prototype</span></h1>
</header>
<main>
<form onsubmit="go(event,1)">
  <input id="q" placeholder="vipassana · विपस्सना · ৱিপস্সনা ·  sotaapatti*" autofocus>
  <label>UI&nbsp;<select id="ui">{options}</select></label>
  <label>Mode&nbsp;<select id="mode"><option>fuzzy</option><option>exact</option><option>wildcard</option></select></label>
  <label>Per&nbsp;page&nbsp;<select id="pp"><option>10</option><option selected>20</option><option>50</option><option>100</option></select></label>
  <button type="submit">Search</button>
</form>
<p class="tips">Try <code>vipassana</code>, <code>vipassanā</code>, <code>विपस्सना</code> ·
<code>dhammacakka*</code> (wildcard) · <code>sotaapatti*</code> and <code>sotāpatti*</code> (both match) ·
<code>dhamacakka</code> (fuzzy).</p>
<div class="bar" id="bar" style="display:none">
  <span class="meta" id="summary"></span>
  <span class="pager">
    <button id="prev" type="button" onclick="go(null,_state.page-1)">‹ Prev</button>
    <span class="meta" id="pageinfo"></span>
    <button id="next" type="button" onclick="go(null,_state.page+1)">Next ›</button>
  </span>
</div>
<div id="r"></div>
</main>
<script>
const _state={{page:1,total_pages:1}};
function snippet(v){{ if(Array.isArray(v)) return v[0]||''; return v||''; }}
async function go(e,page){{
  if(e) e.preventDefault();
  page=Math.max(1,page||1);
  const q=document.getElementById('q').value.trim();
  if(!q) return;
  const ui=document.getElementById('ui').value;
  const mode=document.getElementById('mode').value;
  const pp=document.getElementById('pp').value;
  const out=document.getElementById('r');
  out.innerHTML='<p class="empty">Searching…</p>';
  const url=`/search?q=${{encodeURIComponent(q)}}&ui_script=${{ui}}&mode=${{mode}}&page=${{page}}&per_page=${{pp}}`;
  let j;
  try{{ j=await (await fetch(url)).json(); }}
  catch(err){{ out.innerHTML='<p class="empty">Request failed: '+err+'</p>'; return; }}
  _state.page=j.page||page; _state.total_pages=j.total_pages||1;
  document.getElementById('bar').style.display='flex';
  document.getElementById('summary').textContent=
    `${{j.total}} hits · input=${{j.detected_script}} · ui=${{j.ui_script}} · mode=${{j.mode}}`;
  const from=(_state.page-1)*(j.per_page||pp);
  document.getElementById('pageinfo').textContent=
    j.total? `${{from+1}}–${{Math.min(from+j.hits.length,j.total)}} of ${{j.total}}` : '0';
  document.getElementById('prev').disabled=_state.page<=1;
  document.getElementById('next').disabled=_state.page>=_state.total_pages;
  if(!j.hits.length){{ out.innerHTML='<p class="empty">No matches.</p>'; return; }}
  out.innerHTML=j.hits.map(h=>`<div class="hit">
    <div class="meta">${{h.book}} · p${{h.p_idx}}${{h.rend?' · '+h.rend:''}} · score ${{(h.score??h._score??0).toFixed?(h.score??h._score).toFixed(2):(h.score??h._score)}}${{h._matched_via_script?' · via '+h._matched_via_script:''}}</div>
    <div class="line"><span class="tag">${{j.detected_script}}</span> ${{snippet(h.input_script_highlight)||h.input_script_text||''}}</div>
    <div class="line"><span class="tag">${{j.ui_script}}</span> ${{snippet(h.ui_script_highlight)||h.ui_script_text||''}}</div>
  </div>`).join('');
  window.scrollTo(0,0);
}}
</script></body></html>"""
