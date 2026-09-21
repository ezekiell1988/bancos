// Puerto de MarkdownReport.cs: renderiza el resultado de db_exec como Markdown con una
// sección numerada por consulta (SQL, estado, filas afectadas, resultsets).
export function renderReport(queries, results, timeoutSeconds) {
  const lines = [
    '# db_exec — Reporte de ejecución',
    '',
    `**Timestamp:** ${new Date().toISOString()}`,
    `**Timeout por consulta:** ${timeoutSeconds}s`,
    `**Consultas:** ${queries.length}`,
    '',
  ];

  queries.forEach((query, index) => {
    const result = results[index];
    const number = index + 1;
    lines.push(`## ${number}. Consulta ${number}`, '', '```sql', query.trim(), '```', '', `**Estado:** ${result.status === 'ok' ? 'OK' : 'ERROR'}`, '');
    if (result.status === 'error') {
      lines.push(`**Error:** ${result.error}`, '');
      return;
    }
    lines.push(`**Filas afectadas:** ${result.rowsAffected.length === 0 ? '0' : result.rowsAffected.join(', ')}`, '');
    if (result.resultsets.length === 0) {
      lines.push('_Sin resultsets._', '');
      return;
    }
    result.resultsets.forEach((set, setIndex) => {
      lines.push(`### Resultset ${setIndex + 1} (${set.rows.length} fila(s))`, '');
      if (set.columns.length === 0) {
        lines.push('_Sin columnas._', '');
        return;
      }
      lines.push(`| ${set.columns.join(' | ')} |`, `| ${set.columns.map(() => '---').join(' | ')} |`);
      set.rows.forEach((row) => lines.push(`| ${set.columns.map((column) => inline(row[column])).join(' | ')} |`));
      lines.push('');
    });
  });

  return lines.join('\n');
}

function inline(value) {
  if (value === undefined || value === null) return 'NULL';
  return String(value).replaceAll('|', '\\|').replaceAll('\r', '').replaceAll('\n', '<br>');
}
