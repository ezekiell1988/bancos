import { execFileSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import path from 'node:path';
import { ToolError, getDatosDir } from '../src/datos.mjs';
import { aplicarMigracion, planMigracion } from '../src/migracion.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';

export default {
  name: 'bancos_migrar_libros',
  order: 90,
  description:
    'Migra datos/ al modelo de 3 niveles de ADR-EBC-03 (una sola vez): cada movimiento con tipo pasa a un libro (nivel 2) con una partida por su total; los comprobantes ' +
    'se vuelven libros con documento (datos/comprobantes se aparta en datos/comprobantes-antes-libros); las facturas pasan a colgar de un libro y las líneas llevan su tipo; ' +
    'los movimientos dejan de guardar el tipo. Devuelve el informe: lo que no cuadró (facturas con varios pagos, sumas distintas) y el total de cada ciclo antes y después. ' +
    'Sin apply:true solo da la vista previa. Con apply:true hace antes un respaldo tar.gz de datos/ en datos/respaldos/ y no aplica si los totales por ciclo no dan igual. ' +
    'Si datos/libros ya tiene datos no hace nada.',
  inputSchema: { type: 'object', properties: { apply: { type: 'boolean', description: 'true para migrar; sin él solo vista previa.' } }, additionalProperties: false },
  async handler({ apply }) {
    const plan = planMigracion();
    const { resumen, totales, informe } = plan;
    if (apply !== true) return { preview: true, applied: false, resumen, totales, informe, message: 'Llamar de nuevo con apply:true para migrar (hace respaldo antes).' };
    const datos = getDatosDir();
    const fecha = new Date().toISOString().slice(0, 10);
    const respaldo = path.join(datos, 'respaldos', `${fecha}-antes-libros-auto.tar.gz`);
    try {
      execFileSync('tar', ['czf', respaldo, '--exclude', 'respaldos', '-C', path.dirname(datos), path.basename(datos)]);
    } catch (e) { throw new ToolError(`No se pudo hacer el respaldo: ${e.message}`); }
    if (!existsSync(respaldo)) throw new ToolError('No quedó el respaldo; no se migra.');
    aplicarMigracion(plan);
    return { applied: true, respaldo: path.relative(datos, respaldo), resumen, totales, informe, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson }) {
    const r = toolJson(await callTool('bancos_migrar_libros', {}));
    check('bancos_migrar_libros no corre dos veces: ya hay libros', /ya se aplicó/.test(r.error ?? ''), r.error);
  },
};
