/**
 * build-xref-index.js
 *
 * Generates xref-index.json — a lookup table mapping each Mūla leaf page
 * (href) to its corresponding Aṭṭhakathā and Ṭīkā leaf pages.
 * 
 * This lookup table is needed so that user can open corresponding AT/TK pages 
 * from a MU page, and vice versa, without needing to manually open the pages. 
 *
 * The index is derived from tree.json by traversing the Mūla (id=1),
 * Aṭṭhakathā (id=825), and Ṭīkā (id=1549) sub-trees.
 * If the tree.json is changed, this script should be re-run to update the xref index file, which is located 
 * in romn/xref-index.json. 
 *
 * Aṭṭhakathā and Ṭīkā are processed independently:
 *
 * Strategy (applied identically for Att and Tik):
 *   1. Walk the Mūla tree and the commentary tree in parallel by child
 *      position at each INTERIOR folder level, PROVIDED the child counts
 *      match (or the commentary has more children from extras tacked at the
 *      end, e.g. Abhinavaṭīkā groups).
 *   2. When the child counts diverge significantly at a folder level
 *      (structural re-organisation, e.g. Tika Vinaya volumes vs Mula books),
 *      fall back to FLAT leaf matching for the entire sub-tree:
 *        - collect all leaves from both sub-trees in DFS order
 *        - offset = max(0, commentaryLeaves.length − mūlaLeaves.length)
 *          (accounts for intro leaves, e.g. Ganthārambhakathā, at the start)
 *        - mūlaLeaf[i] → commentaryLeaf[i + offset]
 *   3. At TERMINAL folders (folder whose direct children are all leaves)
 *      the same flat matching with offset is applied, but commentary
 *      leaves are collected recursively (handles Tika sub-volumes).
 *
 * Usage:
 *   node code/scripts/build-xref-index.js
 *
 * Output:
 *   tipitaka.org/romn/xref-index.json
 */

'use strict';

const fs   = require('fs');
const path = require('path');

// ---------------------------------------------------------------------------
// Load tree.json (UTF-16 LE with BOM)
// ---------------------------------------------------------------------------
const treeJsonPath = path.resolve(__dirname, '../../tipitaka.org/romn/tree.json');
console.log('Reading', treeJsonPath);
const buf  = fs.readFileSync(treeJsonPath);
const data = JSON.parse(buf.toString('utf16le').replace(/^\uFEFF/, ''));

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Find a node by numeric id (depth-first). */
function findById(node, id) {
    if (Array.isArray(node)) {
        for (const n of node) { const r = findById(n, id); if (r) return r; }
        return null;
    }
    if (!node || typeof node !== 'object') return null;
    if (node.id === id) return node;
    if (node.children) return findById(node.children, id);
    return null;
}

/** Collect all leaf nodes from a subtree in DFS order. */
function collectLeaves(node) {
    if (!node) return [];
    if (node.type === 'leaf') return [node];
    if (!node.children) return [];
    const result = [];
    for (const child of node.children) {
        for (const leaf of collectLeaves(child)) result.push(leaf);
    }
    return result;
}

/** True when every direct child of node is a leaf. */
function isTerminalFolder(node) {
    return node.children &&
           node.children.length > 0 &&
           node.children.every(c => c.type === 'leaf');
}

/** Build a ref object from a leaf node, or null. */
function leafRef(leaf) {
    if (!leaf) return null;
    const href = (leaf.a_attr || {}).href;
    if (!href) return null;
    return { href, id: leaf.id };
}

/**
 * Count leading "intro" leaves in a commentary leaf list.
 * These are leaves like "Ganthārambhakathā" / "Ganthārambhakathāvaṇṇanā"
 * that are prefixed to a section commentary but have no Mūla counterpart.
 * Only LEADING consecutive intro leaves are counted.
 */
const INTRO_PATTERN = /ganthārambha/i;
function countLeadingIntros(leaves) {
    let n = 0;
    for (const leaf of leaves) {
        if (INTRO_PATTERN.test(leaf.text || '')) n++;
        else break;
    }
    return n;
}

/**
 * Compute the effective offset to align commentary leaves with Mūla leaves.
 * Takes the larger of:
 *   - count-based offset (when commentary has more total leaves)
 *   - intro-based offset (intro leaves at the start of the commentary list)
 */
function computeOffset(mulLeaves, cmmLeaves) {
    const countOffset = Math.max(0, cmmLeaves.length - mulLeaves.length);
    const introOffset = countLeadingIntros(cmmLeaves);
    return Math.max(countOffset, introOffset);
}

/**
 * Prune a Ṭīkā node tree, keeping only leaves whose href contains '.tik'.
 * This removes alternative/secondary tikas (Abhinavaṭīkā, Vajirabuddhi-Ṭīkā,
 * Vimativinodanī-Ṭīkā, etc.) which use '.nrf' filenames, leaving only the
 * primary Ṭīkā content for each section.
 * Returns null if the node has no primary-tika leaves.
 */
function pruneToPrimaryTika(node) {
    if (!node) return null;
    if (node.type === 'leaf') {
        const href = (node.a_attr || {}).href || '';
        return href.includes('.tik') ? node : null;
    }
    if (!node.children) return node;
    const kept = node.children.map(pruneToPrimaryTika).filter(c => c !== null);
    if (kept.length === 0) return null;
    return Object.assign({}, node, { children: kept });
}

// ---------------------------------------------------------------------------
// Single-side traversal  (Mūla vs one commentary tree)
// Returns { mulHref → leafRef | null }
// ---------------------------------------------------------------------------
function buildSideMap(mulNode, cmmNode) {
    const sideMap = {};

    function traverse(mulNode, cmmNode) {
        if (!mulNode) return;

        // ---- leaf ----
        if (mulNode.type === 'leaf') {
            const mulHref = (mulNode.a_attr || {}).href;
            if (mulHref) {
                sideMap[mulHref] = (cmmNode && cmmNode.type === 'leaf')
                    ? leafRef(cmmNode) : null;
            }
            return;
        }

        const mulChildren = mulNode.children || [];
        if (mulChildren.length === 0) return;

        // ---- terminal folder (all direct children are leaves) ----
        if (isTerminalFolder(mulNode)) {
            // Collect commentary leaves RECURSIVELY in case the commentary
            // reorganised the same content into sub-volumes (e.g. vin01t1/t2).
            const mulLeaves = mulChildren;
            const cmmLeaves = cmmNode ? collectLeaves(cmmNode) : [];

            // Compute offset: skips intro leaves (e.g. Ganthārambhakathā) that
            // appear at the start of commentary sections with no Mūla counterpart.
            const offset = computeOffset(mulLeaves, cmmLeaves);

            mulLeaves.forEach((ml, i) => {
                const mulHref = (ml.a_attr || {}).href;
                if (mulHref) sideMap[mulHref] = leafRef(cmmLeaves[i + offset] || null);
            });
            return;
        }

        // ---- interior folder ----
        const cmmChildren = cmmNode ? (cmmNode.children || []) : [];
        const mulCount   = mulChildren.length;
        const cmmCount   = cmmChildren.length;

        // Structural match: commentary has the same number of children, OR has
        // MORE children, BUT the ratio is not too extreme (extras at the end
        // like Abhinavaṭīkā groups are fine; a 2.6× ratio like Tika Vinaya's
        // 13 volumes vs Mūla Vinaya's 5 books signals reorganisation instead).
        const withinRatio = cmmCount <= mulCount * 2;
        if (cmmCount >= mulCount && withinRatio) {
            mulChildren.forEach((mc, i) => traverse(mc, cmmChildren[i] || null));
            return;
        }

        // Structural DIVERGENCE (commentary has fewer top-level children, e.g.
        // Tika Vinaya's 3 volumes vs Mūla Vinaya's many books).
        // Fall back to flat leaf matching for this entire sub-tree.
        const mulLeaves = collectLeaves(mulNode);
        const cmmLeaves = cmmNode ? collectLeaves(cmmNode) : [];
        const offset    = computeOffset(mulLeaves, cmmLeaves);
        mulLeaves.forEach((ml, i) => {
            const mulHref = (ml.a_attr || {}).href;
            if (mulHref) sideMap[mulHref] = leafRef(cmmLeaves[i + offset] || null);
        });
    }

    traverse(mulNode, cmmNode);
    return sideMap;
}

// ---------------------------------------------------------------------------
// Run
// ---------------------------------------------------------------------------
const mulRoot = findById(data, 1);
const attRoot = findById(data, 825);
const tikRoot = findById(data, 1549);

if (!mulRoot) { console.error('ERROR: Mūla root (id=1) not found');        process.exit(1); }
if (!attRoot) { console.error('ERROR: Aṭṭhakathā root (id=825) not found'); process.exit(1); }
if (!tikRoot) { console.error('ERROR: Ṭīkā root (id=1549) not found');      process.exit(1); }

console.log('Building Aṭṭhakathā map…');
const attMap = buildSideMap(mulRoot, attRoot);

console.log('Building Ṭīkā map (primary .tik content only)…');
const filteredTikRoot = pruneToPrimaryTika(tikRoot);
const tikMap = buildSideMap(mulRoot, filteredTikRoot);

// Merge both side-maps into a single xref index.
// Also collect Mūla node IDs so the browser can build reverse lookups
// (Aṭṭhakathā → Mūla, Ṭīkā → Mūla).
const mulIds = {};
(function collectMulIds(node) {
    if (!node) return;
    if (node.type === 'leaf') {
        const href = (node.a_attr || {}).href;
        if (href) mulIds[href] = node.id;
        return;
    }
    (node.children || []).forEach(collectMulIds);
}(mulRoot));

const xrefMap = {};
let noAtt = 0, noTik = 0;

for (const href of Object.keys(attMap)) {
    xrefMap[href] = {
        mulId: mulIds[href] || null,
        att:   attMap[href],
        tik:   tikMap[href] || null
    };
    if (!attMap[href]) noAtt++;
    if (!tikMap[href]) noTik++;
}

const total = Object.keys(xrefMap).length;

// ---------------------------------------------------------------------------
// Write output
// ---------------------------------------------------------------------------
const outPath = path.resolve(__dirname, '../../tipitaka.org/romn/xref-index.json');
fs.writeFileSync(outPath, JSON.stringify(xrefMap, null, 0));

console.log(`\nDone.`);
console.log(`  Total Mūla leaves mapped : ${total}`);
console.log(`  Without Aṭṭhakathā match : ${noAtt}`);
console.log(`  Without Ṭīkā match       : ${noTik}`);
console.log(`  Output                   : ${outPath}`);
