// Plantilla de test end-to-end para MarketingOneOnOneWeb2 en un navegador real.
//
// Cubre la parte que siempre se repite (login por PIN sin acceso a email real) y deja
// un bloque "--- TU TEST AQUI ---" para pegar los pasos específicos de lo que se
// quiere verificar. Ver SKILL.md para el procedimiento completo (levantar backend/
// frontend, permisos, limpieza de datos).
//
// USO:
//   BASE_URL=https://localhost:4201 EMAIL=usuario@dominio.com node login-and-test.mjs

import { chromium } from 'playwright';
import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '../../../..');

const BASE_URL = process.env.BASE_URL || 'https://localhost:4201';
const EMAIL = process.env.EMAIL;
const SHOT_DIR = process.env.SHOT_DIR || __dirname;

if (!EMAIL) {
  console.error('Falta EMAIL=usuario@dominio.com (debe existir en tbLogin; consulta MCP dbQuery si hace falta)');
  process.exit(1);
}

function getPin(email) {
  const out = execFileSync('pwsh', [
    path.join(REPO_ROOT, '.agents/skills/m1on1-web2-browser-test/examples/get-pin.ps1'),
    '-Email', email,
  ], { encoding: 'utf-8' });
  return out.trim().split('\n').filter(Boolean).pop();
}

const browser = await chromium.launch({ headless: true, ignoreHTTPSErrors: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true });
const page = await context.newPage();

// Deja ver errores reales de la app en la salida del script — no te quedes ciego a un
// ResourceValueError o un HttpErrorResponse silencioso solo porque el screenshot "se ve bien".
page.on('pageerror', err => console.log('[pageerror]', err.message));
page.on('console', msg => { if (msg.type() === 'error') console.log('[console:error]', msg.text()); });

async function shot(name) {
  await page.screenshot({ path: path.join(SHOT_DIR, `${name}.png`), fullPage: true });
}

// ── 1. Login por PIN (genérico, no tocar salvo que cambie el flujo de auth) ────────
console.log('--- Login ---');
await page.goto(BASE_URL + '/login', { waitUntil: 'networkidle' });
await page.fill('#email-input', EMAIL);
await page.click('button[type=submit]');
await page.waitForTimeout(2000); // job Hangfire encola el LoginToken antes de responder

const pin = getPin(EMAIL);
console.log('PIN:', pin);
if (!pin || pin.length !== 5) throw new Error('PIN inválido: ' + pin);

for (let i = 0; i < pin.length; i++) {
  await page.fill(`input[data-pin-index="${i}"]`, pin[i]);
}
await page.waitForTimeout(2000);
console.log('URL tras login:', page.url());
await shot('01-after-login');

// ── 2. TU TEST AQUÍ ─────────────────────────────────────────────────────────────
// Ejemplos de patrones útiles (ver el resto del SKILL.md para más):
//
// await page.goto(BASE_URL + '/art-library', { waitUntil: 'networkidle' });
//
// // Diálogos con SweetAlert2 (el proyecto NO usa window.confirm/alert/prompt —
// // ver ADR-25/TASK-EBC-FE-56 — si un flujo dispara un window.* nativo en vez de
// // .swal2-popup, es casi siempre un bug, no algo que el test deba tolerar):
// await page.click('button:has-text("Fork")');
// await page.waitForSelector('.swal2-popup');
// await page.click('.swal2-confirm');
//
// // Confirmar un toast de la app (shared/components/toast):
// const toastVisible = await page.locator('[class*=toast]').count() > 0;
//
// // Esperar una respuesta HTTP específica en vez de un sleep a ciegas cuando el
// // paso dispara una llamada asíncrona real (ej. generación con Azure AI Foundry,
// // que puede tardar 30-100s):
// const [resp] = await Promise.all([
//   page.waitForResponse(r => r.url().includes('/versions') && r.request().method() === 'POST'),
//   page.click('button:has-text("Generar nueva versión")'),
// ]);
// console.log('status:', resp.status());

await shot('02-final-state');

await browser.close();
console.log('--- DONE ---');
