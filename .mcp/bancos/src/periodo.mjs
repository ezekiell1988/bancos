// Rango de fechas común a los tools de consulta: ciclo (AAAA-MM, del 19 al 18), mes (AAAA-MM) o desde/hasta (AAAA-MM-DD).
// El periodo de la familia es el ciclo del 19 de un mes al 18 del siguiente (corte de la BAC), no el mes calendario.
import { ToolError, checkFecha, checkMes } from './datos.mjs';

export const periodoProps = {
  ciclo: {
    type: 'string',
    description: 'AAAA-MM: el periodo de la familia, del 19 del mes anterior al 18 de este mes (corte de la BAC). ' +
      'Ej. 2026-11 = 2026-10-19 a 2026-11-18. Es el que se usa por defecto para reportes.',
  },
  mes: { type: 'string', description: 'Mes calendario AAAA-MM (del 1 al 31). Solo si se pide explícitamente; el periodo normal es ciclo.' },
  desde: { type: 'string', description: 'Fecha inicial AAAA-MM-DD (incluida).' },
  hasta: { type: 'string', description: 'Fecha final AAAA-MM-DD (incluida).' },
};

/** Ciclo AAAA-MM → del 19 del mes anterior al 18 de ese mes. */
export function rangoCiclo(ciclo) {
  checkMes(ciclo);
  const [a, m] = ciclo.split('-').map(Number);
  const [pa, pm] = m === 1 ? [a - 1, 12] : [a, m - 1];
  return { desde: `${pa}-${String(pm).padStart(2, '0')}-19`, hasta: `${ciclo}-18` };
}

/** Ciclo al que pertenece una fecha AAAA-MM-DD: del 19 en adelante es el del mes siguiente. */
export function cicloDe(fecha) {
  const [a, m, d] = fecha.split('-').map(Number);
  if (d < 19) return fecha.slice(0, 7);
  return m === 12 ? `${a + 1}-01` : `${a}-${String(m + 1).padStart(2, '0')}`;
}

export function resolverPeriodo({ ciclo, mes, desde, hasta }) {
  if ([ciclo, mes, desde || hasta].filter(Boolean).length > 1) throw new ToolError('Usar ciclo, mes o desde/hasta, uno solo.');
  if (ciclo) return rangoCiclo(ciclo);
  if (mes) {
    checkMes(mes);
    return { desde: `${mes}-01`, hasta: `${mes}-31` };
  }
  if (!desde && !hasta) throw new ToolError('Indicar ciclo, mes o desde/hasta.');
  if (desde) checkFecha(desde, 'desde');
  if (hasta) checkFecha(hasta, 'hasta');
  return { desde: desde ?? '0000-01-01', hasta: hasta ?? '9999-12-31' };
}
