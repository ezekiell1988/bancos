import {
  ADICIONAL_PROP, CUENTAS, DE_PROP, FECHA_REAL_PROP, REFERENCIA_PROP, ToolError, checkFecha, checkTipo, guardarMovimiento, leerTodosLosTipos, plasticoDe, sinUbicacion, slug, TARJETA_PROP,
  ubicarMovimiento,
} from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';

export default {
  name: 'bancos_editar_movimiento',
  order: 41,
  description:
    'Cambia el tipo, el rubro (de), la tarjeta y el adicional, el comercio, la nota, la referencia o la fecha real (solo referencia) de un movimiento bancario, ' +
    'por id o por cuenta + fecha + línea (salen de bancos_movimientos). Sin confirm:true solo muestra el antes y el después. ' +
    'Para cambiar fecha, total o clase: bancos_borrar_movimiento y volver a anotar.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'id del movimiento (m-...). Alternativa a cuenta + fecha + linea.' },
      cuenta: { type: 'string', enum: CUENTAS },
      fecha: { type: 'string', description: 'AAAA-MM-DD.' },
      linea: { type: 'integer', minimum: 1, description: 'Número de línea (desde 1).' },
      tipo: { type: 'string', description: 'id nuevo de bancos_tipos de la misma clase.' },
      de: { ...DE_PROP, description: `${DE_PROP.description} "" lo pasa al hogar.` },
      fechaReal: { ...FECHA_REAL_PROP, description: `${FECHA_REAL_PROP.description} "" la quita.` },
      referencia: { ...REFERENCIA_PROP, description: `${REFERENCIA_PROP.description} "" la quita.` },
      tarjeta: { ...TARJETA_PROP, description: `${TARJETA_PROP.description} "" la quita.` },
      adicional: { ...ADICIONAL_PROP, description: `${ADICIONAL_PROP.description} "" la pasa al titular.` },
      comercio: { type: 'string' },
      nota: { type: 'string' },
      confirm: { type: 'boolean', description: 'true para guardar; sin él solo preview.' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const { tipo, de, tarjeta, adicional, fechaReal, referencia, comercio, nota, confirm } = args;
    const ubicado = ubicarMovimiento(args);
    const antes = sinUbicacion(ubicado);
    if ([tipo, de, tarjeta, adicional, fechaReal, referencia, comercio, nota].every((v) => v === undefined)) throw new ToolError('Indicar al menos tipo, de, tarjeta, adicional, fechaReal, referencia, comercio o nota.');
    if (fechaReal) checkFecha(fechaReal, 'fechaReal');
    if (tipo !== undefined) checkTipo(leerTodosLosTipos(), antes.clase, tipo);
    const { id, fecha, fechaReal: realAntes, referencia: refAntes, tarjeta: tAntes, adicional: aAntes, archivo, ...resto } = antes;
    const plastico = plasticoDe({ tarjeta: tarjeta ?? tAntes, adicional: adicional ?? aAntes });
    const real = fechaReal === undefined ? realAntes : fechaReal;
    const ref = referencia === undefined ? refAntes : referencia.trim();
    const despues = { id, fecha, ...(real && real !== fecha ? { fechaReal: real } : {}), ...(ref ? { referencia: ref } : {}), ...resto };
    if (tipo !== undefined) despues.tipo = tipo;
    if (comercio !== undefined) despues.comercio = comercio.trim();
    if (nota !== undefined) despues.nota = nota.trim();
    if (de !== undefined) { delete despues.de; if (slug(de)) despues.de = slug(de); }
    Object.assign(despues, plastico);
    despues.archivo = archivo ?? null;
    if (confirm !== true) return { preview: true, applied: false, cuenta: ubicado.cuenta, linea: ubicado.linea, antes, despues, message: 'Llamar de nuevo con confirm:true para guardarlo.' };
    guardarMovimiento(ubicado, despues);
    return { applied: true, cuenta: ubicado.cuenta, linea: ubicado.linea, antes, despues, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, toolText }) {
    const prev = toolJson(await callTool('bancos_editar_movimiento', { cuenta: 'bcr', fecha: '2026-10-20', linea: 1, de: 'Papá' }));
    check('bancos_editar_movimiento hace preview sin confirm', prev.preview === true && prev.despues?.de === 'papa' && !prev.antes?.de, JSON.stringify(prev).slice(0, 200));
    const ok = toolJson(await callTool('bancos_editar_movimiento', { id: prev.antes?.id, de: 'papa', nota: 'corte de pelo', referencia: '555', confirm: true }));
    check('bancos_editar_movimiento guarda con confirm por id', ok.applied === true && ok.despues?.nota === 'corte de pelo' && ok.despues?.referencia === '555', JSON.stringify(ok).slice(0, 200));
    const papa = toolText(await callTool('bancos_resumen', { mes: '2026-10', de: 'papa' }));
    check('bancos_resumen ve el cambio de rubro', /movimientos: 2/.test(papa) && papa.includes('CRC,0,9500,-9500,2'), papa.slice(0, 200));
    const hogar = toolText(await callTool('bancos_movimientos', { mes: '2026-10', de: 'hogar' }));
    check('bancos_movimientos con de=hogar excluye los del papá', /total: 2/.test(hogar) && !hogar.includes('Soda X'), hogar.slice(0, 200));
    const malo = toolJson(await callTool('bancos_editar_movimiento', { id: prev.antes?.id, tipo: 'inventado' }));
    check('bancos_editar_movimiento rechaza tipo desconocido', /desconocido/.test(malo.error ?? ''), malo.error);
    const adic = toolJson(await callTool('bancos_editar_movimiento', { cuenta: 'bac', fecha: '2026-10-24', linea: 1, tarjeta: '****8608', adicional: '3517', confirm: true }));
    check('bancos_editar_movimiento asocia el adicional a su tarjeta', adic.despues?.tarjeta === '8608' && adic.despues?.adicional === '3517', JSON.stringify(adic).slice(0, 300));
    const tarjetas = toolText(await callTool('bancos_resumen', { mes: '2026-10', cuenta: 'bac' }));
    check('bancos_resumen separa la tarjeta por plástico', /8608,3517,CRC,0,8500,-8500,1/.test(tarjetas), tarjetas.slice(-300));
    const huerfano = toolJson(await callTool('bancos_editar_movimiento', { id: adic.antes?.id, tarjeta: '' }));
    check('bancos_editar_movimiento pide la tarjeta del adicional', /necesita su tarjeta/.test(huerfano.error ?? ''), huerfano.error);
  },
};
