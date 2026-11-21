import { existsSync } from 'node:fs';
import path from 'node:path';
import {
  ADICIONAL_PROP, CLASES, CUENTAS, DE_PROP, FECHA_PROP, FECHA_REAL_PROP, MONEDAS, REFERENCIA_PROP, appendJsonl, completarMovimiento, copiarRespaldo, detalleFile, detalleSinUbicacion,
  diaFile, emparejar, getDatosDir, guardarDetalles, leerDetalles, leerMovimientos, leerTodosLosTipos, mismoMovimiento, normalizarMovimiento,
  planRespaldo, r2, TARJETA_PROP,
} from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';
import { libroParaMovimiento } from '../src/vinculos.mjs';

export default {
  name: 'bancos_registrar',
  order: 20,
  description:
    'Nivel 1 (base de verdad de los reportes): anota los movimientos de un estado de cuenta (HTML del BCR, PDF de la tarjeta BAC o del BN) ' +
    'en datos/<cuenta>/AAAA-MM/AAAA-MM-DD.jsonl, con id propio, y copia el respaldo a datos/multimedia/AAAA-MM/. ' +
    'fecha = fecha de proceso del banco; fechaReal = fecha de compra, solo referencia; referencia = número del movimiento en el banco. ' +
    'Cada movimiento queda con su libro (nivel 2, ADR-EBC-03): si hay un pago pendiente (bancos_comprobante) se vincula solo, por referencia o por monto y fecha, ' +
    'y trae su categoría, su de y su nota; si no, se crea un libro sin documento con una partida por todo el total (tipo, o «otros»). ' +
    'Sin apply:true solo devuelve el preview. Omite los ya anotados (misma fecha, clase, total, moneda y referencia o comercio) salvo permitirDuplicados:true. ' +
    'Comprobantes sueltos (SINPE, voucher) van con bancos_comprobante y facturas con bancos_factura, no aquí.',
  inputSchema: {
    type: 'object',
    properties: {
      cuenta: { type: 'string', enum: CUENTAS, description: 'bcr = cuenta BCR (gastos e ingresos como salarios); bac = tarjeta BAC; bncr = tarjeta del Banco Nacional.' },
      movimientos: {
        type: 'array',
        minItems: 1,
        description: 'Líneas del estado de cuenta.',
        items: {
          type: 'object',
          properties: {
            fecha: FECHA_PROP,
            fechaReal: FECHA_REAL_PROP,
            referencia: REFERENCIA_PROP,
            clase: { type: 'string', enum: CLASES },
            tipo: { type: 'string', description: 'id de bancos_tipos de esa clase: la categoría de su libro. Sin tipo va a «otros» y se revisa al cerrar el ciclo.' },
            total: { type: 'number', description: 'Monto positivo.' },
            moneda: { type: 'string', enum: MONEDAS },
            comercio: { type: 'string', description: 'Comercio, empresa o persona.' },
            nota: { type: 'string', description: 'Detalle opcional.' },
            de: DE_PROP,
            tarjeta: TARJETA_PROP,
            adicional: ADICIONAL_PROP,
          },
          required: ['fecha', 'clase', 'total', 'moneda'],
          additionalProperties: false,
        },
      },
      origen: { type: 'string', description: 'Ruta absoluta del estado de cuenta (p. ej. el que llegó por Telegram). Si ya está dentro de datos/, solo se referencia.' },
      nombreArchivo: { type: 'string', description: 'Nombre para el respaldo en multimedia/ (sin carpetas). Default: el del origen.' },
      permitirDuplicados: { type: 'boolean', description: 'true para anotar aunque ya exista uno igual.' },
      apply: { type: 'boolean', description: 'true para guardar; sin él solo preview.' },
    },
    required: ['cuenta', 'movimientos'],
    additionalProperties: false,
  },
  async handler({ cuenta, movimientos, origen, nombreArchivo, permitirDuplicados, apply }) {
    const nuevos = movimientos.map((m) => normalizarMovimiento(m, leerTodosLosTipos()));
    const respaldo = origen ? planRespaldo(origen, nuevos[0].fecha, nombreArchivo) : null;
    if (respaldo) for (const m of nuevos) m.archivo = respaldo.relativo;

    const fechas = nuevos.map((m) => m.fecha).sort();
    const existentes = leerMovimientos(cuenta, fechas[0], fechas.at(-1));
    const agregar = [];
    const omitidos = [];
    for (const m of nuevos) {
      // Cada anotado cubre a un solo nuevo: dos líneas iguales del mismo documento son dos movimientos.
      const igual = permitirDuplicados ? null : existentes.find((e) => !e.usado && mismoMovimiento(e, m));
      if (igual) { igual.usado = true; omitidos.push({ fecha: m.fecha, total: m.total, moneda: m.moneda, comercio: m.comercio, id: igual.id }); continue; }
      agregar.push(m);
    }

    // Pagos (libros) que esperaban este estado; el resto de movimientos recibe un libro sin documento con su categoría.
    const tipoDe = new Map(agregar.map((m) => [m.id, m.tipo]));
    const sinTipo = agregar.map(({ tipo: _t, ...m }) => m);
    const pendientes = leerDetalles('libros').filter((l) => !l.movimiento && !l.transito);
    const pares = emparejar(pendientes, sinTipo.map((m) => ({ cuenta, ...m })));
    const porId = new Map(sinTipo.map((m) => [m.id, m]));
    const vinculados = pares.map((p) => {
      const { nuevo, cambios } = completarMovimiento(porId.get(p.movimiento.id), p.detalle);
      porId.set(p.movimiento.id, nuevo);
      return { detalle: p.detalle.id, movimiento: p.movimiento.id, fecha: p.movimiento.fecha, total: p.movimiento.total, comercio: p.movimiento.comercio,
        por: p.por, ...(p.empate ? { empate: true } : {}), cambios: cambios.join('; ') };
    });
    const finales = sinTipo.map((m) => porId.get(m.id));
    const conLibro = new Set(pares.map((p) => p.movimiento.id));
    const libros = finales.filter((m) => !conLibro.has(m.id)).map((m) => libroParaMovimiento({ ...m, cuenta }, { tipo: tipoDe.get(m.id) }));

    const porArchivo = new Map();
    for (const m of finales) {
      const file = diaFile(cuenta, m.fecha);
      porArchivo.set(file, [...(porArchivo.get(file) ?? []), m]);
    }
    const totales = {};
    for (const m of finales) totales[`${m.clase} ${m.moneda}`] = r2((totales[`${m.clase} ${m.moneda}`] ?? 0) + m.total);
    const resumen = {
      cuenta,
      anota: finales.length,
      totales,
      archivos: [...porArchivo].map(([file, rows]) => ({ archivo: path.relative(getDatosDir(), file), nuevo: !existsSync(file), agrega: rows.length })),
      respaldo: respaldo ? { archivo: respaldo.relativo, copia: respaldo.copiar } : undefined,
      omitidosPorDuplicado: omitidos.length ? omitidos : undefined,
      vinculados: vinculados.length ? vinculados : undefined,
      librosNuevos: libros.length,
      enOtros: libros.filter((l) => l.partidas[0].tipo === 'otros').length || undefined,
    };

    if (apply !== true) return { preview: true, applied: false, ...resumen, movimientos: finales, message: 'Llamar de nuevo con apply:true para guardar.' };
    if (!finales.length) return { applied: false, ...resumen, message: 'Todos ya estaban anotados; no se guardó nada.' };
    if (respaldo) copiarRespaldo(respaldo);
    for (const [file, rows] of porArchivo) appendJsonl(file, rows);
    guardarDetalles(pares.map((p) => ({ detalle: p.detalle, nuevo: { ...detalleSinUbicacion(p.detalle), movimiento: p.movimiento.id } })));
    for (const l of libros) appendJsonl(detalleFile('libros', l.fecha), [l]);
    return { applied: true, ...resumen, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, toolText, state }) {
    const movimientos = [
      { fecha: '2026-10-24', referencia: '1001', clase: 'gasto', tipo: 'comida', total: 8500, moneda: 'CRC', comercio: 'Soda X' },
      { fecha: '2026-10-25', clase: 'gasto', tipo: 'supermercado', total: 12000.5, moneda: 'CRC', comercio: 'Súper Y' },
    ];
    const prev = toolJson(await callTool('bancos_registrar', { cuenta: 'bac', movimientos, origen: state.respaldo }));
    check('bancos_registrar hace preview sin apply', prev.preview === true && prev.anota === 2 && prev.respaldo?.copia === true && prev.librosNuevos === 2, JSON.stringify(prev).slice(0, 200));
    check('bancos_registrar le pone id y referencia, y no guarda el tipo en el movimiento', /^m-[0-9a-f]{8}$/.test(prev.movimientos?.[0]?.id ?? '') && prev.movimientos[0].referencia === '1001' && !('tipo' in prev.movimientos[0]), JSON.stringify(prev.movimientos?.[0]));

    const ok = toolJson(await callTool('bancos_registrar', { cuenta: 'bac', movimientos, origen: state.respaldo, apply: true }));
    check('bancos_registrar guarda con apply', ok.applied === true && ok.anota === 2 && ok.archivos?.length === 2 && !ok.aviso, JSON.stringify(ok).slice(0, 200));
    check('bancos_registrar copia el respaldo a multimedia/AAAA-MM', ok.respaldo?.archivo === 'multimedia/2026-10/factura-soda.jpg', ok.respaldo?.archivo);
    const lista = toolText(await callTool('bancos_movimientos', { mes: '2026-10' }));
    check('todo movimiento queda con su libro y su categoría', /Soda X.*l-[0-9a-f]{8},comida:8500/.test(lista), lista.slice(0, 400));

    const dup = toolJson(await callTool('bancos_registrar', { cuenta: 'bac', movimientos: movimientos.slice(0, 1), apply: true }));
    check('bancos_registrar omite duplicados', dup.anota === 0 && dup.omitidosPorDuplicado?.length === 1, JSON.stringify(dup).slice(0, 200));

    const malo = toolJson(await callTool('bancos_registrar', { cuenta: 'bcr', movimientos: [{ ...movimientos[0], tipo: 'inventado' }] }));
    check('bancos_registrar rechaza tipo desconocido', /desconocido/.test(malo.error ?? ''), malo.error);

    const ingreso = toolJson(await callTool('bancos_registrar', {
      cuenta: 'bcr', apply: true,
      movimientos: [{ fecha: '2026-10-15', clase: 'ingreso', tipo: 'salario-itqs', total: 500000, moneda: 'CRC', comercio: 'ITQS' }],
    }));
    check('bancos_registrar anota ingreso en bcr', ingreso.applied === true && ingreso.anota === 1, JSON.stringify(ingreso).slice(0, 200));

    const dos = toolJson(await callTool('bancos_registrar', {
      cuenta: 'bcr', apply: true,
      movimientos: [1, 2].map(() => ({ fecha: '2026-10-20', clase: 'gasto', tipo: 'comida', total: 1000, moneda: 'CRC', comercio: 'SINPE' })),
    }));
    check('bancos_registrar anota dos líneas iguales del mismo documento', dos.anota === 2, JSON.stringify(dos).slice(0, 200));

    const sinTipo = toolJson(await callTool('bancos_registrar', { cuenta: 'bcr', apply: true, movimientos: [{ fecha: '2026-11-04', clase: 'gasto', total: 5000, moneda: 'CRC', comercio: 'Retiro cajero' }] }));
    check('bancos_registrar sin tipo lo manda a «otros»', sinTipo.applied === true && sinTipo.enOtros === 1, JSON.stringify(sinTipo).slice(0, 200));
    state.idRetiro = toolText(await callTool('bancos_movimientos', { mes: '2026-11', texto: 'Retiro' })).match(/m-[0-9a-f]{8}/)?.[0];
  },
};
