/**
 * xref.js
 *
 * Cross-reference (MU / AT / TK) button logic for the Pāḷi Tipiṭaka reader.
 *
 * When a leaf page is loaded, small MU / AT / TK buttons are injected next to
 * every subheading (<p rend="subhead">) in the content.  The button matching
 * the current page type is disabled; the other two open the cross-referenced
 * file in a named popup window, scrolled to the same subheading position.
 *
 * Subheading targeting uses the URL query string:
 *   index.html?subhead=2#nodeId
 * The popup reads ?subhead=N and scrolls to the Nth <p rend="subhead"> after
 * its content loads.
 *
 * Loaded by every script's index.html via:
 *   <script defer src="../js/xref.js"></script>
 *
 * Called from the jQuery .load() callback in changed.jstree:
 *   $("#t-content").load(lnk, function(){ xrefUpdate(lnk); });
 */

(function () {
    var xrefIndex = null;   // raw index, loaded lazily
    var attLookup = null;   // att_href  → { mul:{href,id}, tik }
    var tikLookup = null;   // tik_href  → { mul:{href,id}, att }

    // Path to xref-index.json relative to the current page.
    var xrefIndexUrl = (window.location.pathname.indexOf('/romn/') !== -1)
        ? 'xref-index.json'
        : '../romn/xref-index.json';

    // If this page was opened by a cross-reference click, scroll to this subhead index.
    var targetSubheadIndex = (function () {
        var m = /[?&]subhead=(\d+)/.exec(window.location.search);
        return m ? parseInt(m[1], 10) : null;
    })();

    /* Build reverse lookup tables once the index is loaded. */
    function buildReverseLookups() {
        attLookup = {};
        tikLookup = {};
        for (var href in xrefIndex) {
            var e = xrefIndex[href];
            var mulRef = (e.mulId != null) ? { href: href, id: e.mulId } : null;
            if (e.att) attLookup[e.att.href] = { mul: mulRef, tik: e.tik };
            if (e.tik) tikLookup[e.tik.href] = { mul: mulRef, att: e.att };
        }
    }

    /* Load xref-index.json once, then invoke callback. */
    function ensureIndex(cb) {
        if (xrefIndex !== null) { cb(); return; }
        fetch(xrefIndexUrl)
            .then(function (r) { return r.json(); })
            .then(function (data) { xrefIndex = data; buildReverseLookups(); cb(); })
            .catch(function ()   { xrefIndex = {};   buildReverseLookups(); cb(); });
    }

    /* Detect whether href is a Mūla (.mul), Aṭṭhakathā (.att) or Ṭīkā (.tik) page. */
    function pageType(href) {
        if (!href || href === '#') return null;
        if (href.indexOf('.mul') !== -1) return 'mul';
        if (href.indexOf('.att') !== -1) return 'att';
        if (href.indexOf('.tik') !== -1) return 'tik';
        return null;
    }

    /* Remove all previously injected button groups from the content area. */
    function clearButtons() {
        var existing = document.querySelectorAll('#t-content .xref-inline-group');
        for (var i = 0; i < existing.length; i++) {
            existing[i].parentNode.removeChild(existing[i]);
        }
    }

    /* Scroll to the Nth subheading in the content, if a target was requested. */
    function scrollToTargetSubhead(subheads) {
        if (targetSubheadIndex === null) return;
        var idx = targetSubheadIndex;
        targetSubheadIndex = null;  // consume — only scroll once per page open
        var target = subheads[idx] || subheads[subheads.length - 1];  // clamp to last if out of range
        if (target) {
            target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    /* Build a button group span for one subheading. */
    function makeGroup(mulRef, attRef, tikRef, type, subheadIndex) {
        var group = document.createElement('span');
        group.className = 'xref-inline-group';

        var defs = [
            { cls: 'xref-btn xref-mul', label: 'MU', title: 'View M\u016bla (Root text)',          ref: mulRef, disabled: type === 'mul' },
            { cls: 'xref-btn xref-att', label: 'AT', title: 'View A\u1e6d\u1e6dakath\u0101 (Commentary)', ref: attRef, disabled: type === 'att' },
            { cls: 'xref-btn xref-tik', label: 'TK', title: 'View \u1e62\u012bk\u0101 (Sub-commentary)',  ref: tikRef, disabled: type === 'tik' }
        ];

        defs.forEach(function (d) {
            var btn = document.createElement('button');
            btn.className = d.cls;
            btn.textContent = d.label;
            btn.title = d.title;
            btn.disabled = d.disabled || !d.ref;
            (function (ref) {
                btn.addEventListener('click', function () {
                    if (!ref) return;
                    var url = 'index.html?subhead=' + subheadIndex + '#' + ref.id;
                    window.open(url, 'tipitaka-xref',
                        'width=960,height=720,resizable=yes,scrollbars=yes');
                });
            })(d.ref);
            group.appendChild(btn);
        });

        return group;
    }

    /* Inject button groups next to every subheading in the loaded content. */
    function injectButtons(mulRef, attRef, tikRef, type) {
        var content = document.getElementById('t-content');
        if (!content) return;
        var subheads = content.querySelectorAll('p[rend="subhead"]');
        for (var i = 0; i < subheads.length; i++) {
            subheads[i].appendChild(makeGroup(mulRef, attRef, tikRef, type, i));
        }
        scrollToTargetSubhead(subheads);
    }

    /**
     * Called from the .load() callback after leaf content is in the DOM.
     * Looks up cross-references for the given href and injects buttons.
     */
    window.xrefUpdate = function (href) {
        ensureIndex(function () {
            clearButtons();

            var type = pageType(href);
            var mulRef = null, attRef = null, tikRef = null;
            var entry;

            if (type === 'mul') {
                entry = xrefIndex[href] || null;
                attRef = entry ? entry.att : null;
                tikRef = entry ? entry.tik : null;
            } else if (type === 'att') {
                entry = attLookup[href] || null;
                mulRef = entry ? entry.mul : null;
                tikRef = entry ? entry.tik : null;
            } else if (type === 'tik') {
                entry = tikLookup[href] || null;
                mulRef = entry ? entry.mul : null;
                attRef = entry ? entry.att : null;
            }

            injectButtons(mulRef, attRef, tikRef, type);
        });
    };

    /** Called when a folder node is selected — remove any injected buttons. */
    window.xrefHide = function () {
        clearButtons();
    };
})();
