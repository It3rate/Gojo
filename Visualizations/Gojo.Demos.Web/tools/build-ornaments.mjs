// Publish selected, recorded ornament studies. Does not execute or revise a study.
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { interiorsPublication } from './interiors-publication.mjs';
import { interiorOrnamentPublication } from './interior-ornament-publication.mjs';
const web = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repo = path.resolve(web, '../..');
const output = path.join(web, 'ornaments');
const check = process.argv.includes('--check');
const dryRun = process.argv.includes('--dry-run');
if (check && dryRun) throw new Error('Choose either --check or --dry-run');
const { collections } = JSON.parse(await fs.readFile(path.join(web, 'tools/ornament-collections.json'), 'utf8'));
const escape = value => String(value).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
const slash = value => value.replaceAll(path.sep, '/');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const key = value => path.resolve(value).toLowerCase();
function galleryAlias(route, title) {
  // Keep historical gallery.html URLs without duplicating a large captured page.
  // Both the selected case and fragment survive the move to the canonical route.
  const target = JSON.stringify(route).replaceAll('<', '\\u003c');
  return `<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${escape(title)} · Gojo</title><script>location.replace(${target}+location.search+location.hash)</script></head><body><p><a href="${escape(route)}">Open ${escape(title)}</a>.</p><noscript><p>JavaScript is needed to forward your selected case automatically. The collection link above opens the complete gallery.</p></noscript></body></html>\n`;
}
function inside(root, relative) {
  const resolved = path.resolve(root, relative);
  if (!resolved.startsWith(root + path.sep)) throw new Error('Path leaves publication scope: ' + relative);
  return resolved;
}
async function walk(dir) {
  const files = [];
  for (const item of await fs.readdir(dir, { withFileTypes: true })) {
    if (item.isSymbolicLink()) throw new Error('Unexpected symbolic link: ' + item.name);
    const file = path.join(dir, item.name);
    if (item.isDirectory()) files.push(...await walk(file));
    else files.push(file);
  }
  return files.sort();
}
const expected = new Map();
function emit(relative, content) {
  const target = inside(output, relative);
  if (expected.has(target)) throw new Error('Duplicate output: ' + relative);
  expected.set(target, Buffer.isBuffer(content) ? content : Buffer.from(content));
}
const cards = [], ids = new Set(), selections = [], publicUrls = new Map();
for (const collection of collections) {
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(collection.id) || ids.has(collection.id)) throw new Error('Invalid or duplicate collection id');
  ids.add(collection.id);
  const root = inside(repo, collection.source), route = '/ornaments/' + collection.id + '/';
  const sources = new Map();
  const include = collection.includeFiles?.map(pattern => new RegExp(pattern));
  async function addDirectory(source, destination, filtered = false) {
    for (const file of await walk(source)) {
      const relative = slash(path.relative(source, file));
      if (filtered && include && !include.some(pattern => pattern.test(relative))) continue;
      sources.set(file, destination + relative);
    }
  }
  await addDirectory(root, route, true);
  for (const item of collection.relatedDirectories || []) {
    inside(output, collection.id + '/' + item.destination);
    await addDirectory(inside(repo, item.source), route + item.destination + '/');
  }
  for (const item of collection.documents || []) {
    inside(output, collection.id + '/' + item.destination);
    sources.set(inside(repo, item.source), route + item.destination);
  }
  const urls = new Map([...sources].map(([source, url]) => [key(source), url]));
  const gallery = inside(root, collection.gallery);
  if (!urls.has(key(gallery))) throw new Error('Gallery is not in the selected source directory');
  urls.set(key(gallery), route);
  for (const [source, url] of urls) publicUrls.set(source, url);
  selections.push({ collection, root, route, sources, gallery });
}
for (const { collection, root, route, sources, gallery } of selections) {
  const urls = publicUrls;
  const catalogue = JSON.parse(await fs.readFile(inside(root, collection.catalog), 'utf8'));
  const provenance = [];
  const omissions = new Set();
  const omitted = (collection.omitLinks || []).map(pattern => new RegExp(pattern));
  function adapt(html, source) {
    if (collection.presentation === 'interiors-compact') html = interiorsPublication(html, source, catalogue);
    if (collection.presentation === 'interior-ornament-compact') html = interiorOrnamentPublication(html, source, catalogue);
    // A compact publication is a declared presentation subset. Omitted controls
    // and downloads must disappear together; no missing asset is silently mapped
    // to a different design or an HTML fallback.
    if (['refined-compact', 'interiors-compact'].includes(collection.presentation)) {
      html = html.replace(/<label\b[^>]*>(?:(?!<\/label>)[\s\S])*?\bid=['"](?:curves|construction)['"](?:(?!<\/label>)[\s\S])*?<\/label>/g, '');
      html = html.replace(/<a\b([^>]*?)\bhref=(['"])(.*?)\2([^>]*)>([\s\S]*?)<\/a>/g, (match, before, quote, value) => {
        if (!value || value.startsWith('#') || /^(?:https?:|data:|mailto:)/i.test(value)) return match;
        const filename = value.split(/[?#]/, 1)[0];
        const resolved = path.resolve(path.dirname(source), decodeURIComponent(filename));
        if (urls.has(key(resolved))) return match;
        const relative = slash(path.relative(root, resolved));
        if (!omitted.some(pattern => pattern.test(relative))) return match;
        omissions.add(relative);
        return '';
      });
      html = html.replace(/(<p[^>]*>)\s*(?:·\s*)+/g, '$1').replace(/(?:\s*·)+\s*(<\/p>)/g, '$1').replace(/(?:\s*·){2,}/g, ' · ');
      if (collection.presentation === 'refined-compact') {
        const notice = '<aside class="publication-scope" aria-label="Publication scope">This website edition includes all 108 designs and their executed unit views. Construction archives, alternate chord views and earlier review images remain in the research checkout. <a href="/ornaments/bourgoin/">Compare the original skeleton studies</a>.</aside>';
        html = html.replace(/(<p class=['"]intro['"][^>]*>[\s\S]*?<\/p>)/, '$1' + notice);
        html = html.replace('</head>', '<style>.publication-scope{font-size:13px;max-width:1050px;padding:13px 17px;border-left:3px solid #a28a61;background:#f7f1e5;margin:16px 0 24px}</style></head>');
      }
    }
    // Absolute site URLs also work when Azure serves a directory without a trailing slash.
    html = html.replace(/\b(href|src)=(['"])(.*?)\2/g, (match, attribute, quote, value) => {
      if (!value || value.startsWith('#') || /^(?:https?:|data:|mailto:)/i.test(value)) return match;
      if (value.startsWith('/ornaments/') && collections.some(item => value.startsWith('/ornaments/' + item.id + '/'))) return match;
      const [filename, suffix = ''] = value.split(/(?=[?#])/s, 2);
      const resolved = key(path.resolve(path.dirname(source), decodeURIComponent(filename)));
      const target = urls.get(resolved);
      if (!target) throw new Error('Unpublished dependency in ' + source + ': ' + value);
      return attribute + '=' + quote + escape(target + suffix) + quote;
    });
    // Gallery controls also construct relative image URLs in JavaScript. A base
    // keeps those requests correct at both the directory and no-slash aliases.
    const pageRoute = key(source) === key(gallery) ? route : urls.get(key(source));
    html = html.replace(/<head\b[^>]*>/i, match => match + `<base href="${escape(pageRoute)}">`);
    const navigation = `<nav class="ornament-navigation" aria-label="Ornament collection"><a href="/">Gojo</a><span aria-hidden="true">/</span><a href="/ornaments/">Ornament studies</a><span aria-hidden="true">/</span><a href="${route}">${escape(collection.title)}</a></nav>`;
    if (/<body\b[^>]*>/i.test(html)) html = html.replace(/<body\b[^>]*>/i, match => match + navigation);
    else html = html.replace(/<nav\b/i, navigation + '<nav');
    const style = '<style>.ornament-navigation{display:flex;flex-wrap:wrap;align-items:center;gap:10px;margin:0;padding:16px max(16px,calc((100% - 1328px)/2));border-bottom:1px solid #d6d3c5;background:#faf7ee;font:14px/1.5 system-ui,sans-serif}.ornament-navigation a{margin:0;padding:0;background:none;color:#276d78;text-decoration:underline;text-underline-offset:3px}.ornament-navigation span{color:#898a7c}a:focus-visible,select:focus-visible,input:focus-visible,button:focus-visible,summary:focus-visible{outline:3px solid #b56a37;outline-offset:3px}img{max-width:100%}</style>';
    html = html.replace('</html>', style + '</html>');
    return html;
  }
  for (const [source, url] of sources) {
    const original = await fs.readFile(source);
    const textFile = /\.(html|svg|json|txt|md)$/i.test(source);
    const bytes = textFile ? Buffer.from(original.toString('utf8').replace(/\r\n/g, '\n')) : original;
    provenance.push({ source: slash(path.relative(repo, source)), url, sha256: hash(bytes) });
    const published = source.endsWith('.html') ? adapt(bytes.toString('utf8'), source) : bytes;
    if (key(source) === key(gallery)) {
      emit(url.slice('/ornaments/'.length), galleryAlias(route, collection.title));
      emit(collection.id + '/index.html', published);
    } else emit(url.slice('/ornaments/'.length), published);
  }
  const entries = catalogue[collection.countProperty || 'Entries'];
  if (!Array.isArray(entries)) throw new Error('Catalogue count property is not an array: ' + collection.id);
  const count = entries.length;
  emit(collection.id + '/publication.json', JSON.stringify({ collection: collection.id, sourceGallery: slash(path.relative(repo, gallery)), textNewlines: 'LF', count, presentation: collection.presentation || 'complete', omittedLinks: [...omissions].sort(), files: provenance }, null, 2) + '\n');
  const previews = collection.previews.map(p => {
    const url = urls.get(key(inside(root, p.file)));
    if (!url) throw new Error('Preview not included: ' + p.file);
    return `<img src="${url}" alt="${escape(p.alt)}" width="1000" height="600" loading="lazy">`;
  }).join('');
  cards.push(`<article class="collection"><a class="collection-link" href="${route}" aria-labelledby="title-${collection.id}"><div class="preview-strip">${previews}</div><div class="collection-copy"><p class="eyebrow">${escape(collection.subtitle)}</p><h2 id="title-${collection.id}">${escape(collection.title)}</h2><p>${escape(collection.description)}</p><div class="collection-meta"><span>${count} ${escape(collection.countLabel)}</span><span class="open">Explore collection <span aria-hidden="true">→</span></span></div></div></a></article>`);
}
emit('index.html', `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><meta name="description" content="Gojo ornament studies: repeating patterns, historical sources and inspectable constructions."><title>Ornament studies · Gojo</title><link rel="stylesheet" href="/ornaments/ornaments.css"></head>
<body><a class="skip" href="#collections">Skip to collections</a><main><nav aria-label="Main"><a href="/">Gojo</a><a href="/syntax/">Syntax examples</a></nav><header><p class="eyebrow">Gojo · Design collections</p><h1>Ornament studies</h1><p class="intro">Repeating fields, motifs and the constructions that connect them.</p><p class="description">Explore a collection, compare its sources and follow how small parts form a larger design. Each study identifies the views and data included in its website edition.</p></header><section id="collections" aria-labelledby="collections-heading"><div class="section-heading"><h2 id="collections-heading">Collections</h2><p>${collections.length} ${collections.length === 1 ? 'study' : 'studies'}</p></div><div class="collections">${cards.join('\n')}</div></section><footer><p>These are recorded Gojo studies. Each collection explains what is constructed, what is chosen for display and where the interpretation remains open.</p><a href="/">All Gojo visualizations</a></footer></main></body></html>
`);
for (const [file, bytes] of expected) {
  if (dryRun) continue;
  if (check) {
    const actual = await fs.readFile(file);
    if (!actual.equals(bytes)) throw new Error('Stale publication: ' + path.relative(web, file));
  } else {
    await fs.mkdir(path.dirname(file), { recursive: true });
    let actual;
    try { actual = await fs.readFile(file); } catch (error) { if (error.code !== 'ENOENT') throw error; }
    if (actual?.equals(bytes)) continue;
    const temporary = file + '.' + crypto.randomUUID() + '.tmp';
    try { await fs.writeFile(temporary, bytes); await fs.rename(temporary, file); }
    finally { await fs.rm(temporary, { force:true }); }
  }
}
console.log(`${dryRun ? 'Planned' : check ? 'Checked' : 'Built'} ${collections.length} ornament collection(s), ${expected.size} files, ${[...expected.values()].reduce((sum, bytes) => sum + bytes.length, 0)} bytes; captured source files unchanged.`);
for (const collection of collections) {
  const prefix = path.join(output, collection.id) + path.sep;
  const files = [...expected].filter(([file]) => file.startsWith(prefix));
  console.log(`${collection.id}: ${files.length} files, ${files.reduce((sum, [, bytes]) => sum + bytes.length, 0)} bytes.`);
}
