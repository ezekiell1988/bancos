// Índice SQLite derivado de los jsonl (ADR-05): se reconstruye completo tras cada escritura del MCP.
// Nunca se escribe en la base directo; borrarla no pierde nada. Pagos de facturas en factura_pagos (ADR-06).
import { existsSync, renameSync, rmSync } from 'node:fs';
import path from 'node:path';
import { DatabaseSync } from 'node:sqlite';
import { CLASES, HOGAR, ToolError, getDatosDir, leerDetalles, leerTipos, leerTodosLosMovimientos } from './datos.mjs';

export const sqliteFile = () => path.join(getDatosDir(), 'bancos.sqlite');

const ESQUEMA = `
CREATE TABLE tipos (clase TEXT, id TEXT, nombre TEXT, PRIMARY KEY (clase, id));
CREATE TABLE movimientos (
  id TEXT PRIMARY KEY, cuenta TEXT, fecha TEXT, mes TEXT, fecha_real TEXT, referencia TEXT, clase TEXT, tipo TEXT,
  total REAL, moneda TEXT, comercio TEXT, nota TEXT, de TEXT, rubro TEXT, archivo TEXT);
CREATE TABLE comprobantes (
  id TEXT PRIMARY KEY, fecha TEXT, hora TEXT, cuenta TEXT, clase TEXT, total REAL, moneda TEXT, comercio TEXT,
  referencia TEXT, tipo TEXT, de TEXT, nota TEXT, archivo TEXT, movimiento_id TEXT);
CREATE TABLE facturas (
  id TEXT PRIMARY KEY, fecha TEXT, comercio TEXT, numero TEXT, total REAL, moneda TEXT, cuenta TEXT, tipo TEXT,
  de TEXT, nota TEXT, archivo TEXT, movimiento_id TEXT);
CREATE TABLE factura_pagos (factura_id TEXT, comprobante_id TEXT, PRIMARY KEY (factura_id, comprobante_id));
CREATE TABLE factura_lineas (
  factura_id TEXT, n INTEGER, descripcion TEXT, cantidad REAL, unidad TEXT, precio REAL, total REAL, tipo TEXT,
  PRIMARY KEY (factura_id, n));
CREATE INDEX movimientos_fecha ON movimientos (fecha);
CREATE INDEX comprobantes_mov ON comprobantes (movimiento_id);
CREATE INDEX facturas_mov ON facturas (movimiento_id);
CREATE INDEX factura_pagos_comp ON factura_pagos (comprobante_id);
CREATE VIEW movimientos_detalle AS
  SELECT m.*,
    (SELECT count(*) FROM comprobantes c WHERE c.movimiento_id = m.id) AS comprobantes,
    (SELECT count(DISTINCT f.id) FROM facturas f LEFT JOIN factura_pagos p ON p.factura_id = f.id
       LEFT JOIN comprobantes c ON c.id = p.comprobante_id WHERE f.movimiento_id = m.id OR c.movimiento_id = m.id) AS facturas
  FROM movimientos m;
CREATE VIEW facturas_saldo AS
  SELECT *, round(total - pagado, 2) AS saldo FROM (
    SELECT f.id, f.fecha, f.comercio, f.numero, f.total, f.moneda, f.de, count(p.comprobante_id) AS pagos,
      CASE WHEN count(p.comprobante_id) = 0 THEN (CASE WHEN f.movimiento_id IS NULL THEN 0 ELSE f.total END)
        ELSE round(coalesce(sum(c.total), 0), 2) END AS pagado
    FROM facturas f LEFT JOIN factura_pagos p ON p.factura_id = f.id LEFT JOIN comprobantes c ON c.id = p.comprobante_id
    GROUP BY f.id);
`;

const v = (x) => (x === undefined || x === '' ? null : x);

/** Reconstruye datos/bancos.sqlite desde los jsonl (en un archivo temporal que luego reemplaza al actual). */
export function reconstruirSqlite() {
  const destino = sqliteFile();
  const tmp = `${destino}.tmp`;
  rmSync(tmp, { force: true });
  const db = new DatabaseSync(tmp);
  const n = { tipos: 0, movimientos: 0, comprobantes: 0, facturas: 0, lineas: 0, pagos: 0 };
  try {
    db.exec(ESQUEMA);
    db.exec('BEGIN');
    const tipo = db.prepare('INSERT INTO tipos VALUES (?, ?, ?)');
    for (const c of CLASES) for (const t of leerTipos(c)) { tipo.run(c, t.id, t.nombre); n.tipos++; }
    const mov = db.prepare('INSERT INTO movimientos VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)');
    for (const m of leerTodosLosMovimientos()) {
      mov.run(m.id, m.cuenta, m.fecha, m.fecha.slice(0, 7), v(m.fechaReal), v(m.referencia), m.clase, m.tipo, m.total, m.moneda,
        v(m.comercio), v(m.nota), v(m.de), m.de || HOGAR, v(m.archivo));
      n.movimientos++;
    }
    const comp = db.prepare('INSERT INTO comprobantes VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)');
    for (const c of leerDetalles('comprobantes')) {
      comp.run(c.id, c.fecha, v(c.hora), v(c.cuenta), c.clase, c.total, c.moneda, v(c.comercio), v(c.referencia), v(c.tipo), v(c.de),
        v(c.nota), v(c.archivo), v(c.movimiento));
      n.comprobantes++;
    }
    const fac = db.prepare('INSERT INTO facturas VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)');
    const pago = db.prepare('INSERT INTO factura_pagos VALUES (?, ?)');
    const lin = db.prepare('INSERT INTO factura_lineas VALUES (?, ?, ?, ?, ?, ?, ?, ?)');
    for (const f of leerDetalles('facturas')) {
      fac.run(f.id, f.fecha, v(f.comercio), v(f.numero), f.total, f.moneda, v(f.cuenta), v(f.tipo), v(f.de), v(f.nota), v(f.archivo),
        v(f.movimiento));
      n.facturas++;
      for (const c of f.comprobantes ?? []) { pago.run(f.id, c); n.pagos++; }
      (f.lineas ?? []).forEach((l, i) => {
        lin.run(f.id, i + 1, l.descripcion, v(l.cantidad), v(l.unidad), v(l.precio), l.total, v(l.tipo));
        n.lineas++;
      });
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
