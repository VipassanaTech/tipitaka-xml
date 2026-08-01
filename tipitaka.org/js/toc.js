// tipitaka.org ToC (Table of Contents) functionality
// Shows sutta count badges on tree leaf nodes and a popup modal with sutta list

// Detect script folder from URL (e.g. /deva/index.html → deva)
var _tocScriptDir = (function() {
    var path = window.location.pathname.replace(/\/index\.html$/, '').replace(/\/$/, '');
    var parts = path.split('/');
    return parts[parts.length - 1] || 'romn';
})();

// Load sutta counts data (per-script file)
var suttaCounts = {};
var _tocDataReady = false;
$.getJSON('./' + _tocScriptDir + '_subheads.json', function(data) {
    suttaCounts = data;
    _tocDataReady = true;
    addTocToggles();
}).fail(function() {
    console.log(_tocScriptDir + '_subheads.json not loaded');
});

// Add ToC toggle icons to all visible leaf nodes (only when count > 1)
function addTocToggles() {
    // Wait until data is loaded
    if (!_tocDataReady) {
        setTimeout(addTocToggles, 200);
        return;
    }
    var tree = $('#tree').jstree(true);
    // Wait until tree is initialized
    if (!tree) {
        setTimeout(addTocToggles, 200);
        return;
    }
    var allNodes = tree.get_json('#', { flat: true });
    $(allNodes).each(function(i, node) {
        if (node.type === 'leaf' && node.id && suttaCounts[node.id] !== undefined) {
            var count = suttaCounts[node.id].count;
            if (count <= 1) return; // skip 0 or 1 — not useful
            var anchor = $('#' + node.id + '_anchor');
            if (anchor.length && anchor.find('.toc-toggle').length === 0) {
                var tocHtml = '<span class="toc-toggle" data-node-id="' + node.id + '">' +
                    '<span class="toc-arrow"></span>' +
                    '<span class="toc-num">' + count + '</span>' +
                    '</span>';
                anchor.append(tocHtml);
            }
        }
    });
}

// Ensure subhead elements in content have IDs for scroll navigation
function ensureSubheadIds() {
    $('#t-content .subhead').each(function(i) {
        if (!$(this).attr('id')) {
            $(this).attr('id', 'subhead-' + i);
        }
    });
}

// Show ToC modal with sutta list
function showTocModal($toggle, subheads, nodeId) {
    var $modal = $('#toc-modal');
    var $list = $('#toc-list');
    var $title = $('#toc-title');

    $modal.data('node-id', nodeId || '');

    $title.text('Sections (' + subheads.length + ')');
    $list.empty();

    if (subheads.length === 0) {
        $list.append('<li class="toc-empty">No suttas found</li>');
    } else {
        $(subheads).each(function(i, sutta) {
            // Build list item via jQuery to avoid HTML-escaping issues
            var $li = $('<li></li>').text(sutta);
            $list.append($li);
        });
    }

    var offset = $toggle.offset();
    var sidebarOffset = $('#tree').offset();
    var sidebarWidth = $('#tree').width();
    var sidebarRight = sidebarOffset.left + sidebarWidth;
    var modalWidth = Math.min(350, sidebarWidth - 10);
    $modal.css({
        top: (offset.top + 20) + 'px',
        left: Math.max(5, sidebarRight - modalWidth - 5) + 'px',
        maxWidth: modalWidth + 'px'
    });

    $('#toc-overlay').show();
    $modal.show();
}

// Close modal
function closeTocModal() {
    $('#toc-modal').hide();
    $('#toc-overlay').hide();
}

// Find a subhead element in the content pane by its text
function findSubheadByText(text) {
    var $match = $();
    // Try multiple possible selectors that might represent subheads
    $('#t-content .subhead, #t-content p[rend="subhead"], #t-content p.subhead').each(function() {
        if ($(this).text().trim() === text) {
            $match = $(this);
            return false;
        }
    });
    return $match;
}

// Scroll to a subhead element and highlight it
function scrollToSubhead($el) {
    if (!$el.length) return;
    if (!$el.attr('id')) {
        $el.attr('id', 'subhead-' + Date.now());
    }
    var $content = $('#t-content');
    var scrollTarget = $el.offset().top - $content.offset().top + $content.scrollTop() - 20;
    $content.animate({ scrollTop: scrollTarget }, 300);
    $el.css({backgroundColor: '#fff3cd'});
    setTimeout(function() {
        $el.css({backgroundColor: ''});
    }, 2000);
}

// Set up event handlers on DOM ready
$(document).ready(function() {

    // Re-add ToC toggles when tree nodes are opened
    $('#tree').on('open_node.jstree', function(e, data) {
        setTimeout(addTocToggles, 200);
    });

    // Handle ToC toggle click — open modal with sutta list from pre-computed data
    $(document).on('click', '.toc-toggle', function(e) {
        e.preventDefault();
        e.stopPropagation();

        var nodeId = $(this).data('node-id');
        var $toggle = $(this);
        var data = suttaCounts[nodeId];

        if (data && data.subheads && data.subheads.length > 0) {
            showTocModal($toggle, data.subheads, nodeId);
        } else {
            // Fallback: try to extract from loaded DOM content
            var subheads = [];
            $('#t-content .subhead').each(function() {
                var text = $(this).text().trim();
                if (text) subheads.push(text);
            });
            if (subheads.length === 0) {
                var anchor = $('#' + nodeId + '_anchor');
                var href = anchor.attr('href');
                if (href && href !== '#') {
                    $('#t-content').load(href, function() {
                        var sh = [];
                        $('#t-content .subhead').each(function() {
                            var t = $(this).text().trim();
                            if (t) sh.push(t);
                        });
                        showTocModal($toggle, sh, nodeId);
                    });
                    return;
                }
            }
            showTocModal($toggle, subheads, nodeId);
        }
    });

    // Close modal handlers
    $('#toc-close').on('click', closeTocModal);
    $('#toc-overlay').on('click', closeTocModal);

    // Handle sutta click in modal — find and scroll to that subhead in content
    $(document).on('click', '#toc-list li:not(.toc-empty)', function() {
        var suttaText = $(this).text().trim();
        var $modal = $('#toc-modal');
        var nodeId = $modal.data('node-id');
        closeTocModal();

        function tryScroll() {
            var $target = findSubheadByText(suttaText);
            // Broad fallback: search all <p> elements in content
            if (!$target.length) {
                $('#t-content p').each(function() {
                    if ($(this).text().trim() === suttaText) {
                        $target = $(this);
                        return false;
                    }
                });
            }
            if ($target.length) {
                scrollToSubhead($target);
                return true;
            }
            return false;
        }

        if (tryScroll()) return;

        // Content not matching — load the correct leaf content, then retry
        if (nodeId) {
            var anchor = $('#' + nodeId + '_anchor');
            var href = anchor.attr('href');
            if (href && href !== '#') {
                $('#t-content').load(href, function() {
                    tryScroll();
                });
            }
        }
    });

});
