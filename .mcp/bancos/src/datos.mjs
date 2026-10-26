// Dominio del registro: rutas de datos/, lectura y escritura de jsonl, tipos y movimientos.
// datos/ vive en la raíz del repo y está fuera de git (repo público). --datos-dir lo cambia (smoke).
import { appendFileSync, copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { getArgvFlag } from '../../_shared/cli.mjs';
import { expandHome, isInside } from '../../_shared/workspace.mjs';
import { ToolError } from '../../_shared/results.mjs';

export { ToolError };

export const CUENTAS = ['bcr', 'bac'];
export const CLASES = ['gasto', 'ingreso'];
export const MONEDAS = ['CRC', 'USD'];

const DEFAULT_DATOS = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..', '..', 'datos');
let datosDir = DEFAULT_DATOS;

export function initDatos(argv = process.argv) {
  const flag = getArgvFlag(argv, '--datos-dir');
  datosDir = flag ? path.resolve(expandHome(flag)) : DEFAULT_DATOS;
}

export const getDatosDir = () => datosDir;

const FECHA = /^\d{4}-\d{2}-\d{2}$/;
const MES = /^\d{4}-\d{2}$/;

export function checkFecha(fecha, campo = 'fecha') {
  if (!FECHA.test(fecha ?? '') || Number.isNaN(Date.parse(`${fecha}T00:00:00Z`))) {
    throw new ToolError(`${campo} inválida: "${fecha}" (usar AAAA-MM-DD)`);
  }
}

export function checkMes(mes) {
  if (!MES.test(mes ?? '')) throw new ToolError(`mes inválido: "${mes}" (usar AAAA-MM)`);
}

export function readJsonl(file) {
  if (!existsSync(file)) return [];
  return readFileSync(file, 'utf8').split('\n').filter((l) => l.trim()).map((l, i) => {
    try { return JSON.parse(l); } catch { throw new ToolError(`Línea ${i + 1} inválida en ${path.relative(datosDir, file)}`); }
  });
}

export function writeJsonl(file, rows) {
  mkdirSync(path.dirname(file), { recursive: true });
  writeFileSync(file, rows.map((r) => JSON.stringify(r)).join('\n') + (rows.length ? '\n' : ''));
}

export function appendJsonl(file, rows) {
  mkdirSync(path.dirname(file), { recursive: true });
  appendFileSync(file, rows.map((r) => JSON.stringify(r)).join('\n') + '\n');
}

// --- Tipos ---

export const tiposFile = (clase) => path.join(datosDir, `tipos-${clase}.jsonl`);

export function leerTipos(clase) {
  return readJsonl(tiposFile(clase));
}

export function slug(texto) {
  return String(texto ?? '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase()
    .replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 40);
}

// --- Movimientos ---

export const diaFile = (cuenta, fecha) => path.join(datosDir, cuenta, fecha.slice(0, 7), `${fecha}.jsonl`);

/** Movimientos de una cuenta entre dos fechas (incluidas), con la línea de su archivo. */
export function leerMovimientos(cuenta, desde, hasta) {
  const base = path.join(datosDir, cuenta);
  if (!existsSync(base)) return [];
  const out = [];
  for (const mes of readdirSync(base).filter((d) => MES.test(d)).sort()) {
    if (mes < desde.slice(0, 7) || mes > hasta.slice(0, 7)) continue;
    for (const f of readdirSync(path.join(base, mes)).filter((n) => /^\d{4}-\d{2}-\d{2}\.jsonl$/.test(n)).sort()) {
      const fecha = f.slice(0, 10);
      if (fecha < desde || fecha > hasta) continue;
      readJsonl(path.join(base, mes, f)).forEach((m, i) => out.push({ cuenta, linea: i + 1, ...m }));
    }
  }
  return out;
}

/** Valida un movimiento contra los tipos y lo normaliza al formato del README. */
export function normalizarMovimiento(m, tipos) {
  checkFecha(m.fecha);
  if (!(m.total > 0)) throw new ToolError(`total debe ser mayor que 0 (${m.fecha} ${m.comercio ?? ''})`);
  const ids = tipos[m.clase].map((t) => t.id);
  if (!ids.includes(m.tipo)) {
    throw new ToolError(`Tipo de ${m.clase} desconocido: "${m.tipo}". Válidos: ${ids.join(', ')}. Si hace falta uno nuevo, usar bancos_agregar_tipo.`);
  }
  return {
    fecha: m.fecha,
    clase: m.clase,
    tipo: m.tipo,
    total: Math.round(m.total * 100) / 100,
    moneda: m.moneda,
    comercio: (m.comercio ?? '').trim(),
    nota: (m.nota ?? '').trim(),
    archivo: null,
  };
}

export const mismoMovimiento = (a, b) =>
  a.fecha === b.fecha && a.clase === b.clase && a.total === b.total && a.moneda === b.moneda
  && slug(a.comercio) === slug(b.comercio);

// --- Multimedia ---

/**
 * Decide dónde queda el respaldo. Si `origen` ya está dentro de datos/ se referencia tal cual;
 * si no, se copia a multimedia/AAAA-MM/<nombre> sin pisar uno existente.
 */
export function planRespaldo(origen, fecha, nombre) {
  const abs = path.resolve(expandHome(origen));
  if (!existsSync(abs) || !statSync(abs).isFile()) throw new ToolError(`No existe el archivo de respaldo: ${origen}`);
  if (isInside(datosDir, abs)) return { abs, destino: abs, relativo: path.relative(datosDir, abs), copiar: false };

  const ext = path.extname(abs).toLowerCase();
  const base = slug(nombre ? path.basename(nombre, path.extname(nombre)) : path.basename(abs, ext)) || 'respaldo';
  const dir = path.join(datosDir, 'multimedia', fecha.slice(0, 7));
  let destino = path.join(dir, `${base}${ext}`);
  for (let n = 2; existsSync(destino); n++) destino = path.join(dir, `${base}-${n}${ext}`);
  return { abs, destino, relativo: path.relative(datosDir, destino), copiar: true };
}

export function copiarRespaldo(plan) {
  if (!plan.copiar) return;
  mkdirSync(path.dirname(plan.destino), { recursive: true });
  copyFileSync(plan.abs, plan.destino);
}
