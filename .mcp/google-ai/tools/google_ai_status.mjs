import { getProjectRoot } from '../src/common.mjs';
import { getCredentials, checkEndpoint } from '../src/client.mjs';

function maskApiKey(apiKey) {
  return apiKey.length > 8 ? `${apiKey.slice(0, 4)}...${apiKey.slice(-4)}` : '***';
}

export default {
  name: 'google_ai_status',
  description:
    'Verifica el estado de las credenciales de Google AI (Gemini API) cargadas desde ai-google.json y ai-google-veo.json, y los modelos disponibles (texto Gemini, video Veo 3.1/3.1 Fast). Enmascara la apiKey por seguridad.',
  inputSchema: {
    type: 'object',
    properties: {
      checkEndpoint: { type: 'boolean', description: 'Si es true, intenta una llamada GET al endpoint base para validar conectividad de red.' },
    },
    required: [],
    additionalProperties: false,
  },
  async handler(args) {
    const projectRoot = getProjectRoot();
    const creds = getCredentials(projectRoot);

    let endpointCheck = null;
    if (args.checkEndpoint === true) {
      const { reachable, status } = await checkEndpoint(projectRoot);
      endpointCheck = { reachable, httpStatus: status };
    }

    return {
      status: 'ready',
      credentialsFiles: creds.sourcePaths,
      endpoint: creds.endpoint,
      apiKeyMasked: maskApiKey(creds.apiKey),
      models: creds.models,
      endpointCheck,
    };
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('google_ai_status', {}));
    if (res.error) {
      check('google_ai_status sin credenciales responde error controlado', typeof res.error === 'string', res.error);
      return;
    }
    check('google_ai_status responde status:ready', res.status === 'ready', JSON.stringify(res).slice(0, 150));
    check('google_ai_status enmascara la apiKey', typeof res.apiKeyMasked === 'string' && res.apiKeyMasked.includes('...'), res.apiKeyMasked);
  },
};
