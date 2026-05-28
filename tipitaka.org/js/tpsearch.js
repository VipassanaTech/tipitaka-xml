/**
 * Tipitaka Search Module
 * Provides Solr-based search integrated into script folder pages.
 * Search terms are entered in Roman Pali; results link to current script's XML files.
 *
 * This JS is only loaded on script pages (deva/, romn/, etc.), so the search
 * icon in the top bar will only appear there.
 */
(function ($) {
    'use strict';

    var SOLR_URL = 'https://search.tipitaka.org/solr/web';
    var PAGE_SIZE = 20;
    var currentQuery = '';
    var currentStart = 0;
    var currentFilter = '';       // active volume filter (fq), empty = all
    var displayFilterForSubfacets = ''; // transient filter used to render subfacet UI when checkboxes applied
    var lastFacets = {};          // cached facets from the most recent unfiltered search
    var unfilteredTotal = 0;      // numFound without any filter
    var currentIsDeva = false;    // whether current query is Devanagari
    var currentExpandedFacet = ''; // which top-level facet is currently expanded (not applied)
    // Cached jQuery objects (initialized on document ready)
    var $tContent = null;
    var $tpSearchInput = null;
    var $tpSearchClear = null;
    var __delegationBound = false;

    // Quick check if a string contains Devanagari characters
    function isDevanagari(s) {
        return /[\u0900-\u097F]/.test(s);
    }

    // Insert a Pali character at the cursor position in the search input
    function insertPaliChar(ch) {
        var input = document.getElementById('tp-search-input');
        if (!input) return;
        var start = input.selectionStart;
        var end = input.selectionEnd;
        var val = input.value;
        input.value = val.substring(0, start) + ch + val.substring(end);
        input.selectionStart = input.selectionEnd = start + ch.length;
        input.focus();
    }

    // Build the search bar HTML (hidden by default)
    function buildSearchBar() {
        var paliChars = [
            'ā', 'ī', 'ū', 'ṅ', 'ñ', 'ṭ', 'ḍ', 'ṇ', 'ḷ', 'ṃ'
        ];

        var html = '<div id="tp-search-bar" style="display:none;">';
        // Left column: Limit Search checkboxes
            html += '<div id="tp-limit-search" class="tp-limit-search tp-limit-search-expanded">';
        // list container for the table-based layout
        html += '<div class="tp-limit-list">';
        // Table layout: rows = Mula / Attha. / Tika / Añña / All
        // columns = Label | Vinaya | Sutta | Abhi
        html += '<table class="tp-limit-table">';
        html += '<tbody>';
        // Mula row (add row-level checkbox to toggle the three child checkboxes)
        html += '<tr>';
        html += '<td><label class="limit-item item-mula-all"><input type="checkbox" class="tp-limit-row-checkbox" id="tp-limit-row-mula" /> Mula</label></td>';
            html += '<td><label class="limit-item item-mula-vinaya"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-mula-vinaya" data-val="pitaka:&quot;Vinayapiṭaka&quot;" /> Vinaya</label></td>';
            html += '<td><label class="limit-item item-mula-sutta"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-mula-sutta" data-val="pitaka:&quot;Suttapiṭaka&quot;" /> Sutta</label></td>';
            html += '<td><label class="limit-item item-mula-abhd"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-mula-abhd" data-val="pitaka:&quot;Abhidhammapiṭaka&quot;" /> Abhi</label></td>';
        html += '</tr>';
        // Attha. row (aṭṭhakathā) (add row-level checkbox)
        html += '<tr>';
        html += '<td><label class="limit-item item-atth-all"><input type="checkbox" class="tp-limit-row-checkbox" id="tp-limit-row-atth" /> Attha.</label></td>';
            html += '<td><label class="limit-item item-atth-vinaya"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-atth-vinaya" data-val="volume:&quot;Aṭṭhakathā&quot; AND pitaka:&quot;Vinayapiṭaka (aṭṭhakathā)&quot;" /> Vinaya</label></td>';
            html += '<td><label class="limit-item item-atth-sutta"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-atth-sutta" data-val="volume:&quot;Aṭṭhakathā&quot; AND pitaka:&quot;Suttapiṭaka (aṭṭhakathā)&quot;" /> Sutta</label></td>';
            html += '<td><label class="limit-item item-atth-abhd"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-atth-abhd" data-val="volume:&quot;Aṭṭhakathā&quot; AND pitaka:&quot;Abhidhammapiṭaka (aṭṭhakathā)&quot;" /> Abhi</label></td>';
        html += '</tr>';
        // Tika row (ṭīkā) (add row-level checkbox)
        html += '<tr>';
        html += '<td><label class="limit-item item-tika-all"><input type="checkbox" class="tp-limit-row-checkbox" id="tp-limit-row-tika" /> Tika</label></td>';
            html += '<td><label class="limit-item item-tika-vinaya"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-tika-vinaya" data-val="volume:&quot;Tīkā&quot; AND pitaka:&quot;Vinayapiṭaka (ṭīkā)&quot;" /> Vinaya</label></td>';
            html += '<td><label class="limit-item item-tika-sutta"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-tika-sutta" data-val="volume:&quot;Tīkā&quot; AND pitaka:&quot;Suttapiṭaka (ṭīkā)&quot;" /> Sutta</label></td>';
            html += '<td><label class="limit-item item-tika-abhd"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-tika-abhd" data-val="volume:&quot;Tīkā&quot; AND pitaka:&quot;Abhidhammapiṭaka (ṭīkā)&quot;" /> Abhi</label></td>';
        html += '</tr>';
        // Añña + All row
        html += '<tr>';
        html += '<td><label class="limit-item item-anya"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-anya" data-val="volume:&quot;Anya&quot;" /> Añña</label></td>';
        html += '<td><label class="limit-item item-all"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-all" data-val="ALL" checked /> All</label></td>';
        html += '<td></td>';
        html += '<td></td>';
        html += '</tr>';
        html += '</tbody></table>';
        html += '</div>'; // tp-limit-list
        // Nikaya sub-row: shown below the table when any Sutta checkbox is checked
        html += '<div id="tp-nikaya-row" style="display:none">';
        html += '<span class="tp-nikaya-label">Nikāya:</span>';
        html += '<label class="tp-nikaya-item"><input type="checkbox" class="tp-nikaya-checkbox" data-val="Dīghanikāya" /> D.N.</label>';
        html += '<label class="tp-nikaya-item"><input type="checkbox" class="tp-nikaya-checkbox" data-val="Majjhimanikāya" /> M.N.</label>';
        html += '<label class="tp-nikaya-item"><input type="checkbox" class="tp-nikaya-checkbox" data-val="Saṃyuttanikāya" /> S.N.</label>';
        html += '<label class="tp-nikaya-item"><input type="checkbox" class="tp-nikaya-checkbox" data-val="Aṅguttaranikāya" /> A.N.</label>';
        html += '<label class="tp-nikaya-item"><input type="checkbox" class="tp-nikaya-checkbox" data-val="Khuddakanikāya" /> K.N.</label>';
        html += '</div>';
        html += '</div>'; // tp-limit-search

        html += '  <form id="tp-search-form" onsubmit="return false;">';
        html += '    <div class="tp-search-row">';
        // Note: do NOT include a title attribute here — we use aria-label for screen readers
        // and show the full help popup on hover/focus. The native title tooltip would duplicate it.
        // Help icon removed from the row; it will be rendered next to the Roman button
        html += '      <input type="text" id="tp-search-input" placeholder="Enter Roman Pāḷi or देवनागरी..." autocomplete="off" />';
        html += '      <button type="submit" id="tp-search-btn" title="Search" aria-label="Search"><i class="fa fa-search" aria-hidden="true"></i></button>';
        html += '      <button type="button" id="tp-search-clear" title="Clear" aria-label="Clear" style="display:none;"><i class="fa fa-times" aria-hidden="true"></i></button>';
        html += '    </div>';
        // Roman Pali character row (moved below mode buttons)

        // Input mode switch + Devanagari palette
        var devaRow1 = ['अ','आ','इ','ई','उ','ऊ','ए','ओ','क','ख','ग','घ','ङ'];
        var devaRow2 = ['च','छ','ज','झ','ञ','ट','ठ','ड','ढ','ण','त','थ','द'];
        var devaRow3 = ['ध','न','प','फ','भ','म','य','र','ल','व','स','ह','ळ','अं'];
        var devaRow4 = ['ं','ा','ि','ी','ु','ू','े','ो','्'];
        html += '    <div class="tp-deva-controls">';
        html += '      <div class="tp-mode-switch">';
        html += '        <span id="tp-help-btn" class="tp-help-icon" aria-label="Help"><i class="fa fa-info-circle" aria-hidden="true"></i></span>';
        html += '        <button type="button" id="tp-mode-roman" class="tp-mode-btn" aria-pressed="false">Roman</button>';
        html += '        <button type="button" id="tp-mode-deva" class="tp-mode-btn" aria-pressed="false">देव</button>';
        html += '      </div>';
        // Exact match checkbox placed to the right of the Devanagari button
        html += '      <label class="tp-exact-label" style="margin-left:8px; font-size:13px; color:#1E3461;">';
        html += '        <input type="checkbox" id="tp-exact-match" /> Exact Match';
        html += '      </label>';
        // Proximity mode radio buttons: As-is (ordered) or Any order (unordered)
        html += '      <div class="tp-prox-mode" style="display:none;">';
        html += '        <span style="margin-right:6px;">Proximity:</span>';
        html += '        <label style="margin-right:6px;"><input type="radio" name="tp-prox-mode" id="tp-prox-strict" value="strict" checked /> As-is</label>';
        html += '        <label style="margin-right:6px;"><input type="radio" name="tp-prox-mode" id="tp-prox-any" value="any" /> Any</label>';
        html += '      </div>';
        // Filter button (placed to the right of proximity controls)
        html += '      <button type="button" id="tp-filter-btn" class="tp-filter-btn" aria-label="Filter">Filter</button>';
        html += '      <div class="tp-search-type-group">';
        html += '        <span class="tp-search-type-label">Search Type</span>';
        html += '        <label class="tp-search-type-opt"><input type="radio" name="tp-search-type" value="fuzzy" checked /> Fuzzy</label>';
        html += '        <label class="tp-search-type-opt"><input type="radio" name="tp-search-type" value="exact" /> Exact</label>';
        html += '      </div>';
        // Inline proximity syntax supported: use `termA /N termB` in the main input
        html += '    </div>';

        // Insert Roman Pali character row below the mode buttons (hidden/shown by mode)
        html += '    <div class="tp-pali-chars">';
        for (var i = 0; i < paliChars.length; i++) {
            html += '<button type="button" class="tp-pali-btn" data-char="' + paliChars[i] + '">' + paliChars[i] + '</button>';
        }
        html += '    </div>';

        html += '    <div id="tp-deva-palette" class="tp-deva-palette">';
        html += '<div class="tp-deva-row">';
        for (var d = 0; d < devaRow1.length; d++) {
            html += '<button type="button" class="tp-deva-btn" data-char="' + devaRow1[d] + '">' + devaRow1[d] + '</button>';
        }
        html += '</div>';
        html += '<div class="tp-deva-row">';
        for (var d = 0; d < devaRow2.length; d++) {
            html += '<button type="button" class="tp-deva-btn" data-char="' + devaRow2[d] + '">' + devaRow2[d] + '</button>';
        }
        html += '</div>';
        html += '<div class="tp-deva-row">';
        for (var d = 0; d < devaRow3.length; d++) {
            html += '<button type="button" class="tp-deva-btn" data-char="' + devaRow3[d] + '">' + devaRow3[d] + '</button>';
        }
        html += '</div>';
        html += '<div class="tp-deva-row">';
        for (var d = 0; d < devaRow4.length; d++) {
            html += '<button type="button" class="tp-deva-btn" data-char="' + devaRow4[d] + '">' + devaRow4[d] + '</button>';
        }
        html += '</div>';
        html += '    </div>';
        // Help popup (hidden by default) appended inside search bar container
        html += '<div id="tp-help-popup" class="tp-help-popup" style="display:none;">';
        html += '<div class="tp-help-content">';
        html += '<div class="tp-help-title">How to use Search</div>';
        html += '<ol class="tp-help-list">';
        html += '<li>Typing in the proper Pāḷi characters is not necessary. Searching for <b>vipassanā</b> or <b>vipassana</b> will produce the same results.</li>';
        html += '<li>Wildcard search is provided by the use of * in the search term. For example, searching for <b>dhammacakka*</b> will find all instances that start with dhammacakka.</li>';
        html += '<li>For proximity search, place /n between the two terms to search, e.g. <b>metta /5 mudita</b> will find instances where metta and mudita are within 5 words of each other.</li>';
        html += '</ol>';
        html += '</div>';
        html += '</div>';
        html += '  </form>';
        // Right placeholder section (empty) to reserve space in the 3-column layout
        html += '<div id="tp-search-right" class="tp-search-right"></div>';
        html += '</div>';
        return html;
    }

    // State: whether the search icon has been activated at least once
    var _tpsearchActivated = false;

    // Open the search bar and mark the header icon active
    function openSearchBar() {
        var $bar = $('#tp-search-bar');
        var $collapse = $('#tp-search-collapse-btn');
        var $expand = $('#tp-search-expand-btn');
        var $icon = $('#tp-topbar-search-icon');
        if ($expand && $expand.length) $expand.hide();
        $bar.stop(true,true).slideDown(200, function() {
            $('#tp-search-input').focus();
            if ($collapse && $collapse.length) $collapse.show();
        });
        if ($icon && $icon.length) $icon.addClass('tp-topbar-search-active');
        _tpsearchActivated = true;
    }

    // Minimize the search bar but keep the header icon active (search session still active)
    function minimizeSearchBar() {
        var $bar = $('#tp-search-bar');
        var $collapse = $('#tp-search-collapse-btn');
        var $expand = $('#tp-search-expand-btn');
        $bar.stop(true,true).slideUp(200, function() {
            if ($collapse && $collapse.length) $collapse.hide();
            // only show the expand handle if the header icon is in selected state
            if ($expand && $expand.length) {
                if ($('#tp-topbar-search-icon').hasClass('tp-topbar-search-active')) $expand.show();
                else $expand.hide();
            }
        });
        // keep header icon active if the user had opened search earlier
        if (_tpsearchActivated) {
            $('#tp-topbar-search-icon').addClass('tp-topbar-search-active');
        }
    }

    // (toggleSearchBar removed - unused; handlers call open/minimize directly)

    // Inject a search icon into the top navigation bar
    function injectTopBarSearchIcon() {
        // Wait until topnav is loaded (it is fetched async by topnavscript.js)
        var attempts = 0;
        var timer = setInterval(function () {
            var $nav = $('#myTopnav');
            if ($nav.children().length > 0) {
                // Remove any duplicate icons
                $('#tp-topbar-search-icon, #tp-topbar-search-icon-mobile').remove();
                // Find Home link and Scripts dropdown
                var $links = $nav.find('a');
                var $home = $links.filter(function () { return $(this).text().trim() === 'Home'; }).first();
                var $dropdowns = $nav.find('.dropdown');
                var $scriptsDropdown = $dropdowns.first();
                var iconHtml = document.createElement('a');
                iconHtml.href = 'javascript:void(0)';
                iconHtml.id = 'tp-topbar-search-icon';
                iconHtml.title = 'Search Tipiṭaka';
                iconHtml.innerHTML = '<i class="fa fa-search"></i>';
                // Prefer placing the search icon immediately next to the GitHub Repo link
                var $github = $links.filter(function () { return $(this).text().trim() === 'GitHub Repo'; }).first();
                if ($github.length) {
                    // Insert after the GitHub link so the icon appears next to it
                    $github[0].parentNode.insertBefore(iconHtml, $github[0].nextSibling);
                } else if ($home.length && $scriptsDropdown.length) {
                    $nav[0].insertBefore(iconHtml, $scriptsDropdown[0]);
                } else if ($home.length) {
                    $home[0].parentNode.insertBefore(iconHtml, $home[0].nextSibling);
                } else {
                    $nav[0].insertBefore(iconHtml, $nav[0].firstChild);
                }
                // Inject bookmark icon immediately after the search icon
                $('#tp-topbar-bookmark-icon').remove();
                var bmIcon = document.createElement('a');
                bmIcon.href = 'javascript:void(0)';
                bmIcon.id = 'tp-topbar-bookmark-icon';
                bmIcon.title = 'Add or remove bookmarks';
                bmIcon.innerHTML = '<i class="fa fa-bookmark-o"></i>';
                iconHtml.parentNode.insertBefore(bmIcon, iconHtml);
                // Ensure the search icon is visible in the topnav (including mobile responsive mode)
                $(iconHtml).show();
                updateBookmarkIcon();
                // For mobile: also insert after Home in collapsed menu if menu is open
                var $hamburger = $nav.find('a.icon');
                if ($hamburger.length && $nav.hasClass('responsive')) {
                    var $mobileLinks = $nav.find('a');
                    var $mobileHome = $mobileLinks.filter(function () { return $(this).text().trim() === 'Home'; }).first();
                    if ($mobileHome.length) {
                        var mobileIcon = document.createElement('a');
                        mobileIcon.href = 'javascript:void(0)';
                        mobileIcon.id = 'tp-topbar-search-icon-mobile';
                        mobileIcon.title = 'Search Tipiṭaka';
                        mobileIcon.innerHTML = '<i class="fa fa-search"></i>';
                        mobileIcon.onclick = function(e) {
                            e.preventDefault();
                            $('#tp-search-bar').toggle();
                            $('#tp-search-input').focus();
                        };
                        // Try to insert after mobile GitHub link if present, otherwise after Home
                        var $mobileGit = $mobileLinks.filter(function () { return $(this).text().trim() === 'GitHub Repo'; }).first();
                        if ($mobileGit.length) {
                            $mobileGit[0].parentNode.insertBefore(mobileIcon, $mobileGit[0].nextSibling);
                        } else {
                            $mobileHome[0].parentNode.insertBefore(mobileIcon, $mobileHome[0].nextSibling);
                        }
                    }
                }
                clearInterval(timer);
            }
            if (++attempts > 50) clearInterval(timer); // give up after 5 s
        }, 100);
    }

    // Execute search against Solr
    function doSearch(query, start, filterVolume) {
        if (!query || !query.trim()) return;
        query = query.trim();
        try {
            var _mode = sessionStorage.getItem('tpsearch-mode');
            if (_mode === 'deva') currentIsDeva = true;
            else currentIsDeva = isDevanagari(query);
        } catch (e) {
            currentIsDeva = isDevanagari(query);
        }
        currentQuery = query;
        currentStart = start || 0;
        currentFilter = (filterVolume !== undefined) ? filterVolume : currentFilter;
        // Applying a real filter clears any previously-expanded (but not applied) facet
        if (filterVolume) currentExpandedFacet = '';

        var $content = $tContent || $('#t-content');
        $content.html('<div class="tp-search-loading"><i class="fa fa-spinner fa-spin"></i> Searching…</div>');
        $('#tp-search-clear').show();

        // Make sure the search bar is visible when a search is triggered
        if (!$('#tp-search-bar').is(':visible')) {
            $('#tp-search-bar').slideDown(200);
            $('#tp-topbar-search-icon').addClass('tp-topbar-search-active');
        }

        // If checkboxes are used, determine an effective filter to drive
        // which facet fields/pivot to request (we prefer the pitaka clause
        // when present so the UI can render sub-collection and category pills).
        var limitFq = null;
        try { limitFq = buildLimitFq(); } catch (e) { limitFq = null; }
        var effectiveFilter = currentFilter;
        if (limitFq) {
            var candidatePitaka = null;
            var searchList = [];
            if (Array.isArray(limitFq)) searchList = limitFq;
            else if (limitFq.pitakaClauses) searchList = limitFq.pitakaClauses;
            else if (limitFq.clauses) searchList = limitFq.clauses;
            for (var ci = 0; ci < searchList.length; ci++) {
                var m = String(searchList[ci]).match(/^pitaka\s*:\s*"([^"]+)"$/i);
                if (m) { candidatePitaka = 'pitaka:"' + m[1] + '"'; break; }
            }
            if (candidatePitaka) {
                effectiveFilter = candidatePitaka;
            }
        }

        // Determine which facet fields and pivot key to request based on the
        // effective filter so we retrieve the next hierarchical level.
        var facetFields = ['volume'];
        var pivotKey = 'volume,pitaka';
        if (effectiveFilter) {
            var _pf = parseFilter(effectiveFilter);
            if (_pf.field === 'volume') {
                facetFields.push('pitaka');
                pivotKey = 'volume,pitaka';
            } else if (_pf.field === 'pitaka') {
                facetFields.push('book');
                pivotKey = 'pitaka,book';
            } else if (_pf.field === 'book') {
                facetFields.push('chapter');
                pivotKey = 'book,chapter';
            } else {
                facetFields.push('pitaka');
                pivotKey = 'volume,pitaka';
            }
        }
        var exactMatch = false;
        try { exactMatch = !!$('#tp-exact-match').prop('checked'); } catch(e) { exactMatch = false; }

        // Detect inline proximity syntax: `termA /N termB` (distance = N)
        var qparam = query;
        try {
            var proxRe = /^\s*([^\/]+?)\s*\/\s*(\d+)\s+([^\/]+?)\s*$/;
            var proxMatch = query.match(proxRe);
            if (proxMatch) {
                    var termA = proxMatch[1].trim();
                    var dist = parseInt(proxMatch[2], 10) || 0;
                    var termB = proxMatch[3].trim();
                    var escq = function(s) { return s.replace(/"/g, '\\"'); };
                    // Determine proximity mode: 'strict' = As-is, 'any' = unordered
                    var proxMode = 'strict';
                    try {
                        proxMode = (document.querySelector('input[name="tp-prox-mode"]:checked') || {}).value || 'strict';
                    } catch (e) { proxMode = 'strict'; }
                    if (proxMode === 'strict') {
                        // where N is the distance and W indicates word-based proximity
                        var q1 = 'text:"' + escq(termA) + ' ' + escq(termB) + '"~' + dist;
                        qparam = '(' + q1 + ')';
                    } else {
                        // Unordered: build an OR of both ordered phrase queries using phrase slop
                        var q1 = 'text:"' + escq(termA) + ' ' + escq(termB) + '"~' + dist;
                        var q2 = 'text:"' + escq(termB) + ' ' + escq(termA) + '"~' + dist;
                        qparam = '(' + q1 + ' OR ' + q2 + ')';
                    }
            } else {
                // Check Search Type radio: exact uses text_exact phrase query
                var searchType = 'fuzzy';
                try {
                    searchType = (document.querySelector('input[name="tp-search-type"]:checked') || {}).value || 'fuzzy';
                } catch (e) { searchType = 'fuzzy'; }
                if (searchType === 'exact') {
                    var escq2 = query.replace(/"/g, '\\"');
                    qparam = 'text_exact:"' + escq2 + '"';
                }
            }
        } catch (e) {
            // fallback to simple query
            qparam = query;
        }

        var params = {
            q: qparam,
            wt: 'json',
            start: currentStart,
            rows: PAGE_SIZE,
            hl: 'on',
            'hl.fl': 'text',
            'hl.simple.pre': '<em>',
            'hl.simple.post': '</em>',
            facet: 'on',
            'facet.field': facetFields,
            'facet.pivot': pivotKey
        };

        // Apply limit search filters (if any). buildLimitFq() may return either
        // an array of clauses or an object with details. Use the returned
        // information both to add fq params and to set a transient display
        // filter used for subfacet rendering.
        try {
            if (limitFq) {
                var lclauses = [];
                if (Array.isArray(limitFq)) lclauses = limitFq;
                else if (typeof limitFq === 'string') lclauses = [limitFq];
                else lclauses = (limitFq.clauses || []);
                params.fq = (params.fq || []).concat(lclauses);
                // Compute a displayFilter (prefer pitaka if present)
                var pfCand = null;
                if (!Array.isArray(limitFq) && limitFq.pitakaClauses && limitFq.pitakaClauses.length) {
                    pfCand = limitFq.pitakaClauses[0];
                } else {
                    for (var xi = 0; xi < lclauses.length; xi++) {
                        if (/^pitaka\s*:\s*/i.test(lclauses[xi])) { pfCand = lclauses[xi]; break; }
                    }
                }
                var rawDisplay = pfCand || (lclauses.length ? lclauses[0] : '');
                // Normalize: remove wrapping quotes from values like pitaka:"Name"
                var mdisp = String(rawDisplay).match(/^(\w+)\s*:\s*"?(.+?)"?$/);
                if (mdisp) displayFilterForSubfacets = mdisp[1] + ':' + mdisp[2];
                else displayFilterForSubfacets = rawDisplay;
            } else {
                displayFilterForSubfacets = '';
            }
        } catch (e) { displayFilterForSubfacets = ''; }
            // If any limitFq was applied (checkboxes), prefer book-level facets
            // so we can show counts per book under the selected pitaka(s).
            try {
                if (limitFq) {
                    facetFields = facetFields.indexOf('book') === -1 ? facetFields.concat(['book']) : facetFields;
                    pivotKey = 'pitaka,book';
                    params['facet.field'] = facetFields;
                    params['facet.pivot'] = pivotKey;
                }
            } catch (e) { }

        // Apply nikaya filter: replace any limit fq with context-aware pitaka + book clauses
        try {
            var nikayaResult = buildNikayaFq();
            if (nikayaResult) {
                params.fq = [nikayaResult.pitakaFq, nikayaResult.bookFq];
                params['facet.field'] = ['chapter'];
                params['facet.pivot'] = 'book,chapter';
            }
        } catch (e) { }

        // Apply volume or field-prefixed filter if set — add as another fq entry
        if (currentFilter) {
            var pf = parseFilter(currentFilter);
            if (pf.value) {
                var pfExpr = pf.field + ':"' + pf.value + '"';
                params.fq = (params.fq || []).concat([pfExpr]);
            }
        }

        // Debug: show what facet fields / pivot and final params are being sent
        if (window.console && window.console.debug) {
            console.debug('tpsearch: doSearch params', { facetFields: facetFields, pivotKey: pivotKey, params: params });
        }

        $.ajax({
            url: SOLR_URL,
            data: params,
            // Send arrays as repeated parameters (facet.field=volume&facet.field=pitaka)
            traditional: true,
            dataType: 'jsonp',
            jsonp: 'json.wrf',
            timeout: 15000,
            success: function (data) {
                // When doing an unfiltered search, cache the facets
                if (!currentFilter) {
                    lastFacets = parseFacets(data);
                    unfilteredTotal = (data.response || {}).numFound || 0;
                }
                renderResults(data);
                // Always fetch pivot counts when a volume filter is active so the
                // Pitaka (subfacet) counts reflect the entire filtered result set.
                if (currentFilter) {
                    fetchPivotCounts(currentFilter);
                }
                // If we have a transient display filter derived from checkboxes
                // (and it's different from currentFilter), fetch pivot counts
                // for it so the category pills can be shown.
                try {
                    if (displayFilterForSubfacets && displayFilterForSubfacets !== currentFilter) {
                        fetchPivotCounts(displayFilterForSubfacets);
                    }
                } catch (e) { }
            },
            error: function () {
                $content.html(
                    '<div class="tp-search-error">' +
                    '<i class="fa fa-exclamation-triangle"></i> ' +
                    'Search failed. Please try again or use the ' +
                    '<a href="https://search.tipitaka.org/solr/web" target="_blank">online search</a>.' +
                    '</div>'
                );
            }
        });
    }

    // Parse facet counts from Solr response
    function parseFacets(data) {
        var facets = {};
        if (data.facet_counts && data.facet_counts.facet_fields && data.facet_counts.facet_fields.volume) {
            var fv = data.facet_counts.facet_fields.volume;
            for (var i = 0; i < fv.length; i += 2) {
                facets[fv[i]] = fv[i + 1];
            }
        }
        return facets;
    }

    // Build fq values for nikaya search.
    // Returns { pitakaFq, bookFq } when nikayas are selected, or null if none selected.
    // pitakaFq: OR of pitaka clauses for the checked Sutta rows
    // bookFq:   OR of book:"<nikaya><suffix>" for each checked nikaya × Sutta-row suffix
    function buildNikayaFq() {
        if (!window.jQuery) return null;
        var $ = window.jQuery;
        var nikayas = [];
        $('.tp-nikaya-checkbox:checked').each(function () {
            var v = $(this).data('val');
            if (v) nikayas.push(v);
        });
        if (!nikayas.length) return null;

        // Determine which Sutta rows are active and their pitaka/suffix values
        var suttaRows = [];
        if ($('#tp-limit-mula-sutta').is(':checked'))
            suttaRows.push({ pitaka: 'Suttapiṭaka',               suffix: '' });
        if ($('#tp-limit-atth-sutta').is(':checked'))
            suttaRows.push({ pitaka: 'Suttapiṭaka (aṭṭhakathā)',  suffix: ' (aṭṭhakathā)' });
        if ($('#tp-limit-tika-sutta').is(':checked'))
            suttaRows.push({ pitaka: 'Suttapiṭaka (ṭīkā)',        suffix: ' (ṭīkā)' });
        if (!suttaRows.length) return null;

        // Build pitaka fq
        var pitakaClauses = suttaRows.map(function (r) { return 'pitaka:"' + r.pitaka + '"'; });
        var pitakaFq = pitakaClauses.length === 1 ? pitakaClauses[0] : '(' + pitakaClauses.join(' OR ') + ')';

        // Build book fq: each nikaya × each sutta suffix
        var bookClauses = [];
        for (var i = 0; i < nikayas.length; i++) {
            for (var j = 0; j < suttaRows.length; j++) {
                bookClauses.push('book:"' + nikayas[i] + suttaRows[j].suffix + '"');
            }
        }
        var bookFq = bookClauses.length === 1 ? bookClauses[0] : '(' + bookClauses.join(' OR ') + ')';

        return { pitakaFq: pitakaFq, bookFq: bookFq };
    }

    // Build fq string for Limit Search checkboxes; return null if no limiting
    function buildLimitFq() {
        if (!window.jQuery) return null;
        var $ = window.jQuery;
        var vals = [];
        $('.tp-limit-checkbox:checked').each(function () {
            var v = $(this).data('val');
            if (v && v !== 'ALL') vals.push(v);
        });
        // If none selected or all selected, do not limit
        if (!vals.length) return null;
        var totalBoxes = $('.tp-limit-checkbox').length;
        var checkedBoxes = $('.tp-limit-checkbox:checked').length;
        if (checkedBoxes === totalBoxes) return null;
        // The checkbox `data-val` now contains a ready-to-use filter expression
        // (for example: volume:"Tipiṭaka (Mūla)" AND pitaka:"Suttapitaka").
        // Wrap each selected expression in parens and join with OR.
        // Build an array of individual fq clauses. Each checkbox `data-val`
        // may itself contain multiple clauses joined by ' AND ' (e.g.
        // volume:"..." AND pitaka:"..."). Split those into separate
        // clauses so we can send them as repeated `fq` params to Solr.
        var clauses = [];
        var pitakaClauses = [];
        for (var i = 0; i < vals.length; i++) {
            var raw = String(vals[i]);
            var parts = (raw.indexOf(' AND ') > -1) ? raw.split(' AND ') : [raw];
            for (var si = 0; si < parts.length; si++) {
                var s = parts[si].trim();
                if (!s) continue;
                clauses.push(s);
                // detect pitaka: clauses specifically
                if (/^pitaka\s*:\s*/i.test(s)) {
                    pitakaClauses.push(s);
                }
            }
        }

        // If multiple checkboxes were selected, build a single OR expression
        // joining the original checkbox expressions so Solr receives one fq
        // that represents (expr1) OR (expr2) ... This avoids multiple fq
        // params being ANDed together by Solr.
        if (vals.length > 1) {
            var orExprParts = [];
            for (var vi = 0; vi < vals.length; vi++) {
                var rv = String(vals[vi]).trim();
                if (!rv) continue;
                orExprParts.push('(' + rv + ')');
            }
            if (orExprParts.length) {
                var grouped = orExprParts.join(' OR ');
                if (window.console && window.console.debug) console.debug('tpsearch: buildLimitFq -> grouped OR', { selectedVals: vals, grouped: grouped });
                return grouped; // return as a single fq string
            }
        }
        if (!clauses.length) return null;

        // If exactly one checkbox is checked and that checkbox contains a
        // pitaka clause, we usually want to request pitaka-level pivot counts.
        // However, for entries that explicitly include a non-Mūla `volume`
        // (for example Aṭṭhakathā or Tīkā) the fq should include both the
        // volume and pitaka clauses. Detect the presence of a volume clause
        // and only treat the selection as "pitaka-only" when the volume is
        // either absent or represents the Mūla collection.
        if (checkedBoxes === 1 && pitakaClauses.length > 0) {
            var volumeVal = null;
            for (var ci = 0; ci < clauses.length; ci++) {
                var m = clauses[ci].match(/^volume\s*:\s*"([^"]+)"$/i);
                if (m) { volumeVal = m[1]; break; }
            }
            var isMula = false;
            try { isMula = !!(volumeVal && volumeVal.indexOf('Mūla') !== -1); } catch (e) { isMula = false; }
            if (isMula || !volumeVal) {
                if (window.console && window.console.debug) {
                    console.debug('tpsearch: buildLimitFq -> pitakaOnly', { selectedVals: vals, clauses: clauses, pitakaClauses: pitakaClauses, volumeVal: volumeVal });
                }
                return { clauses: clauses, pitakaOnly: true, pitakaClauses: pitakaClauses };
            } else {
                // return the full clauses so the caller will send both volume
                // and pitaka fq entries (e.g. Aṭṭhakathā / Suttapiṭaka (aṭṭhakathā)).
                if (window.console && window.console.debug) {
                    console.debug('tpsearch: buildLimitFq -> single non-mula checkbox', { selectedVals: vals, clauses: clauses, volumeVal: volumeVal });
                }
                return { clauses: clauses };
            }
        }

        if (window.console && window.console.debug) {
            console.debug('tpsearch: buildLimitFq ->', { selectedVals: vals, clauses: clauses });
        }
        return clauses;
    }

    // Fetch pivot-only counts when the main (paged) response doesn't include pivot data.
    // This performs a lightweight rows=0 request with the same filter so we can render
    // accurate subfacet counts for the UI.
    function fetchPivotCounts(filterStr) {
        var pf = parseFilter(filterStr || currentFilter);
        var isGroupedOr = false;
        try {
            var sraw = String(filterStr || '');
            if (/\sOR\s/i.test(sraw) || /^\(/.test(sraw)) isGroupedOr = true;
        } catch (e) { isGroupedOr = false; }
        // Choose facet fields / pivot pair that correspond to the next-level
        // children for the provided parent filter.
        var facetFields = ['volume'];
        var pivotKey = 'volume,pitaka';
        if (isGroupedOr) {
            // For grouped OR filters (multiple checkbox selection), request
            // pitaka->book pivot so we can aggregate books across selected
            // pitakas.
            facetFields = ['volume', 'book'];
            pivotKey = 'pitaka,book';
        } else if (pf && pf.field) {
            if (pf.field === 'volume') {
                facetFields.push('pitaka');
                pivotKey = 'volume,pitaka';
            } else if (pf.field === 'pitaka') {
                facetFields.push('book');
                pivotKey = 'pitaka,book';
            } else if (pf.field === 'book') {
                facetFields.push('chapter');
                pivotKey = 'book,chapter';
            } else {
                facetFields.push('pitaka');
                pivotKey = 'volume,pitaka';
            }
        } else {
            facetFields.push('pitaka');
        }

        var params = {
            q: currentQuery || '*:*',
            wt: 'json',
            start: 0,
            rows: 0,
            facet: 'on',
            'facet.field': facetFields,
            'facet.pivot': pivotKey
        };
        if (isGroupedOr) {
            params.fq = filterStr;
        } else if (pf && pf.value) {
            params.fq = pf.field + ':"' + pf.value + '"';
        }

        // Debug: show what pivot request is being sent for the current filter
        if (window.console && window.console.debug) {
            console.debug('tpsearch: fetchPivotCounts params', { facetFields: facetFields, pivotKey: pivotKey, params: params });
        }

        $.ajax({
            url: SOLR_URL,
            data: params,
            traditional: true,
            dataType: 'jsonp',
            jsonp: 'json.wrf',
            timeout: 10000,
            success: function (pdata) {
                try {
                    // Use the pivotKey and child field determined earlier
                    var childField = (pf && pf.field === 'pitaka') ? 'book' : (pf && pf.field === 'book') ? 'chapter' : 'pitaka';
                    var pivotData = null;
                    if (pdata.facet_counts && pdata.facet_counts.facet_pivot) {
                        pivotData = pdata.facet_counts.facet_pivot[pivotKey] || pdata.facet_counts.facet_pivot[pivotKey.replace(',', ', ')];
                    }

                    var children = [];
                    if (Array.isArray(pivotData) && pivotData.length) {
                        if (isGroupedOr) {
                            // Aggregate book counts across all returned pitaka parents
                            var agg = {};
                            for (var pi = 0; pi < pivotData.length; pi++) {
                                var p = pivotData[pi];
                                var sub = p.pivot || p['pivot'] || [];
                                for (var si = 0; si < sub.length; si++) {
                                    var s = sub[si];
                                    agg[s.value] = (agg[s.value] || 0) + (s.count || 0);
                                }
                            }
                            Object.keys(agg).sort().forEach(function(k) { children.push({ key: k, count: agg[k], field: childField }); });
                        } else {
                            var parentVal = pf.value || '';
                            for (var pi2 = 0; pi2 < pivotData.length; pi2++) {
                                var p2 = pivotData[pi2];
                                if ((p2.value + '') === (parentVal + '')) {
                                    var sub2 = p2.pivot || p2['pivot'] || [];
                                    for (var si2 = 0; si2 < sub2.length; si2++) {
                                        var s2 = sub2[si2];
                                        children.push({ key: s2.value, count: s2.count, field: childField });
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    // Fallback to facet_fields.<childField> if pivot not available
                    if (children.length === 0) {
                        var ffields = (pdata.facet_counts && pdata.facet_counts.facet_fields) || {};
                        var arr = ffields[childField] || null;
                        if (Array.isArray(arr)) {
                            for (var pi2 = 0; pi2 < arr.length; pi2 += 2) {
                                var k2 = arr[pi2];
                                var v2 = arr[pi2 + 1] || 0;
                                if (v2 > 0) children.push({ key: k2, count: v2, field: childField });
                            }
                        }
                    }

                    if (children.length) {
                        var pf = parseFilter(filterStr || currentFilter);
                        if (pf.field === 'chapter') {
                            $('#tp-sub-facets-container').empty();
                            currentExpandedFacet = '';
                        } else {
                            renderSubfacetsHtml(filterStr, children);
                        }
                    }
                } catch (e) {
                    // silently ignore pivot parse errors; UI will continue to show per-page counts
                    console.warn('fetchPivotCounts: failed to parse pivot response', e);
                }
            },
            error: function () {
                // ignore pivot fetch failures; per-page counts remain usable
            }
        });
    }

    // Render search results into #t-content
    function renderResults(data) {
        var $content = $tContent || $('#t-content');
        var resp = data.response || {};
        var docs = resp.docs || [];
        var numFound = resp.numFound || 0;
        var highlighting = data.highlighting || {};

        // Use cached unfiltered facets for the top-level pill display
        var displayFacets = lastFacets || parseFacets(data);
        if (!currentFilter) {
            // Cache unfiltered facets and counts when no filter is active
            lastFacets = parseFacets(data);
            unfilteredTotal = numFound;
        }

        if (numFound === 0) {
            var noResultHtml = '<div class="tp-search-results">';
            noResultHtml += '<h3 class="tp-results-header">No results found for "' + escapeHtml(currentQuery) + '"';
            if (currentFilter) {
                var _pf = parseFilter(currentFilter);
                var filterLabelMap = { 'volume': 'Collection', 'pitaka': 'Sub-collection', 'book': 'Category', 'chapter': 'Sub-category' };
                var fLabel = filterLabelMap[_pf.field] || _pf.field || 'Filter';
                var fVal = _pf.value || currentFilter;
                noResultHtml += ' in ' + escapeHtml(fLabel) + ': ' + escapeHtml(fVal);
            }
            noResultHtml += '</h3>';
            noResultHtml += buildFacetsHtml(displayFacets);
            noResultHtml += '<p>Try different search terms or check your spelling. ' +
                'Use the Pāḷi character buttons above for special characters.</p>';
            noResultHtml += '</div>';
            $content.html(noResultHtml);
            bindFacetClicks($content);
            return;
        }

        var startNum = currentStart + 1;
        var endNum = Math.min(currentStart + PAGE_SIZE, numFound);

        var html = '<div class="tp-search-results">';

        // Header
        html += '<h3 class="tp-results-header">Results for "' + escapeHtml(currentQuery) + '"';
        if (currentFilter) {
            var _pfh = parseFilter(currentFilter);
            var headerLabelMap = { 'volume': 'Collection', 'pitaka': 'Sub-collection', 'book': 'Category', 'chapter': 'Sub-category' };
            var hLabel = headerLabelMap[_pfh.field] || _pfh.field || 'Filter';
            var hVal = _pfh.value || currentFilter;
            html += ' <span class="tp-filter-label">in ' + escapeHtml(hLabel) + ': ' + escapeHtml(hVal) + '</span>';
        }
        html += '</h3>';
        html += '<div class="tp-results-summary">';
        html += 'Showing ' + startNum + '\u2013' + endNum + ' of ' + numFound.toLocaleString() + ' results';
        html += '</div>';

        // Top-level Facets (always show using cached/unfiltered facet counts)
        html += buildFacetsHtml(displayFacets);

        // Placeholder for sub-facets. If a filter is active, we'll populate this
        // using the facet_fields returned in this filtered response (e.g. pitaka).
        html += '<div id="tp-sub-facets-container"></div>';

        // Results list
        html += '<div class="tp-results-list">';
        var _savedBookmarks = loadBookmarks();
        for (var d = 0; d < docs.length; d++) {
            var doc = docs[d];
            var docId = doc.id || '';
            var path = doc.path || '';
            var localPath = path;
            var snippet = '';

            if (highlighting[docId] && highlighting[docId].text) {
                var hlTexts = highlighting[docId].text;
                snippet = hlTexts.join(' \u2026 ');
                if (snippet.length > 400) {
                    snippet = snippet.substring(0, 400) + '\u2026';
                }
            }

            var breadcrumb = [];
            if (doc.volume) breadcrumb.push(doc.volume);
            if (doc.pitaka) breadcrumb.push(doc.pitaka);
            if (doc.book) breadcrumb.push(doc.book);

            var title = doc.chapter || doc.section || doc.book || path;

            html += '<div class="tp-result-item" data-path="' + escapeHtml(localPath) + '">';
            html += '  <div class="tp-result-title">';
            html += '    <a href="javascript:void(0)" class="tp-result-link" data-path="' + escapeHtml(localPath) + '">';
            html += '      <i class="fa fa-file-text-o"></i> ' + escapeHtml(title);
            html += '    </a>';
            var _isBookmarked = _savedBookmarks.some(function(b) { return b.href === localPath; });
            html += '    <a href="#" class="tp-bookmark-toggle" data-href="' + escapeHtml(localPath) + '" data-title="' + escapeHtml(title) + '" title="Bookmark this page">';
            html += '      <i class="fa ' + (_isBookmarked ? 'fa-star tp-bm-starred' : 'fa-star-o') + '" aria-hidden="true"></i>';
            html += '    </a>';
            // small external/open-in-new-tab icon; data-href stores file path, data-id will be set when tree.json is loaded
            html += '    <a href="#" class="tp-open-newtab" data-href="' + escapeHtml(localPath) + '" title="Open in new tab" style="margin-left:6px; color:#1E3461;">';
            html += '      <i class="fa fa-external-link" aria-hidden="true"></i>';
            html += '    </a>';
            html += '  </div>';
            if (breadcrumb.length > 0) {
                // Display mapping: show 'Anya' as 'Añña' to match facet label
                var displayBreadcrumb = breadcrumb.map(function(b) {
                    if (!b) return b;
                    return (b === 'Anya') ? 'Añña' : b;
                });
                html += '  <div class="tp-result-breadcrumb">' + escapeHtml(displayBreadcrumb.join(' \u203A ')) + '</div>';
            }
            if (snippet) {
                html += '  <div class="tp-result-snippet">' + snippet + '</div>';
            }
            html += '</div>';
        }
        html += '</div>';

        // Pagination
        if (numFound > PAGE_SIZE) {
            html += buildPagination(numFound, currentStart);
        }

        html += '</div>';
        $content.html(html);

        // Result link clicks are handled via delegated handlers bound at init
    // (moved injected styles to initialization to avoid repeated insertion)

        // Pagination clicks are handled via delegated handlers bound at init

        // Bind facet clicks
        bindFacetClicks($content);

        // If a filter is active (e.g. volume was applied), attempt to extract
        // sub-facets from the response and render them under the top-level pills.
        var $sub = $('#tp-sub-facets-container');
        $sub.empty();
        var df = (displayFilterForSubfacets && displayFilterForSubfacets.length) ? displayFilterForSubfacets : currentFilter;
        if (df) {
            // Show a small inline loading indicator while we fetch pivot counts
            $sub.html('<div class="tp-sub-facets-loading"><i class="fa fa-spinner fa-spin"></i> Loading…</div>');

            // Prefer pivot facet data for hierarchical counts if present in this response
            var children = [];
            var pf = parseFilter(df);
            var childField = (pf.field === 'pitaka') ? 'book' : (pf.field === 'book') ? 'chapter' : 'pitaka';
            var pivotKey = (pf.field === 'pitaka') ? 'pitaka,book' : (pf.field === 'book') ? 'book,chapter' : 'volume,pitaka';
            var pivotData = null;
            if (data.facet_counts && data.facet_counts.facet_pivot) {
                pivotData = data.facet_counts.facet_pivot[pivotKey] || data.facet_counts.facet_pivot[pivotKey.replace(',', ', ')];
            }
            if (Array.isArray(pivotData) && pivotData.length) {
                // Find the pivot entry matching the current parent value
                var parentVal = pf.value || df || '';
                for (var pi = 0; pi < pivotData.length; pi++) {
                    var p = pivotData[pi];
                    if ((p.value + '') === (parentVal + '')) {
                        var sub = p.pivot || p['pivot'] || [];
                        for (var si = 0; si < sub.length; si++) {
                            var s = sub[si];
                            children.push({ key: s.value, count: s.count, field: childField });
                        }
                        break;
                    }
                }
            }

            // If no pivot data, fall back to facet_fields.<childField> when available
            if (children.length === 0) {
                var ffields = (data.facet_counts && data.facet_counts.facet_fields) || {};
                var arr = ffields[childField] || null;
                if (Array.isArray(arr)) {
                    for (var pi2 = 0; pi2 < arr.length; pi2 += 2) {
                        var k2 = arr[pi2];
                        var v2 = arr[pi2 + 1] || 0;
                        if (v2 > 0) children.push({ key: k2, count: v2, field: childField });
                    }
                }
            }

            // If Solr didn't return facet_fields (or pitaka was empty), derive subcategories
            // from the returned docs array by counting the next logical field.
            if (children.length === 0) {
                // Determine current filter field and next field to group by
                var pf = parseFilter(currentFilter);
                var filterField = pf.field || 'volume';
                var filterVal = pf.value || currentFilter;
                var nextField = null;
                if (filterField === 'volume') nextField = 'pitaka';
                else if (filterField === 'pitaka') nextField = 'book';
                else if (filterField === 'book') nextField = 'chapter';

                if (nextField) {
                    var counts = {};
                    var docs = (data.response && data.response.docs) || [];
                    for (var di = 0; di < docs.length; di++) {
                        var doc = docs[di];
                        var val = doc[nextField];
                        if (!val) continue;
                        // If field may be an array, normalize to string
                        if (Array.isArray(val)) val = val[0];
                        counts[val] = (counts[val] || 0) + 1;
                    }
                    var keys = Object.keys(counts).sort();
                    for (var ki = 0; ki < keys.length; ki++) {
                        var k2 = keys[ki];
                        children.push({ key: k2, count: counts[k2], field: nextField });
                    }
                }
            }

            if (children.length) {
                // Only suppress rendering when the active filter itself is a
                // chapter (i.e. we're already at the lowest level). This lets
                // Book -> Chapter transitions render Chapter pills normally.
                var pf2 = parseFilter(df);
                if (pf2.field === 'chapter') {
                    $sub.empty();
                    currentExpandedFacet = '';
                } else {
                    renderSubfacetsHtml(df, children);
                }
            } else {
                // If no subcategory info available yet, keep the loading indicator
                // and let fetchPivotCounts (called unconditionally) populate when ready.
                currentExpandedFacet = '';
            }
        } else {
            // Clear expanded facet state on fresh unfiltered render
            currentExpandedFacet = '';
        }
    }

    // Build the facet pills HTML
    function buildFacetsHtml(facets) {
        var facetOrder = [
            'Tipi\u1E6Daka (M\u016Bla)',  // Tipiṭaka (Mūla)
            'A\u1E6D\u1E6Dhakath\u0101',  // Aṭṭhakathā
            'T\u012Bk\u0101',              // Tīkā
            'Anya'
        ];
        // First, prefer the canonical ordering (Roman/diacritic names) if present
        var hasAny = false;
        for (var k = 0; k < facetOrder.length; k++) {
            if (facets[facetOrder[k]] > 0) { hasAny = true; break; }
        }

        // If none of the canonical names are present, fall back to whatever facet
        // keys the Solr response returned (this handles Devanagari-labelled facet keys).
        var fallbackKeys = Object.keys(facets || {}).filter(function(k) { return facets[k] > 0; });
        if (!hasAny && fallbackKeys.length === 0) return '';

        var html = '<div class="tp-facets">';
        html += '<span class="tp-facets-label">Collection: </span>';

        // Determine whether UI is in Roman mode (prefer explicit session setting)
        var romanMode = false;
        try {
            var _m = sessionStorage.getItem('tpsearch-mode');
            if (_m) romanMode = (_m === 'roman');
            else romanMode = !currentIsDeva;
        } catch (e) {
            romanMode = !currentIsDeva;
        }

        // "All" pill — active when no filter is selected
        var allActive = (!currentFilter && !currentExpandedFacet) ? ' tp-facet-active' : '';
        html += '<a href="javascript:void(0)" class="tp-facet-item' + allActive + '" data-volume="">';
        html += 'All (' + unfilteredTotal.toLocaleString() + ')</a>';

        if (hasAny) {
            // Render in the preferred (ordered) names
            for (var fi = 0; fi < facetOrder.length; fi++) {
                var fname = facetOrder[fi];
                if (facets[fname] === undefined || facets[fname] <= 0) continue;
                var active = (currentFilter === fname || currentExpandedFacet === fname) ? ' tp-facet-active' : '';
                // Show localized spelling for 'Anya' as 'Añña' in the UI
                var displayName = fname;
                if (fname === 'Anya') displayName = 'Añña';
                html += '<a href="javascript:void(0)" class="tp-facet-item' + active + '" data-volume="' + escapeHtml(fname) + '">';
                html += escapeHtml(displayName) + ' (' + facets[fname] + ')</a>';
            }
        } else {
            // Fallback: render whatever facet keys Solr returned (use actual key values)
            for (var fk = 0; fk < fallbackKeys.length; fk++) {
                var key = fallbackKeys[fk];
                var active2 = (currentFilter === key) ? ' tp-facet-active' : '';
                var disp = key;
                if (key === 'Anya') disp = 'Añña';
                html += '<a href="javascript:void(0)" class="tp-facet-item' + active2 + '" data-volume="' + escapeHtml(key) + '">';
                html += escapeHtml(disp) + ' (' + facets[key] + ')</a>';
            }
        }

        html += '</div>';
        return html;
    }

    // Bind click handlers on facet pills
    function bindFacetClicks($container) {
        // Delegated facet click binding (bound once during init)
        if (!__delegationBound && ($tContent || $('#t-content')).length) {
            var $root = $tContent || $('#t-content');
            $root.on('click', '.tp-facet-item', function (e) {
                e.preventDefault();
                var $el = $(this);
                var volume = $el.data('volume') || '';
                var filter = $el.data('filter') || '';
                if (filter) { doSearch(currentQuery, 0, filter); return; }
                if (!volume) { doSearch(currentQuery, 0, ''); return; }
                doSearch(currentQuery, 0, volume);
            });
            __delegationBound = true;
        }
    }

    // (removed unused fetchAndRenderSubfacets)

    // Render HTML for sub-facets under the given parent
    function renderSubfacetsHtml(parent, children) {
        var $sub = $('#tp-sub-facets-container');
        if (!$sub.length) return;
        var html = '<div class="tp-facets" style="margin-top:8px;">';

        // Determine parent field and label for the next level
        var pf = parseFilter(parent);
        // If parent appears to be a grouped OR expression (e.g. contains
        // multiple pitaka:... clauses) parseFilter will not return a useful
        // field. Detect common field mentions and coerce to the expected
        // parent field so the UI label and child selection behave correctly.
        try {
            if (!pf || !pf.field || pf.field.indexOf('pitaka') === 0 && pf.value === '') {
                if (/\bpitaka\s*:/i.test(parent)) pf = { field: 'pitaka', value: '' };
                else if (/\bvolume\s*:/i.test(parent)) pf = { field: 'volume', value: '' };
            }
        } catch (e) { /* ignore */ }
        var parentField = pf.field || 'volume';
        var pval = pf.value || '';
        // Map internal field names to UI labels. Rename per request:
        // Volume -> Sub-collection, Pitaka (book) -> Category, Book -> Sub-category
        var nextLabelMap = { 'volume': 'Sub-collection', 'pitaka': 'Category', 'book': 'Sub-category' };
        var labelName = nextLabelMap[parentField] || 'Pitaka';

        html += '<span class="tp-facets-label">' + escapeHtml(labelName) + ': </span>';

        // Render child pills only (do not include the parent value here)
        for (var i = 0; i < children.length; i++) {
            var c = children[i];
            var filterKey = c.field + ':' + c.key; // e.g., pitaka:Vinayapitaka
            var active = ((currentFilter === filterKey) || (displayFilterForSubfacets === filterKey)) ? ' tp-facet-active' : '';
            html += '<a href="javascript:void(0)" class="tp-facet-item' + active + '" data-filter="' + escapeHtml(filterKey) + '">';
            html += escapeHtml(c.key) + ' (' + c.count + ')</a>';
        }

        html += '</div>';
        if (window.console && window.console.debug) console.debug('tpsearch: renderSubfacetsHtml', { parent: parent, children: children });
        $sub.html(html);
        // Bind clicks for these new sub-facet items
        bindFacetClicks($sub);
        // Update top-level facet pill active state so 'All' is deactivated
        // and the expanded parent shows as active.
        updateTopFacetActiveState(pval);
    }

    // Update the top-level facet pills to reflect the currently-expanded facet
    function updateTopFacetActiveState(parent) {
        // Find the main facets block (first one in results area)
        var $mainFacets = ($tContent || $('#t-content')).find('.tp-facets').first();
        if (!$mainFacets.length) return;
        // Remove active class from all top-level facet items
        $mainFacets.find('.tp-facet-item').removeClass('tp-facet-active');
        // Deactivate 'All' explicitly
        $mainFacets.find('.tp-facet-item[data-volume=""]').removeClass('tp-facet-active');
        // Activate the matching top-level facet whose data-volume equals parent
        var $match = $mainFacets.find('.tp-facet-item').filter(function() {
            return ($(this).data('volume') + '') === parent + '';
        }).first();
        if ($match.length) {
            $match.addClass('tp-facet-active');
        }
    }

    // Parse a filter string like "pitaka:Vinayapitaka" or a plain volume name
    function parseFilter(filterStr) {
        var raw = (filterStr === undefined || filterStr === null) ? '' : String(filterStr);
        if (!raw) return { field: 'volume', value: '' };
        if (raw.indexOf(':') > -1) {
            var parts = raw.split(':');
            var f = parts.shift();
            var v = parts.join(':');
            return { field: f, value: v };
        }
        return { field: 'volume', value: raw };
    }

    // Build pagination HTML
    function buildPagination(total, start) {
        var currentPage = Math.floor(start / PAGE_SIZE) + 1;
        var totalPages = Math.ceil(total / PAGE_SIZE);
        var maxVisible = 9;

        var html = '<div class="tp-pagination">';

        if (currentPage > 1) {
            html += '<a href="javascript:void(0)" class="tp-page-link tp-page-prev" data-start="' + ((currentPage - 2) * PAGE_SIZE) + '">&laquo; Prev</a>';
        }

        var startPage = Math.max(1, currentPage - Math.floor(maxVisible / 2));
        var endPage = Math.min(totalPages, startPage + maxVisible - 1);
        if (endPage - startPage < maxVisible - 1) {
            startPage = Math.max(1, endPage - maxVisible + 1);
        }

        if (startPage > 1) {
            html += '<a href="javascript:void(0)" class="tp-page-link" data-start="0">1</a>';
            if (startPage > 2) html += '<span class="tp-page-ellipsis">\u2026</span>';
        }

        for (var p = startPage; p <= endPage; p++) {
            if (p === currentPage) {
                html += '<span class="tp-page-current">' + p + '</span>';
            } else {
                html += '<a href="javascript:void(0)" class="tp-page-link" data-start="' + ((p - 1) * PAGE_SIZE) + '">' + p + '</a>';
            }
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) html += '<span class="tp-page-ellipsis">\u2026</span>';
            html += '<a href="javascript:void(0)" class="tp-page-link" data-start="' + ((totalPages - 1) * PAGE_SIZE) + '">' + totalPages + '</a>';
        }

        if (currentPage < totalPages) {
            html += '<a href="javascript:void(0)" class="tp-page-link tp-page-next" data-start="' + (currentPage * PAGE_SIZE) + '">Next &raquo;</a>';
        }

        html += '</div>';
        return html;
    }

    // HTML escape helper
    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    // Highlight loosely matching terms inside a container.
    // Supports wildcard '*' in the search term and ignores diacritics when matching.
    function highlightLooseMatches($container, term, isDeva) {
        if (!term) return;
        var container = ($container instanceof jQuery) ? $container.get(0) : $container;
        if (!container) return;

        // Build a regex pattern from the term: preserve '*' as wildcard, escape other regex chars
        var placeholder = '__WILDCARD__';
        var tmp = term.replace(/\*/g, placeholder);
        tmp = tmp.replace(/[.+?^${}()|[\]\\]/g, '\\$&');
        // Do not allow wildcard to span whitespace (keep match within one token)
        tmp = tmp.replace(new RegExp(placeholder, 'g'), '[^\\s]*');

        // Normalize pattern: remove diacritic marks so matching is diacritic-insensitive
        var normPattern = tmp.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
        var flags = 'giu';
        var re = null;
        try {
            re = new RegExp(normPattern, flags);
        } catch (e) {
            // Fallback to simple escaped term
            var esc = tmp.replace(/[\\/\[\]\-]/g, '\\$&');
            re = new RegExp(esc, 'giu');
        }

        // Walk text nodes and apply highlights (skip nodes already inside highlights)
        var walker = document.createTreeWalker(container, NodeFilter.SHOW_TEXT, null, false);
        var nodes = [];
        while (walker.nextNode()) nodes.push(walker.currentNode);
        nodes = nodes.filter(function(n) {
            var p = n.parentElement;
            return p && !p.closest('.tpsearch-highlight');
        });

        nodes.forEach(function (textNode) {
            var s = textNode.nodeValue;
            if (!s || !s.trim()) return;

            // Build normalized string and map from normalized index -> original index
            var norm = '';
            var map = [];
            var origIndex = 0;
            for (var i = 0; i < s.length; ) {
                // Grab next code point (handles surrogate pairs)
                var cp = s.charAt(i);
                var code = s.charCodeAt(i);
                if (0xD800 <= code && code <= 0xDBFF && i + 1 < s.length) {
                    // surrogate pair
                    cp = s.substr(i, 2);
                    i += 2;
                } else {
                    i += 1;
                }
                var base = cp.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
                for (var k = 0; k < base.length; k++) {
                    norm += base.charAt(k);
                    map.push(origIndex);
                }
                origIndex += cp.length;
            }

            var matches = [];
            var m;
            while ((m = re.exec(norm)) !== null) {
                var nstart = m.index;
                var nend = m.index + m[0].length;
                var ostart = map[nstart];
                // To include any combining marks (or multi-unit codepoints) attached
                // to the last matched normalized character, use the original index
                // of the next normalized char as the exclusive end; fall back to
                // the full text length when matching to the end.
                var oend = (nend < map.length) ? map[nend] : s.length;
                // Include any following combining marks so the highlighting includes attached diacritics
                while (oend < s.length) {
                    var ch = s.charAt(oend);
                    if (/\p{M}/u.test(ch)) {
                        oend++;
                        continue;
                    }
                    break;
                }
                // Ensure indices are within bounds
                if (ostart >= 0 && oend > ostart && ostart < s.length) {
                    if (oend > s.length) oend = s.length;
                    matches.push({ start: ostart, end: oend });
                }
                // Prevent infinite loops for zero-length matches
                if (m.index === re.lastIndex) re.lastIndex++;
            }

            if (matches.length === 0) return;

            // Merge overlapping matches
            matches.sort(function (a, b) { return a.start - b.start; });
            var merged = [matches[0]];
            for (var mi = 1; mi < matches.length; mi++) {
                var cur = matches[mi];
                var last = merged[merged.length - 1];
                if (cur.start <= last.end) {
                    last.end = Math.max(last.end, cur.end);
                } else merged.push(cur);
            }

            // Replace text node with a DocumentFragment to avoid problematic splitText behavior
            var parent = textNode.parentNode;
            var frag = document.createDocumentFragment();
            var lastIdx = 0;
            for (var mj = 0; mj < merged.length; mj++) {
                var mm = merged[mj];
                if (mm.start > lastIdx) {
                    frag.appendChild(document.createTextNode(s.substring(lastIdx, mm.start)));
                }
                var span = document.createElement('span');
                span.className = 'tpsearch-highlight';
                span.textContent = s.substring(mm.start, mm.end);
                frag.appendChild(span);
                lastIdx = mm.end;
            }
            if (lastIdx < s.length) frag.appendChild(document.createTextNode(s.substring(lastIdx)));
            parent.replaceChild(frag, textNode);
        });
    }

    // Build and attach the right-click context menu
    function buildContextMenu() {
        var menuHtml =
            '<div id="tp-context-menu" class="tp-context-menu" style="display:none;">' +
            '  <div class="tp-context-item" id="tp-context-search">' +
            '    <i class="fa fa-search"></i> Search "<span id="tp-context-text"></span>"' +
            '  </div>' +
            '</div>';
        $('body').append(menuHtml);

        $(document).on('contextmenu', '#t-content', function (e) {
            var sel = window.getSelection().toString().trim();
            if (!sel || sel.length < 1 || sel.length > 200) return;

            e.preventDefault();
            var displayText = sel.length > 30 ? sel.substring(0, 30) + '\u2026' : sel;
            $('#tp-context-text').text(displayText);
            $('#tp-context-menu')
                .data('search-text', sel)
                .css({ top: e.pageY + 'px', left: e.pageX + 'px' })
                .show();
        });

        $(document).on('click', '#tp-context-search', function () {
            var text = $('#tp-context-menu').data('search-text');
            if (text) {
                $('#tp-search-input').val(text);
                currentFilter = '';  // reset filter for new context-menu search
                doSearch(text, 0, '');
            }
            $('#tp-context-menu').hide();
        });

        $(document).on('click', function (e) {
            if (!$(e.target).closest('#tp-context-menu').length) {
                $('#tp-context-menu').hide();
            }
        });
    }

    // ───── Bookmark helpers ─────
    var TP_BM_KEY = 'tpsearch-bookmarks';

    function loadBookmarks() {
        try { return JSON.parse(localStorage.getItem(TP_BM_KEY) || '[]'); } catch (e) { return []; }
    }

    function saveBookmarks(arr) {
        try { localStorage.setItem(TP_BM_KEY, JSON.stringify(arr)); } catch (e) {}
    }

    function renderBookmarkDropdown() {
        var $dd = $('#tp-bookmark-dropdown');
        if (!$dd.length) return;
        var bms = loadBookmarks();
        var updated = false;
        // If we have a tree map loaded, prefer to populate missing ids so
        // bookmarks open by #id (index page) instead of raw href when possible.
        try {
            if (bms && bms.length && typeof _treeHrefToId === 'object') {
                for (var bi = 0; bi < bms.length; bi++) {
                    var bb = bms[bi] || {};
                    if ((!bb.id || bb.id === '') && bb.href) {
                        var mapped = _treeHrefToId[bb.href] || _treeHrefToId[(bb.href || '').split('/').pop()];
                        if (mapped) { bb.id = mapped; bms[bi] = bb; updated = true; }
                    }
                }
                if (updated) saveBookmarks(bms);
            }
        } catch (e) { /* ignore mapping errors */ }
        if (!bms.length) {
            $dd.html('<div class="tp-bm-empty">No bookmarks yet.</div>');
            return;
        }
        var html = '';
        bms.forEach(function(b) {
            html += '<div class="tp-bm-row">';
            html += '<a href="#" class="tp-bm-remove" data-href="' + escapeHtml(b.href) + '" title="Remove bookmark"><i class="fa fa-star tp-bm-starred"></i></a>';
            var bmLabel = escapeHtml(b.title) + (b.query ? ' <span class="tp-bm-query">[' + escapeHtml(b.query) + ']</span>' : '');
            html += '<a href="#" class="tp-bm-open" data-href="' + escapeHtml(b.href) + '" data-id="' + escapeHtml(b.id || '') + '" data-query="' + escapeHtml(b.query || '') + '" data-isdeva="' + (b.isDeva ? '1' : '0') + '" data-section="' + escapeHtml(b.sectionId || '') + '">' + bmLabel + '</a>';
            html += '</div>';
        });
        $dd.html(html);
    }

    function updateBookmarkIcon() {
        var $icon = $('#tp-topbar-bookmark-icon');
        $icon.show();
        if (loadBookmarks().length > 0) {
            $icon.find('i').removeClass('fa-bookmark-o').addClass('fa-bookmark');
        } else {
            $icon.find('i').removeClass('fa-bookmark').addClass('fa-bookmark-o');
            $('#tp-bookmark-dropdown').hide();
        }
    }

    // ───── Initialisation ─────
    $(document).ready(function () {
        // Ensure external tpsearch stylesheet is loaded (reduces runtime CSS injection)
        if (!document.querySelector('link[href*="tpsearch.css"]')) {
            var link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = '/css/tpsearch.css';
            document.head.appendChild(link);
        }
        // Insert search bar (hidden) between .header and .bodycontainer
        var $header = $('.header');
        if ($header.length) {
            $header.after(buildSearchBar());
            // Hide Limit Search by default; revealed when user clicks Filter
            $('#tp-limit-search').hide();
            // Insert a small expand handle (triangle) before the search bar so it is visible
            // when the bar is collapsed, and append a collapse button inside the bar.
            if (!$('#tp-search-handle-container').length) {
                $('#tp-search-bar').before('<div id="tp-search-handle-container" style="position:relative; height:0;">' +
                    '<button type="button" id="tp-search-expand-btn" class="tp-search-triangle tp-search-expand" aria-label="Open search" style="display:block;"></button>' +
                '</div>');
            }
            // Append collapse button inside search bar for proper positioning
            if (!$('#tp-search-collapse-btn').length) {
                $('#tp-search-bar').append('<button type="button" id="tp-search-collapse-btn" class="tp-search-triangle tp-search-collapse" aria-label="Collapse search" style="display:none;"></button>');
            }
            // Set initial visibility of handles based on whether the bar is visible
            // Default: do not show the downward (expand) triangle unless search
            // has been activated via the header icon. Keep expand hidden by default.
            if ($('#tp-search-bar').is(':visible')) {
                $('#tp-search-collapse-btn').show();
                $('#tp-search-expand-btn').hide();
            } else {
                $('#tp-search-collapse-btn').hide();
                $('#tp-search-expand-btn').hide();
            }
        }

        // Inject search icon into the top nav bar
        injectTopBarSearchIcon();

        // Toggle Limit Search when Filter button is clicked
        $(document).on('click', '#tp-filter-btn', function (e) {
            e.preventDefault();
            var $btn = $(this);
            var $panel = $('#tp-limit-search');
            if (!$panel.length) return;
            $panel.stop(true, true).slideToggle(180);
            $btn.toggleClass('tp-filter-active');
        });

        // Toggle search bar when the top-bar icon is clicked
        // If the icon is already active, treat click as a deselect: close bar
        // and remove the downward expand triangle. Otherwise open the bar.
        $(document).on('click', '#tp-topbar-search-icon', function (e) {
            e.preventDefault();
            var $icon = $(this);
            var $bar = $('#tp-search-bar');
            var $collapse = $('#tp-search-collapse-btn');
            var $expand = $('#tp-search-expand-btn');
            if ($icon.hasClass('tp-topbar-search-active')) {
                // user is deselecting: close bar and hide expand triangle
                $icon.removeClass('tp-topbar-search-active');
                _tpsearchActivated = false;
                $bar.stop(true,true).slideUp(200, function() {
                    if ($collapse && $collapse.length) $collapse.hide();
                    if ($expand && $expand.length) $expand.hide();
                });
            } else {
                openSearchBar();
            }
        });

        // Expand/collapse triangle handlers
        $(document).on('click', '#tp-search-expand-btn', function (e) {
            e.preventDefault();
            openSearchBar();
        });
        $(document).on('click', '#tp-search-collapse-btn', function (e) {
            e.preventDefault();
            minimizeSearchBar();
        });

        // Show help popup on hover or focus; hide on leave/blur or outside click
        // Position popup near the help icon using fixed viewport coordinates
        $(document).on('mouseenter focusin', '#tp-help-btn', function (e) {
            var $popup = $('#tp-help-popup');
            var $btn = $(this);
            if (!$btn.length || !$popup.length) return;
            var off = $btn.offset();
            // Use viewport-fixed positioning so we don't depend on any container offsets
            var scrollTop = $(window).scrollTop() || 0;
            var scrollLeft = $(window).scrollLeft() || 0;
            var popupW = $popup.outerWidth();
            var top = off.top - scrollTop + $btn.outerHeight() - 4; // slightly below the icon
            var left = off.left - scrollLeft + Math.round(($btn.outerWidth() - popupW) / 2);
            $popup.css({ position: 'fixed', top: top + 'px', left: left + 'px' }).show();
        });

        // Hide with a short delay to allow moving between button and popup
        $(document).on('mouseleave focusout', '#tp-help-btn', function (e) {
            var $popup = $('#tp-help-popup');
            setTimeout(function () {
                if (!$('#tp-help-btn').is(':hover') && !$popup.is(':hover') && !$popup.find(':focus').length) {
                    $popup.hide();
                }
            }, 150);
        });

        // Prevent clicks inside popup from closing it immediately
        $(document).on('click', '#tp-help-popup', function (e) { e.stopPropagation(); });

        // Hide popup when clicking elsewhere
        $(document).on('click', function (e) {
            if (!$(e.target).closest('#tp-help-popup, #tp-help-btn').length) {
                $('#tp-help-popup').hide();
            }
        });

        // Mode switch: Roman vs Devanagari
        var setInputMode = function(mode) {
            // mode: 'deva', 'roman', or falsy/other for no selection
            if (mode === 'deva') {
                $('#tp-mode-deva').addClass('tp-mode-active').attr('aria-pressed', 'true');
                $('#tp-mode-roman').removeClass('tp-mode-active').attr('aria-pressed', 'false');
                $('.tp-pali-chars').hide();
                $('#tp-deva-palette').show();
                currentIsDeva = true;
                try { sessionStorage.setItem('tpsearch-mode', 'deva'); } catch(e){}
            } else if (mode === 'roman') {
                $('#tp-mode-roman').addClass('tp-mode-active').attr('aria-pressed', 'true');
                $('#tp-mode-deva').removeClass('tp-mode-active').attr('aria-pressed', 'false');
                $('.tp-pali-chars').show();
                $('#tp-deva-palette').hide();
                currentIsDeva = false;
                try { sessionStorage.setItem('tpsearch-mode', 'roman'); } catch(e){}
            } else {
                // No mode selected: clear active state and hide palettes
                $('#tp-mode-roman').removeClass('tp-mode-active').attr('aria-pressed', 'false');
                $('#tp-mode-deva').removeClass('tp-mode-active').attr('aria-pressed', 'false');
                $('.tp-pali-chars').hide();
                $('#tp-deva-palette').hide();
                currentIsDeva = false;
                try { sessionStorage.removeItem('tpsearch-mode'); } catch(e){}
            }
        };

        $(document).on('click', '#tp-mode-roman', function () {
            // Toggle: if already active, clear mode (hide palettes), otherwise enable Roman
            if ($(this).hasClass('tp-mode-active')) {
                setInputMode('');
            } else {
                setInputMode('roman');
            }
        });
        $(document).on('click', '#tp-mode-deva', function () {
            // Toggle: if already active, clear mode (hide palettes), otherwise enable Devanagari
            if ($(this).hasClass('tp-mode-active')) {
                setInputMode('');
            } else {
                setInputMode('deva');
            }
        });

        // No Solr field toggle in UI

        // Insert Devanagari char into search input
        $(document).on('click', '.tp-deva-btn', function () {
            var ch = $(this).data('char') || '';
            var $inp = $('#tp-search-input');
            var el = $inp.get(0);
            if (!el) return;
            var start = el.selectionStart || 0;
            var end = el.selectionEnd || 0;
            var val = $inp.val() || '';
            var newVal = val.substring(0, start) + ch + val.substring(end);
            $inp.val(newVal);
            // place caret after inserted char
            el.selectionStart = el.selectionEnd = start + ch.length;
            $inp.focus();
            // switch to Devanagari mode
            setInputMode('deva');
        });

        // Initialize mode from session (default roman)
        (function initMode() {
            try {
                var m = sessionStorage.getItem('tpsearch-mode');
                if (m) setInputMode(m); else setInputMode('');
            } catch (e) { setInputMode(''); }
        })();

        // Helper to process a pending highlight request (used by fragment parsing
        // and by the postMessage listener). Parameters:
        //  - pendingId: (string|null) numeric tree id
        //  - pendingQ: (string) query to highlight
        //  - pendingIsDeva: (bool) whether Devanagari mode
        function processPending(pendingId, pendingQ, pendingIsDeva) {
            if (!pendingQ) return;
            // Ensure $tContent is initialized (in case this runs before the normal init)
            if (typeof $tContent === 'undefined' || !$tContent) {
                $tContent = $('#t-content');
            }
            if (window.console) console.log('tpsearch: processPending id=', pendingId, 'q=', pendingQ, 'isDeva=', pendingIsDeva);

            var clearPendingStorage = function() {
                try { localStorage.removeItem('tpsearch-newtab-query'); } catch (e) {}
                try { localStorage.removeItem('tpsearch-newtab-isdeva'); } catch (e) {}
            };

            var applyHighlight = function() {
                if (!pendingQ) return 0;
                if (window.console) console.log('tpsearch: applying highlight for', pendingQ, 'isDeva=', pendingIsDeva);
                try { highlightLooseMatches($tContent, pendingQ, pendingIsDeva); } catch (e) {
                    var esc = pendingQ.replace(/[.*+?^${}()|[\\]\\]/g, '\\$&');
                    var flags = pendingIsDeva ? 'giu' : 'gi';
                    var regex = new RegExp('(' + esc + ')', flags);
                    $tContent.html(function (_, html) { return html.replace(regex, '<span class="tpsearch-highlight">$1</span>'); });
                }
                var count = $tContent.find('.tpsearch-highlight').length;
                if (count > 0) {
                    var $first = $tContent.find('.tpsearch-highlight').first();
                    if ($first.length) {
                        var top = $first.offset().top - 80;
                        $('html, body').animate({ scrollTop: top }, 300);
                    }
                }
                return count;
            };

            var applyHighlightWithRetries = function(maxAttempts, delayMs) {
                var attempt = 0;
                var runner = function() {
                    attempt++;
                    var hitCount = applyHighlight();
                    if (hitCount > 0) {
                        if (window.console) console.log('tpsearch: highlight succeeded on attempt', attempt, 'hits=', hitCount);
                        clearPendingStorage();
                        return;
                    }
                    if (attempt < maxAttempts) {
                        setTimeout(runner, delayMs);
                    } else {
                        if (window.console) console.log('tpsearch: highlight gave no matches after retries');
                        clearPendingStorage();
                    }
                };
                runner();
            };

            if (pendingId) {
                var _highlightApplied = false;
                var safeApplyHighlight = function() {
                    if (_highlightApplied) return;
                    _highlightApplied = true;
                    applyHighlightWithRetries(50, 200);
                };

                // Watch $tContent independently — the site's tree may load content
                // automatically via the #id hash without us needing to click anything.
                var watchContent = function(tries) {
                    if (_highlightApplied) return;
                    if ($tContent && $tContent.length && $.trim($tContent.html()).length) {
                        if (window.console) console.log('tpsearch: watchContent detected content');
                        safeApplyHighlight();
                    } else if (tries > 0) {
                        setTimeout(function() { watchContent(tries - 1); }, 200);
                    } else {
                        if (window.console) console.log('tpsearch: watchContent timed out');
                        safeApplyHighlight();
                    }
                };
                watchContent(50); // up to 10s

                var tryLoad = function(attemptsLeft) {
                    if (_highlightApplied) return; // watchContent already handled it
                    var href = _treeIdToHref[pendingId];
                    if (window.console) console.log('tpsearch: tryLoad, attemptsLeft=', attemptsLeft, 'mapped href=', href);
                    try {
                        var nodeLink = document.querySelector('a[href="#' + pendingId + '"]');
                        if (nodeLink) {
                            if (window.console) console.log('tpsearch: found tree node link for id=', pendingId, '- clicking it');
                            nodeLink.click();
                            // watchContent will detect when content appears and apply highlight
                            return;
                        } else {
                            if (window.console) console.log('tpsearch: no tree node link for id=', pendingId);
                        }
                    } catch (e) { if (window.console) console.log('tpsearch: error while checking for node link', e); }
                        if (href && $tContent && $tContent.length) {
                            if (window.console) console.log('tpsearch: loading href directly', href);
                            $tContent.load(href, function() { if (window.console) console.log('tpsearch: loaded href', href); safeApplyHighlight(); insertSubheadBookmarkIcons(); });
                        return;
                    }
                    if (attemptsLeft > 0) {
                        if (window.console) console.log('tpsearch: retrying tryLoad in 200ms');
                        setTimeout(function() { tryLoad(attemptsLeft - 1); }, 200);
                    }
                };
                tryLoad(25);
            } else {
                // No id: just apply highlight to whatever content is present
                applyHighlightWithRetries(50, 200);
            }
        }

        // Accept postMessage from the opener to receive a search query after the page
        // has loaded. Adjust the origin whitelist below for your dev host(s).
        window.addEventListener('message', function(e) {
            try {
                var origin = (e && e.origin) ? e.origin : '';
                // Allow same-origin messages or localhost/127.0.0.1 origins used during dev.
                var allowed = (origin === window.location.origin) || /localhost|127\.0\.0\.1/.test(origin);
                if (!allowed) {
                    if (window.console) console.log('tpsearch: message ignored from origin', origin);
                    return;
                }
                var data = e.data || {};
                if (!data) return;
                var tpq = data.tpq || data.q || '';
                var tpd = data.tpd || data.d || '0';
                var id = data.id || null;
                if (!tpq) return;
                if (window.console) console.log('tpsearch: received postMessage payload', data);
                processPending(String(id || ''), tpq, (tpd === '1' || tpd === 1 || tpd === true));
            } catch (err) { if (window.console) console.log('tpsearch: postMessage handler error', err); }
        }, false);
        // Build context menu
        buildContextMenu();

        // Cache commonly used jQuery selectors and bind delegated handlers
        $tpSearchInput = $('#tp-search-input');
        $tContent = $('#t-content');
        $tpSearchClear = $('#tp-search-clear');

        // Insert bookmark icons for subheads inside loaded content and
        // provide a MutationObserver to re-run when content is dynamically replaced.
        function insertSubheadBookmarkIcons() {
            try {
                var $cont = $tContent || $('#t-content');
                if (!$cont || !$cont.length) return;
                var bookmarks = loadBookmarks();
                // Target elements marked with class="subhead" or attribute rend="subhead"
                $cont.find('.subhead, [rend="subhead"]').each(function (idx) {
                    var $h = $(this);
                    // skip if we've already attached a bookmark toggle
                    if ($h.find('.tp-subhead-bm').length) return;
                    var hid = $h.attr('id');
                    if (!hid) {
                        // generate a safe id from text content
                        var base = ($h.text() || '').trim().toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
                        if (!base) base = 'subhead-' + idx;
                        hid = 'tp-sub-' + base;
                        var probe = hid, n = 1;
                        while (document.getElementById(probe)) { probe = hid + '-' + (n++); }
                        hid = probe;
                        $h.attr('id', hid);
                    }
                    var path = (window.location.pathname || '').replace(/^\//, '');
                    var href = path + '#' + hid;
                    var title = ($h.text() || '').trim();
                    var $a = $('<a href="#" class="tp-bookmark-toggle tp-subhead-bm" />');
                    $a.attr('data-href', href);
                    // Resolve the current page's tree node id so the bookmark opens
                    // the logical index page (index.html#<id>) rather than the raw XML file.
                    var _pageTreeId = '';
                    try {
                        var _currPath = (window.location.pathname || '').replace(/^\//, '');
                        _pageTreeId = (_treeHrefToId && (_treeHrefToId[_currPath] || _treeHrefToId[_currPath.split('/').pop()])) || '';
                        if (!_pageTreeId) {
                            // Fallback: if the URL hash is already a plain tree node id (e.g. #67)
                            var _hashMatch = (window.location.hash || '').match(/^#(\d+)$/);
                            if (_hashMatch) _pageTreeId = _hashMatch[1];
                        }
                    } catch (e3) {}
                    $a.attr('data-id', _pageTreeId);
                    $a.attr('data-section', hid);
                    $a.attr('data-title', title);
                    // Add native tooltip and accessibility label
                    $a.attr('title', 'Bookmark this section');
                    $a.attr('aria-label', 'Bookmark this section');
                    // Use the same star markup used elsewhere (outline by default)
                    $a.html('<i class="fa fa-star-o" aria-hidden="true"></i>');
                    // append a small separator then the icon
                    // For centered headings it looks better to append a non-breaking space before the icon
                    $h.append('\u00A0').append($a);
                    // reflect saved state
                    for (var bi = 0; bi < bookmarks.length; bi++) {
                        var bb = bookmarks[bi] || {};
                        var bh = (bb.href || '');
                        // compare several normalized forms to match stored hrefs
                        var bhNorm = bh.replace(/^\//, '');
                        var hrefNorm = href.replace(/^\//, '');
                        var bhTail = (bh || '').split('/').pop();
                        if (bh === href || bhNorm === hrefNorm || bhTail === hrefNorm || bh === ('#' + hid) ) {
                            $a.find('i').removeClass('fa-star-o').addClass('fa-star tp-bm-starred');
                            break;
                        }
                    }
                });
            } catch (e) { console.warn('insertSubheadBookmarkIcons error', e); }
        }

        // Observe changes to content so that injected icons persist after dynamic loads
        try {
            var contNode = ($tContent && $tContent.length) ? $tContent.get(0) : null;
            if (contNode) {
                var moTimer = null;
                var mo = new MutationObserver(function(mutations) {
                    if (moTimer) clearTimeout(moTimer);
                    moTimer = setTimeout(function() { insertSubheadBookmarkIcons(); }, 150);
                });
                mo.observe(contNode, { childList: true, subtree: true });
            }
        } catch (e) { /* ignore observer failures */ }

        // run once at init
        insertSubheadBookmarkIcons();

        // Determine current script folder (e.g. 'romn', 'deva') from the URL
        var _scriptRoot = (function() {
            try {
                var p = window.location.pathname || '/';
                var parts = p.replace(/^\//, '').split('/');
                return parts[0] || '';
            } catch (e) { return ''; }
        })();

        // Map of href -> id and id -> href loaded from tree.json (if available)
        var _treeHrefToId = {};
        var _treeIdToHref = {};
        function _buildTreeMap(nodes) {
            if (!Array.isArray(nodes)) return;
            nodes.forEach(function(n) {
                if (!n) return;
                if (n.a_attr && n.a_attr.href && n.id) {
                    var href = n.a_attr.href;
                    _treeHrefToId[href] = n.id;
                    try { _treeHrefToId[href.split('/').pop()] = n.id; } catch (e) {}
                    try { _treeIdToHref[n.id] = href; } catch (e) {}
                }
                if (n.children) _buildTreeMap(n.children);
            });
        }

        // Called after tree.json loads: build the map, patch data-id attrs, populate bookmark ids
        function afterTreeMapLoaded(data) {
            _buildTreeMap(data);
            $('.tp-open-newtab[data-href], .tp-bookmark-toggle[data-href]').each(function() {
                var href = $(this).attr('data-href') || '';
                var id = _treeHrefToId[href] || _treeHrefToId[href.split('/').pop()];
                if (id) $(this).attr('data-id', id);
            });
            try {
                var bms = loadBookmarks();
                var changed = false;
                for (var bi = 0; bi < bms.length; bi++) {
                    var bb = bms[bi] || {};
                    if ((!bb.id || bb.id === '') && bb.href) {
                        var mapped = _treeHrefToId[bb.href] || _treeHrefToId[(bb.href || '').split('/').pop()];
                        if (mapped) { bb.id = mapped; bms[bi] = bb; changed = true; }
                    }
                }
                if (changed) saveBookmarks(bms);
            } catch (e) { /* ignore */ }
        }

        // Load tree.json only from the known script folders: 'romn' or 'deva'.
        // The search widget is limited to these two scripts, so no other candidates are necessary.
        (function loadTreeForKnownScripts() {
            var root = null;
            try {
                var p = window.location.pathname || '/';
                if (p.indexOf('/romn/') !== -1 || p.indexOf('/romn') === 0) root = 'romn';
                else if (p.indexOf('/deva/') !== -1 || p.indexOf('/deva') === 0) root = 'deva';
                else if (_scriptRoot && (_scriptRoot === 'romn' || _scriptRoot === 'deva')) root = _scriptRoot;
            } catch (e) {}
            if (!root) return; // not in a supported script folder
            // Build a tree.json URL relative to the current path so we don't hit the server root.
            // Example: if pathname is '/tipitaka.org/romn/s0101t.tik.html' -> use '/tipitaka.org/romn/tree.json'
            var treeUrl = null;
            try {
                var p = window.location.pathname || '/';
                var m = p.match(/^(.*?\/(?:romn|deva))(?:\/|$)/);
                if (m && m[1]) treeUrl = m[1].replace(/\/+$/, '') + '/tree.json';
            } catch (e) {}
            if (!treeUrl) treeUrl = '/' + root + '/tree.json';

            $.getJSON(treeUrl).done(function(data) {
                _scriptRoot = root;
                afterTreeMapLoaded(data);
            }).fail(function() {
                // as a last resort, try the project-root-prefixed path
                var alt = '/tipitaka.org/' + root + '/tree.json';
                if (alt === treeUrl) return;
                $.getJSON(alt).done(function(data) {
                    _scriptRoot = root;
                    afterTreeMapLoaded(data);
                }).fail(function() {
                    // mapping unavailable
                });
            });
        })();

        // If a new-tab open requested a highlight, perform it now and then clear the flag.
        // Support two mechanisms: localStorage (same-origin) and encoded hash params (cross-origin).
        try {
            var _pendingQ = null;
            var _pendingIsDeva = false;
            var _pendingIdFromHash = null;
            var _pendingHandled = false;
            // Extract id from the URL hash (clean format: #674)
            try {
                var _h = window.location.hash || '';
                var _hm = _h.match(/^#([^\?&;\/]+)/);
                if (_hm) _pendingIdFromHash = _hm[1];
                if (window.console) console.log('tpsearch: extracted _pendingIdFromHash=', _pendingIdFromHash, 'from hash=', _h);
            } catch (e) {}

            // 1) localStorage-based (works when origin matches)
            try { _pendingQ = localStorage.getItem('tpsearch-newtab-query'); } catch (e) { _pendingQ = null; }
            if (_pendingQ && _pendingQ.length) {
                _pendingIsDeva = (localStorage.getItem('tpsearch-newtab-isdeva') === '1');
                // Apply when a hash exists (new-tab node open); use parsed id when available.
                if ((window.location.hash || '').length > 1) {
                    // Clear localStorage immediately so it doesn't fire again on the next page load
                    try { localStorage.removeItem('tpsearch-newtab-query'); } catch (e) {}
                    try { localStorage.removeItem('tpsearch-newtab-isdeva'); } catch (e) {}
                    // Use processPending with the hash id so it waits for content to load first
                    processPending(_pendingIdFromHash || '', _pendingQ, _pendingIsDeva);
                    _pendingHandled = true;
                } else {
                    // No tree id: this just a normal page load, not a bookmark new-tab.
                    // Clear localStorage immediately and don't apply highlight
                    try { localStorage.removeItem('tpsearch-newtab-query'); } catch (e) {}
                    try { localStorage.removeItem('tpsearch-newtab-isdeva'); } catch (e) {}
                }
            } else {
                // 2) parse hash params for tpq/tpd (format: #<id>?tpq=...&tpd=1)
                try {
                    var h = window.location.hash || '';
                    var m = h.match(/^#([^\?&;\/]+)(?:[\?&;](.*))?$/);
                    if (m && m[2]) {
                        var qs = m[2];
                        var pendingId = m[1] || null;
                        var pairs = qs.split(/[&;]/);
                        var map = {};
                        pairs.forEach(function(p) {
                            var kv = p.split('=');
                            if (kv.length >= 2) map[kv[0]] = decodeURIComponent(kv.slice(1).join('='));
                        });
                        if (map.tpq) {
                                _pendingQ = map.tpq;
                                _pendingIsDeva = (map.tpd === '1');
                                if (window.console) console.log('tpsearch: parsed fragment tpq, tpd:', _pendingQ, _pendingIsDeva);
                                // remove the params from the URL while keeping the hash id
                                try {
                                    var cleanHash = '#' + (m[1] || '');
                                    history.replaceState(null, null, window.location.pathname + cleanHash);
                                } catch (e) {}
                            } else {
                                if (window.console) console.log('tpsearch: no tpq param found in fragment');
                            }
                        // If we have an id, try to load the corresponding file before highlighting
                        if (_pendingQ) processPending(pendingId, _pendingQ, _pendingIsDeva);
                    }
                } catch (e) {}
            }
        } catch (e) {}

        // ── Scroll to bookmarked subheading in new tab ──
        // When a subheading bookmark is opened, tpsearch-newtab-section holds the
        // element id to scroll to after the page's content has loaded.
        try {
            var _pendingSection = null;
            try { _pendingSection = localStorage.getItem('tpsearch-newtab-section'); } catch (e) {}
            if (_pendingSection) {
                try { localStorage.removeItem('tpsearch-newtab-section'); } catch (e) {}
                var scrollToSection = function(tries) {
                    var el = document.getElementById(_pendingSection);
                    if (el) {
                        var top = $(el).offset().top - 80;
                        $('html, body').animate({ scrollTop: top }, 400);
                    } else if (tries > 0) {
                        setTimeout(function() { scrollToSection(tries - 1); }, 200);
                    }
                };
                // Wait for content to finish loading before scrolling (up to 10s)
                scrollToSection(50);
            }
        } catch (e) {}

        // Do not perform live searches while typing. Show/hide clear button only.
        $tpSearchInput.on('input', function () {
            var v = $tpSearchInput.val();
            if (v && v.trim().length) $tpSearchClear.show(); else $tpSearchClear.hide();
        });

        // Delegated handler for result links (single binding)
        if ($tContent && $tContent.length) {
            $tContent.on('click', '.tp-result-link', function (e) {
                e.preventDefault();
                var linkPath = $(this).data('path');
                if (!linkPath) return;
                // Save current search state for back button
                try { window.sessionStorage.setItem('tpsearch-last-results', $tContent.html()); } catch (err) {}
                try { window.sessionStorage.setItem('tpsearch-scroll', window.scrollY); } catch (err) {}
                try { window.sessionStorage.setItem('tpsearch-query', currentQuery); } catch (err) {}
                try { window.sessionStorage.setItem('tpsearch-filter', currentFilter); } catch (err) {}
                $tContent.load(linkPath, function () {
                    var term = currentQuery;
                    if (term) {
                        try {
                            highlightLooseMatches($tContent, term, currentIsDeva);
                        } catch (e) {
                            var esc = term.replace(/[.*+?^${}()|[\\]\\]/g, '\\$&');
                            var flags = currentIsDeva ? 'giu' : 'gi';
                            var regex = new RegExp('(' + esc + ')', flags);
                            $tContent.html(function (_, html) { return html.replace(regex, '<span class="tpsearch-highlight">$1</span>'); });
                        }
                        var $first = $tContent.find('.tpsearch-highlight').first();
                        if ($first.length) {
                            var top = $first.offset().top - 80;
                            $('html, body').animate({ scrollTop: top }, 300);
                        }
                    }
                    // Insert subhead bookmark icons for newly loaded content
                    try { insertSubheadBookmarkIcons(); } catch (e) {}

                    if (!$('#tp-search-back').length) {
                        var $btn = $('<button id="tp-search-back" class="tpsearch-back-btn" title="Back" aria-label="Back">&lt;</button>');
                        $btn.on('click', function () {
                            var last = null;
                            try { last = window.sessionStorage.getItem('tpsearch-last-results'); } catch (e) {}
                            if (last) {
                                $tContent.html(last);
                                var scroll = null;
                                try { scroll = window.sessionStorage.getItem('tpsearch-scroll'); } catch (e) {}
                                if (scroll) window.scrollTo(0, parseInt(scroll, 10));
                                $('#tp-search-back').remove();
                                // no rebind needed; delegated handlers handle events
                            } else {
                                var q = '';
                                var f = '';
                                try { q = window.sessionStorage.getItem('tpsearch-query') || ''; } catch (e) {}
                                try { f = window.sessionStorage.getItem('tpsearch-filter') || ''; } catch (e) {}
                                doSearch(q, 0, f);
                                $('#tp-search-back').remove();
                            }
                        });
                        var $xbtn = $('#tp-search-clear');
                        $xbtn.after($btn);
                    }
                });
            });

            // Delegated handler for open-in-new-tab icon
            $tContent.on('click', '.tp-open-newtab', function (e) {
                e.preventDefault();
                var $btn = $(this);
                var href = ($btn.attr('data-href') || '').toString();
                var id = $btn.attr('data-id') || _treeHrefToId[href] || _treeHrefToId[href.split('/').pop()];
                // tree.json should provide an id for every href; no fallback required
                // Save query data so the opened page can highlight it.
                // localStorage won't work across origins, so include the encoded query in the fragment.
                var encQ = '';
                try { encQ = encodeURIComponent(currentQuery || ''); } catch (e) { encQ = ''; }
                var encD = currentIsDeva ? '1' : '0';
                try { localStorage.setItem('tpsearch-newtab-query', currentQuery || ''); } catch (err) {}
                try { localStorage.setItem('tpsearch-newtab-isdeva', currentIsDeva ? '1' : '0'); } catch (err) {}
                var targetUrl = null;
                // Build URLs relative to the current host so this works on both
                // local dev and production without hardcoding a domain.
                var _pageBase = window.location.href.replace(/[^/]*$/, '');
                var _pageOrigin = window.location.origin;
                if (id) {
                    // Open the index page on the same host with the tree node id as the hash.
                    targetUrl = _pageBase + 'index.html#' + id;
                } else if (href) {
                    targetUrl = _pageBase + href.replace(/^\//, '');
                }
                if (targetUrl) {
                    var newWin = window.open(targetUrl, '_blank');
                    // Post the query to the opened window. Use retries in case the page
                    // hasn't attached its listener yet. Use same origin as current page.
                    if (newWin && encQ) {
                        var payload = { tpq: currentQuery || '', tpd: encD, id: id || '' };
                        var tries = 0;
                        var maxTries = 12; // ~3.6s of retries at 300ms
                        var sendMsg = function() {
                            try {
                                newWin.postMessage(payload, _pageOrigin);
                                if (window.console) console.log('tpsearch: postMessage sent to new window', payload);
                            } catch (err) {
                                if (window.console) console.log('tpsearch: postMessage error', err);
                            }
                            tries++;
                            if (tries < maxTries) setTimeout(sendMsg, 300);
                        };
                        sendMsg();
                    }
                }
            });

            // Delegated pagination handler
            $tContent.on('click', '.tp-page-link', function (e) {
                e.preventDefault();
                var pageStart = parseInt($(this).data('start'), 10);
                if (!isNaN(pageStart)) {
                    doSearch(currentQuery, pageStart, currentFilter);
                    $('html, body').animate({ scrollTop: 0 }, 200);
                }
            });
        }

        // Show/hide the nikaya sub-row based on whether any Sutta checkbox is checked
        function updateNikayaRow() {
            var anySutta = $('#tp-limit-mula-sutta, #tp-limit-atth-sutta, #tp-limit-tika-sutta').filter(':checked').length > 0;
            $('#tp-nikaya-row').toggle(anySutta);
            if (!anySutta) $('.tp-nikaya-checkbox').prop('checked', false);
        }

        // Limit Search checkbox behavior
        $(document).on('change', '.tp-limit-checkbox', function () {
            var $all = $('#tp-limit-all');
            if ($(this).attr('id') === 'tp-limit-all') {
                if ($(this).is(':checked')) {
                    // All: uncheck every other checkbox
                    $('.tp-limit-checkbox').not(this).prop('checked', false);
                    $('.tp-limit-row-checkbox').prop('checked', false);
                }
            } else {
                if ($(this).is(':checked')) {
                    // Child checked: uncheck the row checkbox for this row, uncheck All
                    $(this).closest('tr').find('.tp-limit-row-checkbox').prop('checked', false);
                    $all.prop('checked', false);
                } else {
                    // If nothing is checked anywhere, default back to All
                    var any = $('.tp-limit-checkbox').not('#tp-limit-all').filter(':checked').length
                            + $('.tp-limit-row-checkbox:checked').length;
                    if (!any) $all.prop('checked', true);
                }
            }
            updateNikayaRow();
        });

        // Row-level limit checkbox: checking it checks all child checkboxes in the row
        $(document).on('change', '.tp-limit-row-checkbox', function () {
            var $row = $(this).closest('tr');
            var isChecked = $(this).is(':checked');
            if (isChecked) {
                // Row checked: check all child checkboxes in this row, uncheck All
                $row.find('.tp-limit-checkbox').prop('checked', true);
                $('#tp-limit-all').prop('checked', false);
            } else {
                // Row unchecked: uncheck child checkboxes in this row
                $row.find('.tp-limit-checkbox').prop('checked', false);
                // If nothing is checked anywhere, default back to All
                var any = $('.tp-limit-checkbox').not('#tp-limit-all').filter(':checked').length
                        + $('.tp-limit-row-checkbox:checked').length;
                if (!any) $('#tp-limit-all').prop('checked', true);
            }
            updateNikayaRow();
        });

        // Search form submit
        $(document).on('submit', '#tp-search-form', function (e) {
            e.preventDefault();
            currentFilter = '';  // new search resets filter
            doSearch($tpSearchInput ? $tpSearchInput.val() : $('#tp-search-input').val(), 0, '');
        });

        // Search button click
        $(document).on('click', '#tp-search-btn', function () {
            currentFilter = '';
            doSearch($tpSearchInput ? $tpSearchInput.val() : $('#tp-search-input').val(), 0, '');
        });

        // Inline proximity syntax is handled in doSearch(): use `termA /N termB` in the main input

        // Enter key
        $(document).on('keydown', '#tp-search-input', function (e) {
            if (e.keyCode === 13) {
                e.preventDefault();
                currentFilter = '';
                doSearch($tpSearchInput ? $tpSearchInput.val() : $(this).val(), 0, '');
            }
        });

        // Clear input only (do not close the search bar)
        $(document).on('click', '#tp-search-clear', function () {
            $('#tp-search-input').val('');
            // hide the clear button until there's input again
            $('#tp-search-clear').hide();
            // remove the Back button since it only applies to the current search
            $('#tp-search-back').remove();
            // keep current results and filters intact; refocus input for convenience
            $('#tp-search-input').focus();
        });

        // Pali character buttons
        $(document).on('click', '.tp-pali-btn', function () {
            insertPaliChar($(this).data('char'));
        });

        // ── Bookmark: star toggle on result item ──
        $(document).on('click', '.tp-bookmark-toggle', function(e) {
            e.preventDefault();
            var $a = $(this);
            var href = $a.data('href') || '';
            var id = $a.data('id') || $a.closest('.tp-result-title').find('.tp-open-newtab').attr('data-id') || '';
            // If id still missing, look it up from the tree map in real time
            if (!id && href) {
                id = _treeHrefToId[href] || _treeHrefToId[href.split('/').pop()] || '';
            }
            var title = $a.data('title') || '';
            var sectionId = $a.data('section') || '';
            var bms = loadBookmarks();
            var idx = -1;
            for (var i = 0; i < bms.length; i++) { if (bms[i].href === href) { idx = i; break; } }
            var $icon = $a.find('i');
            if (idx >= 0) {
                bms.splice(idx, 1);
                $icon.removeClass('fa-star tp-bm-starred').addClass('fa-star-o');
            } else {
                var bmQuery = $a.hasClass('tp-subhead-bm') ? '' : (currentQuery || '');
                bms.push({ href: href, id: id, title: title, query: bmQuery, isDeva: currentIsDeva ? 1 : 0, sectionId: sectionId });
                $icon.removeClass('fa-star-o').addClass('fa-star tp-bm-starred');
            }
            saveBookmarks(bms);
            updateBookmarkIcon();
            renderBookmarkDropdown();
        });

        // ── Bookmark: topnav icon toggles dropdown ──
        $(document).on('click', '#tp-topbar-bookmark-icon', function(e) {
            e.preventDefault();
            e.stopPropagation();
            var $icon = $(this);
            if (!$('#tp-bookmark-dropdown').length) {
                $('body').append('<div id="tp-bookmark-dropdown"></div>');
            }
            var $dd = $('#tp-bookmark-dropdown');
            if ($dd.is(':visible')) {
                $dd.hide();
                return;
            }
            renderBookmarkDropdown();
            var offset = $icon.offset();
            $dd.css({
                top: offset.top + $icon.outerHeight(),
                right: $(window).width() - offset.left - $icon.outerWidth()
            }).show();
        });

        // ── Bookmark: close dropdown when clicking outside ──
        $(document).on('click.tpbookmark', function(e) {
            if (!$(e.target).closest('#tp-bookmark-dropdown, #tp-topbar-bookmark-icon').length) {
                $('#tp-bookmark-dropdown').hide();
            }
        });

        // ── Bookmark: remove entry from dropdown ──
        $(document).on('click', '.tp-bm-remove', function(e) {
            e.preventDefault();
            // Prevent the global outside-click handler from closing the dropdown
            e.stopPropagation();
            var href = $(this).data('href');
            var bms = loadBookmarks().filter(function(b) { return b.href !== href; });
            saveBookmarks(bms);
            $('.tp-bookmark-toggle[data-href="' + href + '"] i').removeClass('fa-star tp-bm-starred').addClass('fa-star-o');
            updateBookmarkIcon();
            renderBookmarkDropdown();
            // Keep the dropdown open and reposition it relative to the topbar icon
            var $icon = $('#tp-topbar-bookmark-icon');
            var $dd = $('#tp-bookmark-dropdown');
            if ($dd.length && $icon.length) {
                var offset = $icon.offset();
                $dd.css({
                    top: offset.top + $icon.outerHeight(),
                    right: $(window).width() - offset.left - $icon.outerWidth()
                }).show();
            }
        });

        // ── Bookmark: open title in new tab from dropdown ──
        $(document).on('click', '.tp-bm-open', function(e) {
            e.preventDefault();
            $('#tp-bookmark-dropdown').hide();
            var href = $(this).data('href') || '';
            var id = $(this).data('id') || '';
            var _pageBase = window.location.href.replace(/[^/]*$/, '');
            var _pageOrigin = window.location.origin;
            var bmQuery = $(this).data('query') || currentQuery || '';
            var bmIsDeva = ($(this).data('isdeva') === '1' || $(this).data('isdeva') === 1) ? true : currentIsDeva;
            var bmSectionId = $(this).data('section') || '';
            var encD = bmIsDeva ? '1' : '0';

            var persistBookmarkId = function(resolvedId) {
                if (!resolvedId) return;
                try {
                    var bms = loadBookmarks();
                    var changed = false;
                    for (var bi = 0; bi < bms.length; bi++) {
                        if (bms[bi].href === href && (!bms[bi].id || bms[bi].id === '')) {
                            bms[bi].id = resolvedId;
                            changed = true;
                        }
                    }
                    if (changed) saveBookmarks(bms);
                } catch (e2) {}
            };

            var resolveIdFromMap = function() {
                if (!href) return '';
                return _treeHrefToId[href] || _treeHrefToId[href.split('/').pop()] || '';
            };

            var openBookmarkTarget = function(finalId) {
                // Write query to localStorage so the new tab can pick it up immediately on load.
                if (bmQuery) {
                    try { localStorage.setItem('tpsearch-newtab-query', bmQuery); } catch (err) {}
                    try { localStorage.setItem('tpsearch-newtab-isdeva', encD); } catch (err) {}
                }
                // Write section id so the new tab can scroll to the bookmarked subheading.
                if (bmSectionId) {
                    try { localStorage.setItem('tpsearch-newtab-section', bmSectionId); } catch (err) {}
                } else {
                    try { localStorage.removeItem('tpsearch-newtab-section'); } catch (err) {}
                }

                // Prefer index.html#id so tpsearch.js is loaded in the new tab.
                var targetUrl = finalId ? (_pageBase + 'index.html#' + finalId) : (_pageBase + href.replace(/^\//, ''));
                if (!targetUrl) return;

                var newWin = window.open(targetUrl, '_blank');
                // postMessage as an additional fallback in case localStorage is unavailable
                if (newWin && bmQuery) {
                    var payload = { tpq: bmQuery, tpd: encD, id: finalId || '' };
                    var tries = 0;
                    var sendMsg = function() {
                        try { newWin.postMessage(payload, _pageOrigin); } catch (err) {}
                        if (++tries < 12) setTimeout(sendMsg, 300);
                    };
                    sendMsg();
                }
            };

            // First try current in-memory tree map
            if (!id && href) {
                id = resolveIdFromMap();
                if (id) persistBookmarkId(id);
            }

            if (id) {
                openBookmarkTarget(id);
                return;
            }

            // If still missing id, fetch tree.json lazily and try again before opening.
            var root = _scriptRoot;
            try {
                var p = window.location.pathname || '/';
                if (p.indexOf('/romn/') !== -1 || p.indexOf('/romn') === 0) root = 'romn';
                else if (p.indexOf('/deva/') !== -1 || p.indexOf('/deva') === 0) root = 'deva';
            } catch (e3) {}

            if (!root) {
                openBookmarkTarget('');
                return;
            }

            var treeUrl = null;
            try {
                var p2 = window.location.pathname || '/';
                var m2 = p2.match(/^(.*?\/(?:romn|deva))(?:\/|$)/);
                if (m2 && m2[1]) treeUrl = m2[1].replace(/\/+$/, '') + '/tree.json';
            } catch (e4) {}
            if (!treeUrl) treeUrl = '/' + root + '/tree.json';

            var afterTreeLoaded = function(data) {
                try { _buildTreeMap(data); } catch (e5) {}
                var mapped = resolveIdFromMap();
                if (mapped) persistBookmarkId(mapped);
                openBookmarkTarget(mapped || '');
            };

            $.getJSON(treeUrl).done(function(data) {
                afterTreeLoaded(data);
            }).fail(function() {
                var alt = '/tipitaka.org/' + root + '/tree.json';
                if (alt === treeUrl) {
                    openBookmarkTarget('');
                    return;
                }
                $.getJSON(alt).done(function(data2) {
                    afterTreeLoaded(data2);
                }).fail(function() {
                    openBookmarkTarget('');
                });
            });
        });
    });

})(jQuery);
