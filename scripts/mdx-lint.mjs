#!/usr/bin/env node
/**
 * Fast MDX hazard linter.
 *
 * `astro build` is the real gate, but it writes to dist/ and takes seconds,
 * which makes it useless for parallel content agents (they race on dist/ and
 * only find out at the very end). This catches the one class of mistake that
 * actually breaks these builds — a raw `<` or `{` in prose, which MDX parses
 * as JSX — per file, in milliseconds, without touching dist/.
 *
 *   node scripts/mdx-lint.mjs                    # every .mdx under src/pages
 *   node scripts/mdx-lint.mjs src/pages/a/b.mdx  # specific files
 *
 * Exits non-zero on findings.
 */
import { readFileSync, globSync } from 'node:fs';

/** Tags that may legally appear as a raw `<` in MDX prose. */
const ALLOWED_TAGS = new Set([
  // site components
  'Callout', 'Watch', 'Cells', 'ProblemList', 'Solution', 'Viz',
  // html that shows up in article bodies
  'br', 'sup', 'sub', 'kbd', 'strong', 'em', 'code', 'pre', 'details', 'summary',
  'table', 'thead', 'tbody', 'tr', 'th', 'td', 'ul', 'ol', 'li', 'p', 'div', 'span', 'a', 'img',
]);

/**
 * Character scanner over the whole file. Tracks the four regions where a `<`
 * or `{` is legal — frontmatter, fenced code, inline code spans, and the
 * inside of a JSX tag (props are JS, and may span lines) — and reports the
 * ones left over in prose.
 */
function lintFile(file) {
  const src = readFileSync(file, 'utf8');
  const findings = [];
  const lines = src.split('\n');

  let inFence = false;
  let inFrontmatter = lines[0]?.trim() === '---';
  /** depth of nested JSX tags we are currently *inside the angle brackets* of */
  let jsxDepth = 0;
  let quote = null; // ' or " while inside a JSX prop string
  let braceDepth = 0; // {} depth inside a JSX prop
  /** a `code span` may wrap across lines, so this persists between them */
  let inInlineCode = false;

  const add = (lineNo, rule, hint, text) =>
    findings.push({ file, line: lineNo, rule, hint, text: text.trim() });

  for (let li = 0; li < lines.length; li++) {
    const line = lines[li];
    const lineNo = li + 1;
    const trimmed = line.trimStart();

    // frontmatter
    if (inFrontmatter) {
      if (li > 0 && trimmed === '---') inFrontmatter = false;
      continue;
    }
    // fences
    if (jsxDepth === 0 && (trimmed.startsWith('```') || trimmed.startsWith('~~~'))) {
      inFence = !inFence;
      continue;
    }
    if (inFence) continue;

    // site conventions (prose lines only)
    if (jsxDepth === 0) {
      if (/^#\s/.test(line)) {
        add(lineNo, 'h1', 'no `#` h1 in MDX — the layout renders the title; start at `##`', line);
      }
      if (/^#{2,}\s*\/\//.test(line)) {
        add(lineNo, 'typed-slashes', 'never type `// ` in a heading — CSS injects the prefix', line);
      }
    }

    if (trimmed === '') inInlineCode = false; // a blank line ends any span
    for (let i = 0; i < line.length; i++) {
      const c = line[i];

      // ---- inside a JSX tag: props are JS, skip to the closing `>` --------
      if (jsxDepth > 0) {
        if (quote) {
          if (c === quote) quote = null;
          continue;
        }
        if (c === '"' || c === "'") { quote = c; continue; }
        if (c === '{') { braceDepth++; continue; }
        if (c === '}') { braceDepth = Math.max(0, braceDepth - 1); continue; }
        if (c === '>' && braceDepth === 0) jsxDepth--;
        continue;
      }

      // ---- prose --------------------------------------------------------
      if (c === '`') { inInlineCode = !inInlineCode; continue; }
      if (inInlineCode) continue;

      if (c === '<') {
        const rest = line.slice(i + 1);
        const tag = rest.match(/^\/?([A-Za-z][A-Za-z0-9]*)/);
        if (!tag) {
          add(lineNo, 'raw-lt', 'a `<` not starting a tag (e.g. `i < n`) — wrap it in backticks or a fence', line);
        } else if (!ALLOWED_TAGS.has(tag[1])) {
          add(lineNo, 'unknown-tag',
            `"<${tag[1]}" reads as a JSX tag — if it is a generic like Dictionary<K,V>, wrap the whole expression in backticks`, line);
        } else {
          // a legal tag: enter tag-region until its `>`
          jsxDepth++;
          i += tag[0].length; // skip the name; the scanner handles the rest
        }
        continue;
      }

      if (c === '{') {
        add(lineNo, 'raw-brace',
          'a `{` outside a component prop — MDX evaluates it as an expression; wrap it in backticks', line);
      }
    }
  }

  return findings;
}

const argv = process.argv.slice(2);
const files = argv.length ? argv : globSync('src/pages/**/*.mdx');
const all = files.flatMap(lintFile);

if (all.length === 0) {
  console.log(`mdx-lint: ${files.length} file(s) clean`);
  process.exit(0);
}
for (const f of all) {
  console.log(`${f.file}:${f.line}  [${f.rule}] ${f.hint}\n    ${f.text}`);
}
console.log(`\nmdx-lint: ${all.length} finding(s) in ${new Set(all.map((f) => f.file)).size} file(s)`);
process.exit(1);
