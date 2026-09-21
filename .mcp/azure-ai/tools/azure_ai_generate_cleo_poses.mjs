import { copyFileSync, existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { getProjectRoot, ToolError } from '../src/common.mjs';
import { resolveWorkspacePath } from '../src/workspace-paths.mjs';
import { editImage } from '../src/client.mjs';

// Puerto de AzureAiCleoPosesTool.cs — mismo diccionario de poses.
const POSES = {
  idle: 'relaxed neutral pose',
  waving: 'one hand raised waving hello',
  thinking: 'thinking pose',
  happy: 'celebration and joy',
  talking: 'gesturing while speaking',
  curious: 'curious expression',
  'pointing-left': 'pointing left',
  'pointing-right': 'pointing right',
  yes: 'thumbs up',
  no: 'stop gesture',
  configuring: 'holding clipboard',
  error: 'apologetic pose',
  'phone-otp': 'holding smartphone',
  otp: 'holding verification code',
  'menu-cleo': 'holding menu',
  'delivery-choice': 'welcoming pose',
  cart: 'holding shopping bag',
  'pointing-down': 'pointing downward',
  profile: 'close-up portrait',
  heart: 'finger heart gesture',
  'walk-in': 'walking in carrying pizza',
  'walk-out': 'goodbye wave',
};

export default {
  name: 'azure_ai_generate_cleo_poses',
  description: 'Genera poses de Cleo preservando identidad desde una imagen de referencia y crea backups opcionales antes de sobrescribir.',
  inputSchema: {
    type: 'object',
    properties: {
      poses: { type: 'array', items: { type: 'string' }, description: 'Nombres de poses o ["all"].' },
      framing: { type: 'string', description: '"halfbody" (default) o "fullbody".' },
      outputDir: { type: 'string', description: 'Directorio de salida (default: src/VoiceBot.Web/src/assets/cleo).' },
      referenceImagePath: { type: 'string', description: 'Imagen de referencia (default según framing).' },
      customPosePrompt: { type: 'string', description: 'Prompt de una pose personalizada.' },
      customPoseName: { type: 'string', description: 'Nombre de la pose personalizada (default: "custom").' },
      quality: { type: 'string', description: 'Calidad (default: low).' },
      backup: { type: 'boolean', description: 'Si es false, no crea backup del PNG existente (default: true).' },
    },
    required: [],
    additionalProperties: false,
  },
  async handler(args) {
    const projectRoot = getProjectRoot();
    const framing = args.framing ?? 'halfbody';
    const outputDirRel = args.outputDir ?? 'src/VoiceBot.Web/src/assets/cleo';
    const outputDir = resolveWorkspacePath(projectRoot, outputDirRel);
    mkdirSync(outputDir, { recursive: true });

    const referenceRel =
      args.referenceImagePath ??
      (framing === 'fullbody' ? 'src/VoiceBot.Web/src/assets/cleo/cleo.png' : 'src/VoiceBot.Web/src/assets/cleo/cleo-avatar.png');
    const referenceResolved = resolveWorkspacePath(projectRoot, referenceRel, true);
    const referenceBytes = readFileSync(referenceResolved);

    const selected = {};
    if (Array.isArray(args.poses) && args.poses.includes('all')) {
      Object.assign(selected, POSES);
    } else if (Array.isArray(args.poses)) {
      for (const name of args.poses) {
        if (!(name in POSES)) throw new ToolError(`Pose desconocida: "${name}"`);
        selected[name] = POSES[name];
      }
    }
    if (args.customPosePrompt) selected[args.customPoseName ?? 'custom'] = args.customPosePrompt;
    if (Object.keys(selected).length === 0) throw new ToolError('Debe especificar al menos una pose');

    const quality = args.quality ?? 'low';
    const backup = args.backup !== false;
    const results = [];

    for (const [name, description] of Object.entries(selected)) {
      const filePath = path.join(outputDir, `${name}.png`);
      if (backup && existsSync(filePath)) {
        const backupDir = path.join(outputDir, '_backup');
        mkdirSync(backupDir, { recursive: true });
        copyFileSync(filePath, path.join(backupDir, path.basename(filePath)));
      }

      const result = await editImage(projectRoot, {
        imageBytes: referenceBytes,
        imageName: path.basename(referenceRel),
        prompt: `A cute cartoon pizza delivery girl mascot named Cleo, red uniform yellow trim, dark brown ponytail. ${description}. ${framing}`,
        model: null,
        size: '1024x1024',
        quality,
        inputFidelity: 'high',
        maskBytes: null,
      });

      writeFileSync(filePath, result.bytes);
      results.push({ pose: name, file: `${outputDirRel}/${name}.png`, sizeBytes: result.bytes.length, revisedPrompt: result.revised });
    }

    return {
      status: 'completed',
      framing,
      referenceUsed: referenceRel,
      outputDir: outputDirRel,
      backupCreated: backup,
      totalGenerated: results.length,
      generatedPoses: results,
    };
  },
  async smoke({ callTool, check, toolJson }) {
    const missing = toolJson(await callTool('azure_ai_generate_cleo_poses', { poses: ['no-existe'] }));
    check('azure_ai_generate_cleo_poses rechaza pose desconocida', typeof missing.error === 'string', missing.error);
  },
};
