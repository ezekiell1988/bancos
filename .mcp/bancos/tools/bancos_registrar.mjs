import { existsSync } from 'node:fs';
import path from 'node:path';
import {
  CLASES, CUENTAS, MONEDAS, ToolError, appendJsonl, copiarRespaldo, diaFile, getDatosDir, leerTipos,
  mismoMovimiento, normalizarMovimiento, planRespaldo, readJsonl,
} from '../src/datos.mjs';

export default {
  name: 'bancos_registrar',
  order: 20,
  description:
    'Anota uno o varios movimientos (gastos o ingresos) de un documento en datos/<cuenta>/AAAA-MM/AAAA-MM-DD.jsonl ' +
    'y, si se pasa origen, copia el respaldo (foto, PDF, Excel) a datos/multimedia/AAAA-MM/. ' +
    'Sin apply:true solo devuelve el preview. Omite los que ya estén anotados (misma fecha, clase, total, moneda y comercio) ' +
    'salvo permitirDuplicados:true. El tipo debe existir en bancos_tipos.',
  inputSchema: {
    type: 'object',
    properties: {
      cuenta: { type: 'string', enum: CUENTAS, description: 'bcr = cuenta BCR (gastos e ingresos como salarios); bac = tarjeta BAC.' },
      movimientos: {
        type: 'array',
        minItems: 1,
        description: 'Movimientos del documento.',
        items: {
          type: 'object',
          properties: {
            fecha: { type: 'string', description: 'AAAA-MM-DD.' },
            clase: { type: 'string', enum: CLASES },
            tipo: { type: 'string', description: 'id de bancos_tipos de esa clase.' },
            total: { type: 'number', description: 'Monto positivo.' },
            moneda: { type: 'string', enum: MONEDAS },
            comercio: { type: 'string', description: 'Comercio, empresa o persona.' },
            nota: { type: 'string', description: 'Detalle opcional.' },
          },
          required: ['fecha', 'clase', 'tipo', 'total', 'moneda'],
          additionalProperties: false,
        },
      },
      origen: { type: 'string', description: 'Ruta absoluta del archivo de respaldo (p. ej. el que llegó por Telegram). Si ya está dentro de datos/, solo se referencia.' },
      nombreArchivo: { type: 'string', description: 'Nombre para el respaldo en multimedia/ (sin carpetas). Default: el del origen.' },
      permitirDuplicados: { type: 'boolean', description: 'true para anotar aunque ya exista uno igual.' },
      apply: { type: 'boolean', description: 'true para guardar; sin él solo preview.' },
    },
    required: ['cuenta', 'movimientos'],
    additionalProperties: false,
  },
  async handler({ cuenta, movimientos, origen, nombreArchivo, permitirDuplicados, apply }) {
    const tipos = { gasto: leerTipos('gasto'), ingreso: leerTipos('ingreso') };
    const nuevos = movimientos.map((m) => normalizarMovimiento(m, tipos));

    const respaldo = origen ? planRespaldo(origen, nuevos[0].fecha, nombreArchivo) : null;
    if (respaldo) for (const m of nuevos) m.archivo = respaldo.relativo;

    const porArchivo = new Map();
    const omitidos = [];
    for (const m of nuevos) {
      const file = diaFile(cuenta, m.fecha);
      if (!porArchivo.has(file)) porArchivo.set(file, { existentes: readJsonl(file), agregar: [] });
      const grupo = porArchivo.get(file);
      if (!permitirDuplicados && [...grupo.existentes, ...grupo.agregar].some((e) => mismoMovimiento(e, m))) {
        omitidos.push({ fecha: m.fecha, total: m.total, moneda: m.moneda, comercio: m.comercio });
        continue;
      }
      grupo.agregar.push(m);
    }

    const archivos = [...porArchivo.entries()]
      .filter(([, g]) => g.agregar.length)
      .map(([file, g]) => ({ archivo: path.relative(getDatosDir(), file), nuevo: !existsSync(file), agrega: g.agregar.length }));
    const totales = {};
    for (const [, g] of porArchivo) for (const m of g.agregar) {
      const k = `${m.clase} ${m.moneda}`;
      totales[k] = Math.round(((totales[k] ?? 0) + m.total) * 100) / 100;
    }
    const resumen = {
      cuenta,
      anota: archivos.reduce((n, a) => n + a.agrega, 0),
      totales,
      archivos,
      respaldo: respaldo ? { archivo: respaldo.relativo, copia: respaldo.copiar } : undefined,
      omitidosPorDuplicado: omitidos.length ? omitidos : undefined,
    };

    if (apply !== true) {
      return { preview: true, applied: false, ...resumen, movimientos: [...porArchivo.values()].flatMap((g) => g.agregar), message: 'Llamar de nuevo con apply:true para guardar.' };
    }
    if (!resumen.anota && !omitidos.length) throw new ToolError('No hay movimientos para anotar.');
    if (respaldo && resumen.anota) copiarRespaldo(respaldo);
    for (const [file, g] of porArchivo) if (g.agregar.length) appendJsonl(file, g.agregar);
    return { applied: true, ...resumen };
  },
  async smoke({ callTool, check, toolJson, state }) {
    const movimientos = [
      { fecha: '2026-10-24', clase: 'gasto', tipo: 'comida', total: 8500, moneda: 'CRC', comercio: 'Soda X' },
      { fecha: '2026-10-25', clase: 'gasto', tipo: 'supermercado', total: 12000.5, moneda: 'CRC', comercio: 'Súper Y' },
    ];
    const prev = toolJson(await callTool('bancos_registrar', { cuenta: 'bac', movimientos, origen: state.respaldo }));
    check('bancos_registrar hace preview sin apply', prev.preview === true && prev.anota === 2 && prev.respaldo?.copia === true, JSON.stringify(prev).slice(0, 200));

    const ok = toolJson(await callTool('bancos_registrar', { cuenta: 'bac', movimientos, origen: state.respaldo, apply: true }));
    check('bancos_registrar guarda con apply', ok.applied === true && ok.anota === 2 && ok.archivos?.length === 2, JSON.stringify(ok).slice(0, 200));
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
  },
};
