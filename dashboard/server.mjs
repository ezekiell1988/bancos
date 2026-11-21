#!/usr/bin/env node
// Dashboard de finanzas de Bancos (solo consulta). Escucha solo en 127.0.0.1; la Mini App lo abre por el proxy /svc/ del bot,
// que solo deja GET: por eso «enviar» es un GET. Rutas relativas siempre (el proxy cuelga todo de /svc/<token>/).
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync } from 'node:fs';
import http from 'node:http';
import net from 'node:net';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { initDatos } from '../.mcp/bancos/src/datos.mjs';
import { archivoDeFactura, filtroDe, htmlCategoria, htmlDetalle, htmlDeuda, htmlMovimiento, htmlIndex, htmlReporte, periodoDe, queryDe, REPORTES, tituloDetalle } from './render.mjs';

const aqui = path.dirname(fileURLToPath(import.meta.url));
const cfg = JSON.parse(readFileSync(path.join(aqui, 'config.json'), 'utf8'));
const home = (p) => p.replace(/^~(?=\/)/, os.homedir());
const correr = promisify(execFile);
const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const SALIDA = path.join(aqui, 'out');
initDatos([]);
mkdirSync(SALIDA, { recursive: true });

const recientes = new Map(); // clave del envío → hora; evita duplicados por toques repetidos
let ocupado = false;

/** Abre el reporte en Chrome sin ventana: primero mide su alto, luego lo captura a 1080 px de ancho. */
async function capturar(url, out) {
  const tmp = mkdtempSync(path.join(os.tmpdir(), 'finanzas-'));
  const base = ['--headless=new', '--disable-gpu', '--hide-scrollbars', `--user-data-dir=${path.join(tmp, 'perfil')}`];
  try {
    const { stdout: dom } = await correr(CHROME, [...base, '--window-size=1080,800', '--dump-dom', url], { timeout: 60000, maxBuffer: 64 * 1024 * 1024 });
    const alto = Math.min(16000, Number(/data-alto="(\d+)"/.exec(dom)?.[1] ?? 1600) + 8);
    await correr(CHROME, [...base, `--window-size=1080,${alto}`, `--screenshot=${out}`, url], { timeout: 60000 });
  } finally {
    rmSync(tmp, { recursive: true, force: true });
  }
  if (!existsSync(out)) throw new Error('Chrome no generó la imagen.');
}

function bridge(req) {
  return new Promise((resolve, reject) => {
    const s = net.createConnection(home(cfg.socket));
    let buf = '';
    s.on('connect', () => s.write(`${JSON.stringify(req)}\n`));
    s.on('data', (c) => { buf += c; });
    s.on('end', () => { try { const r = JSON.parse(buf); r.error ? reject(new Error(r.error)) : resolve(r.result); } catch { reject(new Error('Respuesta inválida del bot.')); } });
    s.on('error', reject);
    s.setTimeout(30000, () => { s.destroy(); reject(new Error('El bot no respondió.')); });
  });
}

/** dest: header x-canal-destino que el proxy del bot pone desde el token firmado (wa<número>); sin él va al chat de Telegram. */
async function enviar(q, dest) {
  const id = q.get('reporte');
  const f = filtroDe(q);
  const NIVEL = ['categoria', 'movimiento'].includes(id);
  if (id !== 'detalle' && !NIVEL && !REPORTES[id]) return { status: 400, json: { error: 'Reporte desconocido.' } };
  const p = periodoDe(q);
  const clave = `${dest ?? ''}|${id}|${NIVEL ? q.toString() : queryDe(p, Object.fromEntries(Object.entries(f).map(([k, x]) => [`f_${k}`, x])))}`;
  const ahora = Date.now();
  if (ahora - (recientes.get(clave) ?? 0) < cfg.segundosEntreEnvios * 1000) return { json: { mensaje: 'Ya se mandó hace un momento ✅' } };
  if (ocupado) return { status: 429, json: { error: 'Ya hay un envío en curso.' } };
  ocupado = true;
  recientes.set(clave, ahora);
  try {
    const out = path.join(SALIDA, `${id}-${ahora}.png`);
    const nivelQ = new URLSearchParams(q); nivelQ.delete('reporte'); nivelQ.set('img', '1');
    const extra = id === 'detalle' ? { img: 1, ...Object.fromEntries(Object.entries(f).map(([k, x]) => [`f_${k}`, x])) } : { reporte: id, img: 1 };
    await capturar(NIVEL ? `http://127.0.0.1:${cfg.puerto}/${id}.html?${nivelQ}` : `http://127.0.0.1:${cfg.puerto}/${id === 'detalle' ? 'detalle' : 'reporte'}.html?${queryDe(p, extra)}`, out);
    await bridge({ op: 'send_photo', chatId: dest ?? cfg.chatId, path: out, caption: `💰 ${id === 'detalle' ? tituloDetalle(f) : NIVEL ? (id === 'categoria' ? 'Categoría' : 'Movimiento') : REPORTES[id]} · ${p.modo === 'ciclo' ? `ciclo ${p.ciclo}` : p.modo === 'mes' ? p.mes : 'rango'}${p.cuenta ? ` · ${p.cuenta.toUpperCase()}` : ''}` });
    return { json: { mensaje: 'Enviado al chat ✅' } };
  } catch (e) {
    recientes.delete(clave);
    return { status: 500, json: { error: `No se pudo enviar: ${e.message}` } };
  } finally {
    rmSync(path.join(SALIDA, `${id}-${ahora}.png`), { force: true });
    ocupado = false;
  }
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, 'http://127.0.0.1');
  const send = (status, tipo, cuerpo) => { res.writeHead(status, { 'Content-Type': `${tipo}; charset=utf-8`, 'Cache-Control': 'no-store' }); res.end(cuerpo); };
  if (!['GET', 'HEAD'].includes(req.method)) return send(405, 'text/plain', 'Solo GET');
  try {
    if (url.pathname === '/' || url.pathname === '/index.html') return send(200, 'text/html', await htmlIndex(periodoDe(url.searchParams)));
    if (url.pathname === '/reporte.html') return send(200, 'text/html', await htmlReporte(periodoDe(url.searchParams), url.searchParams.get('reporte'), { imagen: url.searchParams.get('img') === '1' }));
    if (url.pathname === '/detalle.html') return send(200, 'text/html', await htmlDetalle(periodoDe(url.searchParams), filtroDe(url.searchParams), { imagen: url.searchParams.get('img') === '1' }));
    if (url.pathname === '/categoria.html') return send(200, 'text/html', await htmlCategoria(periodoDe(url.searchParams), url.searchParams, { imagen: url.searchParams.get('img') === '1' }));
    if (url.pathname === '/movimiento.html') return send(200, 'text/html', await htmlMovimiento(periodoDe(url.searchParams), url.searchParams, { imagen: url.searchParams.get('img') === '1' }));
    if (url.pathname === '/deuda.html') { const h = await htmlDeuda(periodoDe(url.searchParams)); return h ? send(200, 'text/html', h) : send(502, 'text/plain', 'Sin resumen'); }
    if (url.pathname === '/archivo') {
      const a = archivoDeFactura(url.searchParams.get('f'));
      if (!a) return send(404, 'text/plain', 'No existe');
      res.writeHead(200, { 'Content-Type': a.tipo, 'Cache-Control': 'no-store' });
      return req.method === 'HEAD' ? res.end() : res.end(readFileSync(a.ruta));
    }
    if (url.pathname === '/enviar') { const r = await enviar(url.searchParams, /^wa\d{8,15}$/.test(String(req.headers['x-canal-destino'] ?? '')) ? req.headers['x-canal-destino'] : null); return send(r.status ?? 200, 'application/json', JSON.stringify(r.json)); }
    if (url.pathname === '/health') return send(200, 'text/plain', 'ok');
    return send(404, 'text/plain', 'No existe');
  } catch (e) {
    return send(500, 'text/plain', `Error: ${e.message}`);
  }
});
server.listen(cfg.puerto, '127.0.0.1', () => console.log(`Dashboard de finanzas en http://127.0.0.1:${cfg.puerto}`));
