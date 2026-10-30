// Dominio del registro: rutas de datos/, lectura y escritura de jsonl, tipos, movimientos (nivel 1)
// y detalles (nivel 2 comprobantes, nivel 3 facturas). Ver ADR-03 en ia/.
// datos/ vive en la raíz del repo y está fuera de git (repo público). --datos-dir lo cambia (smoke).
import { randomBytes } from 'node:crypto';
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
/** Niveles auxiliares: carpeta en datos/ y prefijo de su id. */
export const NIVELES = { comprobantes: 'c', facturas: 'f' };
/** Rubro de los movimientos sin `de` (el dueño y Karen). */
export const HOGAR = 'hogar';

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

export const sumarDias = (fecha, n) => new Date(Date.parse(`${fecha}T00:00:00Z`) + n * 864e5).toISOString().slice(0, 10);
const difDias = (a, b) => Math.round((Date.parse(`${a}T00:00:00Z`) - Date.parse(`${b}T00:00:00Z`)) / 864e5);
export const r2 = (n) => Math.round(n * 100) / 100;

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

/** Id corto y estable: m- movimiento, c- comprobante, f- factura. */
export const nuevoId = (prefijo) => `${prefijo}-${randomBytes(4).toString('hex')}`;

// --- Tipos ---

export const tiposFile = (clase) => path.join(datosDir, `tipos-${clase}.jsonl`);

export function leerTipos(clase) {
  return readJsonl(tiposFile(clase));
}

export const leerTodosLosTipos = () => ({ gasto: leerTipos('gasto'), ingreso: leerTipos('ingreso') });

export function checkTipo(tipos, clase, tipo) {
  const ids = tipos[clase].map((t) => t.id);
  if (!ids.includes(tipo)) {
    throw new ToolError(`Tipo de ${clase} desconocido: "${tipo}". Válidos: ${ids.join(', ')}. Si hace falta uno nuevo, usar bancos_agregar_tipo.`);
  }
}

export function slug(texto) {
  return String(texto ?? '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase()
    .replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 40);
}

// --- Movimientos (nivel 1) ---

export const diaFile = (cuenta, fecha) => path.join(datosDir, cuenta, fecha.slice(0, 7), `${fecha}.jsonl`);

/** Archivos AAAA-MM/AAAA-MM-DD.jsonl de una carpeta entre dos fechas (incluidas). */
function archivosDeDia(base, desde, hasta) {
  if (!existsSync(base)) return [];
  const out = [];
  for (const mes of readdirSync(base).filter((d) => MES.test(d)).sort()) {
    if (mes < desde.slice(0, 7) || mes > hasta.slice(0, 7)) continue;
    for (const f of readdirSync(path.join(base, mes)).filter((n) => /^\d{4}-\d{2}-\d{2}\.jsonl$/.test(n)).sort()) {
      const fecha = f.slice(0, 10);
      if (fecha >= desde && fecha <= hasta) out.push(path.join(base, mes, f));
    }
  }
  return out;
}

/** Movimientos de una cuenta entre dos fechas (incluidas), con la línea de su archivo. */
export function leerMovimientos(cuenta, desde = '0000-01-01', hasta = '9999-12-31') {
  return archivosDeDia(path.join(datosDir, cuenta), desde, hasta)
    .flatMap((file) => readJsonl(file).map((m, i) => ({ cuenta, linea: i + 1, ...m })));
}

export const leerTodosLosMovimientos = (desde, hasta, cuentas = CUENTAS) => cuentas.flatMap((c) => leerMovimientos(c, desde, hasta));

export function buscarMovimiento(id) {
  const m = leerTodosLosMovimientos().find((x) => x.id === id);
  if (!m) throw new ToolError(`No existe el movimiento ${id}. Ver bancos_movimientos.`);
  return m;
}

/** Ubica un movimiento por id o por cuenta + fecha + línea. */
export function ubicarMovimiento({ id, cuenta, fecha, linea }) {
  if (id) return buscarMovimiento(id);
  if (!cuenta || !fecha || !linea) throw new ToolError('Indicar id, o cuenta + fecha + linea.');
  checkFecha(fecha);
  const m = readJsonl(diaFile(cuenta, fecha))[linea - 1];
  if (!m) throw new ToolError(`No hay línea ${linea} en ${cuenta} ${fecha}.`);
  return { cuenta, linea, ...m };
}

/** Reemplaza (o borra, con null) la línea de un movimiento ubicado. */
export function guardarMovimiento(ubicado, nuevo) {
  const file = diaFile(ubicado.cuenta, ubicado.fecha);
  const rows = readJsonl(file);
  if (rows[ubicado.linea - 1]?.id !== ubicado.id) throw new ToolError(`La línea ${ubicado.linea} de ${ubicado.cuenta} ${ubicado.fecha} cambió; volver a consultar.`);
  if (nuevo) rows[ubicado.linea - 1] = nuevo; else rows.splice(ubicado.linea - 1, 1);
  writeJsonl(file, rows);
}

/** Quita los campos internos (cuenta y línea) que agrega la lectura. */
export const sinUbicacion = ({ cuenta: _c, linea: _l, nivel: _n, ...resto }) => resto;

/** Valida un movimiento contra los tipos y lo normaliza al formato del README. */
export function normalizarMovimiento(m, tipos) {
  checkFecha(m.fecha);
  if (m.fechaReal) checkFecha(m.fechaReal, 'fechaReal');
  if (!(m.total > 0)) throw new ToolError(`total debe ser mayor que 0 (${m.fecha} ${m.comercio ?? ''})`);
  checkTipo(tipos, m.clase, m.tipo);
  const referencia = String(m.referencia ?? '').trim();
  return {
    id: nuevoId('m'),
    fecha: m.fecha,
    ...(m.fechaReal && m.fechaReal !== m.fecha ? { fechaReal: m.fechaReal } : {}),
    ...(referencia ? { referencia } : {}),
    clase: m.clase,
    tipo: m.tipo,
    total: r2(m.total),
    moneda: m.moneda,
    comercio: (m.comercio ?? '').trim(),
    nota: (m.nota ?? '').trim(),
    ...(slug(m.de) ? { de: slug(m.de) } : {}),
    ...plasticoDe(m),
    archivo: null,
  };
}

/**
 * Tarjeta de crédito: `tarjeta` = la cuenta que se paga (los 4 últimos dígitos del titular) y
 * `adicional` = el plástico adicional que hizo la compra; sin `adicional` la hizo el titular.
 */
export function plasticoDe(m) {
  const t = checkDigitos(m.tarjeta, 'tarjeta');
  const a = checkDigitos(m.adicional, 'adicional');
  if (a && !t) throw new ToolError('adicional necesita su tarjeta (la cuenta titular).');
  return { ...(t ? { tarjeta: t } : {}), ...(a && a !== t ? { adicional: a } : {}) };
}

function checkDigitos(v, campo) {
  const d = String(v ?? '').replace(/\D/g, '').slice(-4);
  if (v && d.length !== 4) throw new ToolError(`${campo} inválida: "${v}" (usar los 4 últimos dígitos)`);
  return d;
}

/** Descripción común de las fechas: la del banco manda; la real es solo referencia. */
export const FECHA_PROP = { type: 'string', description: 'AAAA-MM-DD. Fecha de proceso del banco: BCR = "Fecha contable"; BAC = MMDD inicial de la referencia. Es la que usan los reportes y los ciclos.' };
export const FECHA_REAL_PROP = { type: 'string', description: 'AAAA-MM-DD. Fecha real de la compra o transacción, solo de referencia; se guarda si difiere de fecha.' };
export const REFERENCIA_PROP = { type: 'string', description: 'Número del movimiento en el banco: BCR = columna "Documento"; BAC = referencia del estado. Sirve para vincular comprobantes.' };

/** Descripción común del campo `de` (rubro de otra persona). */
export const DE_PROP = { type: 'string', description: 'Rubro de otra persona cuando no es del hogar (dueño y Karen), p. ej. "papa" (lo que se le paga y lo que él aporta) o "primo". Vacío = hogar.' };

export const TARJETA_PROP = { type: 'string', description: 'Tarjeta de crédito (cuenta BAC): 4 últimos dígitos de la cuenta titular, la que se paga, p. ej. "8608". Las compras de un adicional también llevan la del titular.' };
export const ADICIONAL_PROP = { type: 'string', description: '4 últimos dígitos del plástico adicional que hizo la compra (sale en su propio bloque del estado). Vacío = la usó el titular.' };

/** Plástico que hizo el movimiento: el adicional o, si no, la tarjeta titular. */
export const plasticoUsado = (m) => m.adicional || m.tarjeta || '';

/** Rubro de un movimiento o detalle: su `de`, o hogar. */
export const rubroDe = (m) => m.de || HOGAR;

/**
 * Filtro por rubro: `de` deja solo ese rubro ("hogar" = sin `de`; "dueno" es sinónimo)
 * y `excluirDe` quita los rubros listados. Sin nada no filtra: el papá entra por defecto (ADR-04).
 */
export function filtroRubro(de, excluirDe = []) {
  const norm = (v) => (['dueno', HOGAR].includes(slug(v)) ? HOGAR : slug(v));
  const solo = de ? norm(de) : null;
  const fuera = new Set(excluirDe.map(norm));
  return (m) => (!solo || rubroDe(m) === solo) && !fuera.has(rubroDe(m));
}

export const RUBRO_PROPS = {
  de: { type: 'string', description: 'Solo ese rubro: "hogar" (dueño y Karen, sin de), "papa", "primo"... Default: todos.' },
  excluirDe: { type: 'array', items: { type: 'string' }, description: 'Rubros a excluir, p. ej. ["papa"]. Default: ninguno.' },
};

/** Mismo movimiento: misma fecha, clase, total y moneda; y misma referencia si ambos la tienen, si no mismo comercio. */
export const mismoMovimiento = (a, b) =>
  a.fecha === b.fecha && a.clase === b.clase && a.total === b.total && a.moneda === b.moneda
  && (a.referencia && b.referencia ? a.referencia === b.referencia : slug(a.comercio) === slug(b.comercio));

// --- Detalles (nivel 2 comprobantes, nivel 3 facturas) ---

export const detalleFile = (nivel, fecha) => path.join(datosDir, nivel, fecha.slice(0, 7), `${fecha}.jsonl`);

export function leerDetalles(nivel, desde = '0000-01-01', hasta = '9999-12-31') {
  return archivosDeDia(path.join(datosDir, nivel), desde, hasta)
    .flatMap((file) => readJsonl(file).map((d, i) => ({ nivel, linea: i + 1, ...d })));
}

export const leerTodosLosDetalles = () => Object.keys(NIVELES).flatMap((n) => leerDetalles(n));

export function buscarDetalle(id) {
  const nivel = Object.keys(NIVELES).find((n) => id?.startsWith(`${NIVELES[n]}-`));
  const d = nivel && leerDetalles(nivel).find((x) => x.id === id);
  if (!d) throw new ToolError(`No existe el comprobante o factura ${id}. Ver bancos_detalles.`);
  return d;
}

/** Reemplaza (o borra, con null) la línea de un detalle leído con leerDetalles. */
export function guardarDetalle(ubicado, nuevo) {
  const file = detalleFile(ubicado.nivel, ubicado.fecha);
  const rows = readJsonl(file);
  if (rows[ubicado.linea - 1]?.id !== ubicado.id) throw new ToolError(`El ${ubicado.id} cambió de línea; volver a consultar.`);
  if (nuevo) rows[ubicado.linea - 1] = nuevo; else rows.splice(ubicado.linea - 1, 1);
  writeJsonl(file, rows);
}

/** Aplica cambios a varios detalles ya leídos, agrupando por archivo. cambios: [{ detalle, nuevo }] */
export function guardarDetalles(cambios) {
  const porArchivo = new Map();
  for (const c of cambios) {
    const file = detalleFile(c.detalle.nivel, c.detalle.fecha);
    porArchivo.set(file, [...(porArchivo.get(file) ?? []), c]);
  }
  for (const [file, lista] of porArchivo) {
    const rows = readJsonl(file);
    for (const c of lista) {
      const i = rows.findIndex((r) => r.id === c.detalle.id);
      if (i >= 0) rows[i] = c.nuevo;
    }
    writeJsonl(file, rows);
  }
}

const soloDigitos = (s) => String(s ?? '').replace(/\D/g, '');

/** 0 = misma referencia, 1 = no comparable (falta alguna), 2 = distintas. Acepta que una termine en la otra (≥ 6 cifras). */
function rangoReferencia(a, b) {
  const x = soloDigitos(a);
  const y = soloDigitos(b);
  if (x.length < 4 || y.length < 4) return 1;
  if (x === y || (Math.min(x.length, y.length) >= 6 && (x.endsWith(y) || y.endsWith(x)))) return 0;
  return 2;
}

/** Ventana de fechas en que el banco procesa lo de un comprobante o factura. */
export const VENTANA = { antes: 3, despues: 10 };

/**
 * Empareja detalles pendientes con movimientos: misma moneda y total, misma clase (las facturas son gastos),
 * misma cuenta si el detalle la trae, y fecha del banco dentro de la ventana. Prefiere misma referencia,
 * después la fecha real igual a la del detalle y la fecha más cercana. Cada movimiento toma un solo detalle
 * de cada nivel (`ocupados`: Set de "nivel|idMovimiento").
 */
export function emparejar(detalles, movimientos, ocupados = new Set()) {
  const pares = [];
  for (const d of [...detalles].sort((a, b) => a.fecha.localeCompare(b.fecha) || (a.hora ?? '').localeCompare(b.hora ?? ''))) {
    const clase = d.nivel === 'facturas' ? 'gasto' : d.clase;
    const candidatos = movimientos
      .filter((m) => !ocupados.has(`${d.nivel}|${m.id}`) && m.moneda === d.moneda && Math.abs(m.total - d.total) < 0.01
        && m.clase === clase && (!d.cuenta || m.cuenta === d.cuenta)
        && difDias(m.fecha, d.fecha) >= -VENTANA.antes && difDias(m.fecha, d.fecha) <= VENTANA.despues)
      .map((m) => ({ m, rango: [rangoReferencia(d.referencia, m.referencia), (m.fechaReal ?? m.fecha) === d.fecha ? 0 : 1, Math.abs(difDias(m.fecha, d.fecha))] }))
      .sort((a, b) => a.rango[0] - b.rango[0] || a.rango[1] - b.rango[1] || a.rango[2] - b.rango[2]);
    if (!candidatos.length) continue;
    const [mejor] = candidatos;
    ocupados.add(`${d.nivel}|${mejor.m.id}`);
    const empate = candidatos.filter((c) => c.rango.join() === mejor.rango.join()).length > 1;
    pares.push({ detalle: d, movimiento: mejor.m, por: ['referencia', 'monto y fecha', 'monto (referencia distinta)'][mejor.rango[0]], empate });
  }
  return pares;
}

/** Movimientos que ya tienen un detalle de cada nivel. */
export const ocupadosPor = (detalles) => new Set(detalles.filter((d) => d.movimiento).map((d) => `${d.nivel}|${d.movimiento}`));

/**
 * Lo que un comprobante o factura le completa a su movimiento: el tipo (si es de la misma clase), el `de`
 * y la nota si el movimiento no tiene. Devuelve el movimiento nuevo y la lista de cambios.
 */
export function completarMovimiento(m, d) {
  const nuevo = { ...m };
  const cambios = [];
  const clase = d.nivel === 'facturas' ? 'gasto' : d.clase;
  if (d.tipo && d.tipo !== m.tipo && clase === m.clase) { nuevo.tipo = d.tipo; cambios.push(`tipo ${m.tipo} → ${d.tipo}`); }
  if (d.de && d.de !== m.de) { nuevo.de = d.de; cambios.push(`de ${m.de ?? HOGAR} → ${d.de}`); }
  if (d.nota && !m.nota) { nuevo.nota = d.nota; cambios.push(`nota "${d.nota}"`); }
  return { nuevo, cambios };
}

// --- Multimedia ---

/**
 * Decide dónde queda el respaldo. Si `origen` ya está dentro de datos/ se referencia tal cual;
 * si no, se copia a multimedia/AAAA-MM/<nombre> sin pisar uno existente (ni otro planeado en la misma llamada).
 */
export function planRespaldo(origen, fecha, nombre, reservados = new Set()) {
  const abs = path.resolve(expandHome(origen));
  if (!existsSync(abs) || !statSync(abs).isFile()) throw new ToolError(`No existe el archivo de respaldo: ${origen}`);
  if (isInside(datosDir, abs)) return { abs, destino: abs, relativo: path.relative(datosDir, abs), copiar: false };

  const ext = path.extname(abs).toLowerCase();
  const base = slug(nombre ? path.basename(nombre, path.extname(nombre)) : path.basename(abs, ext)) || 'respaldo';
  const dir = path.join(datosDir, 'multimedia', fecha.slice(0, 7));
  let destino = path.join(dir, `${base}${ext}`);
  for (let n = 2; existsSync(destino) || reservados.has(destino); n++) destino = path.join(dir, `${base}-${n}${ext}`);
  reservados.add(destino);
  return { abs, destino, relativo: path.relative(datosDir, destino), copiar: true };
}

export function copiarRespaldo(plan) {
  if (!plan.copiar) return;
  mkdirSync(path.dirname(plan.destino), { recursive: true });
  copyFileSync(plan.abs, plan.destino);
}
