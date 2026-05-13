/**
 * xref.js
 *
 * Cross-reference (MU / AT / TK) button logic for the Pāḷi Tipiṭaka reader.
 *
 * Loaded by every script's index.html.  Detects its location automatically
 * so it always fetches xref-index.json from the romn/ folder regardless of
 * which script is currently being viewed.
 */

(function () {
    var xrefIndex  = null;   // raw index, loaded lazily
    var attLookup  = null;   // att_href  → { mul:{href,id}, tik }
    var tikLookup  = null;   // tik_href  → { mul:{href,id}, att }

    // Current targets for each button (null → button hidden)
    var currentMul = null;   // { href, id }
    var currentAtt = null;
    var currentTik = null;

    var mulBtn = document.getElementById('xref-mul-btn');
    var attBtn = document.getElementById('xref-att-btn');
    var tikBtn = document.getElementById('xref-tik-btn');

    // Path to xref-index.json, relative to the current page.
    // All script folders sit alongside romn/, so non-romn pages use ../romn/.
    var xrefIndexUrl = (window.location.pathname.indexOf('/romn/') !== -1)
        ? 'xref-index.json'
        : '../romn/xref-index.json';

    function showBtn(btn, ref) {
        if (btn) btn.style.display = ref ? 'block' : 'none';
    }

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

    /* Called whenever a leaf node is selected in the tree. */
    window.xrefUpdate = function (href) {
        ensureIndex(function () {
            currentMul = null;
            currentAtt = null;
            currentTik = null;

            var type = pageType(href);
            var entry;

            if (type === 'mul') {
                // Mūla page: offer AT and TK
                entry = xrefIndex[href] || null;
                currentAtt = entry ? entry.att : null;
                currentTik = entry ? entry.tik : null;
            } else if (type === 'att') {
                // Aṭṭhakathā page: offer MU and TK
                entry = attLookup[href] || null;
                currentMul = entry ? entry.mul : null;
                currentTik = entry ? entry.tik : null;
            } else if (type === 'tik') {
                // Ṭīkā page: offer MU and AT
                entry = tikLookup[href] || null;
                currentMul = entry ? entry.mul : null;
                currentAtt = entry ? entry.att : null;
            }

            showBtn(mulBtn, currentMul);
            showBtn(attBtn, currentAtt);
            showBtn(tikBtn, currentTik);
        });
    };

    /* Called when a folder node is selected (no content page). */
    window.xrefHide = function () {
        currentMul = currentAtt = currentTik = null;
        showBtn(mulBtn, null);
        showBtn(attBtn, null);
        showBtn(tikBtn, null);
    };

    /* Open the cross-referenced page in a named popup window. */
    window.openXref = function (type) {
        var ref = type === 'mul' ? currentMul
                : type === 'att' ? currentAtt
                : currentTik;
        if (!ref) return;
        var url = 'index.html#' + ref.id;
        window.open(url, 'tipitaka-xref',
            'width=960,height=720,resizable=yes,scrollbars=yes');
    };
})();
