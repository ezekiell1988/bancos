import { CLASES, CUENTAS, DE_PROP, MONEDAS, leerTodosLosTipos } from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';
import { PARTIDAS_PROP, prepararLibros } from '../src/vinculos.mjs';

export default {
  name: 'bancos_comprobante',
  order: 22,
  description:
    'Nivel 2 (Libros, ADR-EBC-03): anota el PAGO (colilla, SINPE, voucher, transferencia, o un documento que diga «pago de factura X»; nunca la factura) ' +
    'en datos/libros/AAAA-MM/AAAA-MM-DD.jsonl y copia cada respaldo a datos/multimedia/AAAA-MM/. Va 1 a 1 con un movimiento del nivel 1 y lleva la categoría ' +
    'en partidas [{tipo, monto}] que suman el total (sin partidas: una por todo, con tipo o «otros»). Se vincula solo a su movimiento (por referencia, o por monto y fecha ' +
    'dentro de -3/+10 días) y le completa el de y la nota; si el movimiento ya tenía un libro sin documento, lo reemplaza. Si el estado de cuenta aún no llegó queda pendiente ' +
    '(nivel 2 sin nivel 1) y se vincula al anotarlo con bancos_registrar; transito:"razón" lo marca en tránsito (no es pendiente). Sin apply:true solo devuelve el preview.',
  inputSchema: {
    type: 'object',
    properties: {
      comprobantes: {
        type: 'array',
        minItems: 1,
        items: {
          type: 'object',
          properties: {
            fecha: { type: 'string', description: 'AAAA-MM-DD del comprobante.' },
            hora: { type: 'string', description: 'HH:MM, si la trae.' },
            cuenta: { type: 'string', enum: CUENTAS, description: 'Cuenta de donde salió o a donde entró, si se sabe (SINPE = bcr; voucher de la tarjeta = bac o bncr).' },
            clase: { type: 'string', enum: CLASES },
            total: { type: 'number', description: 'Monto positivo.' },
            moneda: { type: 'string', enum: MONEDAS },
            comercio: { type: 'string', description: 'Destinatario, comercio o quien manda.' },
            referencia: { type: 'string', description: 'Número de movimiento, documento o referencia que trae el comprobante.' },
            tipo: { type: 'string', description: 'id de bancos_tipos de esa clase si todo el pago es de una categoría (atajo de partidas). Default: otros.' },
            partidas: PARTIDAS_PROP,
            transito: { type: 'string', description: 'Razón por la que el pago aún no aparece en el banco (en tránsito). No cuenta como pendiente.' },
            de: DE_PROP,
            nota: { type: 'string', description: 'Detalle del comprobante (p. ej. el motivo del SINPE).' },
            origen: { type: 'string', description: 'Ruta absoluta de la foto o PDF del comprobante.' },
            nombreArchivo: { type: 'string', description: 'Nombre para el respaldo en multimedia/. Default: el del origen.' },
            movimiento: { type: 'string', description: 'id del movimiento (m-...) si ya se sabe cuál es; si no, se busca solo. Tiene que ser de la misma clase y monto.' },
          },
          required: ['fecha', 'clase', 'total', 'moneda'],
          additionalProperties: false,
        },
      },
      apply: { type: 'boolean', description: 'true para guardar; sin él solo preview.' },
    },
    required: ['comprobantes'],
    additionalProperties: false,
  },
  async handler({ comprobantes, apply }) {
    const tipos = leerTodosLosTipos();
    const plan = prepararLibros(comprobantes, tipos);
    if (apply !== true) return { preview: true, applied: false, ...plan.resumen, libros: plan.filas, message: 'Llamar de nuevo con apply:true para guardar.' };
    if (!plan.filas.length) return { applied: false, ...plan.resumen, message: 'Todos ya estaban anotados; no se guardó nada.' };
    plan.guardar();
    return { applied: true, ...plan.resumen, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, toolText, state }) {
    // Movimiento 1001 de Soda X (bac, 2026-10-24) ya está anotado con su libro (comida 8500).
    const soda = { fecha: '2026-10-24', cuenta: 'bac', clase: 'gasto', total: 8500, moneda: 'CRC', comercio: 'Soda X', referencia: '1001', de: 'papa', nota: 'almuerzo papá', origen: state.respaldo,
      partidas: [{ tipo: 'comida', monto: 5000 }, { tipo: 'supermercado', monto: 3500 }] };
    const prev = toolJson(await callTool('bancos_comprobante', { comprobantes: [soda] }));
    check('bancos_comprobante hace preview, encuentra su movimiento por referencia y reemplaza el libro sin documento',
      prev.preview === true && prev.vinculados?.[0]?.por === 'referencia' && prev.vinculados[0].reemplazaA && /de hogar → papa/.test(prev.vinculados[0].cambios), JSON.stringify(prev).slice(0, 300));
    const malo = toolJson(await callTool('bancos_comprobante', { comprobantes: [{ ...soda, partidas: [{ tipo: 'comida', monto: 5000 }] }] }));
    check('bancos_comprobante rechaza partidas que no suman el total', /suman 5000/.test(malo.error ?? ''), malo.error);
    const ok = toolJson(await callTool('bancos_comprobante', {
      apply: true,
      comprobantes: [soda, { fecha: '2026-11-02', cuenta: 'bcr', clase: 'gasto', total: 3500, moneda: 'CRC', comercio: 'Tati', tipo: 'supermercado', nota: 'feria', referencia: '777888', origen: state.respaldo, nombreArchivo: 'tati.jpg' }],
    }));
    check('bancos_comprobante guarda, vincula uno y deja el otro pendiente (nivel 2 sin nivel 1)',
      ok.applied === true && ok.anota === 2 && ok.vinculados?.length === 1 && ok.pendientes?.length === 1 && !ok.aviso, JSON.stringify(ok).slice(0, 300));
    state.libroPendiente = ok.pendientes?.[0]?.id;
    state.libroSoda = ok.vinculados?.[0]?.detalle;
    const mov = toolText(await callTool('bancos_movimientos', { mes: '2026-10', texto: 'Soda' }));
    check('el movimiento toma las 2 partidas de su libro y queda un solo libro', /comida:5000 supermercado:3500/.test(mov) && mov.includes(state.libroSoda), mov.slice(0, 300));
    const dup = toolJson(await callTool('bancos_comprobante', { comprobantes: [{ fecha: '2026-11-02', clase: 'gasto', total: 3500, moneda: 'CRC', comercio: 'Tati', referencia: '777888', origen: state.respaldo }] }));
    check('bancos_comprobante omite duplicados', dup.anota === 0 && dup.omitidosPorDuplicado?.length === 1, JSON.stringify(dup).slice(0, 200));
    const tr = toolJson(await callTool('bancos_comprobante', { apply: true, comprobantes: [{ fecha: '2026-11-08', clase: 'gasto', total: 900, moneda: 'CRC', comercio: 'Bus', transito: 'se cobra el próximo ciclo' }] }));
    check('bancos_comprobante acepta un pago en tránsito', tr.applied === true && tr.anota === 1, JSON.stringify(tr).slice(0, 200));

    // Llega el estado de noviembre: el pago pendiente se vincula solo y trae su categoría.
    const estado = toolJson(await callTool('bancos_registrar', {
      cuenta: 'bcr', apply: true,
      movimientos: [{ fecha: '2026-11-03', fechaReal: '2026-11-02', referencia: '777888', clase: 'gasto', tipo: 'otros', total: 3500, moneda: 'CRC', comercio: 'SINPE Tati' }],
    }));
    const tati = toolText(await callTool('bancos_movimientos', { mes: '2026-11', texto: 'Tati' }));
    check('bancos_registrar vincula el pago pendiente y toma su categoría',
      estado.vinculados?.[0]?.detalle === state.libroPendiente && estado.librosNuevos === 0 && /supermercado:3500/.test(tati), JSON.stringify(estado).slice(0, 300) + tati.slice(0, 200));
  },
};
