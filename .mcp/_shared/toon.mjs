// Encoder TOON (Token-Oriented Object Notation) mínimo para respuestas tabulares.
// Arrays de objetos uniformes → bloque tabular `key[N]{cols}:` con una fila por línea.

function isPrimitive(v) {
  return v === null || ['string', 'number', 'boolean'].includes(typeof v);
}

function uniformKeys(arr) {
  if (!Array.isArray(arr) || arr.length === 0) return null;
  if (!arr.every((x) => x && typeof x === 'object' && !Array.isArray(x))) return null;
  const keys = Object.keys(arr[0]);
  if (keys.length === 0) return null;
  const set = new Set(keys);
  for (const item of arr) {
    const k = Object.keys(item);
    if (k.length !== keys.length || !k.every((key) => set.has(key))) return null;
    if (!k.every((key) => isPrimitive(item[key]))) return null;
  }
  return keys;
}

function cell(v) {
  if (v === null || v === undefined) return '';
  const s = String(v).replace(/\r?\n/g, ' ');
  return /[,|\n]/.test(s) ? JSON.stringify(s) : s;
}

function encodeValue(value, key, indent) {
  const pad = ' '.repeat(indent);
  if (Array.isArray(value)) {
    const cols = uniformKeys(value);
    if (cols) {
      const lines = [`${pad}${key}[${value.length}]{${cols.join(',')}}:`];
      for (const item of value) lines.push(`${pad} ${cols.map((c) => cell(item[c])).join(',')}`);
      return lines.join('\n');
    }
    if (value.every(isPrimitive)) return `${pad}${key}[${value.length}]: ${value.map(cell).join(',')}`;
    const lines = [`${pad}${key}[${value.length}]:`];
    value.forEach((item, i) => lines.push(encodeValue(item, String(i), indent + 1)));
    return lines.join('\n');
  }
  if (value && typeof value === 'object') {
    const lines = [`${pad}${key}:`];
    for (const [k, v] of Object.entries(value)) lines.push(encodeValue(v, k, indent + 1));
    return lines.join('\n');
  }
  return `${pad}${key}: ${cell(value)}`;
}

/** Codifica un payload a TOON. Objetos raíz: una línea por campo. */
export function encodeToon(payload) {
  if (payload === null || payload === undefined) return '';
  if (isPrimitive(payload)) return String(payload);
  if (Array.isArray(payload)) return encodeValue(payload, 'items', 0);
  return Object.entries(payload)
    .map(([k, v]) => encodeValue(v, k, 0))
    .join('\n');
}
