/*
 * feature-banner.js — Scrolling feature-announcement banner
 * Displayed in all script folders (romn, deva, cyrl, etc.). Not shown on main index.html.
 *
 * HOW TO UPDATE FOR A NEW FEATURE ANNOUNCEMENT
 * ─────────────────────────────────────────────
 * 1. Update the message text
 *    Change the `msg` variable below to reflect the new announcement.
 *
 * 2. Change the localStorage key (BANNER_KEY)
 *    Use a new unique name, e.g. 'dpd_banner_seen_v2', so the banner
 *    reappears for users who already dismissed the previous announcement.
 *
 * 3. Update the expiry date (EXPIRY)
 *    Set a new date after which the banner will never show, regardless
 *    of whether the user has seen or dismissed it.
 *    Format: new Date('YYYY-MM-DD').getTime()
 */

(function () {
    var BANNER_KEY = 'dpd_banner_seen';
    var EXPIRY     = new Date('2026-06-13').getTime();

    if (Date.now() >= EXPIRY || localStorage.getItem(BANNER_KEY)) return;

    /* ── Inject CSS ─────────────────────────────────────────────────── */
    var style = document.createElement('style');
    style.textContent = [
        '#feature-banner{display:none;background:#fff3cd;color:#3a2500;border-bottom:2px solid #c8860a;',
        'width:100%;box-sizing:border-box;overflow:hidden;align-items:center;height:1.8rem;font-size:14px;}',
        '#feature-banner.visible{display:flex;}',
        'body.banner-visible{padding-top:calc(50px + 1.8rem);}',
        /* Mobile: shift the tree panel and its toggle button down by banner height */
        '@media(max-width:768px){',
        'body.banner-visible #tree{top:calc(130px + 1.8rem);}',
        'body.banner-visible #nav-toggle{top:calc(60px + 1.8rem);}',
        '#feature-banner{position:relative;}',
        '#feature-banner-close{position:absolute;right:1.5rem;top:50%;transform:translateY(-50%);}',
        '}',
        '@keyframes featureBannerScroll{0%{transform:translateX(0);}100%{transform:translateX(-70%);}}'
    ].join('');
    document.head.appendChild(style);

    /* ── Build banner HTML ──────────────────────────────────────────── */
    var msg = '\uD83D\uDD14&nbsp;&nbsp;<strong>NEW FEATURE ALERT:</strong> '
            + 'You can now look up the meaning of a Pali word by simply '
            + '<strong>double clicking</strong> on it and its meaning will be '
            + 'displayed in a pop-up window, courtesy of '
            + '<strong>Digital Pali Dictionary</strong>.'
            + '&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;';

    var banner = document.createElement('div');
    banner.id = 'feature-banner';
    banner.innerHTML =
        '<div style="overflow:hidden;flex:1;height:100%;display:flex;align-items:center;">'
      +   '<div style="display:inline-block;white-space:nowrap;will-change:transform;'
      +        'animation:featureBannerScroll 28s linear infinite;">'
      +     '<span>' + msg + '</span><span>' + msg + '</span>'
      +   '</div>'
      + '</div>'
      + '<button id="feature-banner-close" style="flex-shrink:0;background:#c8860a;color:#fff;'
      +   'border:none;border-radius:4px;padding:0.2rem 0.9em 0.2em 0.4em;font-size:0.8rem;cursor:pointer;'
      +   'margin:0 0.6rem;white-space:nowrap;font-family:inherit;">Got it!</button>';

    /* ── Insert into nav-wrapper (directly after the topnav div) ────── */
    var navWrapper = document.querySelector('.nav-wrapper');
    if (!navWrapper) return;
    navWrapper.appendChild(banner);

    /* ── Show ───────────────────────────────────────────────────────── */
    banner.classList.add('visible');
    document.body.classList.add('banner-visible');

    /* ── Dismiss ────────────────────────────────────────────────────── */
    document.getElementById('feature-banner-close').addEventListener('click', function () {
        banner.classList.remove('visible');
        document.body.classList.remove('banner-visible');
        localStorage.setItem(BANNER_KEY, '1');
    });
})();
