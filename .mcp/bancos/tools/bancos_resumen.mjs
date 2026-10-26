import { CUENTAS, leerMovimientos } from '../src/datos.mjs';
import { periodoProps, resolverPeriodo } from '../src/periodo.mjs';

const r2 = (n) => Math.round(n * 100) / 100;

export default {
  name: 'bancos_resumen',
  format: 'toon',
  order: 31,
  description:
    'Totales de un mes o rango: por clase y tipo (con cantidad) y balance ingresos - gastos por moneda. ' +
    'Filtro opcional por cuenta. Respuesta TOON. Para ver cada movimiento usar bancos_movimientos.',
  inputSchema: {
    type: 'object',
    properties: {
      ...periodoProps,
      cuenta: { type: 'string', enum: CUENTAS, description: 'Default: todas.' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const { desde, hasta } = resolverPeriodo(args);
    const movs = (args.cuenta ? [args.cuenta] : CUENTAS).flatMap((c) => leerMovimientos(c, desde, hasta));
    const porTipo = new Map();
    const balance = new Map();
    for (const m of movs) {
      const k = `${m.clase}|${m.tipo}|${m.moneda}`;
      const t = porTipo.get(k) ?? { clase: m.clase, tipo: m.tipo, moneda: m.moneda, cantidad: 0, total: 0 };
      t.cantidad += 1; t.total = r2(t.total + m.total); porTipo.set(k, t);
      const b = balance.get(m.moneda) ?? { moneda: m.moneda, ingresos: 0, gastos: 0, balance: 0 };
      if (m.clase === 'ingreso') b.ingresos = r2(b.ingresos + m.total); else b.gastos = r2(b.gastos + m.total);
      b.balance = r2(b.ingresos - b.gastos); balance.set(m.moneda, b);
    }
    const tipos = [...porTipo.values()].sort((a, b) => a.clase.localeCompare(b.clase) || b.total - a.total);
    return { desde, hasta, cuenta: args.cuenta ?? 'todas', movimientos: movs.length, balance: [...balance.values()], tipos };
  },
  async smoke({ callTool, check, toolText }) {
    const text = toolText(await callTool('bancos_resumen', { mes: '2026-10' }));
    check('bancos_resumen calcula el balance en CRC', text.includes('CRC,500000,20500.5,479499.5'), text.slice(0, 300));
    const bac = toolText(await callTool('bancos_resumen', { mes: '2026-10', cuenta: 'bac' }));
    check('bancos_resumen filtra por cuenta', /movimientos: 2/.test(bac), bac.slice(0, 120));
  },
};
