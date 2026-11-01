// Común a los tools de pago de contado de tarjetas (BAC y BN): leer el PDF, tipo de cambio de InfoDolar,
// formato de montos como en CR y la imagen del detalle con Chrome sin ventana.
import { execFileSync } from 'node:child_process';
import { existsSync, mkdtempSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { ToolError } from './datos.mjs';

export const INFODOLAR = {
  bac: 'https://www.infodolar.co.cr/precio-dolar-entidad-bac-credomatic.aspx',
  bncr: 'https://www.infodolar.co.cr/precio-dolar-entidad-banco-nacional-de-costa-rica.aspx',
};

/** El estado más reciente en datos/multimedia cuyo nombre cumple `patron`. */
export function ultimoEstado(datosDir, patron) {
  const base = path.join(datosDir, 'multimedia');
  if (!existsSync(base)) return null;
  const todos = readdirSync(base, { withFileTypes: true }).filter((d) => d.isDirectory())
    .flatMap((d) => readdirSync(path.join(base, d.name)).filter((f) => patron.test(f)).map((f) => path.join('multimedia', d.name, f)));
  return todos.sort((a, b) => path.basename(a).localeCompare(path.basename(b))).at(-1) ?? null;
}

export function textoDelEstado(file) {
  if (file.endsWith('.txt')) return readFileSync(file, 'utf8');
  try {
    return execFileSync('pdftotext', ['-layout', file, '-'], { encoding: 'utf8', maxBuffer: 20 * 1024 * 1024 });
  } catch (e) {
    throw new ToolError(`No pude leer el PDF con pdftotext: ${e.message}`);
  }
}

/** Compra y venta del dólar de una entidad según InfoDolar (las páginas de los bancos no responden desde la Mac). */
export async function cambioInfodolar(url) {
  try {
    const res = await fetch(url, { headers: { 'User-Agent': 'Mozilla/5.0' }, signal: AbortSignal.timeout(15000) });
    const html = await res.text();
    const valores = [...html.matchAll(/class="colCompraVenta" data-order="₡([\d.,]+)"/g)].map((m) => Number(m[1].replace(/\./g, '').replace(',', '.')));
    if (valores.length >= 2 && valores[1] > 0) return { compra: valores[0], venta: valores[1], fuente: url };
  } catch { /* sin red: se pide tipoCambio */ }
  return null;
}

const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const MESES_LARGOS = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'set', 'oct', 'nov', 'dic'];
export const fechaCorta = (iso) => (iso ? `${Number(iso.slice(8, 10))}-${MESES_LARGOS[Number(iso.slice(5, 7)) - 1]}-${iso.slice(0, 4)}` : '—');
/** 1006173.81 → "1.006.173,81" (como lo escriben los bancos en CR). */
export const fmt = (n, dec = 2) => { const [e, d] = n.toFixed(dec).split('.'); return e.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + (d ? `,${d}` : ''); };
export const crc = (n) => (n ? `₡${fmt(n)}` : '—');
export const usd = (n) => (n ? `$${fmt(n)}` : '—');
export const esc = (s) => String(s).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);

export const ESTILO = `*{box-sizing:border-box}body{margin:0;font:22px -apple-system,'Helvetica Neue',sans-serif;color:#1d2433;background:#fff}
.c{width:900px;padding:36px}h1{margin:0;font-size:34px}.sub{color:#5b6475;margin:6px 0 22px}
.chips{display:flex;gap:12px;margin-bottom:22px}.chip{flex:1;background:#f2f4f8;border-radius:14px;padding:12px 16px}
.chip span{display:block;font-size:17px;color:#5b6475}.chip b{font-size:24px}.chip.alerta{background:#fff1e6}.chip.alerta b{color:#c2410c}
table{width:100%;border-collapse:collapse}th{font-size:17px;color:#5b6475;text-align:right;padding:8px 10px;border-bottom:2px solid #d8dde6}
th:first-child,td:first-child{text-align:left}td{text-align:right;padding:12px 10px;border-bottom:1px solid #e6e9ef;white-space:nowrap}
td small{display:block;color:#7a8394;font-size:16px}tr.t td{border-top:2px solid #1d2433;border-bottom:none;font-weight:700}
.pedir{margin-top:24px;background:#e8f5ec;border-radius:16px;padding:20px 24px;display:flex;justify-content:space-between;align-items:center}
.pedir span{font-size:20px;color:#1f6b3a}.pedir b{font-size:38px;color:#15803d}
ul{margin:16px 0 0;padding-left:22px;color:#4a5263;font-size:18px}li{margin:4px 0}`;

/** Captura del HTML con Chrome sin ventana. */
export function renderPng(html, out) {
  if (!existsSync(CHROME)) throw new ToolError('No encontré Google Chrome para generar la imagen. Usá imagen: false.');
  const tmp = mkdtempSync(path.join(os.tmpdir(), 'pago-tarjetas-'));
  try {
    const htmlFile = path.join(tmp, 'detalle.html');
    writeFileSync(htmlFile, html);
    const alto = 300 + html.split('<tr>').length * 70 + html.split('<li>').length * 30;
    execFileSync(CHROME, ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=2',
      `--user-data-dir=${path.join(tmp, 'perfil')}`, `--window-size=900,${alto}`, `--screenshot=${out}`, `file://${htmlFile}`],
    { stdio: 'ignore', timeout: 60000 });
  } finally {
    rmSync(tmp, { recursive: true, force: true });
  }
  if (!existsSync(out)) throw new ToolError('Chrome no generó la imagen.');
}
