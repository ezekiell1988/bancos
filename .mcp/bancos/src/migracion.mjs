// Migración al modelo de 3 niveles de ADR-EBC-03: cada movimiento con tipo pasa a un libro (nivel 2) con una partida,
// los comprobantes se vuelven libros con documento y las facturas cuelgan de un libro. Idempotente: no corre si ya hay libros.
// Modo vista previa por defecto; aplicar exige un respaldo de datos/ hecho antes.
import { existsSync, readdirSync, renameSync, rmSync } from 'node:fs';
import path from 'node:path';
import {
  CUENTAS, ToolError, appendJsonl, cuadra, detalleFile, diaFile, getDatosDir, leerTodosLosTipos, r2, readJsonl, writeJsonl,
} from './datos.mjs';
import { partidasDeFacturas } from './vinculos.mjs';
import { cicloDe } from './sqlite.mjs';

/** Todos los jsonl de datos/<carpeta>/AAAA-MM/AAAA-MM-DD.jsonl, con su ruta. */
function archivos(carpeta) {
  const base = path.join(getDatosDir(), carpeta);
  if (!existsSync(base)) return [];
  return readdirSync(base).filter((d) => /^\d{4}-\d{2}$/.test(d)).sort()
    .flatMap((mes) => readdirSync(path.join(base, mes)).filter((n) => /^\d{4}-\d{2}-\d{2}\.jsonl$/.test(n)).sort().map((n) => path.join(base, mes, n)));
}
const leerCarpeta = (carpeta) => archivos(carpeta).flatMap((file) => readJsonl(file).map((r) => ({ ...r, __file: file })));
const limpio = ({ __file: _f, ...r }) => r;

const idLibro = (id) => `l-${id.slice(2)}`;

/** Arma el plan de migración y el informe sin escribir nada. */
export function planMigracion() {
  if (archivos('libros').length) throw new ToolError('datos/libros ya tiene datos: la migración ya se aplicó.');
  const tipos = leerTodosLosTipos();
  const valido = (clase, tipo) => (tipos[clase]?.some((t) => t.id === tipo) ? tipo : 'otros');
  const movs = CUENTAS.flatMap((cuenta) => leerCarpeta(cuenta).map((m) => ({ ...m, cuenta })));
  const comps = leerCarpeta('comprobantes');
  const facts = leerCarpeta('facturas');
  const movPorId = new Map(movs.map((m) => [m.id, m]));
  const informe = { variasPagos: [], noCuadran: [], lineasNoSuman: [], movimientoRepetido: [], movimientoDesconocido: [] };

  // Nivel 2 desde los comprobantes (1 a 1 con su movimiento).
  const libros = [];
  const libroDeMov = new Map();
  const libroDeComp = new Map();
  for (const c of comps) {
    let mov = c.movimiento ? movPorId.get(c.movimiento) : null;
    if (c.movimiento && !mov) { informe.movimientoDesconocido.push({ comprobante: c.id, movimiento: c.movimiento }); mov = null; }
    if (mov && libroDeMov.has(mov.id)) { informe.movimientoRepetido.push({ comprobante: c.id, movimiento: mov.id }); mov = null; }
    const clase = mov?.clase ?? c.clase;
    const total = mov ? mov.total : c.total;
    const tipo = valido(clase, mov?.tipo ?? c.tipo);
    const libro = {
      id: idLibro(c.id), fecha: c.fecha, ...(c.hora ? { hora: c.hora } : {}), ...((c.cuenta ?? mov?.cuenta) ? { cuenta: c.cuenta ?? mov.cuenta } : {}),
      clase, total, moneda: c.moneda, comercio: c.comercio ?? '', ...(c.referencia ? { referencia: c.referencia } : {}), ...(c.de ? { de: c.de } : {}),
      nota: c.nota ?? '', documento: c.archivo ?? null, movimiento: mov?.id ?? null, partidas: [{ tipo, monto: total }],
    };
    libros.push(libro);
    libroDeComp.set(c.id, libro);
    if (mov) libroDeMov.set(mov.id, libro);
  }
  // Movimientos sin pago anotado: libro sin documento solo para la categoría.
  for (const m of movs) {
    if (libroDeMov.has(m.id)) continue;
    const libro = {
      id: idLibro(m.id), fecha: m.fecha, cuenta: m.cuenta, clase: m.clase, total: m.total, moneda: m.moneda, comercio: m.comercio ?? '',
      ...(m.referencia ? { referencia: m.referencia } : {}), ...(m.de ? { de: m.de } : {}), nota: m.nota ?? '', documento: null, movimiento: m.id,
      partidas: [{ tipo: valido(m.clase, m.tipo), monto: m.total }],
    };
    libros.push(libro);
    libroDeMov.set(m.id, libro);
  }

  // Nivel 3: cada factura cuelga de un libro.
  const nuevasFacturas = facts.map((f) => {
    let libro;
    const pagos = (f.comprobantes ?? []).map((id) => libroDeComp.get(id)).filter(Boolean);
    if (pagos.length > 1) {
      libro = [...pagos].sort((a, b) => b.total - a.total)[0];
      informe.variasPagos.push({ factura: f.id, proveedor: f.comercio, total: f.total, libros: pagos.map((l) => `${l.id}:${l.total}`), colgadaDe: libro.id });
    } else if (pagos.length === 1) [libro] = pagos;
    else if (f.movimiento && libroDeMov.has(f.movimiento)) libro = libroDeMov.get(f.movimiento);
    else {
      libro = { id: idLibro(f.id), fecha: f.fecha, ...(f.cuenta ? { cuenta: f.cuenta } : {}), clase: 'gasto', total: f.total, moneda: f.moneda, comercio: f.comercio ?? '',
        ...(f.de ? { de: f.de } : {}), nota: f.nota ?? '', documento: null, movimiento: null, partidas: [{ tipo: valido('gasto', f.tipo), monto: f.total }] };
      libros.push(libro);
    }
    const base = libro.partidas[0]?.tipo;
    const lineas = (f.lineas ?? []).map((l) => ({
      descripcion: l.descripcion, ...(l.cantidad !== undefined ? { cantidad: l.cantidad } : {}), ...(l.unidad ? { unidad: l.unidad } : {}),
      ...(l.precio !== undefined ? { precio: l.precio } : {}), total: l.total, tipo: valido('gasto', l.tipo ?? f.tipo ?? base),
    }));
    const suma = r2(lineas.reduce((s, l) => s + l.total, 0));
    if (!cuadra(suma, f.total, f.moneda)) informe.lineasNoSuman.push({ factura: f.id, proveedor: f.comercio, lineas: suma, total: f.total });
    const { comercio, comprobantes: _c, movimiento: _m, lineas: _l, tipo: _t, __file, ...resto } = f;
    return { fila: { ...resto, proveedor: comercio ?? '', clase: 'factura', libro: libro.id, lineas }, libro, __file };
  });

  // Cuadre por libro: suma de facturas = pago; si cuadra, las partidas salen de las líneas por categoría.
  const porLibro = new Map();
  for (const x of nuevasFacturas) porLibro.set(x.libro.id, [...(porLibro.get(x.libro.id) ?? []), x.fila]);
  for (const [id, facturas] of porLibro) {
    const libro = libros.find((l) => l.id === id);
    const suma = r2(facturas.reduce((s, f) => s + f.total, 0));
    if (!cuadra(suma, libro.total, libro.moneda)) {
      informe.noCuadran.push({ libro: id, movimiento: libro.movimiento, pago: libro.total, facturas: suma, diferencia: r2(libro.total - suma), cuelgan: facturas.map((f) => f.id) });
      continue;
    }
    const partidas = partidasDeFacturas(facturas);
    const diff = r2(libro.total - partidas.reduce((s, p) => s + p.monto, 0));
    if (partidas.length) {
      const mayor = partidas.reduce((a, b) => (b.monto > a.monto ? b : a));
      mayor.monto = r2(mayor.monto + diff);
      if (partidas.every((p) => p.monto > 0)) libro.partidas = partidas;
    }
  }

  // Totales por ciclo: antes (movimientos) = después (partidas de los libros con movimiento).
  const sumar = (mapa, clave, monto) => mapa.set(clave, r2((mapa.get(clave) ?? 0) + monto));
  const antes = new Map();
  const despues = new Map();
  for (const m of movs) sumar(antes, `${cicloDe(m.fecha)}|${m.clase}|${m.moneda}`, m.total);
  for (const l of libros.filter((x) => x.movimiento)) {
    const m = movPorId.get(l.movimiento);
    for (const p of l.partidas) sumar(despues, `${cicloDe(m.fecha)}|${m.clase}|${m.moneda}`, p.monto);
  }
  const claves = [...new Set([...antes.keys(), ...despues.keys()])].sort();
  const totales = claves.map((k) => ({ ciclo_clase_moneda: k, antes: antes.get(k) ?? 0, despues: despues.get(k) ?? 0, igual: cuadra(antes.get(k) ?? 0, despues.get(k) ?? 0, k.split('|')[2]) }));

  const resumen = {
    movimientos: movs.length, comprobantes: comps.length, facturas: facts.length,
    libros: libros.length, librosConDocumento: libros.filter((l) => l.documento).length, librosSinDocumento: libros.filter((l) => !l.documento && l.movimiento).length,
    librosSinMovimiento: libros.filter((l) => !l.movimiento).length,
    librosEnOtros: libros.filter((l) => l.partidas.some((p) => p.tipo === 'otros')).length,
    librosConPartidasDeFacturas: libros.filter((l) => l.partidas.length > 1 || porLibro.has(l.id)).length,
    totalesIguales: totales.every((t) => t.igual),
  };
  return { resumen, totales, informe, movs, libros, nuevasFacturas, comps };
}

/** Escribe los libros, las facturas nuevas y los movimientos sin tipo; aparta datos/comprobantes. */
export function aplicarMigracion(plan) {
  if (!plan.resumen.totalesIguales) throw new ToolError('Los totales por ciclo no dan igual antes y después: no se aplica.');
  for (const l of plan.libros) appendJsonl(detalleFile('libros', l.fecha), [l]);
  // Facturas: se reescribe cada archivo con el formato nuevo.
  const porArchivo = new Map();
  for (const x of plan.nuevasFacturas) porArchivo.set(x.__file, [...(porArchivo.get(x.__file) ?? []), x.fila]);
  for (const [file, filas] of porArchivo) writeJsonl(file, filas);
  // Movimientos: ya no llevan tipo (la categoría vive en el libro).
  const movPorArchivo = new Map();
  for (const m of plan.movs) movPorArchivo.set(diaFile(m.cuenta, m.fecha), [...(movPorArchivo.get(diaFile(m.cuenta, m.fecha)) ?? []), m]);
  for (const [file, filas] of movPorArchivo) {
    const originales = readJsonl(file);
    writeJsonl(file, originales.map((o) => { const { tipo: _t, ...resto } = o; return resto; }));
  }
  const viejo = path.join(getDatosDir(), 'comprobantes');
  const apartado = path.join(getDatosDir(), 'comprobantes-antes-libros');
  if (existsSync(viejo)) { rmSync(apartado, { recursive: true, force: true }); renameSync(viejo, apartado); }
}
