// Pali lookup: double-click selection -> fetch dpdict -> show modal
(function(){
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
    document.body.appendChild(overlay);

    var popup = document.createElement('div');
    popup.id = 'pali-meaning-popup';
    popup.className = 'pali-meaning-popup';
    popup.innerHTML = '<div class="pm-window">'
        + '<div class="pm-header-row">'
        + '<div class="pm-title-holder"></div>'
        + '<div class="pm-spacer"></div>'
        + '<div class="pm-right">'
        + '<div class="pm-credit-holder"></div>'
        + '<button class="pm-close" aria-label="Close">×</button>'
        + '</div>'
        + '</div>'
        + '<div class="pm-content"></div>'
        + '</div>';
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

    function getSelectionText(){
        var s = window.getSelection();
        return s ? s.toString().trim() : '';
    }

    // Word-selection helper: set pointer cursor on content area and expand
    // a double-click selection to the nearest surrounding space-delimited token.
    (function(){
        var root = document.getElementById('t-content');
        if (!root) return;
        try { root.style.cursor = 'pointer'; } catch(e) {}

        function getTextNodes(container){
            var walker = document.createTreeWalker(container, NodeFilter.SHOW_TEXT, null, false);
            var nodes = [];
            var n; while ((n = walker.nextNode())) nodes.push(n);
            return nodes;
        }

        // Search backwards from fromIndex for the nearest whitespace character
        // (space, tab, newline, non-breaking space, etc.) rather than a literal
        // ASCII space, so paragraph/element boundaries (which are often joined
        // only by a newline text node) are correctly treated as word breaks.
        function lastWhitespaceIndex(str, fromIndex){
            for (var i = Math.min(fromIndex, str.length - 1); i >= 0; i--) {
                if (/\s/.test(str.charAt(i))) return i;
            }
            return -1;
        }

        function firstWhitespaceIndex(str, fromIndex){
            for (var i = Math.max(fromIndex, 0); i < str.length; i++) {
                if (/\s/.test(str.charAt(i))) return i;
            }
            return -1;
        }

        function expandSelectionToSpaces(){
            try {
                var sel = window.getSelection();
                if (!sel || !sel.rangeCount) return;
                var anchor = sel.anchorNode;
                if (!anchor || !root.contains(anchor)) return;

                var tnodes = getTextNodes(root);
                if (!tnodes.length) return;

                var total = 0, nodeIndex = -1, offsetInNode = 0;
                for (var i = 0; i < tnodes.length; i++) {
                    if (tnodes[i] === anchor) {
                        nodeIndex = i;
                        offsetInNode = sel.anchorOffset || 0;
                        break;
                    }
                    total += tnodes[i].textContent.length;
                }
                if (nodeIndex === -1) {
                    for (var j = 0; j < tnodes.length; j++) {
                        if (tnodes[j].parentElement && tnodes[j].parentElement.contains(anchor)) { nodeIndex = j; break; }
                    }
                    if (nodeIndex === -1) return;
                    offsetInNode = 0;
                }
                var globalIndex = total + offsetInNode;

                var full = tnodes.map(function(nd){ return nd.textContent; }).join('');
                globalIndex = Math.max(0, Math.min(full.length - 1, globalIndex));

                var leftSpace  = lastWhitespaceIndex(full, globalIndex - 1);
                var startIdx   = (leftSpace  === -1) ? 0           : leftSpace + 1;
                var rightSpace = firstWhitespaceIndex(full, globalIndex);
                var endIdx     = (rightSpace === -1) ? full.length  : rightSpace;

                var sNode = null, sOffset = 0, eNode = null, eOffset = 0, acc = 0;
                for (var k = 0; k < tnodes.length; k++) {
                    var len = tnodes[k].textContent.length;
                    if (!sNode && acc + len > startIdx)  { sNode = tnodes[k]; sOffset = startIdx - acc; }
                    if (!eNode && acc + len >= endIdx)   { eNode = tnodes[k]; eOffset = endIdx   - acc; break; }
                    acc += len;
                }
                if (!sNode) { sNode = tnodes[0]; sOffset = 0; }
                if (!eNode) { eNode = tnodes[tnodes.length - 1]; eOffset = eNode.textContent.length; }

                var r = document.createRange();
                r.setStart(sNode, Math.max(0, sOffset));
                r.setEnd(eNode,   Math.max(0, eOffset));
                sel.removeAllRanges();
                sel.addRange(r);
            } catch(e) { /* fail silently */ }
        }

        root.addEventListener('dblclick', function(){ expandSelectionToSpaces(); }, true);
    })();

    // on double-click, grab (possibly expanded) selection and lookup
    document.addEventListener('dblclick', function(){
        setTimeout(function(){
            var sel = getSelectionText();
            if (!sel) return;
            var word = normalizeWord(sel.split(/\s+/)[0] || sel);
            if (!word) return;
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
        .catch(function(err){ try{ setEditableTitle(word); }catch(e){} content.innerHTML = '<div class="pm-section"><strong>Error</strong><div>Unable to fetch meaning.</div></div>'; try{ showPopup(); }catch(e){} console.error(err); });
    }

    function renderResult(word, json){
        var content = popup.querySelector('.pm-content');
        try{
            // Build a header + body layout inside pm-content so styling matches expected modal
            var bodyHtml = '';
            var creditHtml = '<div class="pm-credit">Courtesy of <a href="https://www.dpdict.net" target="_blank" rel="noopener noreferrer">Digital Pali Dictionary</a></div>';

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
                    // place the title in the header title-holder and credit in the credit-holder
                    try{ setEditableTitle(word); }catch(e){}
                    try{ var holder = popup.querySelector('.pm-credit-holder'); if (holder) holder.innerHTML = creditHtml; }catch(e){}
                    content.innerHTML = bodyHtml;
                    enhanceModal(content);
                    try{ showPopup(); }catch(e){}
                    return;
                }
                // otherwise continue — fallbacks below will handle arrays or show raw JSON
            }

            // fallback for array-style responses
            if (!Array.isArray(json) || !json.length) {
                bodyHtml += '<div class="pm-body"><div class="dpd"><div class="pm-section"><strong>No results</strong><div>No entries found.</div></div></div></div>';
                try{ setEditableTitle(word); }catch(e){}
                try{ var holder2 = popup.querySelector('.pm-credit-holder'); if (holder2) holder2.innerHTML = creditHtml; }catch(e){}
                content.innerHTML = bodyHtml;
                enhanceModal(content);
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
                bodyHtml += '<div class="pm-section"><strong>Raw data</strong><pre class="pm-raw">' + escapeHtml(JSON.stringify(json,null,2)) + '</pre></div>';
            }
            bodyHtml += '</div></div>';

            try{ setEditableTitle(word); }catch(e){}
            try{ var holder3 = popup.querySelector('.pm-credit-holder'); if (holder3) holder3.innerHTML = creditHtml; }catch(e){}
            content.innerHTML = bodyHtml;
            enhanceModal(content);
            try{ showPopup(); }catch(e){}
        }catch(e){ content.innerHTML = '<div class="pm-section"><strong>Error</strong><div>Unable to render results.</div></div>'; console.error(e); }
    }

    function escapeHtml(s){ return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }

    // Render the popup title as an editable text field pre-filled with `word`.
    // Pressing Enter re-triggers a lookup for whatever the user typed, updating
    // the same modal in place.
    function setEditableTitle(word){
        var th = popup.querySelector('.pm-title-holder');
        if (!th) return;
        th.innerHTML = '<input type="text" class="pm-title pm-title-input" value="' + escapeHtml(word) + '" spellcheck="false" autocomplete="off" autocapitalize="off" />';
        var input = th.querySelector('.pm-title-input');
        if (!input) return;
        function resize(){ input.style.width = Math.max(3, input.value.length + 1) + 'ch'; }
        resize();
        input.addEventListener('input', resize);
        input.addEventListener('keydown', function(ev){
            if (ev.key !== 'Enter') return;
            ev.preventDefault();
            var newWord = normalizeWord(input.value.trim());
            if (newWord) lookupPali(newWord);
        });
    }

    // Post-process the injected dpd HTML to tweak presentation
    function enhanceModal(contentEl){
        try{
            // Find all .dpd containers (there can be multiple result blocks)
            var dpdCandidates = Array.from(contentEl.querySelectorAll('.dpd'));
            if (!dpdCandidates.length) return;

            // Remove the outer-most dpd border (the wrapper) but preserve inner dpd boxes
            try{
                // Prefer a .dpd that contains other .dpd elements (wrapper),
                // else prefer a .dpd directly under .pm-body, else fall back to first .dpd
                var outer = dpdCandidates.find(function(d){ return d.querySelector('.dpd'); }) || contentEl.querySelector('.pm-body > .dpd') || contentEl.querySelector('.dpd');
                if (outer) {
                    outer.style.border = 'none';
                    outer.style.boxShadow = 'none';
                    outer.style.padding = '0';
                }
            }catch(e){}

            // Wire delegated button handling for all .dpd result blocks
            dpdCandidates.forEach(function(dpd){ /* iterate to ensure __dpdDelegated is set per-content */ });

            // Use event delegation on the content element so all buttons (including later results) respond
            if (!contentEl.__dpdDelegated) {
                contentEl.__dpdDelegated = true;
                contentEl.addEventListener('click', function(ev){
                    var btn = ev.target.closest && ev.target.closest('a.dpd-button');
                    if (!btn) return;
                    try{ ev.preventDefault(); ev.stopPropagation(); }catch(e){}
                    
                    var dpd = btn.closest('.dpd') || contentEl.querySelector('.dpd');
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

    // Mobile selection handler: debounced selectionchange + long-press fallback
    (function(){
        var lastMobileTrigger = '';
        var selTimer = null;
        var longPressTimer = null;

        function readSelectionAndLookup(){
            try {
                var s = window.getSelection();
                if (!s) return;
                var text = s.toString().trim();
                if (!text) return;
                // if the selection spans multiple words (contains a space) the user is
                // likely selecting a sentence or paragraph — skip the dictionary lookup
                if (text.indexOf(' ') !== -1) return;
                // same behavior as dblclick: use first token
                var word = (text.split(/\s+/)[0] || text);
                word = normalizeWord(word);
                if (!word || word.length < 2) return;
                // restrict to content area if present
                var contentRoot = document.getElementById('t-content');
                if (contentRoot && s.anchorNode && !contentRoot.contains(s.anchorNode)) return;
                if (word === lastMobileTrigger) return;
                lastMobileTrigger = word;
                lookupPali(word);
            } catch (e) { /* silent */ }
        }

        // Debounced selectionchange covers most mobile browsers (fires after selection handles appear)
        document.addEventListener('selectionchange', function(){
            if (selTimer) clearTimeout(selTimer);
            selTimer = setTimeout(readSelectionAndLookup, 350);
        }, false);

        // Long-press fallback: start timer on touchstart, cancel on move/end
        document.addEventListener('touchstart', function(ev){
            if (ev.touches && ev.touches.length > 1) return;
            longPressTimer = setTimeout(function(){
                // allow browser to update selection first
                setTimeout(readSelectionAndLookup, 50);
            }, 600);
        }, false);
        ['touchend','touchmove','touchcancel'].forEach(function(evName){
            document.addEventListener(evName, function(){ if (longPressTimer) { clearTimeout(longPressTimer); longPressTimer = null; } }, false);
        });
    })();

})();
