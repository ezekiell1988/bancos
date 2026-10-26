// Vínculo entre los niveles: comprobantes y facturas (detalles) con su movimiento bancario (ADR-03),
// y facturas con los comprobantes que las pagan (ADR-06).
import {
  NIVELES, ToolError, VENTANA, appendJsonl, buscarMovimiento, checkFecha, checkTipo, completarMovimiento, copiarRespaldo, detalleFile,
  emparejar, guardarMovimiento, leerDetalles, leerTodosLosDetalles, leerTodosLosMovimientos, nuevoId, ocupadosPor, planRespaldo, r2,
  sinUbicacion, slug, sumarDias,
} from './datos.mjs';

/** Comprobantes (pagos) de una factura (ADR-06). */
export const pagosDe = (f) => f.comprobantes ?? [];

/**
 * Pagado y saldo de una factura: la suma de sus comprobantes, o el total si se vinculó directo a un movimiento.
 * `comprobantes`: Map id → comprobante.
 */
export function estadoPago(f, comprobantes) {
  const pagos = pagosDe(f);
  const pagado = pagos.length ? r2(pagos.reduce((s, id) => s + (comprobantes.get(id)?.total ?? 0), 0)) : f.movimiento ? f.total : 0;
  return { pagado, saldo: r2(f.total - pagado) };
}

/** Movimientos de una factura: el suyo directo o los de sus comprobantes. */
export const movimientosDeFactura = (f, comprobantes) => (f.movimiento
  ? [f.movimiento]
  : [...new Set(pagosDe(f).map((id) => comprobantes.get(id)?.movimiento).filter(Boolean))]);

/** Map id → comprobante de una lista de detalles. */
export const mapaComprobantes = (detalles) => new Map(detalles.filter((d) => d.nivel === 'comprobantes').map((c) => [c.id, c]));

/** Pares que solo completan: cada factura que cita al comprobante le pasa tipo, de y nota a su movimiento. */
const paresDePago = (comprobante, movimiento, facturas) => facturas
  .filter((f) => f.nivel === 'facturas' && pagosDe(f).includes(comprobante))
  .map((f) => ({ detalle: f, movimiento, por: 'pago', empate: false, soloCompleta: true }));

/**
 * Plan de vínculos para detalles pendientes: emparejar() los que se vinculan directo (comprobantes y facturas
 * sin pagos) y, por cada comprobante vinculado, las facturas que lo tienen como pago le completan el movimiento
 * (soloCompleta: la factura no guarda `movimiento`; se concilia por sus comprobantes).
 */
export function planVinculos(pendientes, movimientos, todosLosDetalles) {
  const directos = pendientes.filter((d) => !(d.nivel === 'facturas' && pagosDe(d).length));
  const pares = emparejar(directos, movimientos, ocupadosPor(todosLosDetalles));
  for (const p of [...pares]) {
    if (p.detalle.nivel === 'comprobantes') pares.push(...paresDePago(p.detalle.id, p.movimiento, todosLosDetalles));
  }
  return pares;
}

/**
 * Aplica en memoria lo que cada detalle le completa a su movimiento (tipo, de, nota).
 * `movimientos` es un Map id → movimiento que se actualiza; devuelve el resumen de cada vínculo.
 */
export function completarConPares(pares, movimientos) {
  return pares.map((p) => {
    const actual = movimientos.get(p.movimiento.id) ?? p.movimiento;
    const { nuevo, cambios } = completarMovimiento(actual, p.detalle);
    movimientos.set(p.movimiento.id, nuevo);
    return {
      detalle: p.detalle.id,
      movimiento: p.movimiento.id,
      fecha: p.movimiento.fecha,
      total: p.movimiento.total,
      comercio: p.movimiento.comercio,
      por: p.por,
      ...(p.empate ? { empate: true } : {}),
      cambios: cambios.join('; '),
    };
  });
}

/**
 * Vincula comprobantes o facturas nuevos (con `nivel`): los que traen `movimiento` (id) a mano y el resto
 * por emparejar() contra los movimientos de su ventana. Deja `movimiento` puesto en cada detalle vinculado
 * y devuelve el resumen y guardar(), que reescribe los movimientos a los que el detalle les completó algo.
 */
export function vincularNuevos(nuevos) {
  const todos = leerTodosLosDetalles();
  const pares = nuevos.filter((d) => d.movimiento)
    .map((d) => ({ detalle: d, movimiento: buscarMovimiento(d.movimiento), por: 'a mano', empate: false }));
  const pendientes = nuevos.filter((d) => !d.movimiento);
  if (pendientes.length) {
    const fechas = pendientes.map((d) => d.fecha).sort();
    const movs = leerTodosLosMovimientos(sumarDias(fechas[0], -VENTANA.antes), sumarDias(fechas.at(-1), VENTANA.despues));
    pares.push(...planVinculos(pendientes, movs, [...todos, ...nuevos]));
  }
  // Facturas nuevas cuyos pagos ya tienen movimiento: le completan ese movimiento.
  const comprobantes = mapaComprobantes([...todos, ...nuevos]);
  for (const f of nuevos.filter((d) => d.nivel === 'facturas')) {
    for (const id of pagosDe(f)) {
      const c = comprobantes.get(id);
      if (c?.movimiento && !pares.some((p) => p.detalle === f && p.movimiento.id === c.movimiento)) {
        pares.push(...paresDePago(id, buscarMovimiento(c.movimiento), [f]));
      }
    }
  }
  for (const p of pares) if (!p.soloCompleta) p.detalle.movimiento = p.movimiento.id;
  const porId = new Map();
  const vinculados = completarConPares(pares, porId);
  const originales = new Map(pares.map((p) => [p.movimiento.id, p.movimiento]));
  return {
    vinculados,
    guardar() {
      for (const [id, nuevo] of porId) {
        const original = originales.get(id);
        if (JSON.stringify(sinUbicacion(nuevo)) !== JSON.stringify(sinUbicacion(original))) guardarMovimiento(original, sinUbicacion(nuevo));
      }
    },
  };
}

/** Normaliza los campos comunes de un comprobante o factura nuevo. */
export function normalizarDetalle(nivel, d, tipos) {
  checkFecha(d.fecha);
  if (!(d.total > 0)) throw new ToolError(`total debe ser mayor que 0 (${d.fecha} ${d.comercio ?? ''})`);
  const clase = nivel === 'facturas' ? 'gasto' : d.clase;
  if (d.tipo) checkTipo(tipos, clase, d.tipo);
  const texto = (v) => String(v ?? '').trim();
  return {
    id: nuevoId(NIVELES[nivel]),
    fecha: d.fecha,
    ...(texto(d.hora) ? { hora: texto(d.hora) } : {}),
    ...(d.cuenta ? { cuenta: d.cuenta } : {}),
    ...(nivel === 'comprobantes' ? { clase } : {}),
    total: r2(d.total),
    moneda: d.moneda,
    comercio: texto(d.comercio),
    ...(texto(d.referencia) ? { referencia: texto(d.referencia) } : {}),
    ...(texto(d.numero) ? { numero: texto(d.numero) } : {}),
    ...(d.tipo ? { tipo: d.tipo } : {}),
    ...(slug(d.de) ? { de: slug(d.de) } : {}),
    nota: texto(d.nota),
    archivo: null,
    movimiento: texto(d.movimiento) || null,
  };
}

/** Mismo detalle: misma fecha, total y moneda; y misma referencia (o número) si ambos la tienen, si no mismo comercio. */
export const mismoDetalle = (a, b) => {
  const ra = a.referencia ?? a.numero;
  const rb = b.referencia ?? b.numero;
  return a.fecha === b.fecha && a.total === b.total && a.moneda === b.moneda && (ra && rb ? ra === rb : slug(a.comercio) === slug(b.comercio));
};

/**
 * Pasos comunes de bancos_comprobante y bancos_factura: respaldo de cada uno, duplicados y vínculos.
 * Devuelve el resumen para el preview y guardar() para el apply.
 */
export function prepararDetalles(nivel, entradas, normalizar) {
  const nuevos = entradas.map(normalizar);
  const reservados = new Set();
  const respaldos = entradas.map((e, i) => (e.origen ? planRespaldo(e.origen, nuevos[i].fecha, e.nombreArchivo, reservados) : null));
  respaldos.forEach((r, i) => { if (r) nuevos[i].archivo = r.relativo; });

  const fechas = nuevos.map((d) => d.fecha).sort();
  const existentes = leerDetalles(nivel, fechas[0], fechas.at(-1));
  const agregar = [];
  const omitidos = [];
  nuevos.forEach((d, i) => {
    const igual = existentes.find((e) => !e.usado && mismoDetalle(e, d));
    if (igual) { igual.usado = true; omitidos.push({ fecha: d.fecha, total: d.total, comercio: d.comercio, id: igual.id }); return; }
    agregar.push({ d: { nivel, ...d }, respaldo: respaldos[i] });
  });
  const { vinculados, guardar: guardarMovimientos } = vincularNuevos(agregar.map((a) => a.d));
  const filas = agregar.map((a) => sinUbicacion(a.d));
  return {
    resumen: {
      anota: filas.length,
      vinculados: vinculados.length ? vinculados : undefined,
      pendientes: filas.filter((d) => !d.movimiento && !pagosDe(d).length).map((d) => ({ id: d.id, fecha: d.fecha, total: d.total, comercio: d.comercio })),
      omitidosPorDuplicado: omitidos.length ? omitidos : undefined,
    },
    filas,
    guardar() {
      for (const a of agregar) if (a.respaldo) copiarRespaldo(a.respaldo);
      for (const d of filas) appendJsonl(detalleFile(nivel, d.fecha), [d]);
      guardarMovimientos();
    },
  };
}
