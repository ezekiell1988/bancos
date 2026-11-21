import {
  CUENTAS, DE_PROP, MONEDAS, NIVELES, ToolError, VENTANA_FACTURA, appendJsonl, buscarMovimiento, checkFecha, checkTipo, completarMovimiento, copiarRespaldo,
  cuadra, detalleFile, detalleSinUbicacion, emparejar, guardarDetalle, guardarMovimiento, leerDetalles, leerTodosLosMovimientos, leerTodosLosTipos, nuevoId, planRespaldo,
  r2, sinDerivados, slug, sumarDias,
} from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';
import { buscarLibro, facturasPorLibro, indexarLibros, libroParaMovimiento, partidasDeFacturas, problemasDeCuadre, totalDeLineas } from '../src/vinculos.mjs';

const texto = (v) => String(v ?? '').trim();

const LINEA = {
  type: 'object',
  properties: {
    descripcion: { type: 'string' },
    cantidad: { type: 'number' },
    unidad: { type: 'string', description: 'p. ej. kg, un, kWh, m3.' },
    precio: { type: 'number', description: 'Precio unitario.' },
    impuesto: { type: 'number', description: 'Monto de impuesto (IVA) incluido en el total de la línea, si la factura lo detalla.' },
    total: { type: 'number', description: 'Total de la línea (puede ser negativo para descuentos).' },
    tipo: { type: 'string', description: 'id de gasto de bancos_tipos: la categoría de esta línea (cada línea lleva la suya). Default: el tipo de la factura, o «otros».' },
  },
  required: ['descripcion', 'total'],
  additionalProperties: false,
};

/** Normaliza una factura nueva (sin libro todavía). */
function normalizarFactura(f, tipos) {
  checkFecha(f.fecha);
  if (!(f.total > 0)) throw new ToolError(`total debe ser mayor que 0 (${f.fecha} ${f.proveedor ?? ''})`);
  if (f.tipo) checkTipo(tipos, 'gasto', f.tipo);
  const lineas = f.lineas.map((l) => {
    if (typeof l.total !== 'number') throw new ToolError(`Línea sin total en la factura de ${f.proveedor}`);
    const tipo = l.tipo ?? f.tipo ?? 'otros';
    checkTipo(tipos, 'gasto', tipo);
    return { descripcion: l.descripcion.trim(), ...(l.cantidad !== undefined ? { cantidad: l.cantidad } : {}), ...(l.unidad ? { unidad: l.unidad } : {}),
      ...(l.precio !== undefined ? { precio: l.precio } : {}), ...(l.impuesto !== undefined ? { impuesto: r2(l.impuesto) } : {}), total: r2(l.total), tipo };
  });
  const ajustes = (f.ajustes ?? []).map((a) => ({ descripcion: a.descripcion.trim(), total: r2(a.total) }));
  return {
    id: nuevoId(NIVELES.facturas),
    fecha: f.fecha,
    proveedor: texto(f.proveedor),
    ...(texto(f.cedula) ? { cedula: texto(f.cedula) } : {}),
    ...(texto(f.numero) ? { numero: texto(f.numero) } : {}),
    clase: f.clase ?? 'factura',
    total: r2(f.total),
    moneda: f.moneda,
    ...(f.cuenta ? { cuenta: f.cuenta } : {}),
    ...(f.tipo ? { tipo: f.tipo } : {}),
    ...(slug(f.de) ? { de: slug(f.de) } : {}),
    nota: texto(f.nota),
    archivo: null,
    libro: null,
    lineas,
    ...(ajustes.length ? { ajustes } : {}),
  };
}

const mismaFactura = (a, b) => a.fecha === b.fecha && a.total === b.total && a.moneda === b.moneda && a.clase === b.clase
  && (a.numero && b.numero ? a.numero === b.numero : slug(a.proveedor) === slug(b.proveedor));

export default {
  name: 'bancos_factura',
  order: 23,
  description:
    'Nivel 3 (Cuentas por pagar, ADR-EBC-03): anota facturas con proveedor, cédula jurídica, número, líneas (producto, cantidad, impuesto, total y tipo por línea) y ajustes al final, ' +
    'en datos/facturas/AAAA-MM/AAAA-MM-DD.jsonl, y copia el respaldo a datos/multimedia/AAAA-MM/. Siempre cuelga de un libro (nivel 2, el pago): con libro:"l-..." o movimiento:"m-..." ' +
    '(usa su libro o le crea uno); sin ninguno busca el movimiento por monto y fecha (hasta 35 días después) y, si no hay, crea un libro sin movimiento (pendiente). ' +
    'clase:"ajuste" es un sobrante que no se gastó (p. ej. vuelto devuelto por SINPE) con su respaldo y su propia categoría. La suma de las facturas de un libro tiene que ser igual a su monto: ' +
    'si queda corta da error, salvo parcial:true (falta anotar la otra). Cuando cuadra, las partidas del libro pasan a ser la suma de las líneas por categoría. ' +
    'Avisa si las líneas no suman el total. Sin apply:true solo devuelve el preview.',
  inputSchema: {
    type: 'object',
    properties: {
      facturas: {
        type: 'array',
        minItems: 1,
        items: {
          type: 'object',
          properties: {
            fecha: { type: 'string', description: 'AAAA-MM-DD de la factura.' },
            proveedor: { type: 'string', description: 'Nombre del comercio o proveedor.' },
            cedula: { type: 'string', description: 'Cédula jurídica del proveedor, si la trae.' },
            numero: { type: 'string', description: 'Número o consecutivo de la factura, si lo trae.' },
            clase: { type: 'string', enum: ['factura', 'ajuste'], description: 'factura (default) o ajuste (sobrante que no se gastó).' },
            total: { type: 'number', description: 'Total de la factura (con IVA y servicio).' },
            moneda: { type: 'string', enum: MONEDAS },
            cuenta: { type: 'string', enum: CUENTAS, description: 'Con qué se pagó, si se sabe (ayuda a encontrar el movimiento).' },
            tipo: { type: 'string', description: 'id de gasto por defecto de las líneas que no traen el suyo.' },
            de: DE_PROP,
            nota: { type: 'string' },
            lineas: { type: 'array', description: 'Líneas de la factura.', items: LINEA },
            ajustes: {
              type: 'array',
              description: 'Ajustes al final de la factura (descuentos, redondeo): suman al total junto con las líneas.',
              items: { type: 'object', properties: { descripcion: { type: 'string' }, total: { type: 'number' } }, required: ['descripcion', 'total'], additionalProperties: false },
            },
            origen: { type: 'string', description: 'Ruta absoluta de la foto o PDF de la factura.' },
            nombreArchivo: { type: 'string' },
            libro: { type: 'string', description: 'id del libro (l-...) del que cuelga, si ya se sabe.' },
            movimiento: { type: 'string', description: 'id del movimiento (m-...) que la pagó: usa o crea su libro.' },
          },
          required: ['fecha', 'proveedor', 'total', 'moneda', 'lineas'],
          additionalProperties: false,
        },
      },
      parcial: { type: 'boolean', description: 'true si la suma de las facturas del libro todavía no llega a su monto (falta anotar otra).' },
      apply: { type: 'boolean', description: 'true para guardar; sin él solo preview.' },
    },
    required: ['facturas'],
    additionalProperties: false,
  },
  async handler({ facturas, parcial, apply }) {
    const tipos = leerTodosLosTipos();
    const avisos = [];
    const nuevas = facturas.map((f) => normalizarFactura(f, tipos));
    const reservados = new Set();
    const respaldos = facturas.map((e, i) => (e.origen ? planRespaldo(e.origen, nuevas[i].fecha, e.nombreArchivo, reservados) : null));
    respaldos.forEach((r, i) => { if (r) nuevas[i].archivo = r.relativo; });
    nuevas.forEach((f, i) => {
      const suma = totalDeLineas(f);
      if (!cuadra(suma, f.total, f.moneda)) avisos.push(`${f.proveedor} ${f.fecha}: líneas y ajustes suman ${suma} y el total es ${f.total}.`);
      if (f.clase === 'ajuste' && !facturas[i].libro && !facturas[i].movimiento) throw new ToolError('Un ajuste cuelga del libro del pago: indicar libro o movimiento.');
    });

    const fechas = nuevas.map((f) => f.fecha).sort();
    const existentes = leerDetalles('facturas', fechas[0], fechas.at(-1));
    const agregar = [];
    const omitidos = [];
    nuevas.forEach((f, i) => {
      const igual = existentes.find((e) => !e.usado && mismaFactura(e, f));
      if (igual) { igual.usado = true; omitidos.push({ fecha: f.fecha, total: f.total, proveedor: f.proveedor, id: igual.id }); return; }
      agregar.push({ f, entrada: facturas[i], respaldo: respaldos[i] });
    });

    // Cada factura busca su libro: el indicado, el del movimiento, el del movimiento que la pagó por monto y fecha, o uno nuevo sin movimiento.
    const libros = leerDetalles('libros');
    const { porId, porMovimiento } = indexarLibros(libros);
    const nuevosLibros = new Map();
    // Movimientos cuyo pago ya está cubierto por facturas: no reciben otra por emparejar.
    const yaFacturas = facturasPorLibro(leerDetalles('facturas'));
    const pagados = new Set(libros.filter((l) => l.movimiento && yaFacturas.has(l.id)
      && cuadra(yaFacturas.get(l.id).reduce((s, x) => s + x.total, 0), l.total, l.moneda)).map((l) => l.movimiento));
    const cambiosMov = new Map();
    const vinculados = [];
    const libroDe = (a) => {
      const { f, entrada } = a;
      if (entrada.libro) return porId.get(entrada.libro) ?? buscarLibro(entrada.libro);
      let m = entrada.movimiento ? buscarMovimiento(entrada.movimiento) : null;
      let por = 'a mano';
      if (!m) {
        const movs = leerTodosLosMovimientos(sumarDias(f.fecha, -VENTANA_FACTURA.antes), sumarDias(f.fecha, VENTANA_FACTURA.despues));
        const [par] = f.clase === 'factura' ? emparejar([{ ...f, nivel: 'facturas', total: f.total }], movs, pagados, VENTANA_FACTURA) : [];
        if (par) { m = par.movimiento; por = par.por; }
      }
      if (!m) {
        const nuevo = { id: nuevoId(NIVELES.libros), fecha: f.fecha, ...(f.cuenta ? { cuenta: f.cuenta } : {}), clase: 'gasto', total: f.total, moneda: f.moneda,
          comercio: f.proveedor, ...(f.de ? { de: f.de } : {}), nota: f.nota, documento: null, movimiento: null, partidas: [{ tipo: f.tipo ?? 'otros', monto: f.total }] };
        nuevosLibros.set(nuevo.id, nuevo);
        return nuevo;
      }
      if (m.clase !== 'gasto') throw new ToolError(`La factura de ${f.proveedor} es un gasto y el movimiento ${m.id} es ${m.clase}.`);
      let libro = porMovimiento.get(m.id) ?? [...nuevosLibros.values()].find((l) => l.movimiento === m.id);
      if (!libro) {
        libro = libroParaMovimiento(m, { tipo: f.tipo ?? 'otros' });
        nuevosLibros.set(libro.id, libro);
      }
      const { nuevo, cambios } = completarMovimiento(cambiosMov.get(m.id)?.nuevo ?? m, f);
      cambiosMov.set(m.id, { original: m, nuevo });
      vinculados.push({ factura: f.id, libro: libro.id, movimiento: m.id, fecha: m.fecha, total: m.total, comercio: m.comercio, por, cambios: cambios.join('; ') });
      return libro;
    };
    for (const a of agregar) {
      const libro = libroDe(a);
      if (a.f.clase === 'factura' && libro.clase !== 'gasto') throw new ToolError(`El libro ${libro.id} es de ingreso.`);
      a.f.libro = libro.id;
      a.libro = libro;
    }

    // Cuadre por libro: suma de sus facturas = monto del libro; si cuadra, sus partidas salen de las líneas.
    const todas = facturasPorLibro(leerDetalles('facturas'));
    const afectados = new Map(agregar.map((a) => [a.libro.id, a.libro]));
    const cambiosLibro = [];
    const cuadres = [];
    for (const libro of afectados.values()) {
      const facts = [...(todas.get(libro.id) ?? []), ...agregar.filter((a) => a.libro.id === libro.id).map((a) => a.f)];
      const suma = r2(facts.reduce((s, f) => s + f.total, 0));
      const completo = cuadra(suma, libro.total, libro.moneda);
      if (!completo && suma > libro.total) throw new ToolError(`Las facturas del libro ${libro.id} suman ${suma}, más que el pago (${libro.total}).`);
      if (!completo && !parcial) {
        throw new ToolError(`Las facturas del libro ${libro.id} suman ${suma} y el pago es ${libro.total}: la suma de las facturas (y ajustes) tiene que ser igual al pago. ` +
          'Anotar la factura que falta en la misma llamada, o parcial:true si va a llegar después.');
      }
      const nuevo = { ...libro, ...(completo ? { partidas: partidasDeFacturas(facts) } : {}) };
      const mov = nuevo.movimiento ? (cambiosMov.get(nuevo.movimiento)?.original ?? buscarMovimiento(nuevo.movimiento)) : null;
      cuadres.push({ libro: libro.id, facturas: facts.length, suma, pago: libro.total, completo, partidas: nuevo.partidas, problemas: completo ? problemasDeCuadre(nuevo, facts, mov) : undefined });
      if (nuevosLibros.has(libro.id)) nuevosLibros.set(libro.id, nuevo); else cambiosLibro.push({ libro, nuevo });
    }

    const filas = agregar.map((a) => detalleSinUbicacion(a.f));
    const resumen = {
      anota: filas.length,
      vinculados: vinculados.length ? vinculados : undefined,
      librosNuevos: nuevosLibros.size ? [...nuevosLibros.values()].map((l) => ({ id: l.id, total: l.total, movimiento: l.movimiento })) : undefined,
      pendientes: [...nuevosLibros.values()].filter((l) => !l.movimiento).map((l) => ({ id: l.id, fecha: l.fecha, total: l.total, comercio: l.comercio })),
      omitidosPorDuplicado: omitidos.length ? omitidos : undefined,
      cuadre: cuadres,
      avisos: avisos.length ? avisos : undefined,
    };
    if (apply !== true) return { preview: true, applied: false, ...resumen, facturas: filas, message: 'Llamar de nuevo con apply:true para guardar.' };
    if (!filas.length) return { applied: false, ...resumen, message: 'Todas ya estaban anotadas; no se guardó nada.' };
    for (const a of agregar) if (a.respaldo) copiarRespaldo(a.respaldo);
    for (const l of nuevosLibros.values()) appendJsonl(detalleFile('libros', l.fecha), [l]);
    for (const { libro, nuevo } of cambiosLibro) guardarDetalle(libro, detalleSinUbicacion(nuevo));
    for (const f of filas) appendJsonl(detalleFile('facturas', f.fecha), [f]);
    for (const { original, nuevo } of cambiosMov.values()) {
      if (JSON.stringify(sinDerivados(nuevo)) !== JSON.stringify(sinDerivados(original))) guardarMovimiento(original, sinDerivados(nuevo));
    }
    return { applied: true, ...resumen, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, toolText, state }) {
    // Ejemplo 1 a 1 de ADR-EBC-03: Súper Y (bac, 2026-10-25, 12000.5) con 2 categorías en sus líneas.
    const factura = {
      fecha: '2026-10-25', proveedor: 'Súper Y', cedula: '3-101-123456', numero: 'FE-55', total: 12000.5, moneda: 'CRC', origen: state.respaldo, nombreArchivo: 'factura super.jpg',
      lineas: [{ descripcion: 'Arroz', cantidad: 2, precio: 3000, impuesto: 0, total: 6000, tipo: 'supermercado' }, { descripcion: 'Acetaminofén', total: 6000.5, tipo: 'comida' }],
    };
    const prev = toolJson(await callTool('bancos_factura', { facturas: [{ ...factura, total: 15000 }], parcial: true }));
    check('bancos_factura avisa si las líneas no suman el total y sin pago queda pendiente', prev.preview === true && prev.avisos?.length === 1 && prev.pendientes?.length === 1, JSON.stringify(prev).slice(0, 300));
    const ok = toolJson(await callTool('bancos_factura', { facturas: [factura], apply: true }));
    check('bancos_factura guarda, se vincula por monto y fecha y las partidas salen de las líneas',
      ok.applied === true && ok.vinculados?.[0]?.por === 'monto y fecha' && !ok.avisos && ok.cuadre?.[0]?.completo === true && ok.cuadre[0].partidas.length === 2, JSON.stringify(ok).slice(0, 400));
    state.factura = ok.vinculados?.[0]?.factura;
    state.libroSuper = ok.vinculados?.[0]?.libro;
    const malo = toolJson(await callTool('bancos_factura', { facturas: [{ ...factura, lineas: [{ descripcion: 'x', total: 1, tipo: 'inventado' }] }] }));
    check('bancos_factura rechaza tipo de línea desconocido', /desconocido/.test(malo.error ?? ''), malo.error);

    // Ejemplo 1 a varios: retiro de 5000 = factura de 3000 + ajuste de 2000 (vuelto devuelto por SINPE).
    const retiro = { fecha: '2026-11-04', proveedor: 'Súper Papi', total: 3000, moneda: 'CRC', movimiento: state.idRetiro, lineas: [{ descripcion: 'Compras de papá', total: 3000, tipo: 'comida' }] };
    const corta = toolJson(await callTool('bancos_factura', { facturas: [retiro] }));
    check('bancos_factura exige que la suma de las facturas sea el pago', /tiene que ser igual al pago/.test(corta.error ?? ''), corta.error);
    const ajuste = { fecha: '2026-11-04', proveedor: 'SINPE vuelto', clase: 'ajuste', total: 2000, moneda: 'CRC', movimiento: state.idRetiro, lineas: [{ descripcion: 'Vuelto devuelto por SINPE', total: 2000, tipo: 'supermercado' }] };
    const dos = toolJson(await callTool('bancos_factura', { facturas: [retiro, ajuste], apply: true }));
    check('bancos_factura acepta factura + ajuste y deja 2 partidas en el pago',
      dos.applied === true && dos.cuadre?.[0]?.facturas === 2 && dos.cuadre[0].completo === true && dos.cuadre[0].partidas.length === 2 && !dos.cuadre[0].problemas?.length, JSON.stringify(dos).slice(0, 500));
    state.libroRetiro = dos.vinculados?.[0]?.libro;
    const solo = toolJson(await callTool('bancos_factura', { facturas: [{ ...ajuste, movimiento: undefined, fecha: '2026-11-04' }] }));
    check('bancos_factura: un ajuste no cuelga solo', /libro o movimiento/.test(solo.error ?? ''), solo.error);
  },
};
