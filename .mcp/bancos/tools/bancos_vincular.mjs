import {
  ToolError, buscarDetalle, buscarMovimiento, completarMovimiento, cuadra, detalleSinUbicacion, guardarDetalle, guardarMovimiento, leerDetalles, sinDerivados,
} from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';
import { buscarLibro, bloquea, facturasPorLibro, partidasDeFacturas, problemasDeCuadre } from '../src/vinculos.mjs';

/** Mueve una factura (nivel 3) a otro libro: el pago al que corresponde. Recalcula las partidas de los dos libros cuando cuadran. */
function moverFactura(f, libroId, confirm) {
  const destino = buscarLibro(libroId);
  const origen = f.libro ? leerDetalles('libros').find((l) => l.id === f.libro) : null;
  if (destino.clase !== 'gasto') throw new ToolError(`El libro ${destino.id} es de ingreso.`);
  const todas = facturasPorLibro();
  const enDestino = [...(todas.get(destino.id) ?? []).filter((x) => x.id !== f.id), { ...f, libro: destino.id }];
  const enOrigen = origen && origen.id !== destino.id ? (todas.get(origen.id) ?? []).filter((x) => x.id !== f.id) : null;
  const nuevoDestino = { ...detalleSinUbicacion(destino) };
  const problemas = problemasDeCuadre({ ...nuevoDestino, partidas: partidasDeFacturas(enDestino) }, enDestino, null);
  if (!problemas.length) nuevoDestino.partidas = partidasDeFacturas(enDestino);
  const nuevoOrigen = enOrigen?.length && origen ? { ...detalleSinUbicacion(origen), partidas: partidasDeFacturas(enOrigen) } : null;
  const problemasOrigen = nuevoOrigen ? problemasDeCuadre(nuevoOrigen, enOrigen, null) : [];
  const resumen = {
    detalle: f.id, antes: f.libro ?? '', despues: destino.id, cuadreDestino: problemas.length ? problemas : 'cuadra', partidasDestino: nuevoDestino.partidas,
    ...(enOrigen ? { cuadreOrigen: problemasOrigen.length ? problemasOrigen : enOrigen.length ? 'cuadra' : 'sin facturas: revisar sus partidas' } : {}),
  };
  if (confirm !== true) return { preview: true, applied: false, ...resumen, message: 'Llamar de nuevo con confirm:true para guardarlo.' };
  guardarDetalle(f, { ...detalleSinUbicacion(f), libro: destino.id });
  if (!problemas.length) guardarDetalle(destino, nuevoDestino);
  if (nuevoOrigen && !problemasOrigen.length) guardarDetalle(origen, nuevoOrigen);
  return { applied: true, ...resumen, aviso: refrescarSqlite() };
}

export default {
  name: 'bancos_vincular',
  order: 45,
  description:
    'Vincula a mano un libro (l-..., el pago) con su movimiento bancario (m-...), o lo desvincula con movimiento:"" (queda como nivel 2 sin nivel 1, por conciliar). ' +
    'Es 1 a 1: el movimiento tiene que ser de la misma clase y monto, y si ya tenía un libro sin documento ni facturas, este lo reemplaza (conserva su categoría si el nuevo solo trae «otros»). ' +
    'Al vincular le completa al movimiento el de y la nota del libro (completar:false para no tocarlo). ' +
    'A una factura (f-...) se le cambia el libro del que cuelga con libro:"l-...". También marca o quita «en tránsito» de un libro sin movimiento con transito:"razón" o transito:"". ' +
    'Sin confirm:true solo muestra el preview. Usar cuando el vínculo automático no lo encontró o eligió otro.',
  inputSchema: {
    type: 'object',
    properties: {
      detalle: { type: 'string', description: 'id del libro (l-...) o de la factura (f-...).' },
      movimiento: { type: 'string', description: 'Solo libros: id del movimiento; "" para desvincular.' },
      libro: { type: 'string', description: 'Solo facturas: id del libro (l-...) del que pasa a colgar.' },
      transito: { type: 'string', description: 'Solo libros sin movimiento: razón de que aún no aparece en el banco; "" la quita.' },
      completar: { type: 'boolean', description: 'Default true: pasar de y nota al movimiento.' },
      confirm: { type: 'boolean', description: 'true para guardar; sin él solo preview.' },
    },
    required: ['detalle'],
    additionalProperties: false,
  },
  async handler({ detalle, movimiento, libro, transito, completar = true, confirm }) {
    const d = buscarDetalle(detalle);
    if (d.nivel === 'facturas') {
      if (!libro) throw new ToolError('Una factura se vincula con libro:"l-...".');
      return moverFactura(d, libro, confirm);
    }
    if (libro) throw new ToolError('libro solo se fija en una factura (f-...).');
    if ((movimiento === undefined) === (transito === undefined)) throw new ToolError('Indicar movimiento o transito (uno de los dos).');
    if (transito !== undefined) {
      if (d.movimiento && transito) throw new ToolError(`El libro ${d.id} ya tiene movimiento; «en tránsito» es solo sin movimiento.`);
      const nuevo = { ...detalleSinUbicacion(d) };
      delete nuevo.transito;
      if (transito.trim()) nuevo.transito = { razon: transito.trim() };
      const resumen = { detalle: d.id, antes: d.transito?.razon ?? '', despues: transito.trim() };
      if (confirm !== true) return { preview: true, applied: false, ...resumen, message: 'Llamar de nuevo con confirm:true para guardarlo.' };
      guardarDetalle(d, nuevo);
      return { applied: true, ...resumen, aviso: refrescarSqlite() };
    }
    const m = movimiento ? buscarMovimiento(movimiento) : null;
    if (m && m.clase !== d.clase) throw new ToolError(`El ${d.id} es de clase ${d.clase} y el movimiento ${m.id} es ${m.clase}.`);
    if (m && !cuadra(m.total, d.total, m.moneda)) throw new ToolError(`El ${d.id} es de ${d.total} y el movimiento ${m.id} de ${m.total}: es 1 a 1 y tienen que dar igual.`);
    const facturas = facturasPorLibro();
    const otro = m ? leerDetalles('libros').find((l) => l.movimiento === m.id && l.id !== d.id) : null;
    if (otro && bloquea(otro, facturas)) throw new ToolError(`El movimiento ${m.id} ya tiene su pago (${otro.id}); es 1 a 1. Desvincularlo primero.`);
    const { nuevo, cambios } = m && completar ? completarMovimiento(m, d) : { nuevo: m, cambios: [] };
    const resumen = { detalle: d.id, antes: d.movimiento ?? '', despues: m?.id ?? '', ...(otro ? { reemplaza: otro.id } : {}), cambiosAlMovimiento: cambios.join('; ') || 'ninguno' };
    if (confirm !== true) return { preview: true, applied: false, ...resumen, message: 'Llamar de nuevo con confirm:true para guardarlo.' };
    if (otro) {
      guardarDetalle(otro, null);
      const sinCategoria = d.partidas.length === 1 && d.partidas[0].tipo === 'otros';
      if (sinCategoria) d.partidas = otro.partidas;
    }
    const nuevoLibro = { ...detalleSinUbicacion(d), movimiento: m?.id ?? null };
    delete nuevoLibro.transito;
    guardarDetalle(leerDetalles('libros').find((l) => l.id === d.id), nuevoLibro);
    if (cambios.length) guardarMovimiento(m, sinDerivados(nuevo));
    return { applied: true, ...resumen, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, toolText, state }) {
    // El libro del Súper Y quedó sin movimiento al borrar el suyo; se vuelve a vincular a mano (1 a 1, misma clase y monto).
    const itqs = toolText(await callTool('bancos_movimientos', { mes: '2026-10', texto: 'ITQS' })).match(/m-[0-9a-f]{8}/)?.[0];
    const otra = toolJson(await callTool('bancos_vincular', { detalle: state.libroSuper, movimiento: itqs }));
    check('bancos_vincular rechaza otra clase', /clase gasto/.test(otra.error ?? ''), otra.error);
    const sinpe = toolText(await callTool('bancos_movimientos', { mes: '2026-10', de: 'hogar', texto: 'SINPE' })).match(/m-[0-9a-f]{8}/)?.[0];
    const monto = toolJson(await callTool('bancos_vincular', { detalle: state.libroSuper, movimiento: sinpe }));
    check('bancos_vincular exige el mismo monto (1 a 1)', /tienen que dar igual/.test(monto.error ?? ''), monto.error);
    const ocupado = toolJson(await callTool('bancos_vincular', { detalle: state.libroSuper, movimiento: state.idRetiro }));
    check('bancos_vincular no vincula a un movimiento con otro monto o con su pago', typeof ocupado.error === 'string', ocupado.error);
    const tr = toolJson(await callTool('bancos_vincular', { detalle: state.libroSuper, transito: 'en revisión', confirm: true }));
    check('bancos_vincular marca en tránsito un libro sin movimiento', tr.applied === true && tr.despues === 'en revisión', JSON.stringify(tr));
    const fuera = toolJson(await callTool('bancos_vincular', { detalle: state.libroSuper, transito: '', confirm: true }));
    check('bancos_vincular quita el tránsito', fuera.applied === true && fuera.despues === '', JSON.stringify(fuera));
    const volver = toolJson(await callTool('bancos_vincular', { detalle: state.libroSuper, movimiento: '', confirm: true }));
    check('bancos_vincular desvincula con ""', volver.applied === true && volver.despues === '', JSON.stringify(volver));
    const mover = toolJson(await callTool('bancos_vincular', { detalle: state.factura, libro: state.libroRetiro }));
    check('bancos_vincular mueve una factura de libro con preview y avisa si no cuadra', mover.preview === true && Array.isArray(mover.cuadreDestino), JSON.stringify(mover).slice(0, 300));
  },
};
