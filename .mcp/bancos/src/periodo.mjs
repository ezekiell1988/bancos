// Rango de fechas común a los tools de consulta: mes (AAAA-MM) o desde/hasta (AAAA-MM-DD).
import { ToolError, checkFecha, checkMes } from './datos.mjs';

export const periodoProps = {
  mes: { type: 'string', description: 'Mes AAAA-MM. Alternativa a desde/hasta.' },
  desde: { type: 'string', description: 'Fecha inicial AAAA-MM-DD (incluida).' },
  hasta: { type: 'string', description: 'Fecha final AAAA-MM-DD (incluida).' },
};

export function resolverPeriodo({ mes, desde, hasta }) {
  if (mes) {
    if (desde || hasta) throw new ToolError('Usar mes o desde/hasta, no ambos.');
    checkMes(mes);
    return { desde: `${mes}-01`, hasta: `${mes}-31` };
  }
  if (!desde && !hasta) throw new ToolError('Indicar mes o desde/hasta.');
  if (desde) checkFecha(desde, 'desde');
  if (hasta) checkFecha(hasta, 'hasta');
  return { desde: desde ?? '0000-01-01', hasta: hasta ?? '9999-12-31' };
}
