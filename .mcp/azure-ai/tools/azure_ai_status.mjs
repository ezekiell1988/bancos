import { getProjectRoot } from '../src/common.mjs';
import { getCredentials, checkEndpoint } from '../src/client.mjs';

function maskApiKey(apiKey) {
  return apiKey.length > 8 ? `${apiKey.slice(0, 4)}...${apiKey.slice(-4)}` : '***';
}

export default {
  name: 'azure_ai_status',
  description: 'Verifica credenciales, endpoint y modelos de Azure AI Foundry cargados desde ai-foundry.json. Enmascara la apiKey por seguridad.',
  inputSchema: {
    type: 'object',
    properties: {
      checkEndpoint: { type: 'boolean', description: 'Si es true intenta conectividad al endpoint.' },
    },
    required: [],
    additionalProperties: false,
  },
  async handler(args) {
    const projectRoot = getProjectRoot();
    const creds = getCredentials(projectRoot);

    let endpointCheck = null;
    if (args.checkEndpoint === true) {
      endpointCheck = await checkEndpoint(projectRoot);
    }

    return {
      status: 'ready',
      credentialsFile: creds.sourcePath,
      azureOpenAIEndpoint: creds.endpoint,
      projectEndpoint: creds.projectEndpoint ?? 'No configurado',
      apiKeyMasked: maskApiKey(creds.apiKey),
      models: creds.models,
      endpointCheck,
    };
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('azure_ai_status', {}));
    if (res.error) {
      check('azure_ai_status sin credenciales responde error controlado', typeof res.error === 'string', res.error);
      return;
    }
    check('azure_ai_status responde status:ready', res.status === 'ready', JSON.stringify(res).slice(0, 150));
    check('azure_ai_status enmascara la apiKey', typeof res.apiKeyMasked === 'string' && res.apiKeyMasked.includes('...'), res.apiKeyMasked);
  },
};
