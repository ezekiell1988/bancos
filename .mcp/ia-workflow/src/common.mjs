// Adaptador delgado de configuración de dominio: resuelve --project-root/--ia-root/
// --runtime studio (igual que WorkflowPaths.FromArgs) y expone el workflow ya inicializado.
import { workflowPathsFromArgs } from './paths.mjs';
import { createWorkflow } from './workflow.mjs';

let workflow = null;
let iaRoot = null;

/** Resuelve paths desde argv y crea el workflow. Llamado una vez al arrancar el server. */
export function initWorkflow(argv = process.argv.slice(2)) {
  const paths = workflowPathsFromArgs(argv);
  iaRoot = paths.iaRoot;
  workflow = createWorkflow(iaRoot);
  return workflow;
}

/** Workflow ya inicializado por initWorkflow(). Usado por cada tools/{name}.mjs. */
export function getWorkflow() {
  if (!workflow) throw new Error('initWorkflow() no se ha llamado todavía');
  return workflow;
}

export function getIaRoot() {
  if (!iaRoot) throw new Error('initWorkflow() no se ha llamado todavía');
  return iaRoot;
}

export { ToolError } from '../../_shared/results.mjs';
