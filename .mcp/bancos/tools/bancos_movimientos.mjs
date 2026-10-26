import { CLASES, CUENTAS, RUBRO_PROPS, filtroRubro, leerTodosLosDetalles, leerTodosLosMovimientos, rubroDe, slug } from '../src/datos.mjs';
import { periodoProps, resolverPeriodo } from '../src/periodo.mjs';
import { mapaComprobantes, movimientosDeFactura } from '../src/vinculos.mjs';

export default {
  name: 'bancos_movimientos',
  format: 'toon',
  order: 30,
  description:
    'Lista los movimientos bancarios (nivel 1) de un mes o rango de fechas del banco, con id, cuenta, línea, referencia, rubro ' +
    '(hogar o el de, p. ej. papa) y cuántos comprobantes (c) y facturas (f) tiene cada uno. fechaReal = fecha de compra, solo referencia. ' +
    'Filtros opcionales por cuenta, clase, tipo, rubro (de / excluirDe; por defecto no excluye nada) y texto en comercio, nota o referencia. ' +
    'Respuesta TOON. Para totales usar bancos_resumen; para el detalle de comprobantes y facturas, bancos_detalles.',
  inputSchema: {
    type: 'object',
    properties: {
      ...periodoProps,
      cuenta: { type: 'string', enum: CUENTAS, description: 'Default: todas.' },
      clase: { type: 'string', enum: CLASES },
      tipo: { type: 'string', description: 'id del tipo.' },
      ...RUBRO_PROPS,
      texto: { type: 'string', description: 'Busca en comercio, nota y referencia (sin tildes ni mayúsculas).' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const { desde, hasta } = resolverPeriodo(args);
    const buscar = args.texto ? slug(args.texto) : null;
    const detalles = leerTodosLosDetalles();
    const pagos = mapaComprobantes(detalles);
    const cuenta = (nivel) => {
      const n = new Map();
      for (const d of detalles.filter((x) => x.nivel === nivel)) {
        for (const id of nivel === 'facturas' ? movimientosDeFactura(d, pagos) : [d.movimiento].filter(Boolean)) n.set(id, (n.get(id) ?? 0) + 1);
      }
      return n;
    };
    const [comp, fact] = [cuenta('comprobantes'), cuenta('facturas')];
    const movimientos = leerTodosLosMovimientos(desde, hasta, args.cuenta ? [args.cuenta] : CUENTAS)
      .filter(filtroRubro(args.de, args.excluirDe))
      .filter((m) => (!args.clase || m.clase === args.clase) && (!args.tipo || m.tipo === args.tipo)
        && (!buscar || slug(`${m.comercio} ${m.nota} ${m.referencia ?? ''}`).includes(buscar)))
      .sort((a, b) => a.fecha.localeCompare(b.fecha) || a.cuenta.localeCompare(b.cuenta) || a.linea - b.linea)
      .map((m) => ({
        id: m.id ?? '', fecha: m.fecha, fechaReal: m.fechaReal ?? '', cuenta: m.cuenta, linea: m.linea, referencia: m.referencia ?? '',
        clase: m.clase, tipo: m.tipo, total: m.total, moneda: m.moneda, comercio: m.comercio, nota: m.nota, rubro: rubroDe(m),
        c: comp.get(m.id) ?? 0, f: fact.get(m.id) ?? 0, archivo: m.archivo ?? '',
      }));
    return { desde, hasta, total: movimientos.length, movimientos };
  },
  async smoke({ callTool, check, toolText, toolJson }) {
    const text = toolText(await callTool('bancos_movimientos', { mes: '2026-10' }));
    check('bancos_movimientos lista lo anotado del mes', /total: 5/.test(text) && text.includes('Soda X'), text.slice(0, 300));
    check('bancos_movimientos marca comprobante, factura y rubro', /1001,gasto,comida,8500,CRC,Soda X,almuerzo papá,papa,1,1/.test(text) && /Súper Y,,hogar,0,1/.test(text), text.slice(0, 900));
    const filtro = toolText(await callTool('bancos_movimientos', { mes: '2026-10', texto: 'super' }));
    check('bancos_movimientos filtra por texto sin tildes', /total: 1/.test(filtro), filtro.slice(0, 120));
    const sinPapa = toolText(await callTool('bancos_movimientos', { mes: '2026-10', excluirDe: ['papa'] }));
    check('bancos_movimientos excluye un rubro', /total: 4/.test(sinPapa) && !sinPapa.includes('Soda X'), sinPapa.slice(0, 200));
    const sin = toolJson(await callTool('bancos_movimientos', {}));
    check('bancos_movimientos pide periodo', /mes o desde/.test(sin.error ?? ''), sin.error);
  },
};
