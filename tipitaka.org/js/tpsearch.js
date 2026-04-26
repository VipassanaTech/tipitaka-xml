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
    var currentTotal = 0;
    var currentFilter = '';       // active volume filter (fq), empty = all
    var lastFacets = {};          // cached facets from the most recent unfiltered search
    var unfilteredTotal = 0;      // numFound without any filter
    var currentIsDeva = false;    // whether current query is Devanagari
    var currentExpandedFacet = ''; // which top-level facet is currently expanded (not applied)
    // Cached jQuery objects (initialized on document ready)
    var $tContent = null;
    var $tpSearchInput = null;
    var $tpSearchClear = null;
    var __delegationBound = false;

    // Simple debounce helper
    function debounce(fn, wait) {
        var t = null;
        function wrapper() {
            var ctx = this, args = arguments;
            clearTimeout(t);
            t = setTimeout(function () { fn.apply(ctx, args); }, wait);
        }
        wrapper.cancel = function() { clearTimeout(t); t = null; };
        return wrapper;
    }

    // Quick check if a string contains Devanagari characters
    function isDevanagari(s) {
        return /[\u0900-\u097F]/.test(s);
    }

    // (removed unused getScriptFolder)

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
        html += '<div id="tp-limit-search" class="tp-limit-search">';
        html += '<div class="tp-limit-title" style="font-weight:600; margin-bottom:6px;">Limit Search</div>';
        // list of checkboxes; 'Anya' (Other texts) displayed as 'Añña'
        html += '<div class="tp-limit-list">';
        // Column headings (top row)
        html += '<div class="tp-limit-head tp-limit-head-1">Mula</div>';
        html += '<div class="tp-limit-head tp-limit-head-2">Attha.</div>';
        html += '<div class="tp-limit-head tp-limit-head-3">Tika</div>';
        // Mula column: Vinaya / Sutta / Abdhi
        html += '<label class="limit-item item-mula-vinaya"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-mula-vinaya" data-val="Vinayapitaka" /> Vinaya</label>';
        html += '<label class="limit-item item-mula-sutta"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-mula-sutta" data-val="Suttapitaka" /> Sutta</label>';
        html += '<label class="limit-item item-mula-abhd"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-mula-abhd" data-val="Abhidhammapitaka" /> Abdhi</label>';
        // Attha. column: Vinaya / Sutta / Abdhi (aṭṭhakathā)
        html += '<label class="limit-item item-atth-vinaya"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-atth-vinaya" data-val="Vinayapiṭaka (aṭṭhakathā)" /> Vinaya</label>';
        html += '<label class="limit-item item-atth-sutta"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-atth-sutta" data-val="Suttapiṭaka (aṭṭhakathā)" /> Sutta</label>';
        html += '<label class="limit-item item-atth-abhd"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-atth-abhd" data-val="Abhidhammapiṭaka (aṭṭhakathā)" /> Abdhi</label>';
        // Tika column: Vinaya / Sutta / Abdhi (ṭīkā)
        html += '<label class="limit-item item-tika-vinaya"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-tika-vinaya" data-val="Vinayapiṭaka (ṭīkā)" /> Vinaya</label>';
        html += '<label class="limit-item item-tika-sutta"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-tika-sutta" data-val="Suttapiṭaka (ṭīkā)" /> Sutta</label>';
        html += '<label class="limit-item item-tika-abhd"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-tika-abhd" data-val="Abhidhammapiṭaka (ṭīkā)" /> Abdhi</label>';
        // Other options
        html += '<label class="limit-item item-anya"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-anya" data-val="Anya" /> Añña</label>';
        html += '<label class="limit-item item-all"><input type="checkbox" class="tp-limit-checkbox" id="tp-limit-all" data-val="ALL" style="margin-top:6px;" checked /> All</label>';
        html += '</div>'; // tp-limit-list
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
        html += '      <div class="tp-prox-mode" style="display:inline-block; margin-left:8px; font-size:13px; color:#1E3461;">';
        html += '        <span style="margin-right:6px;">Proximity:</span>';
        html += '        <label style="margin-right:6px;"><input type="radio" name="tp-prox-mode" id="tp-prox-strict" value="strict" checked /> As-is</label>';
        html += '        <label style="margin-right:6px;"><input type="radio" name="tp-prox-mode" id="tp-prox-any" value="any" /> Any</label>';
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
        html += '<li>Typing in the proper Pāḷi characters is not necessary. Searching for vipassanā or vipassana will produce the same results.</li>';
        html += '<li>To only search for part of a word use * to complete the search term. For example, searching for dhammacakka* will find all instances that start with dhammacakka.</li>';
        html += '<li>For proximity search, place /n between the two terms to search, e.g. metta /2 mudita will find instances where metta and mudita are within 2 words of each other.</li>';
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

    // Toggle wrapper that chooses open/minimize based on current visibility
    function toggleSearchBar() {
        var $bar = $('#tp-search-bar');
        if ($bar.is(':visible')) minimizeSearchBar();
        else openSearchBar();
    }

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

        // Determine which facet fields and pivot key to request based on the
        // currently-applied filter so we retrieve the next hierarchical level.
        var facetFields = ['volume'];
        var pivotKey = 'volume,pitaka';
        if (currentFilter) {
            var _pf = parseFilter(currentFilter);
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
                // If user selected unordered/either-order, build an OR of both phrase directions
                var unordered = false;
                try {
                    var _pm = (document.querySelector('input[name="tp-prox-mode"]:checked') || {}).value || 'strict';
                    unordered = (_pm === 'any');
                } catch (e) { unordered = false; }
                if (unordered) {
                    var q1 = 'text:"' + escq(termA) + ' ' + escq(termB) + '"~' + dist;
                    var q2 = 'text:"' + escq(termB) + ' ' + escq(termA) + '"~' + dist;
                    qparam = '(' + q1 + ' OR ' + q2 + ')';
                } else {
                    // Use the `text` field (used for highlighting) with phrase slop (ordered)
                    qparam = 'text:"' + escq(termA) + ' ' + escq(termB) + '"~' + dist;
                }
            } else if (exactMatch) {
                // Use fielded phrase query for an exact match on the `title_exact` field
                var escq2 = query.replace(/"/g, '\\"');
                qparam = 'field_exact:"' + escq2 + '"';
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

        // Apply limit search filters (if any)
        try {
            var limitFq = buildLimitFq();
            if (limitFq) {
                // Combine with existing filter (currentFilter) if present
                if (params.fq) params.fq = '(' + params.fq + ') AND (' + limitFq + ')';
                else params.fq = '(' + limitFq + ')';
            }
        } catch (e) { /* ignore */ }

        // Apply volume or field-prefixed filter if set
        if (currentFilter) {
            var pf = parseFilter(currentFilter);
            if (pf.value) {
                params.fq = pf.field + ':"' + pf.value + '"';
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
        // Build OR expression on the `volume` field
        var parts = [];
        for (var i = 0; i < vals.length; i++) {
            parts.push('volume:"' + vals[i] + '"');
        }
        if (!parts.length) return null;
        return parts.join(' OR ');
    }

    // Fetch pivot-only counts when the main (paged) response doesn't include pivot data.
    // This performs a lightweight rows=0 request with the same filter so we can render
    // accurate subfacet counts for the UI.
    function fetchPivotCounts(filterStr) {
        var pf = parseFilter(filterStr || currentFilter);
        // Choose facet fields / pivot pair that correspond to the next-level
        // children for the provided parent filter.
        var facetFields = ['volume'];
        var pivotKey = 'volume,pitaka';
        if (pf && pf.field) {
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
        if (pf && pf.value) {
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
                        var parentVal = pf.value || '';
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
        currentTotal = numFound;

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
        if (currentFilter) {
            // Show a small inline loading indicator while we fetch pivot counts
            $sub.html('<div class="tp-sub-facets-loading"><i class="fa fa-spinner fa-spin"></i> Loading…</div>');

            // Prefer pivot facet data for hierarchical counts if present in this response
            var children = [];
            var pf = parseFilter(currentFilter);
            var childField = (pf.field === 'pitaka') ? 'book' : (pf.field === 'book') ? 'chapter' : 'pitaka';
            var pivotKey = (pf.field === 'pitaka') ? 'pitaka,book' : (pf.field === 'book') ? 'book,chapter' : 'volume,pitaka';
            var pivotData = null;
            if (data.facet_counts && data.facet_counts.facet_pivot) {
                pivotData = data.facet_counts.facet_pivot[pivotKey] || data.facet_counts.facet_pivot[pivotKey.replace(',', ', ')];
            }
            if (Array.isArray(pivotData) && pivotData.length) {
                // Find the pivot entry matching the current parent value
                var parentVal = pf.value || currentFilter || '';
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
                var pf = parseFilter(currentFilter);
                if (pf.field === 'chapter') {
                    $sub.empty();
                    currentExpandedFacet = '';
                } else {
                    renderSubfacetsHtml(currentFilter, children);
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
            var active = (currentFilter === filterKey) ? ' tp-facet-active' : '';
            html += '<a href="javascript:void(0)" class="tp-facet-item' + active + '" data-filter="' + escapeHtml(filterKey) + '">';
            html += escapeHtml(c.key) + ' (' + c.count + ')</a>';
        }

        html += '</div>';
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

    // Re-bind result link, facet and pagination events after dynamic content replacement
    function rebindSearchEvents() {
        // Delegated handlers are used; no per-element re-binding needed here.
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
        var flags = 'g' + (isDeva ? 'iu' : 'iu');
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

        // Build context menu
        buildContextMenu();

        // Cache commonly used jQuery selectors and bind delegated handlers
        $tpSearchInput = $('#tp-search-input');
        $tContent = $('#t-content');
        $tpSearchClear = $('#tp-search-clear');

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

        // Limit Search checkbox behavior: 'All' toggles others
        $(document).on('change', '.tp-limit-checkbox', function () {
            var $all = $('#tp-limit-all');
            if ($(this).attr('id') === 'tp-limit-all') {
                if ($(this).is(':checked')) {
                    $('.tp-limit-checkbox').not(this).prop('checked', false);
                }
            } else {
                if ($(this).is(':checked')) {
                    $all.prop('checked', false);
                } else {
                    // If none selected, default back to All checked
                    var any = $('.tp-limit-checkbox').not('#tp-limit-all').filter(':checked').length;
                    if (!any) $all.prop('checked', true);
                }
            }
        });

        // Search form submit
        $(document).on('submit', '#tp-search-form', function (e) {
            e.preventDefault();
            if (typeof debouncedSearch !== 'undefined' && debouncedSearch.cancel) debouncedSearch.cancel();
            currentFilter = '';  // new search resets filter
            doSearch($tpSearchInput ? $tpSearchInput.val() : $('#tp-search-input').val(), 0, '');
        });

        // Search button click
        $(document).on('click', '#tp-search-btn', function () {
            if (typeof debouncedSearch !== 'undefined' && debouncedSearch.cancel) debouncedSearch.cancel();
            currentFilter = '';
            doSearch($tpSearchInput ? $tpSearchInput.val() : $('#tp-search-input').val(), 0, '');
        });

        // Inline proximity syntax is handled in doSearch(): use `termA /N termB` in the main input

        // Enter key
        $(document).on('keydown', '#tp-search-input', function (e) {
            if (e.keyCode === 13) {
                e.preventDefault();
                if (typeof debouncedSearch !== 'undefined' && debouncedSearch.cancel) debouncedSearch.cancel();
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
    });

})(jQuery);
