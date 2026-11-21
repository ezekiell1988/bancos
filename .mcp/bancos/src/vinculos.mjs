// Niveles 2 y 3 (ADR-EBC-03): libros (el pago, 1 a 1 con un movimiento del banco, con la categoría en partidas)
// y facturas (cuentas por pagar, siempre cuelgan de un libro). Vínculos, cuadres y alta de libros.
import {
  NIVELES, ToolError, VENTANA, appendJsonl, buscarMovimiento, checkFecha, checkTipo, completarMovimiento, copiarRespaldo, cuadra, detalleFile,
  detalleSinUbicacion, emparejar, guardarDetalle, guardarMovimiento, leerDetalles, leerTodosLosMovimientos, nuevoId, planRespaldo, r2, sinDerivados, slug, sumarDias,
} from './datos.mjs';

const texto = (v) => String(v ?? '').trim();

/** Partidas válidas de un libro: tipos de su clase, montos positivos y suma igual al total. */
export function validarPartidas(partidas, { total, clase, moneda }, tipos) {
  if (!Array.isArray(partidas) || !partidas.length) throw new ToolError('partidas: indicar al menos una {tipo, monto}.');
  const filas = partidas.map((p) => {
    checkTipo(tipos, clase, p.tipo);
    if (!(p.monto > 0)) throw new ToolError(`partida ${p.tipo}: el monto debe ser mayor que 0.`);
    return { tipo: p.tipo, monto: r2(p.monto), ...(slug(p.de) ? { de: slug(p.de) } : {}), ...(texto(p.nota) ? { nota: texto(p.nota) } : {}) };
  });
  const suma = r2(filas.reduce((s, p) => s + p.monto, 0));
  if (Math.abs(suma - total) > 0.01 + 1e-9) throw new ToolError(`Las partidas suman ${suma} y el total es ${total}: tienen que dar igual.`);
  return filas;
}

/** Descripción común del campo `partidas`. */
export const PARTIDAS_PROP = {
  type: 'array',
  minItems: 1,
  description: 'Categorías del pago: [{tipo, monto, de?, nota?}]. La suma tiene que ser igual al total. Sin partidas se usa una por todo el total (tipo, o «otros»).',
  items: {
    type: 'object',
    properties: {
      tipo: { type: 'string', description: 'id de bancos_tipos de esa clase.' },
      monto: { type: 'number', description: 'Monto positivo de esta categoría.' },
      de: { type: 'string', description: 'Rubro de esta partida si difiere del movimiento (p. ej. "papa").' },
      nota: { type: 'string' },
    },
    required: ['tipo', 'monto'],
    additionalProperties: false,
  },
};

/** Partidas de un libro desde sus facturas: las líneas por categoría; los ajustes van a la categoría de la factura o a la mayor. */
export function partidasDeFacturas(facturas) {
  const por = new Map();
  const sumar = (tipo, monto) => por.set(tipo, r2((por.get(tipo) ?? 0) + monto));
  for (const f of facturas) {
    for (const l of f.lineas ?? []) sumar(l.tipo ?? f.tipo ?? 'otros', l.total);
    const ajustes = r2((f.ajustes ?? []).reduce((s, a) => s + a.total, 0));
    if (ajustes) {
      const mayor = [...por].sort((a, b) => b[1] - a[1])[0]?.[0];
      sumar(f.tipo ?? mayor ?? 'otros', ajustes);
    }
  }
  return [...por].filter(([, monto]) => monto > 0).map(([tipo, monto]) => ({ tipo, monto }));
}

/** Total de una factura según sus líneas y ajustes. */
export const totalDeLineas = (f) => r2([...(f.lineas ?? []), ...(f.ajustes ?? [])].reduce((s, l) => s + l.total, 0));

/**
 * Cuadre de un libro con sus facturas y con su movimiento: suma de nivel 3 igual al monto del nivel 2 y partidas
 * iguales a la suma de líneas por categoría cuando hay nivel 3. Devuelve la lista de problemas (vacía = cuadra).
 */
export function problemasDeCuadre(libro, facturas, movimiento) {
  const out = [];
  const sumaPartidas = r2((libro.partidas ?? []).reduce((s, p) => s + p.monto, 0));
  if (!cuadra(sumaPartidas, libro.total, libro.moneda)) out.push(`partidas suman ${sumaPartidas} y el libro es ${libro.total}`);
  if (movimiento && !cuadra(movimiento.total, libro.total, libro.moneda)) out.push(`el libro es ${libro.total} y su movimiento ${movimiento.total}`);
  if (facturas.length) {
    const suma = r2(facturas.reduce((s, f) => s + f.total, 0));
    if (!cuadra(suma, libro.total, libro.moneda)) out.push(`las facturas suman ${suma} y el libro es ${libro.total}`);
    const porLineas = new Map(partidasDeFacturas(facturas).map((p) => [p.tipo, p.monto]));
    const porPartidas = new Map();
    for (const p of libro.partidas ?? []) porPartidas.set(p.tipo, r2((porPartidas.get(p.tipo) ?? 0) + p.monto));
    const tipos = new Set([...porLineas.keys(), ...porPartidas.keys()]);
    const distintos = [...tipos].filter((t) => !cuadra(porLineas.get(t) ?? 0, porPartidas.get(t) ?? 0, libro.moneda));
    if (distintos.length) out.push(`las partidas no son la suma de las líneas por categoría (${distintos.join(', ')})`);
  }
  return out;
}

/** Libros (nivel 2) indexados: por id y por movimiento. */
export function indexarLibros(libros = leerDetalles('libros')) {
  const porId = new Map(libros.map((l) => [l.id, l]));
  const porMovimiento = new Map(libros.filter((l) => l.movimiento).map((l) => [l.movimiento, l]));
  return { porId, porMovimiento };
}

/** Facturas agrupadas por id de libro. */
export function facturasPorLibro(facturas = leerDetalles('facturas')) {
  const por = new Map();
  for (const f of facturas) por.set(f.libro, [...(por.get(f.libro) ?? []), f]);
  return por;
}

export function buscarLibro(id) {
  const l = leerDetalles('libros').find((x) => x.id === id);
  if (!l) throw new ToolError(`No existe el libro ${id}. Ver bancos_detalles.`);
  return l;
}

/** Libro nuevo para un movimiento que no tiene pago anotado: sin documento, solo para ponerle la categoría. */
export function libroParaMovimiento(m, { tipo = 'otros', partidas } = {}) {
  return {
    id: nuevoId(NIVELES.libros),
    fecha: m.fecha,
    ...(m.cuenta ? { cuenta: m.cuenta } : {}),
    clase: m.clase,
    total: m.total,
    moneda: m.moneda,
    comercio: m.comercio ?? '',
    ...(m.referencia ? { referencia: m.referencia } : {}),
    ...(m.de ? { de: m.de } : {}),
    nota: m.nota ?? '',
    documento: null,
    movimiento: m.id,
    partidas: partidas ?? [{ tipo, monto: m.total }],
  };
}

/** Normaliza un libro (pago) nuevo: sin movimiento todavía, con partidas validadas. */
export function normalizarLibro(d, tipos) {
  checkFecha(d.fecha);
  if (!(d.total > 0)) throw new ToolError(`total debe ser mayor que 0 (${d.fecha} ${d.comercio ?? ''})`);
  const base = { total: r2(d.total), clase: d.clase, moneda: d.moneda };
  const partidas = d.partidas ? validarPartidas(d.partidas, base, tipos) : validarPartidas([{ tipo: d.tipo || 'otros', monto: base.total }], base, tipos);
  return {
    id: nuevoId(NIVELES.libros),
    fecha: d.fecha,
    ...(texto(d.hora) ? { hora: texto(d.hora) } : {}),
    ...(d.cuenta ? { cuenta: d.cuenta } : {}),
    clase: d.clase,
    total: base.total,
    moneda: d.moneda,
    comercio: texto(d.comercio),
    ...(texto(d.referencia) ? { referencia: texto(d.referencia) } : {}),
    ...(slug(d.de) ? { de: slug(d.de) } : {}),
    nota: texto(d.nota),
    documento: null,
    movimiento: texto(d.movimiento) || null,
    partidas,
    ...(texto(d.transito) ? { transito: { razon: texto(d.transito) } } : {}),
  };
}

/** Mismo libro: misma fecha, total y moneda; misma hora si ambos la tienen; y misma referencia si ambos la tienen, si no mismo comercio. */
export const mismoLibro = (a, b) => {
  if (a.hora && b.hora && a.hora !== b.hora) return false;
  return a.fecha === b.fecha && a.total === b.total && a.moneda === b.moneda
    && (a.referencia && b.referencia ? a.referencia === b.referencia : slug(a.comercio) === slug(b.comercio));
};

/** Libros que bloquean a su movimiento: tienen documento o facturas. Un libro sin ellos solo guarda la categoría y se absorbe. */
export const bloquea = (libro, facturas) => Boolean(libro.documento) || (facturas.get(libro.id)?.length ?? 0) > 0;

/**
 * Prepara libros (pagos) nuevos: respaldo, duplicados, vínculo con su movimiento (a mano o por emparejar) y, si el movimiento
 * ya tenía un libro sin documento, lo absorbe (queda un solo libro por movimiento). Devuelve resumen, filas y guardar().
 */
export function prepararLibros(entradas, tipos) {
  const nuevos = entradas.map((e) => normalizarLibro(e, tipos));
  const reservados = new Set();
  const respaldos = entradas.map((e, i) => (e.origen ? planRespaldo(e.origen, nuevos[i].fecha, e.nombreArchivo, reservados) : null));
  respaldos.forEach((r, i) => { if (r) nuevos[i].documento = r.relativo; });

  const fechas = nuevos.map((d) => d.fecha).sort();
  const existentes = leerDetalles('libros', fechas[0], fechas.at(-1));
  const agregar = [];
  const omitidos = [];
  nuevos.forEach((d, i) => {
    const igual = existentes.find((e) => !e.usado && e.documento && mismoLibro(e, d));
    if (igual) { igual.usado = true; omitidos.push({ fecha: d.fecha, total: d.total, comercio: d.comercio, id: igual.id }); return; }
    agregar.push({ d: { nivel: 'libros', ...d }, respaldo: respaldos[i] });
  });

  const todosLibros = leerDetalles('libros');
  const facturas = facturasPorLibro();
  const { porMovimiento } = indexarLibros(todosLibros);
  const bloqueados = new Set([...porMovimiento].filter(([, l]) => bloquea(l, facturas)).map(([id]) => id));
  const pares = [];
  const manuales = agregar.filter((a) => a.d.movimiento);
  for (const a of manuales) {
    const m = buscarMovimiento(a.d.movimiento);
    if (bloqueados.has(m.id)) throw new ToolError(`El movimiento ${m.id} ya tiene su pago (${porMovimiento.get(m.id).id}); es 1 a 1.`);
    if (m.clase !== a.d.clase || !cuadra(m.total, a.d.total, m.moneda)) throw new ToolError(`El libro de ${a.d.comercio} no coincide en clase y monto con ${m.id}.`);
    bloqueados.add(m.id);
    pares.push({ detalle: a.d, movimiento: m, por: 'a mano', empate: false });
  }
  const pendientes = agregar.filter((a) => !a.d.movimiento && !a.d.transito);
  if (pendientes.length) {
    const f = pendientes.map((a) => a.d.fecha).sort();
    const movs = leerTodosLosMovimientos(sumarDias(f[0], -VENTANA.antes), sumarDias(f.at(-1), VENTANA.despues));
    pares.push(...emparejar(pendientes.map((a) => a.d), movs, bloqueados));
  }
  for (const p of pares) p.detalle.movimiento = p.movimiento.id;

  const filas = agregar.map((a) => detalleSinUbicacion(a.d));
  const cambiosMov = new Map();
  const vinculados = pares.map((p) => {
    const actual = cambiosMov.get(p.movimiento.id)?.nuevo ?? p.movimiento;
    const { nuevo, cambios } = completarMovimiento(actual, p.detalle);
    cambiosMov.set(p.movimiento.id, { original: p.movimiento, nuevo });
    const absorbe = porMovimiento.get(p.movimiento.id);
    return {
      detalle: p.detalle.id, movimiento: p.movimiento.id, fecha: p.movimiento.fecha, total: p.movimiento.total, comercio: p.movimiento.comercio,
      por: p.por, ...(p.empate ? { empate: true } : {}), ...(absorbe ? { reemplazaA: absorbe.id } : {}), cambios: cambios.join('; '),
    };
  });
  return {
    resumen: {
      anota: filas.length,
      vinculados: vinculados.length ? vinculados : undefined,
      pendientes: filas.filter((d) => !d.movimiento && !d.transito).map((d) => ({ id: d.id, fecha: d.fecha, total: d.total, comercio: d.comercio })),
      omitidosPorDuplicado: omitidos.length ? omitidos : undefined,
    },
    filas,
    guardar() {
      for (const a of agregar) if (a.respaldo) copiarRespaldo(a.respaldo);
      // El libro sin documento que ya tenía el movimiento se reemplaza por el nuevo (conserva su categoría si el nuevo no trae).
      for (const p of pares) {
        const viejo = porMovimiento.get(p.movimiento.id);
        if (!viejo) continue;
        const sinCategoria = !p.detalle.partidas.length || (p.detalle.partidas.length === 1 && p.detalle.partidas[0].tipo === 'otros');
        if (sinCategoria && viejo.partidas.length) p.detalle.partidas = viejo.partidas;
        const ubicado = leerDetalles('libros').find((x) => x.id === viejo.id);
        if (ubicado) guardarDetalle(ubicado, null);
      }
      for (const a of agregar) appendJsonl(detalleFile('libros', a.d.fecha), [detalleSinUbicacion(a.d)]);
      for (const { original, nuevo } of cambiosMov.values()) {
        if (JSON.stringify(sinDerivados(nuevo)) !== JSON.stringify(sinDerivados(original))) guardarMovimiento(original, sinDerivados(nuevo));
      }
    },
  };
}
