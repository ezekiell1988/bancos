import { CLASES, CUENTAS, RUBRO_PROPS, filtroRubro, leerTodosLosMovimientos, plasticoUsado, rubroDe, slug } from '../src/datos.mjs';
import { periodoProps, resolverPeriodo } from '../src/periodo.mjs';
import { facturasPorLibro } from '../src/vinculos.mjs';

export default {
  name: 'bancos_movimientos',
  format: 'toon',
  order: 30,
  description:
    'Lista los movimientos bancarios (nivel 1) de un ciclo (19 al 18), mes o rango de fechas del banco, con id, cuenta, línea, referencia, rubro ' +
    '(hogar o el de, p. ej. papa), tarjeta y plástico (el adicional o el titular) su libro (nivel 2, donde vive la categoría: tipo = la principal y partidas = todas) y cuántas facturas (f) cuelgan de él. fechaReal = fecha de compra, solo referencia. ' +
    'Filtros opcionales por cuenta, clase, tipo, tarjeta, plástico, rubro (de / excluirDe; por defecto no excluye nada) y texto en comercio, nota o referencia. ' +
    'Respuesta TOON. Para totales usar bancos_resumen; para el detalle de libros y facturas, bancos_detalles.',
  inputSchema: {
    type: 'object',
    properties: {
      ...periodoProps,
      cuenta: { type: 'string', enum: CUENTAS, description: 'Default: todas.' },
      clase: { type: 'string', enum: CLASES },
      tipo: { type: 'string', description: 'id del tipo.' },
      tarjeta: { type: 'string', description: 'Cuenta de tarjeta (4 dígitos del titular): todo lo que se paga junto, incluidos los adicionales.' },
      plastico: { type: 'string', description: 'Solo lo de ese plástico (4 dígitos): el de un adicional, o el del titular sin sus adicionales.' },
      ...RUBRO_PROPS,
      texto: { type: 'string', description: 'Busca en comercio, nota y referencia (sin tildes ni mayúsculas).' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const { desde, hasta } = resolverPeriodo(args);
    const buscar = args.texto ? slug(args.texto) : null;
    const facturas = facturasPorLibro();
    const nFacturas = (m) => (m.libro ? facturas.get(m.libro)?.length ?? 0 : 0);
    const movimientos = leerTodosLosMovimientos(desde, hasta, args.cuenta ? [args.cuenta] : CUENTAS)
      .filter(filtroRubro(args.de, args.excluirDe))
      .filter((m) => (!args.clase || m.clase === args.clase) && (!args.tipo || m.tipo === args.tipo)
        && (!args.tarjeta || m.tarjeta === args.tarjeta) && (!args.plastico || plasticoUsado(m) === args.plastico)
        && (!buscar || slug(`${m.comercio} ${m.nota} ${m.referencia ?? ''}`).includes(buscar)))
      .sort((a, b) => a.fecha.localeCompare(b.fecha) || a.cuenta.localeCompare(b.cuenta) || a.linea - b.linea)
      .map((m) => ({
        id: m.id ?? '', fecha: m.fecha, fechaReal: m.fechaReal ?? '', cuenta: m.cuenta, linea: m.linea, referencia: m.referencia ?? '',
        clase: m.clase, tipo: m.tipo, total: m.total, moneda: m.moneda, comercio: m.comercio, nota: m.nota, rubro: rubroDe(m),
        tarjeta: m.tarjeta ?? '', plastico: plasticoUsado(m),
        libro: m.libro ?? '', partidas: (m.partidas ?? []).map((p) => `${p.tipo}:${p.monto}`).join(' '), f: nFacturas(m), archivo: m.archivo ?? '',
      }));
    return { desde, hasta, total: movimientos.length, movimientos };
  },
  async smoke({ callTool, check, toolText, toolJson }) {
    const text = toolText(await callTool('bancos_movimientos', { mes: '2026-10' }));
    check('bancos_movimientos lista lo anotado del mes', /total: 5/.test(text) && text.includes('Soda X'), text.slice(0, 300));
    check('bancos_movimientos muestra el libro, las partidas y las facturas', /Soda X,almuerzo papá,papa,,,l-[0-9a-f]{8},comida:5000 supermercado:3500,0/.test(text) && /Súper Y,,hogar,,,l-[0-9a-f]{8},.*,1/.test(text), text.slice(0, 900));
    const filtro = toolText(await callTool('bancos_movimientos', { mes: '2026-10', texto: 'super' }));
    check('bancos_movimientos filtra por texto sin tildes', /total: 1/.test(filtro), filtro.slice(0, 120));
    const sinPapa = toolText(await callTool('bancos_movimientos', { mes: '2026-10', excluirDe: ['papa'] }));
    check('bancos_movimientos excluye un rubro', /total: 4/.test(sinPapa) && !sinPapa.includes('Soda X'), sinPapa.slice(0, 200));
    const sin = toolJson(await callTool('bancos_movimientos', {}));
    check('bancos_movimientos pide periodo', /mes o desde/.test(sin.error ?? ''), sin.error);
  },
};
