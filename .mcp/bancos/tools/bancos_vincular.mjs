import { ToolError, buscarDetalle, buscarMovimiento, completarMovimiento, guardarDetalle, guardarMovimiento, sinUbicacion } from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';
import { estadoPago, mapaComprobantes, pagosDe } from '../src/vinculos.mjs';

/** Fija la lista de comprobantes (pagos) de una factura y completa los movimientos de esos pagos. */
function fijarPagos(f, ids, completar, confirm) {
  if (f.nivel !== 'facturas') throw new ToolError('comprobantes solo se fija en una factura (f-...).');
  const pagos = ids.map((id) => {
    const c = buscarDetalle(id);
    if (c.nivel !== 'comprobantes' || c.clase !== 'gasto') throw new ToolError(`${id} no es un comprobante de gasto.`);
    return c;
  });
  const nueva = { ...sinUbicacion(f), movimiento: ids.length ? null : f.movimiento ?? null };
  delete nueva.comprobantes;
  if (ids.length) nueva.comprobantes = ids;
  const movimientos = new Map();
  if (completar) {
    for (const c of pagos.filter((x) => x.movimiento)) {
      const m = movimientos.get(c.movimiento)?.nuevo ?? buscarMovimiento(c.movimiento);
      const { nuevo, cambios } = completarMovimiento(m, f);
      if (cambios.length) movimientos.set(c.movimiento, { original: movimientos.get(c.movimiento)?.original ?? m, nuevo, cambios });
    }
  }
  const resumen = {
    detalle: f.id, antes: pagosDe(f).join(' '), despues: ids.join(' '), ...estadoPago(nueva, mapaComprobantes(pagos)),
    cambiosAlMovimiento: [...movimientos].map(([id, x]) => `${id}: ${x.cambios.join('; ')}`).join(' | ') || 'ninguno',
  };
  if (confirm !== true) return { preview: true, applied: false, ...resumen, message: 'Llamar de nuevo con confirm:true para guardarlo.' };
  guardarDetalle(f, nueva);
  for (const x of movimientos.values()) guardarMovimiento(x.original, sinUbicacion(x.nuevo));
  return { applied: true, ...resumen, aviso: refrescarSqlite() };
}

export default {
  name: 'bancos_vincular',
  order: 45,
  description:
    'Vincula a mano un comprobante (c-...) o factura (f-...) con su movimiento bancario (m-...), o lo desvincula con movimiento:"". ' +
    'A una factura también se le fijan sus pagos con comprobantes:[c-...] (lista completa; [] los quita): una factura puede tener varios ' +
    'y un comprobante puede pagar varias facturas. Al vincular le completa al movimiento (o a los de sus pagos) el tipo, el de y la nota ' +
    'del detalle (completar:false para no tocarlo). ' +
    'Sin confirm:true solo muestra el preview. Usar cuando el vínculo automático no lo encontró o eligió otro.',
  inputSchema: {
    type: 'object',
    properties: {
      detalle: { type: 'string', description: 'id del comprobante o factura.' },
      movimiento: { type: 'string', description: 'id del movimiento; "" para desvincular.' },
      comprobantes: { type: 'array', items: { type: 'string' }, description: 'Solo facturas: ids de los comprobantes que la pagan (reemplaza la lista).' },
      completar: { type: 'boolean', description: 'Default true: pasar tipo, de y nota al movimiento.' },
      confirm: { type: 'boolean', description: 'true para guardar; sin él solo preview.' },
    },
    required: ['detalle'],
    additionalProperties: false,
  },
  async handler({ detalle, movimiento, comprobantes, completar = true, confirm }) {
    const d = buscarDetalle(detalle);
    if ((movimiento === undefined) === (comprobantes === undefined)) throw new ToolError('Indicar movimiento o comprobantes (uno de los dos).');
    if (comprobantes) return fijarPagos(d, [...new Set(comprobantes)], completar, confirm);
    if (d.nivel === 'facturas' && movimiento && pagosDe(d).length) throw new ToolError(`La ${d.id} se concilia por sus comprobantes; quitarlos con comprobantes:[] para vincularla directo.`);
    const m = movimiento ? buscarMovimiento(movimiento) : null;
    const clase = d.nivel === 'facturas' ? 'gasto' : d.clase;
    if (m && m.clase !== clase) throw new ToolError(`El ${d.id} es de clase ${clase} y el movimiento ${m.id} es ${m.clase}.`);
    const { nuevo, cambios } = m && completar ? completarMovimiento(m, d) : { nuevo: m, cambios: [] };
    const resumen = { detalle: d.id, antes: d.movimiento ?? '', despues: m?.id ?? '', cambiosAlMovimiento: cambios.join('; ') || 'ninguno' };
    if (confirm !== true) return { preview: true, applied: false, ...resumen, message: 'Llamar de nuevo con confirm:true para guardarlo.' };
    guardarDetalle(d, { ...sinUbicacion(d), movimiento: m?.id ?? null });
    if (cambios.length) guardarMovimiento(m, sinUbicacion(nuevo));
    return { applied: true, ...resumen, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, toolText, state }) {
    const itqs = toolText(await callTool('bancos_movimientos', { mes: '2026-10', texto: 'ITQS' })).match(/m-[0-9a-f]{8}/)?.[0];
    const sinpe = toolText(await callTool('bancos_movimientos', { mes: '2026-10', de: 'hogar', texto: 'SINPE' })).match(/m-[0-9a-f]{8}/)?.[0];
    const malo = toolJson(await callTool('bancos_vincular', { detalle: state.factura, movimiento: itqs }));
    check('bancos_vincular rechaza otra clase', /clase gasto/.test(malo.error ?? ''), malo.error);
    const prev = toolJson(await callTool('bancos_vincular', { detalle: state.factura, movimiento: sinpe }));
    check('bancos_vincular hace preview', prev.preview === true && prev.antes === '' && prev.despues === sinpe, JSON.stringify(prev));
    const ok = toolJson(await callTool('bancos_vincular', { detalle: state.factura, movimiento: sinpe, confirm: true }));
    const det = toolText(await callTool('bancos_detalles', { movimiento: sinpe }));
    check('bancos_vincular guarda y la factura sale con su movimiento', ok.applied === true && det.includes(state.factura), det.slice(0, 300));
    const fuera = toolJson(await callTool('bancos_vincular', { detalle: state.factura, movimiento: '', confirm: true }));
    check('bancos_vincular desvincula', fuera.applied === true && fuera.despues === '', JSON.stringify(fuera));
    const pagos = toolJson(await callTool('bancos_vincular', { detalle: state.facturaTaller, comprobantes: [state.comprobanteSoda], confirm: true }));
    check('bancos_vincular fija los pagos de una factura y da el saldo', pagos.applied === true && pagos.pagado === 8500 && pagos.saldo === 6500, JSON.stringify(pagos));
    const directo = toolJson(await callTool('bancos_vincular', { detalle: state.facturaTaller, movimiento: sinpe }));
    check('bancos_vincular no vincula directo una factura con pagos', /comprobantes/.test(directo.error ?? ''), directo.error);
  },
};
