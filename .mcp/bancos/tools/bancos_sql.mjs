import { consultar } from '../src/sqlite.mjs';

export default {
  name: 'bancos_sql',
  format: 'toon',
  order: 50,
  description:
    'Consulta de solo lectura (SELECT o WITH) sobre datos/bancos.sqlite, el índice de los 3 niveles reconstruido desde los jsonl. ' +
    'Tablas: movimientos (id, cuenta, fecha, mes, fecha_real, referencia, clase, tipo, total, moneda, comercio, nota, de, rubro, archivo), ' +
    'comprobantes (id, fecha, hora, cuenta, clase, total, moneda, comercio, referencia, tipo, de, nota, archivo, movimiento_id), ' +
    'facturas (id, fecha, comercio, numero, total, moneda, cuenta, tipo, de, nota, archivo, movimiento_id = vínculo directo sin pagos), ' +
    'factura_pagos (factura_id, comprobante_id: los comprobantes que pagan cada factura, varios a varios), ' +
    'factura_lineas (factura_id, n, descripcion, cantidad, unidad, precio, total, tipo), tipos (clase, id, nombre) y las vistas ' +
    'movimientos_detalle (movimientos + cantidad de comprobantes y facturas) y facturas_saldo (id, fecha, comercio, numero, total, moneda, de, ' +
    'pagos, pagado, saldo). rubro = de o "hogar". Los reportes suman solo movimientos. Respuesta TOON.',
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
    check('bancos_sql consulta el índice', /hogar,2/.test(r) && /papa,2/.test(r), r);
    const l = toolText(await callTool('bancos_sql', { consulta: 'SELECT descripcion FROM factura_lineas ORDER BY n' }));
    check('bancos_sql ve las líneas de las facturas', l.includes('Arroz'), l);
    const s = toolText(await callTool('bancos_sql', { consulta: "SELECT comercio, pagos, pagado, saldo FROM facturas_saldo WHERE comercio = 'Taller Z'" }));
    check('bancos_sql da el saldo de las facturas', /Taller Z,1,8500,6500/.test(s), s);
    const malo = toolJson(await callTool('bancos_sql', { consulta: 'DELETE FROM movimientos' }));
    check('bancos_sql rechaza lo que no es SELECT', /Solo se permite/.test(malo.error ?? ''), malo.error);
    const dos = toolJson(await callTool('bancos_sql', { consulta: 'SELECT 1; DROP TABLE tipos' }));
    check('bancos_sql rechaza varias sentencias', /Solo se permite/.test(dos.error ?? ''), dos.error);
  },
};
