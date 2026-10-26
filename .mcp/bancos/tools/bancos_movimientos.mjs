import { CLASES, CUENTAS, leerMovimientos, slug } from '../src/datos.mjs';
import { periodoProps, resolverPeriodo } from '../src/periodo.mjs';

export default {
  name: 'bancos_movimientos',
  format: 'toon',
  order: 30,
  description:
    'Lista los movimientos anotados de un mes o rango de fechas, con cuenta y línea (para bancos_borrar_movimiento). ' +
    'Filtros opcionales por cuenta, clase, tipo y texto en comercio o nota. Respuesta TOON. Para totales usar bancos_resumen.',
  inputSchema: {
    type: 'object',
    properties: {
      ...periodoProps,
      cuenta: { type: 'string', enum: CUENTAS, description: 'Default: todas.' },
      clase: { type: 'string', enum: CLASES },
      tipo: { type: 'string', description: 'id del tipo.' },
      texto: { type: 'string', description: 'Busca en comercio y nota (sin tildes ni mayúsculas).' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const { desde, hasta } = resolverPeriodo(args);
    const buscar = args.texto ? slug(args.texto) : null;
    const movimientos = (args.cuenta ? [args.cuenta] : CUENTAS)
      .flatMap((c) => leerMovimientos(c, desde, hasta))
      .filter((m) => (!args.clase || m.clase === args.clase) && (!args.tipo || m.tipo === args.tipo)
        && (!buscar || slug(`${m.comercio} ${m.nota}`).includes(buscar)))
      .sort((a, b) => a.fecha.localeCompare(b.fecha) || a.cuenta.localeCompare(b.cuenta) || a.linea - b.linea)
      .map((m) => ({ fecha: m.fecha, cuenta: m.cuenta, linea: m.linea, clase: m.clase, tipo: m.tipo, total: m.total, moneda: m.moneda, comercio: m.comercio, nota: m.nota, archivo: m.archivo ?? '' }));
    return { desde, hasta, total: movimientos.length, movimientos };
  },
  async smoke({ callTool, check, toolText, toolJson }) {
    const text = toolText(await callTool('bancos_movimientos', { mes: '2026-10' }));
    check('bancos_movimientos lista lo anotado del mes', /total: 3/.test(text) && text.includes('Soda X'), text.slice(0, 200));
    const filtro = toolText(await callTool('bancos_movimientos', { mes: '2026-10', texto: 'super' }));
    check('bancos_movimientos filtra por texto sin tildes', /total: 1/.test(filtro), filtro.slice(0, 120));
    const sin = toolJson(await callTool('bancos_movimientos', {}));
    check('bancos_movimientos pide periodo', /mes o desde/.test(sin.error ?? ''), sin.error);
  },
};
