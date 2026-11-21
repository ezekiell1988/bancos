// Índice SQLite derivado de los jsonl (ADR-05): se reconstruye completo tras cada escritura del MCP.
// Nunca se escribe en la base directo; borrarla no pierde nada. Niveles de ADR-EBC-03: movimientos, libros (+ libro_partidas) y facturas (+ líneas).
import { existsSync, renameSync, rmSync } from 'node:fs';
import path from 'node:path';
import { DatabaseSync } from 'node:sqlite';
import { CLASES, HOGAR, ToolError, getDatosDir, leerDetalles, leerTipos, leerTodosLosMovimientos } from './datos.mjs';

export const sqliteFile = () => path.join(getDatosDir(), 'bancos.sqlite');

const ESQUEMA = `
CREATE TABLE tipos (clase TEXT, id TEXT, nombre TEXT, PRIMARY KEY (clase, id));
CREATE TABLE movimientos (
  id TEXT PRIMARY KEY, cuenta TEXT, fecha TEXT, mes TEXT, ciclo TEXT, fecha_real TEXT, referencia TEXT, clase TEXT, tipo TEXT,
  total REAL, moneda TEXT, comercio TEXT, nota TEXT, de TEXT, rubro TEXT, tarjeta TEXT, adicional TEXT, plastico TEXT, archivo TEXT, libro_id TEXT);
CREATE TABLE libros (
  id TEXT PRIMARY KEY, fecha TEXT, hora TEXT, cuenta TEXT, clase TEXT, total REAL, moneda TEXT, comercio TEXT,
  referencia TEXT, de TEXT, nota TEXT, documento TEXT, movimiento_id TEXT, transito TEXT);
CREATE TABLE libro_partidas (libro_id TEXT, n INTEGER, tipo TEXT, monto REAL, de TEXT, nota TEXT, PRIMARY KEY (libro_id, n));
CREATE TABLE facturas (
  id TEXT PRIMARY KEY, fecha TEXT, proveedor TEXT, cedula TEXT, numero TEXT, clase TEXT, total REAL, moneda TEXT, cuenta TEXT,
  de TEXT, nota TEXT, archivo TEXT, libro_id TEXT);
CREATE TABLE factura_lineas (
  factura_id TEXT, n INTEGER, descripcion TEXT, cantidad REAL, unidad TEXT, precio REAL, impuesto REAL, total REAL, tipo TEXT,
  PRIMARY KEY (factura_id, n));
CREATE TABLE factura_ajustes (factura_id TEXT, n INTEGER, descripcion TEXT, total REAL, PRIMARY KEY (factura_id, n));
CREATE INDEX movimientos_fecha ON movimientos (fecha);
CREATE INDEX movimientos_ciclo ON movimientos (ciclo);
CREATE INDEX movimientos_libro ON movimientos (libro_id);
CREATE INDEX libros_mov ON libros (movimiento_id);
CREATE INDEX facturas_libro ON facturas (libro_id);
-- Nivel 1 repartido por categoría: ciclo → categoría → movimiento. Sin libro, todo el total va a «otros».
CREATE VIEW movimiento_categorias AS
  SELECT m.id AS movimiento_id, m.ciclo, m.cuenta, m.fecha, m.clase, m.moneda, m.comercio, m.libro_id,
    coalesce(p.tipo, 'otros') AS tipo, coalesce(p.monto, m.total) AS monto, coalesce(p.de, m.de, 'hogar') AS rubro
  FROM movimientos m LEFT JOIN libro_partidas p ON p.libro_id = m.libro_id;
CREATE VIEW movimientos_detalle AS
  SELECT m.*, (SELECT count(*) FROM facturas f WHERE f.libro_id = m.libro_id) AS facturas
  FROM movimientos m;
-- Facturas con su pago y su movimiento: categoría → movimiento → factura.
CREATE VIEW facturas_detalle AS
  SELECT f.*, l.movimiento_id, m.ciclo, m.cuenta AS movimiento_cuenta
  FROM facturas f LEFT JOIN libros l ON l.id = f.libro_id LEFT JOIN movimientos m ON m.id = l.movimiento_id;
CREATE VIEW libros_cuadre AS
  SELECT l.id, l.movimiento_id, l.total, l.moneda,
    (SELECT count(*) FROM facturas f WHERE f.libro_id = l.id) AS facturas,
    round(coalesce((SELECT sum(f.total) FROM facturas f WHERE f.libro_id = l.id), 0), 2) AS suma_facturas,
    round(coalesce((SELECT sum(p.monto) FROM libro_partidas p WHERE p.libro_id = l.id), 0), 2) AS suma_partidas
  FROM libros l;
`;

const v = (x) => (x === undefined || x === '' ? null : x);

/** Ciclo de una fecha: del 19 de un mes al 18 del siguiente se llama como el mes en que termina. */
export function cicloDe(fecha) {
  const [a, m, d] = fecha.split('-').map(Number);
  if (d < 19) return fecha.slice(0, 7);
  return m === 12 ? `${a + 1}-01` : `${a}-${String(m + 1).padStart(2, '0')}`;
}

/** Reconstruye datos/bancos.sqlite desde los jsonl (en un archivo temporal que luego reemplaza al actual). */
export function reconstruirSqlite() {
  const destino = sqliteFile();
  const tmp = `${destino}.tmp`;
  rmSync(tmp, { force: true });
  const db = new DatabaseSync(tmp);
  const n = { tipos: 0, movimientos: 0, libros: 0, partidas: 0, facturas: 0, lineas: 0 };
  try {
    db.exec(ESQUEMA);
    db.exec('BEGIN');
    const tipo = db.prepare('INSERT INTO tipos VALUES (?, ?, ?)');
    for (const c of CLASES) for (const t of leerTipos(c)) { tipo.run(c, t.id, t.nombre); n.tipos++; }
    const mov = db.prepare('INSERT INTO movimientos VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)');
    for (const m of leerTodosLosMovimientos()) {
      mov.run(m.id, m.cuenta, m.fecha, m.fecha.slice(0, 7), cicloDe(m.fecha), v(m.fechaReal), v(m.referencia), m.clase, v(m.tipo), m.total, m.moneda,
        v(m.comercio), v(m.nota), v(m.de), m.de || HOGAR, v(m.tarjeta), v(m.adicional), v(m.adicional || m.tarjeta), v(m.archivo), v(m.libro));
      n.movimientos++;
    }
    const lib = db.prepare('INSERT INTO libros VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)');
    const par = db.prepare('INSERT INTO libro_partidas VALUES (?, ?, ?, ?, ?, ?)');
    for (const l of leerDetalles('libros')) {
      lib.run(l.id, l.fecha, v(l.hora), v(l.cuenta), l.clase, l.total, l.moneda, v(l.comercio), v(l.referencia), v(l.de), v(l.nota), v(l.documento),
        v(l.movimiento), v(l.transito?.razon));
      n.libros++;
      (l.partidas ?? []).forEach((p, i) => { par.run(l.id, i + 1, p.tipo, p.monto, v(p.de), v(p.nota)); n.partidas++; });
    }
    const fac = db.prepare('INSERT INTO facturas VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)');
    const lin = db.prepare('INSERT INTO factura_lineas VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)');
    const aju = db.prepare('INSERT INTO factura_ajustes VALUES (?, ?, ?, ?)');
    for (const f of leerDetalles('facturas')) {
      fac.run(f.id, f.fecha, v(f.proveedor), v(f.cedula), v(f.numero), f.clase ?? 'factura', f.total, f.moneda, v(f.cuenta), v(f.de), v(f.nota), v(f.archivo), v(f.libro));
      n.facturas++;
      (f.lineas ?? []).forEach((l, i) => {
        lin.run(f.id, i + 1, l.descripcion, v(l.cantidad), v(l.unidad), v(l.precio), v(l.impuesto), l.total, v(l.tipo));
        n.lineas++;
      });
      (f.ajustes ?? []).forEach((a, i) => aju.run(f.id, i + 1, a.descripcion, a.total));
    }
    db.exec('COMMIT');
  } finally {
    db.close();
  }
  renameSync(tmp, destino);
  return { archivo: path.relative(getDatosDir(), destino), ...n };
}

/** Reconstruye sin cortar la escritura que la llamó: si falla, devuelve el aviso. */
export function refrescarSqlite() {
  try { reconstruirSqlite(); return undefined; } catch (e) { return `No se pudo reconstruir bancos.sqlite: ${e.message}`; }
}

/** Corre una consulta de solo lectura (SELECT o WITH) sobre la base. */
export function consultar(sql, limite = 200) {
  const texto = String(sql ?? '').trim().replace(/;\s*$/, '');
  if (!/^(select|with)\b/i.test(texto) || texto.includes(';')) throw new ToolError('Solo se permite una consulta SELECT o WITH.');
  if (!existsSync(sqliteFile())) reconstruirSqlite();
  const db = new DatabaseSync(sqliteFile(), { readOnly: true });
  try {
    const filas = [];
    for (const fila of db.prepare(texto).iterate()) {
      if (filas.length >= limite) return { filas, cortado: true };
      filas.push({ ...fila });
    }
    return { filas, cortado: false };
  } finally {
    db.close();
  }
}
