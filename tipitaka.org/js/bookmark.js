/**
 * Tipitaka Bookmark Module
 * Provides bookmark functionality for script folder pages.
 * Allows users to bookmark pages and subheadings, and navigate to them.
 *
 * This JS is only loaded on script pages (deva/, romn/, etc.), so the bookmark
 * icon in the top bar will only appear there.
 */
(function ($) {
    'use strict';

    // Cached jQuery objects (initialized on document ready)
    var $tContent = null;

    // HTML escape helper
    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
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
            var bmLabel = escapeHtml(b.title);
            html += '<a href="#" class="tp-bm-open" data-href="' + escapeHtml(b.href) + '" data-id="' + escapeHtml(b.id || '') + '" data-section="' + escapeHtml(b.sectionId || '') + '">' + bmLabel + '</a>';
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

    // Inject bookmark icon into the top navigation bar
    function injectTopBarBookmarkIcon() {
        var attempts = 0;
        var timer = setInterval(function () {
            var $nav = $('#myTopnav');
            if ($nav.children().length > 0) {
                $('#tp-topbar-bookmark-icon').remove();
                var $links = $nav.find('a');
                var $dropdowns = $nav.find('.dropdown');
                var $home = $links.filter(function () { return $(this).text().trim() === 'Home'; }).first();
                var bmIcon = document.createElement('a');
                bmIcon.href = 'javascript:void(0)';
                bmIcon.id = 'tp-topbar-bookmark-icon';
                bmIcon.title = 'Add or remove bookmarks';
                bmIcon.innerHTML = '<i class="fa fa-bookmark-o"></i>';
                var $github = $links.filter(function () { return $(this).text().trim() === 'GitHub Repo'; }).first();
                if ($github.length) {
                    $github[0].parentNode.insertBefore(bmIcon, $github[0].nextSibling);
                } else if ($home.length && $dropdowns.length) {
                    $nav[0].insertBefore(bmIcon, $dropdowns[0]);
                } else if ($home.length) {
                    $home[0].parentNode.insertBefore(bmIcon, $home[0].nextSibling);
                } else {
                    $nav[0].insertBefore(bmIcon, $nav[0].firstChild);
                }
                $(bmIcon).show();
                updateBookmarkIcon();
                clearInterval(timer);
            }
            if (++attempts > 50) clearInterval(timer);
        }, 100);
    }

    // ───── Initialisation ─────
    $(document).ready(function () {
        // Ensure external tpsearch stylesheet is loaded
        if (!document.querySelector('link[href*="tpsearch.css"]')) {
            var link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = '/css/tpsearch.css';
            document.head.appendChild(link);
        }

        // Cache jQuery selectors
        $tContent = $('#t-content');

        // Inject bookmark icon into top nav
        injectTopBarBookmarkIcon();

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
            $('.tp-bookmark-toggle[data-href]').each(function() {
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
        (function loadTreeForKnownScripts() {
            var root = null;
            try {
                var p = window.location.pathname || '/';
                if (p.indexOf('/romn/') !== -1 || p.indexOf('/romn') === 0) root = 'romn';
                else if (p.indexOf('/deva/') !== -1 || p.indexOf('/deva') === 0) root = 'deva';
                else if (_scriptRoot && (_scriptRoot === 'romn' || _scriptRoot === 'deva')) root = _scriptRoot;
            } catch (e) {}
            if (!root) return;
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

        // Insert bookmark icons for subheads inside loaded content and
        // provide a MutationObserver to re-run when content is dynamically replaced.
        function insertSubheadBookmarkIcons() {
            try {
                var $cont = $tContent || $('#t-content');
                if (!$cont || !$cont.length) return;
                var bookmarks = loadBookmarks();
                $cont.find('.subhead, [rend="subhead"]').each(function (idx) {
                    var $h = $(this);
                    if ($h.find('.tp-subhead-bm').length) return;
                    var hid = $h.attr('id');
                    if (!hid) {
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
                    var _pageTreeId = '';
                    try {
                        var _currPath = (window.location.pathname || '').replace(/^\//, '');
                        _pageTreeId = (_treeHrefToId && (_treeHrefToId[_currPath] || _treeHrefToId[_currPath.split('/').pop()])) || '';
                        if (!_pageTreeId) {
                            var _hashMatch = (window.location.hash || '').match(/^#(\d+)$/);
                            if (_hashMatch) _pageTreeId = _hashMatch[1];
                        }
                    } catch (e3) {}
                    $a.attr('data-id', _pageTreeId);
                    $a.attr('data-section', hid);
                    $a.attr('data-title', title);
                    $a.attr('title', 'Bookmark this section');
                    $a.attr('aria-label', 'Bookmark this section');
                    $a.html('<i class="fa fa-star-o" aria-hidden="true"></i>');
                    $h.append('\u00A0').append($a);
                    for (var bi = 0; bi < bookmarks.length; bi++) {
                        var bb = bookmarks[bi] || {};
                        var bh = (bb.href || '');
                        var bhNorm = bh.replace(/^\//, '');
                        var hrefNorm = href.replace(/^\//, '');
                        var bhTail = (bh || '').split('/').pop();
                        if (bh === href || bhNorm === hrefNorm || bhTail === hrefNorm || bh === ('#' + hid)) {
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

        // ── Scroll to bookmarked subheading in new tab ──
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
                scrollToSection(50);
            }
        } catch (e) {}

        // ── Bookmark: star toggle on subheadings ──
        $(document).on('click', '.tp-bookmark-toggle', function(e) {
            e.preventDefault();
            var $a = $(this);
            var href = $a.data('href') || '';
            var id = $a.data('id') || '';
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
                bms.push({ href: href, id: id, title: title, query: '', isDeva: 0, sectionId: sectionId });
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
            var rect = $icon[0].getBoundingClientRect();
            $dd.css({
                top: rect.bottom,
                right: $(window).width() - rect.right
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
            e.stopPropagation();
            var href = $(this).data('href');
            var bms = loadBookmarks().filter(function(b) { return b.href !== href; });
            saveBookmarks(bms);
            $('.tp-bookmark-toggle[data-href="' + href + '"] i').removeClass('fa-star tp-bm-starred').addClass('fa-star-o');
            updateBookmarkIcon();
            renderBookmarkDropdown();
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
            var bmSectionId = $(this).data('section') || '';

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
                // Write section id so the new tab can scroll to the bookmarked subheading.
                if (bmSectionId) {
                    try { localStorage.setItem('tpsearch-newtab-section', bmSectionId); } catch (err) {}
                } else {
                    try { localStorage.removeItem('tpsearch-newtab-section'); } catch (err) {}
                }
                var targetUrl = finalId ? (_pageBase + 'index.html#' + finalId) : (_pageBase + href.replace(/^\//, ''));
                if (targetUrl) window.open(targetUrl, '_blank');
            };

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
