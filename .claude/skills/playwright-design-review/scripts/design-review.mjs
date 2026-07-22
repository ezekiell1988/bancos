#!/usr/bin/env node
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const { chromium } = loadPlaywright();

const args = parseArgs(process.argv.slice(2));

if (!args.url) {
  fail('Missing --url');
}

const viewport = parseViewport(args.viewport ?? '430x932');
const outDir = args.out ?? 'tmp/design-review';
const selector = args.selector ?? 'body';
const waitUntil = args.waitUntil ?? 'networkidle';

mkdirSync(outDir, { recursive: true });

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({
  viewport,
  isMobile: args.mobile !== 'false',
  hasTouch: args.touch !== 'false',
  deviceScaleFactor: Number(args.deviceScaleFactor ?? 1),
  userAgent: args.userAgent ?? 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1',
});

const localStoragePairs = toArray(args.localStorage);
if (localStoragePairs.length > 0) {
  await context.addInitScript((pairs) => {
    for (const pair of pairs) {
      const index = pair.indexOf('=');
      if (index > 0) localStorage.setItem(pair.slice(0, index), pair.slice(index + 1));
    }
  }, localStoragePairs);
}

const page = await context.newPage();

if (args.mockUrl && args.mockFile) {
  const mockPath = path.resolve(args.mockFile);
  if (!existsSync(mockPath)) fail(`Mock file not found: ${mockPath}`);
  const body = readFileSync(mockPath, 'utf8');
  await page.route(args.mockUrl, route => route.fulfill({
    status: Number(args.mockStatus ?? 200),
    contentType: args.mockContentType ?? 'application/json',
    body,
  }));
}

await page.goto(args.url, { waitUntil });

if (args.waitForSelector) {
  await page.waitForSelector(args.waitForSelector, { timeout: Number(args.timeout ?? 15000) });
}

if (args.settleMs) {
  await page.waitForTimeout(Number(args.settleMs));
}

const slug = sanitize(`${viewport.width}x${viewport.height}-${new URL(args.url).pathname || 'page'}`);
const screenshotPath = path.join(outDir, `${slug}.png`);
await page.screenshot({ path: screenshotPath, fullPage: args.fullPage === 'true' });

const report = await page.evaluate((rootSelector) => {
  const root = document.querySelector(rootSelector) ?? document.body;
  const vw = window.innerWidth;
  const all = [...root.querySelectorAll('*')];
  const offenders = all
    .map(el => ({ el, r: el.getBoundingClientRect() }))
    .filter(x => x.r.right > vw + 1 || x.r.left < -1)
    .slice(0, 30)
    .map(x => ({
      tag: x.el.tagName.toLowerCase(),
      className: x.el.className?.toString?.() ?? '',
      left: Math.round(x.r.left),
      right: Math.round(x.r.right),
      width: Math.round(x.r.width),
      text: x.el.textContent.trim().replace(/\s+/g, ' ').slice(0, 100),
    }));

  const rootRect = root.getBoundingClientRect();

  return {
    viewport: { width: window.innerWidth, height: window.innerHeight },
    document: {
      documentElementScrollWidth: document.documentElement.scrollWidth,
      bodyScrollWidth: document.body.scrollWidth,
    },
    root: {
      selector: rootSelector,
      left: Math.round(rootRect.left),
      right: Math.round(rootRect.right),
      width: Math.round(rootRect.width),
      height: Math.round(rootRect.height),
    },
    hasHorizontalOverflow:
      document.documentElement.scrollWidth > window.innerWidth + 1 ||
      document.body.scrollWidth > window.innerWidth + 1 ||
      offenders.length > 0,
    offenders,
  };
}, selector);

report.url = args.url;
report.screenshot = screenshotPath;

const reportPath = path.join(outDir, `${slug}.json`);
writeFileSync(reportPath, `${JSON.stringify(report, null, 2)}\n`);

console.log(JSON.stringify({
  screenshot: screenshotPath,
  report: reportPath,
  hasHorizontalOverflow: report.hasHorizontalOverflow,
  offenderCount: report.offenders.length,
}, null, 2));

await browser.close();

function parseArgs(argv) {
  const parsed = {};
  for (let i = 0; i < argv.length; i += 1) {
    const item = argv[i];
    if (!item.startsWith('--')) continue;
    const key = toCamel(item.slice(2));
    const next = argv[i + 1];
    const value = !next || next.startsWith('--') ? 'true' : argv[++i];
    if (parsed[key] === undefined) parsed[key] = value;
    else if (Array.isArray(parsed[key])) parsed[key].push(value);
    else parsed[key] = [parsed[key], value];
  }
  return parsed;
}

function parseViewport(value) {
  const match = /^(\d+)x(\d+)$/.exec(value);
  if (!match) fail(`Invalid --viewport "${value}". Use WIDTHxHEIGHT, e.g. 430x932.`);
  return { width: Number(match[1]), height: Number(match[2]) };
}

function toArray(value) {
  if (value === undefined) return [];
  return Array.isArray(value) ? value : [value];
}

function toCamel(value) {
  return value.replace(/-([a-z])/g, (_, c) => c.toUpperCase());
}

function sanitize(value) {
  return value.replace(/[^a-zA-Z0-9_-]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 120);
}

function fail(message) {
  console.error(message);
  process.exit(1);
}

function loadPlaywright() {
  const candidates = [
    '@playwright/test',
    path.resolve(process.cwd(), 'node_modules/@playwright/test'),
    path.resolve(process.cwd(), 'src/VoiceBot.Web/node_modules/@playwright/test'),
  ];

  for (const candidate of candidates) {
    try {
      return require(candidate);
    } catch {
      // Try next candidate.
    }
  }

  fail('Cannot find @playwright/test. Run from src/VoiceBot.Web or install dependencies with npm install.');
}
