// Pali lookup: double-click selection -> fetch dpdict -> show modal
(function(){
    try{}catch(e){}
    // expose a flag for quick manual checking in the console
    try{ window._paliLookupLoaded = true; }catch(e){}
    // ensure dpd.css is loaded (the user's provided stylesheet)
    if (!document.querySelector('link[data-dpdcss]')) {
        var link = document.createElement('link');
        link.rel = 'stylesheet';
        // Try to compute a safe absolute href for dpd.css based on existing site stylesheet if present.
        var dpdHref = '/css/dpd.css';
        var ref = document.querySelector('link[href*="tipistyles.css"]');
        if (ref && ref.href) {
            try {
                dpdHref = new URL('../css/dpd.css', ref.href).href;
            } catch (e) {
                dpdHref = '/css/dpd.css';
            }
        }
        link.href = dpdHref;
        link.setAttribute('data-dpdcss','1');
        // Prefer inserting dpd.css before the main site stylesheet so site rules (tipistyles.css) can override dpd's global rules like `body`.
        var tipiRef = document.querySelector('link[href*="tipistyles.css"]');
        if (tipiRef && tipiRef.parentNode) {
            tipiRef.parentNode.insertBefore(link, tipiRef);
        } else {
            document.head.appendChild(link);
        }
    }
    // create overlay + popup (apply minimal inline styles so modal shows even if external css is missing)
    var overlay = document.createElement('div');
    overlay.id = 'pali-meaning-overlay';
    overlay.style.position = 'fixed';
    overlay.style.top = '0'; overlay.style.left = '0'; overlay.style.width = '100%'; overlay.style.height = '100%';
    overlay.style.background = 'rgba(0,0,0,0.4)';
    overlay.style.display = 'none';
    overlay.style.zIndex = '9998';
    document.body.appendChild(overlay);

    var popup = document.createElement('div');
    popup.id = 'pali-meaning-popup';
    // basic container styles
    popup.style.position = 'fixed';
    popup.style.left = '50%'; popup.style.top = '50%';
    popup.style.transform = 'translate(-50%, -50%)';
    popup.style.maxWidth = '900px';
    popup.style.width = '90%';
    popup.style.maxHeight = '80%';
    popup.style.overflow = 'auto';
    popup.style.background = '#fff';
    popup.style.boxShadow = '0 10px 30px rgba(0,0,0,0.25)';
    popup.style.borderRadius = '6px';
    popup.style.padding = '0';
    popup.style.display = 'none';
    popup.style.zIndex = '9999';
    popup.innerHTML = '<div class="pm-window"><button class="pm-close" aria-label="Close">×</button><div class="pm-content" style="padding:18px"></div></div>';
    document.body.appendChild(popup);

    // normalize a selected word by trimming surrounding punctuation
    function normalizeWord(word){
        if (!word) return '';
        try{ return word.replace(/^[^\p{L}]+|[^\p{L}]+$/gu,''); }catch(err){ return word.replace(/^[^A-Za-zĀāĪīŪūḌḍṆṇṚṛŚśṢṣṬṭḶḷḸḸ]+|[^A-Za-zĀāĪīŪūḌḍṆṇṚṛŚśṢṣṬṭḶḷḸḸ]+$/g,''); }
    }

    // pending-promises map for prefetching (no long-term caching)
    var PENDING_TTL_MS = 5 * 1000; // keep resolved promise around briefly to benefit immediate lookups
    var dpdPending = new Map();

    function prefetchPali(word){
        if (!word) return Promise.reject(new Error('empty'));
        if (dpdPending.has(word)) return dpdPending.get(word);
        var p = fetch('https://www.dpdict.net/search_json?q=' + encodeURIComponent(word))
            .then(function(res){ if(!res.ok) throw new Error('Fetch failed'); return res.json(); })
            .then(function(json){
                // leave the resolved promise in the map for a short time, then clear it
                try{
                    setTimeout(function(){ try{ dpdPending.delete(word); }catch(e){} }, PENDING_TTL_MS);
                }catch(e){}
                return json;
            }).catch(function(err){ dpdPending.delete(word); throw err; });
        dpdPending.set(word, p);
        return p;
    }

    // when the user selects text (mouseup/touchend), prefetch the likely word
    var _prefetchTimer = null;
    function schedulePrefetchForSelection(){
        if (_prefetchTimer) clearTimeout(_prefetchTimer);
        _prefetchTimer = setTimeout(function(){
            var sel = getSelectionText();
            if (!sel) return;
            var w = (sel.split(/\s+/)[0] || sel);
            w = normalizeWord(w);
            if (!w || w.length < 2) return;
            prefetchPali(w).catch(function(){});
        }, 120);
    }
    document.addEventListener('mouseup', schedulePrefetchForSelection);
    document.addEventListener('touchend', schedulePrefetchForSelection);

    // position the close button in the top-right of the modal with padding
    (function(){
        var win = popup.querySelector('.pm-window');
        var cb = popup.querySelector('.pm-close');
        var contentEl = popup.querySelector('.pm-content');
        if (win && win.style) win.style.position = 'relative';
        if (cb && cb.style) {
            cb.style.position = 'absolute';
            cb.style.top = '8px';
            cb.style.right = '8px';
            cb.style.padding = '4px 8px';
            cb.style.cursor = 'pointer';
            cb.style.borderRadius = '4px';
        }
        // reduce top padding of pm-content to 4px while keeping horizontal padding
        if (contentEl && contentEl.style) contentEl.style.padding = '4px 18px';
    })();


    function getSelectionText(){
        var s = window.getSelection();
        return s ? s.toString().trim() : '';
    }

    // on double-click, grab selection and lookup
    document.addEventListener('dblclick', function(e){
        try{}catch(e){}
        // delay reading the selection briefly so the browser has updated it
        setTimeout(function(){
            var sel = getSelectionText();
            try{}catch(e){}
            if (!sel) return;
            var word = sel.split(/\s+/)[0] || sel;
            // normalize/strip surrounding punctuation consistently
            word = normalizeWord(word);
            if (!word) return;
            try{}catch(e){}
            lookupPali(word);
        }, 0);
    });

    // close popup
    function hidePopup(){ popup.style.display='none'; overlay.style.display='none'; }
    popup.querySelector('.pm-close').addEventListener('click', hidePopup);
    overlay.addEventListener('click', hidePopup);

    function showPopup(){
        popup.style.display = 'block';
        overlay.style.display = 'block';
    }

    function lookupPali(word){
        var content = popup.querySelector('.pm-content');
        // clear any previous result immediately so old content doesn't flash
        try { content.innerHTML = ''; }catch(e){}
        // prefetch/attach to any in-flight request, otherwise start a fetch
        prefetchPali(word)
        .then(function(json){ renderResult(word,json); })
        .catch(function(err){ content.innerHTML = '<div class="pm-section"><strong>Error</strong><div>Unable to fetch meaning.</div></div>'; try{ showPopup(); }catch(e){} console.error(err); });
    }

    function renderResult(word, json){
        var content = popup.querySelector('.pm-content');
        try{
            // Build a header + body layout inside pm-content so styling matches expected modal
            var titleHtml = '<div class="pm-header"><h3 class="pm-title">' + escapeHtml(word) + '</h3></div>';
            var bodyHtml = '';
            var creditHtml = '<div class="pm-credit" style="font-family:sans-serif;font-size:10px;margin:0;">Courtesy of <a href="https://www.dpdict.net" target="_blank" rel="noopener noreferrer">Digital Pali Dictionary</a></div>';

            // dpdict sometimes returns an object with HTML fragments; render HTML-containing fields
            // NOTE: intentionally skip `summary_html` so the top summary listing is not shown in the modal.
            if (json && typeof json === 'object') {
                var parts = [];
                // prefer full HTML fragments if present; include any field that looks like HTML, but skip the summary
                for (var k in json) {
                    if (!Object.prototype.hasOwnProperty.call(json,k)) continue;
                    if (k === 'summary_html') continue; // skip top summary
                    var v = json[k];
                    if (typeof v === 'string' && v.indexOf('<') !== -1) parts.push(v);
                }
                // if we found any detailed fragments, render them
                if (parts.length > 0) {
                    bodyHtml += '<div class="pm-body"><div class="dpd">' + parts.join('\n') + '</div></div>';
                    content.innerHTML = titleHtml + creditHtml + bodyHtml;
                    try{ showPopup(); }catch(e){}
                    enhanceModal(content);
                    return;
                }
                // otherwise continue — fallbacks below will handle arrays or show raw JSON
            }

            // fallback for array-style responses
            if (!Array.isArray(json) || !json.length) {
                bodyHtml += '<div class="pm-body"><div class="dpd"><div class="pm-section"><strong>No results</strong><div>No entries found.</div></div></div></div>';
                content.innerHTML = titleHtml + bodyHtml;
                try{ showPopup(); }catch(e){}
                return;
            }

            // show top senses / glosses
            var senses = [];
            var examples = [];
            var roots = new Set();
            var compounds = new Set();

            json.forEach(function(entry){
                if (entry.senses) {
                    entry.senses.forEach(function(s){ if (s.gloss) senses.push(s.gloss); if (s.examples) s.examples.forEach(function(ex){ examples.push(ex); }); });
                }
                if (entry.root_family) { (Array.isArray(entry.root_family)?entry.root_family:[entry.root_family]).forEach(function(r){ if(r) roots.add(r); }); }
                if (entry.compound_family) { (Array.isArray(entry.compound_family)?entry.compound_family:[entry.compound_family]).forEach(function(c){ if(c) compounds.add(c); }); }
            });

                bodyHtml += '<div class="pm-body"><div class="dpd">';
            if (senses.length) {
                bodyHtml += '<div class="pm-section"><strong>Meanings / examples</strong><div>' + escapeHtml(senses.slice(0,6).join('; ')) + '</div></div>';
            }
            if (examples.length) {
                bodyHtml += '<div class="pm-section"><strong>Examples</strong>';
                examples.slice(0,6).forEach(function(ex){ bodyHtml += '<div>' + escapeHtml(ex) + '</div>'; });
                bodyHtml += '</div>';
            }
            if (roots.size) {
                bodyHtml += '<div class="pm-section"><strong>Root family</strong><div>' + escapeHtml(Array.from(roots).join(', ')) + '</div></div>';
            }
            if (compounds.size) {
                bodyHtml += '<div class="pm-section"><strong>Compound family</strong><div>' + escapeHtml(Array.from(compounds).join(', ')) + '</div></div>';
            }
            // fallback: show raw JSON if nothing else
            if (!senses.length && !examples.length && !roots.size && !compounds.size) {
                bodyHtml += '<div class="pm-section"><strong>Raw data</strong><pre style="white-space:pre-wrap;max-height:240px;overflow:auto">' + escapeHtml(JSON.stringify(json,null,2)) + '</pre></div>';
            }
            bodyHtml += '</div></div>';

            content.innerHTML = titleHtml + creditHtml + bodyHtml;
            try{ showPopup(); }catch(e){}
            enhanceModal(content);
        }catch(e){ content.innerHTML = '<div class="pm-section"><strong>Error</strong><div>Unable to render results.</div></div>'; console.error(e); }
    }

    function escapeHtml(s){ return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }

    // Post-process the injected dpd HTML to tweak presentation
    function enhanceModal(contentEl){
        try{
            // Find all .dpd containers (there can be multiple result blocks)
            var dpdCandidates = Array.from(contentEl.querySelectorAll('.dpd'));
            if (!dpdCandidates.length) return;

            // Remove the outer-most dpd border (the wrapper) but preserve inner dpd boxes
            try{
                var outer = contentEl.querySelector('.dpd');
                if (outer) {
                    outer.style.border = 'none';
                    outer.style.boxShadow = 'none';
                    outer.style.padding = '0';
                }
            }catch(e){}

            // Tweak each .dpd container and wire its buttons so every result's buttons work
            dpdCandidates.forEach(function(dpd){
                try{
                    dpd.style.border = dpd.style.border; // noop to keep linter happy
                }catch(e){}

            // Make the header/title use site font, bold and left-aligned
            var title = contentEl.querySelector('.pm-title');
            if (title) {
                title.style.fontFamily = 'inherit';
                title.style.fontWeight = '700';
                title.style.textAlign = 'left';
                title.style.color = '#be564d';
            }

                // Add accordion behavior to dpd buttons: we'll also set a data attr to indicate binding
                var buttons = dpd.querySelectorAll('a.dpd-button');
                buttons.forEach(function(btn){
                    // no-op: no DOM marker needed when using delegated listeners
                });
            });

            // Use event delegation on the content element so all buttons (including later results) respond
            if (!contentEl.__dpdDelegated) {
                contentEl.__dpdDelegated = true;
                contentEl.addEventListener('click', function(ev){
                    var btn = ev.target.closest && ev.target.closest('a.dpd-button');
                    if (!btn) return;
                    try{ ev.preventDefault(); ev.stopPropagation(); }catch(e){}
                    
                    var dpd = btn.closest('.dpd') || contentEl.querySelector('.dpd');
                    var text = (btn.textContent || btn.innerText || '').trim();
                    var dt = (btn.getAttribute('data-target') || btn.getAttribute('data-target-id') || '').trim();
                    if (dt && dt.charAt(0) === '#') dt = dt.slice(1);
                    var sel = (window.CSS && CSS.escape) ? CSS.escape(dt) : dt;
                    var targetEl = (dpd ? dpd.querySelector('#' + sel) : null) || contentEl.querySelector('#' + sel) || document.getElementById(dt);
                    var section = null;
                    if (targetEl) section = targetEl.closest('.pm-section, .tertiary, table, div') || targetEl;
                    if (section && section.closest && section.closest('.dpd') !== dpd) {
                        if (!contentEl.contains(section)) section = null;
                    }
                    if (!section) {
                        var parentBox = btn.closest('.button-box') || btn.parentElement;
                        var next = parentBox ? parentBox.nextElementSibling : null;
                        if (next && next.closest && next.closest('.dpd') === dpd) section = next;
                        if (!section && next && contentEl.contains(next)) section = next;
                    }
                    
                    if (section) {
                        section.classList.toggle('hidden');
                        btn.classList.toggle('active');
                        if (!section.classList.contains('hidden')) { section.setAttribute('tabindex','-1'); section.focus && section.focus(); }
                    }
                });
            }

        }catch(e){ console.error('enhanceModal error', e); }
    }

})();
