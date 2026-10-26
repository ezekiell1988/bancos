import { CLASES, CUENTAS, DE_PROP, MONEDAS, leerTodosLosTipos } from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';
import { normalizarDetalle, prepararDetalles } from '../src/vinculos.mjs';

export default {
  name: 'bancos_comprobante',
  order: 22,
  description:
    'Nivel 2 (auxiliar, no suma en reportes): anota comprobantes del momento (SINPE, voucher, transferencia) en datos/comprobantes/AAAA-MM/AAAA-MM-DD.jsonl ' +
    'con su número de movimiento del banco, y copia cada respaldo a datos/multimedia/AAAA-MM/. ' +
    'Se vincula solo a su movimiento de nivel 1 (por referencia, o por monto y fecha dentro de -3/+10 días) y le completa el tipo, el de y la nota; ' +
    'si el estado de cuenta aún no llegó queda pendiente y se vincula al anotarlo con bancos_registrar. Sin apply:true solo devuelve el preview.',
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
            cuenta: { type: 'string', enum: CUENTAS, description: 'Cuenta de donde salió o a donde entró, si se sabe (SINPE = bcr; voucher de la tarjeta = bac).' },
            clase: { type: 'string', enum: CLASES },
            total: { type: 'number', description: 'Monto positivo.' },
            moneda: { type: 'string', enum: MONEDAS },
            comercio: { type: 'string', description: 'Destinatario, comercio o quien manda.' },
            referencia: { type: 'string', description: 'Número de movimiento, documento o referencia que trae el comprobante.' },
            tipo: { type: 'string', description: 'id de bancos_tipos de esa clase, si el comprobante lo aclara. Se le pasa al movimiento.' },
            de: DE_PROP,
            nota: { type: 'string', description: 'Detalle del comprobante (p. ej. el motivo del SINPE).' },
            origen: { type: 'string', description: 'Ruta absoluta de la foto o PDF del comprobante.' },
            nombreArchivo: { type: 'string', description: 'Nombre para el respaldo en multimedia/. Default: el del origen.' },
            movimiento: { type: 'string', description: 'id del movimiento (m-...) si ya se sabe cuál es; si no, se busca solo.' },
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
    const plan = prepararDetalles('comprobantes', comprobantes, (c) => normalizarDetalle('comprobantes', c, tipos));
    if (apply !== true) return { preview: true, applied: false, ...plan.resumen, comprobantes: plan.filas, message: 'Llamar de nuevo con apply:true para guardar.' };
    if (!plan.filas.length) return { applied: false, ...plan.resumen, message: 'Todos ya estaban anotados; no se guardó nada.' };
    plan.guardar();
    return { applied: true, ...plan.resumen, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, state }) {
    // Movimiento 1001 de Soda X (bac, 2026-10-24) ya está anotado con tipo comida.
    const prev = toolJson(await callTool('bancos_comprobante', {
      comprobantes: [{ fecha: '2026-10-24', cuenta: 'bac', clase: 'gasto', total: 8500, moneda: 'CRC', comercio: 'Soda X', referencia: '1001', de: 'papa', nota: 'almuerzo papá', origen: state.respaldo }],
    }));
    check('bancos_comprobante hace preview y encuentra su movimiento por referencia',
      prev.preview === true && prev.vinculados?.[0]?.por === 'referencia' && /de hogar → papa/.test(prev.vinculados[0].cambios), JSON.stringify(prev).slice(0, 300));
    const ok = toolJson(await callTool('bancos_comprobante', {
      apply: true,
      comprobantes: [
        { fecha: '2026-10-24', cuenta: 'bac', clase: 'gasto', total: 8500, moneda: 'CRC', comercio: 'Soda X', referencia: '1001', de: 'papa', nota: 'almuerzo papá', origen: state.respaldo },
        { fecha: '2026-11-02', cuenta: 'bcr', clase: 'gasto', total: 3500, moneda: 'CRC', comercio: 'Tati', tipo: 'supermercado', nota: 'feria', referencia: '777888' },
      ],
    }));
    check('bancos_comprobante guarda, vincula uno y deja el otro pendiente',
      ok.applied === true && ok.anota === 2 && ok.vinculados?.length === 1 && ok.pendientes?.length === 1 && !ok.aviso, JSON.stringify(ok).slice(0, 300));
    state.comprobantePendiente = ok.pendientes?.[0]?.id;
    state.comprobanteSoda = ok.vinculados?.[0]?.detalle;
    const dup = toolJson(await callTool('bancos_comprobante', { comprobantes: [{ fecha: '2026-11-02', clase: 'gasto', total: 3500, moneda: 'CRC', comercio: 'Tati', referencia: '777888' }] }));
    check('bancos_comprobante omite duplicados', dup.anota === 0 && dup.omitidosPorDuplicado?.length === 1, JSON.stringify(dup).slice(0, 200));

    // Llega el estado de noviembre: el comprobante pendiente se vincula solo y le pasa su tipo.
    const estado = toolJson(await callTool('bancos_registrar', {
      cuenta: 'bcr', apply: true,
      movimientos: [{ fecha: '2026-11-03', fechaReal: '2026-11-02', referencia: '777888', clase: 'gasto', tipo: 'otros', total: 3500, moneda: 'CRC', comercio: 'SINPE Tati' }],
    }));
    check('bancos_registrar vincula el comprobante pendiente y toma su tipo',
      estado.vinculados?.[0]?.detalle === state.comprobantePendiente && /tipo otros → supermercado/.test(estado.vinculados[0].cambios), JSON.stringify(estado).slice(0, 300));
  },
};
