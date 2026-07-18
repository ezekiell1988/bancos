import path from "node:path";
import { createIaFileSystem } from "./fs.mjs";
import { createReadTools } from "./read-tools.mjs";
import { createSecretScanner } from "./secrets.mjs";
import { createWriteTools } from "./write-tools.mjs";

const parsed = parseArgs(process.argv.slice(2));
const projectRoot = path.resolve(parsed.projectRoot ?? process.env.IA_MCP_PROJECT_ROOT ?? process.cwd());
const iaRoot = path.resolve(parsed.iaRoot ?? process.env.IA_MCP_IA_ROOT ?? (path.basename(projectRoot) === "ia" ? projectRoot : path.join(projectRoot, "ia")));
const iaFs = createIaFileSystem(iaRoot);
const secretScanner = createSecretScanner({ iaRoot, listMarkdownFiles: iaFs.listMarkdownFiles, readText: iaFs.readText, safeIaPath: iaFs.safeIaPath });
const read = createReadTools({ iaRoot, iaFs, scanForPotentialSecrets: secretScanner.scanForPotentialSecrets });
const write = createWriteTools({ iaRoot, listMarkdownFiles: iaFs.listMarkdownFiles, normalizeRelativePath: iaFs.normalizeRelativePath, optionalText: iaFs.optionalText, readText: iaFs.readText, safeIaPath: iaFs.safeIaPath, validateIa: read.validateIa });

export const runtime = { projectRoot, iaRoot, iaFs, read, write };

function parseArgs(argv) {
  const result = {};
  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];
    if (arg === "--project-root") result.projectRoot = argv[++index];
    else if (arg.startsWith("--project-root=")) result.projectRoot = arg.slice(15);
    else if (arg === "--ia-root") result.iaRoot = argv[++index];
    else if (arg.startsWith("--ia-root=")) result.iaRoot = arg.slice(10);
  }
  return result;
}
