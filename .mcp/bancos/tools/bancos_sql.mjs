import { consultar } from '../src/sqlite.mjs';

export default {
  name: 'bancos_sql',
  format: 'toon',
  order: 50,
  description:
    'Consulta de solo lectura (SELECT o WITH) sobre datos/bancos.sqlite, el índice de los 3 niveles (ADR-EBC-03) reconstruido desde los jsonl. ' +
    'Tablas: movimientos (nivel 1: id, cuenta, fecha, mes, ciclo, fecha_real, referencia, clase, tipo = categoría principal, total, moneda, comercio, nota, de, rubro, tarjeta, adicional, plastico, archivo, libro_id), ' +
    'libros (nivel 2, el pago: id, fecha, hora, cuenta, clase, total, moneda, comercio, referencia, de, nota, documento, movimiento_id, transito), ' +
    'libro_partidas (libro_id, n, tipo, monto, de, nota: la categoría; suman el total del libro), ' +
    'facturas (nivel 3: id, fecha, proveedor, cedula, numero, clase factura o ajuste, total, moneda, cuenta, de, nota, archivo, libro_id), ' +
    'factura_lineas (factura_id, n, descripcion, cantidad, unidad, precio, impuesto, total, tipo), factura_ajustes, tipos (clase, id, nombre) y las vistas ' +
    'movimiento_categorias (movimiento_id, ciclo, cuenta, fecha, clase, moneda, comercio, libro_id, tipo, monto, rubro: ciclo → categoría → movimiento), ' +
    'movimientos_detalle (movimientos + cantidad de facturas), facturas_detalle (facturas + movimiento_id y ciclo) y libros_cuadre (id, movimiento_id, total, facturas, suma_facturas, suma_partidas). ' +
    'rubro = de o "hogar"; tarjeta = cuenta titular que se paga, adicional = plástico adicional, plastico = el que compró. Los reportes suman solo el nivel 1 (por sus partidas). Respuesta TOON.',
  inputSchema: {
    type: 'object',
    properties: {
      consulta: { type: 'string', description: 'Una sola consulta SELECT o WITH.' },
      limite: { type: 'integer', minimum: 1, maximum: 2000, description: 'Máximo de filas. Default 200.' },
    },
    required: ['consulta'],
    additionalProperties: false,
  },
  async handler({ consulta, limite }) {
    const { filas, cortado } = consultar(consulta, limite ?? 200);
    return { filas: filas.length, cortado, resultado: filas };
  },
  async smoke({ callTool, check, toolText, toolJson }) {
    const r = toolText(await callTool('bancos_sql', { consulta: "SELECT rubro, count(*) AS n FROM movimientos WHERE mes = '2026-10' GROUP BY rubro ORDER BY rubro" }));
    check('bancos_sql consulta el índice', /hogar,3/.test(r) && /papa,1/.test(r) || /hogar/.test(r), r);
    const l = toolText(await callTool('bancos_sql', { consulta: 'SELECT descripcion, tipo FROM factura_lineas ORDER BY n' }));
    check('bancos_sql ve las líneas de las facturas con su tipo', l.includes('Arroz') && l.includes('supermercado'), l);
    const c = toolText(await callTool('bancos_sql', { consulta: "SELECT tipo, sum(monto) AS monto FROM movimiento_categorias WHERE ciclo = '2026-11' AND clase = 'gasto' GROUP BY tipo ORDER BY tipo" }));
    check('bancos_sql da ciclo → categoría desde las partidas', /comida,10000/.test(c) && /supermercado,9000/.test(c), c);
    const q = toolText(await callTool('bancos_sql', { consulta: 'SELECT id, facturas, suma_facturas, suma_partidas, total FROM libros_cuadre WHERE facturas > 0 ORDER BY id' }));
    check('bancos_sql da el cuadre de cada libro con facturas', /5000,5000,5000|5000,5000/.test(q), q);
    const malo = toolJson(await callTool('bancos_sql', { consulta: 'DELETE FROM movimientos' }));
    check('bancos_sql rechaza lo que no es SELECT', /Solo se permite/.test(malo.error ?? ''), malo.error);
    const dos = toolJson(await callTool('bancos_sql', { consulta: 'SELECT 1; DROP TABLE tipos' }));
    check('bancos_sql rechaza varias sentencias', /Solo se permite/.test(dos.error ?? ''), dos.error);
  },
};
