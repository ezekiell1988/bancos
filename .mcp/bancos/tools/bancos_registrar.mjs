import { existsSync } from 'node:fs';
import path from 'node:path';
import {
  ADICIONAL_PROP, CLASES, CUENTAS, DE_PROP, FECHA_PROP, FECHA_REAL_PROP, MONEDAS, REFERENCIA_PROP, appendJsonl, copiarRespaldo, diaFile,
  getDatosDir, guardarDetalles, leerMovimientos, leerTodosLosDetalles, leerTodosLosTipos, mismoMovimiento, normalizarMovimiento,
  planRespaldo, r2, sinUbicacion, TARJETA_PROP,
} from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';
import { completarConPares, planVinculos } from '../src/vinculos.mjs';

export default {
  name: 'bancos_registrar',
  order: 20,
  description:
    'Nivel 1 (base de verdad de los reportes): anota los movimientos de un estado de cuenta (HTML del BCR, PDF de la tarjeta BAC) ' +
    'en datos/<cuenta>/AAAA-MM/AAAA-MM-DD.jsonl, con id propio, y copia el respaldo a datos/multimedia/AAAA-MM/. ' +
    'fecha = fecha de proceso del banco; fechaReal = fecha de compra, solo referencia; referencia = número del movimiento en el banco. ' +
    'Vincula solos los comprobantes y facturas pendientes (por referencia o por monto y fecha) y toma de ellos el tipo, el de y la nota. ' +
    'Sin apply:true solo devuelve el preview. Omite los ya anotados (misma fecha, clase, total, moneda y referencia o comercio) salvo permitirDuplicados:true. ' +
    'Comprobantes sueltos (SINPE, voucher) van con bancos_comprobante y facturas con bancos_factura, no aquí.',
  inputSchema: {
    type: 'object',
    properties: {
      cuenta: { type: 'string', enum: CUENTAS, description: 'bcr = cuenta BCR (gastos e ingresos como salarios); bac = tarjeta BAC.' },
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
            tipo: { type: 'string', description: 'id de bancos_tipos de esa clase.' },
            total: { type: 'number', description: 'Monto positivo.' },
            moneda: { type: 'string', enum: MONEDAS },
            comercio: { type: 'string', description: 'Comercio, empresa o persona.' },
            nota: { type: 'string', description: 'Detalle opcional.' },
            de: DE_PROP,
            tarjeta: TARJETA_PROP,
            adicional: ADICIONAL_PROP,
          },
          required: ['fecha', 'clase', 'tipo', 'total', 'moneda'],
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

    // Comprobantes y facturas que esperaban este estado.
    const detalles = leerTodosLosDetalles();
    const pares = planVinculos(detalles.filter((d) => !d.movimiento), agregar.map((m) => ({ cuenta, ...m })), detalles);
    const porId = new Map(agregar.map((m) => [m.id, m]));
    const vinculados = completarConPares(pares, porId);
    const finales = agregar.map((m) => porId.get(m.id));

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
    };

    if (apply !== true) return { preview: true, applied: false, ...resumen, movimientos: finales, message: 'Llamar de nuevo con apply:true para guardar.' };
    if (!finales.length) return { applied: false, ...resumen, message: 'Todos ya estaban anotados; no se guardó nada.' };
    if (respaldo) copiarRespaldo(respaldo);
    for (const [file, rows] of porArchivo) appendJsonl(file, rows);
    guardarDetalles(pares.filter((p) => !p.soloCompleta).map((p) => ({ detalle: p.detalle, nuevo: { ...sinUbicacion(p.detalle), movimiento: p.movimiento.id } })));
    return { applied: true, ...resumen, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, state }) {
    const movimientos = [
      { fecha: '2026-10-24', referencia: '1001', clase: 'gasto', tipo: 'comida', total: 8500, moneda: 'CRC', comercio: 'Soda X' },
      { fecha: '2026-10-25', clase: 'gasto', tipo: 'supermercado', total: 12000.5, moneda: 'CRC', comercio: 'Súper Y' },
    ];
    const prev = toolJson(await callTool('bancos_registrar', { cuenta: 'bac', movimientos, origen: state.respaldo }));
    check('bancos_registrar hace preview sin apply', prev.preview === true && prev.anota === 2 && prev.respaldo?.copia === true, JSON.stringify(prev).slice(0, 200));
    check('bancos_registrar le pone id y referencia', /^m-[0-9a-f]{8}$/.test(prev.movimientos?.[0]?.id ?? '') && prev.movimientos[0].referencia === '1001', JSON.stringify(prev.movimientos?.[0]));

    const ok = toolJson(await callTool('bancos_registrar', { cuenta: 'bac', movimientos, origen: state.respaldo, apply: true }));
    check('bancos_registrar guarda con apply', ok.applied === true && ok.anota === 2 && ok.archivos?.length === 2 && !ok.aviso, JSON.stringify(ok).slice(0, 200));
    check('bancos_registrar copia el respaldo a multimedia/AAAA-MM', ok.respaldo?.archivo === 'multimedia/2026-10/factura-soda.jpg', ok.respaldo?.archivo);

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
  },
};
