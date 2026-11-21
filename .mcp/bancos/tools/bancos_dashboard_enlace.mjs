import { readFileSync } from 'node:fs';
import net from 'node:net';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const CONFIG = path.join(path.dirname(fileURLToPath(import.meta.url)), '../../../dashboard/config.json');
const solo = (t) => String(t ?? '').replace(/[^\d+]/g, '');

/** Números permitidos del dashboard (dashboard/config.json → whatsapp: { yo, novia }); nunca van en el código. */
function leerConfig() {
  try { return JSON.parse(readFileSync(CONFIG, 'utf8')); } catch { return {}; }
}

/** Resuelve «yo», «novia» o un teléfono a E.164 y lo compara con la lista; sin coincidencia no se firma nada. */
export function resolverDestino(para, cfg) {
  const lista = Object.entries(cfg.whatsapp ?? {}).map(([nombre, tel]) => [nombre, solo(tel)]);
  const clave = String(para ?? '').trim().toLowerCase();
  const porNombre = lista.find(([n]) => n === clave);
  const tel = porNombre ? porNombre[1] : solo(para);
  if (!tel || !lista.some(([, t]) => t === tel)) throw new Error('Ese número no está permitido para el dashboard: solo el del dueño y el de su novia.');
  return tel;
}

function bridge(socket, req) {
  return new Promise((resolve, reject) => {
    const s = net.createConnection(socket.replace(/^~(?=\/)/, os.homedir()));
    let buf = '';
    s.on('connect', () => s.write(`${JSON.stringify(req)}\n`));
    s.on('data', (c) => { buf += c; });
    s.on('end', () => { try { const r = JSON.parse(buf); r.error ? reject(new Error(r.error)) : resolve(r.result); } catch { reject(new Error('Respuesta inválida del bot.')); } });
    s.on('error', reject);
    s.setTimeout(30000, () => { s.destroy(); reject(new Error('El bot no respondió.')); });
  });
}

export default {
  name: 'bancos_dashboard_enlace',
  order: 60,
  description:
    'Manda por WhatsApp un enlace al dashboard de Finanzas (solo consulta), válido 1 hora. para: "yo" (dueño), "novia" o un teléfono E.164 de la lista permitida ' +
    '(dashboard/config.json → whatsapp); cualquier otro número se rechaza sin firmar nada. Esa persona tiene que haberle escrito al bot en las últimas 24 h (regla de Meta). ' +
    'Devuelve si se mandó y cuándo vence; el enlace no se devuelve.',
  inputSchema: {
    type: 'object',
    properties: { para: { type: 'string', description: '«yo», «novia» o teléfono con código de país (+506…).' } },
    required: ['para'],
    additionalProperties: false,
  },
  async handler({ para }) {
    const cfg = leerConfig();
    const telefono = resolverDestino(para, cfg);
    if (!cfg.chatDuenoId) throw new Error('Falta chatDuenoId en dashboard/config.json (chat privado del dueño con el bot).');
    const r = await bridge(cfg.socket, { op: 'enlace_pestana', chatId: String(cfg.chatDuenoId), pestana: cfg.pestana ?? 'finanzas', telefono });
    return { enviado: r.enviado === true, vence: r.vence };
  },
  async smoke({ callTool, check }) {
    const r = await callTool('bancos_dashboard_enlace', { para: '+50670000000' });
    check('bancos_dashboard_enlace rechaza un número fuera de la lista sin firmar enlace', JSON.stringify(r).includes('no está permitido'), JSON.stringify(r).slice(0, 200));
  },
};
