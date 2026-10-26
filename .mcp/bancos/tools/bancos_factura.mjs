import { CUENTAS, DE_PROP, MONEDAS, ToolError, buscarDetalle, checkTipo, leerTodosLosTipos, r2 } from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';
import { estadoPago, normalizarDetalle, prepararDetalles } from '../src/vinculos.mjs';

export default {
  name: 'bancos_factura',
  order: 23,
  description:
    'Nivel 3 (auxiliar, no suma en reportes): anota facturas con el detalle de sus líneas (compras de supermercado, recibos de servicios públicos) ' +
    'en datos/facturas/AAAA-MM/AAAA-MM-DD.jsonl y copia cada respaldo a datos/multimedia/AAAA-MM/. ' +
    'Los comprobantes (nivel 2) son sus pagos: una factura puede tener varios (abonos, cuotas) y un comprobante puede pagar varias facturas. ' +
    'Con comprobantes se concilia por ellos y le completa a sus movimientos el tipo, el de y la nota; sin comprobantes se vincula sola a su ' +
    'movimiento de nivel 1 por monto y fecha, o queda pendiente. Devuelve lo pagado y el saldo. Avisa si la suma de las líneas no da el total. ' +
    'Sin apply:true solo devuelve el preview.',
  inputSchema: {
    type: 'object',
    properties: {
      facturas: {
        type: 'array',
        minItems: 1,
        items: {
          type: 'object',
          properties: {
            fecha: { type: 'string', description: 'AAAA-MM-DD de la factura.' },
            comercio: { type: 'string' },
            numero: { type: 'string', description: 'Número o consecutivo de la factura, si lo trae.' },
            total: { type: 'number', description: 'Total pagado (con IVA y servicio).' },
            moneda: { type: 'string', enum: MONEDAS },
            cuenta: { type: 'string', enum: CUENTAS, description: 'Con qué se pagó, si se sabe.' },
            tipo: { type: 'string', description: 'id de gasto de bancos_tipos para el movimiento, si la factura lo aclara.' },
            de: DE_PROP,
            nota: { type: 'string' },
            lineas: {
              type: 'array',
              description: 'Líneas de la factura.',
              items: {
                type: 'object',
                properties: {
                  descripcion: { type: 'string' },
                  cantidad: { type: 'number' },
                  unidad: { type: 'string', description: 'p. ej. kg, un, kWh, m3.' },
                  precio: { type: 'number', description: 'Precio unitario.' },
                  total: { type: 'number', description: 'Total de la línea (puede ser negativo para descuentos).' },
                  tipo: { type: 'string', description: 'id de gasto opcional para analizar la línea (p. ej. salud dentro del súper).' },
                },
                required: ['descripcion', 'total'],
                additionalProperties: false,
              },
            },
            origen: { type: 'string', description: 'Ruta absoluta de la foto o PDF de la factura.' },
            nombreArchivo: { type: 'string' },
            movimiento: { type: 'string', description: 'id del movimiento (m-...) si se pagó directo y ya se sabe; si no, se busca solo. No va con comprobantes.' },
            comprobantes: { type: 'array', items: { type: 'string' }, description: 'ids de los comprobantes (c-...) con que se pagó: uno o varios pagos.' },
          },
          required: ['fecha', 'comercio', 'total', 'moneda', 'lineas'],
          additionalProperties: false,
        },
      },
      apply: { type: 'boolean', description: 'true para guardar; sin él solo preview.' },
    },
    required: ['facturas'],
    additionalProperties: false,
  },
  async handler({ facturas, apply }) {
    const tipos = leerTodosLosTipos();
    const avisos = [];
    const pagos = new Map();
    const plan = prepararDetalles('facturas', facturas, (f) => {
      const base = normalizarDetalle('facturas', f, tipos);
      const comprobantes = [...new Set(f.comprobantes ?? [])];
      if (comprobantes.length && base.movimiento) throw new ToolError(`${f.comercio} ${f.fecha}: va con movimiento o con comprobantes, no con ambos.`);
      for (const id of comprobantes) {
        const c = buscarDetalle(id);
        if (c.nivel !== 'comprobantes' || c.clase !== 'gasto') throw new ToolError(`${id} no es un comprobante de gasto.`);
        pagos.set(id, c);
      }
      const lineas = f.lineas.map((l) => {
        if (l.tipo) checkTipo(tipos, 'gasto', l.tipo);
        if (typeof l.total !== 'number') throw new ToolError(`Línea sin total en la factura de ${f.comercio}`);
        return { descripcion: l.descripcion.trim(), ...(l.cantidad !== undefined ? { cantidad: l.cantidad } : {}), ...(l.unidad ? { unidad: l.unidad } : {}),
          ...(l.precio !== undefined ? { precio: l.precio } : {}), total: r2(l.total), ...(l.tipo ? { tipo: l.tipo } : {}) };
      });
      const suma = r2(lineas.reduce((s, l) => s + l.total, 0));
      if (Math.abs(suma - base.total) > 1) avisos.push(`${f.comercio} ${f.fecha}: las líneas suman ${suma} y el total es ${base.total}.`);
      return { ...base, ...(comprobantes.length ? { comprobantes } : {}), lineas };
    });
    const estado = plan.filas.filter((f) => f.comprobantes || f.movimiento).map((f) => ({ factura: f.id, total: f.total, ...estadoPago(f, pagos) }));
    const extra = { pagos: estado.length ? estado : undefined, avisos: avisos.length ? avisos : undefined };
    if (apply !== true) return { preview: true, applied: false, ...plan.resumen, ...extra, facturas: plan.filas, message: 'Llamar de nuevo con apply:true para guardar.' };
    if (!plan.filas.length) return { applied: false, ...plan.resumen, message: 'Todas ya estaban anotadas; no se guardó nada.' };
    plan.guardar();
    return { applied: true, ...plan.resumen, ...extra, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, state }) {
    // Súper Y (bac, 2026-10-25, 12000.5) ya está anotado.
    const factura = {
      fecha: '2026-10-25', comercio: 'Súper Y', numero: 'FE-55', total: 12000.5, moneda: 'CRC', origen: state.respaldo, nombreArchivo: 'factura super.jpg',
      lineas: [{ descripcion: 'Arroz', cantidad: 2, precio: 3000, total: 6000 }, { descripcion: 'Acetaminofén', total: 6000.5, tipo: 'comida' }],
    };
    const prev = toolJson(await callTool('bancos_factura', { facturas: [{ ...factura, total: 15000 }] }));
    check('bancos_factura avisa si las líneas no suman el total', prev.preview === true && prev.avisos?.length === 1 && prev.pendientes?.length === 1, JSON.stringify(prev).slice(0, 300));
    const ok = toolJson(await callTool('bancos_factura', { facturas: [factura], apply: true }));
    check('bancos_factura guarda y se vincula por monto y fecha', ok.applied === true && ok.vinculados?.[0]?.por === 'monto y fecha' && !ok.avisos, JSON.stringify(ok).slice(0, 300));
    state.factura = ok.vinculados?.[0]?.detalle;
    const malo = toolJson(await callTool('bancos_factura', { facturas: [{ ...factura, lineas: [{ descripcion: 'x', total: 1, tipo: 'inventado' }] }] }));
    check('bancos_factura rechaza tipo de línea desconocido', /desconocido/.test(malo.error ?? ''), malo.error);
    // Factura pagada en 2 pagos (los 2 comprobantes ya vinculados): queda con saldo y completa los 2 movimientos.
    const taller = { fecha: '2026-11-05', comercio: 'Taller Z', total: 15000, moneda: 'CRC', nota: 'reparación', lineas: [{ descripcion: 'Reparación', total: 15000 }],
      comprobantes: [state.comprobanteSoda, state.comprobantePendiente] };
    const ambos = toolJson(await callTool('bancos_factura', { facturas: [{ ...taller, movimiento: 'm-00000000' }] }));
    check('bancos_factura rechaza movimiento junto con comprobantes', /no con ambos/.test(ambos.error ?? ''), ambos.error);
    const dos = toolJson(await callTool('bancos_factura', { facturas: [taller], apply: true }));
    check('bancos_factura acepta varios pagos y da el saldo',
      dos.applied === true && dos.vinculados?.filter((v) => v.por === 'pago').length === 2 && dos.pagos?.[0]?.pagado === 12000 && dos.pagos[0].saldo === 3000 && !dos.pendientes?.length,
      JSON.stringify(dos).slice(0, 400));
    state.facturaTaller = dos.pagos?.[0]?.factura;
  },
};
