export const SUPPORTED_PROTOCOL_VERSIONS = ["2025-06-18"];
export const PROTOCOL_VERSION = SUPPORTED_PROTOCOL_VERSIONS[0];
export const SERVER_VERSION = "0.4.0";

export const requiredFiles = [
  "README.md",
  "SCHEMAS.md",
  "00_context.md",
  "01_requirements.md",
  "02_architecture.md",
  "03_plan.md",
  "04_tasks.md",
  "04_tasks/current.md",
  "04_tasks/backlog.md",
  "04_tasks/blocked.md",
  "05_progress.md",
  "05_progress/current.md",
  "06_decisions.md",
  "07_issues.md",
  "07_issues/current.md",
  "08_retrospective.md",
];

export const requiredDirs = [
  "04_tasks",
  "04_tasks/tasks",
  "04_tasks/done",
  "05_progress",
  "05_progress/by-component",
  "06_decisions",
  "07_issues",
  "07_issues/open",
  "templates",
  "prompts",
];

export const intentFiles = {
  planificar: [
    "00_context.md",
    "04_tasks/current.md",
    "04_tasks/blocked.md",
    "05_progress/current.md",
    "07_issues/current.md",
  ],
  implementar: [
    "00_context.md",
    "02_architecture.md",
    "06_decisions.md",
    "07_issues/current.md",
  ],
  revisar: [
    "00_context.md",
    "02_architecture.md",
    "06_decisions.md",
    "07_issues/current.md",
  ],
  depurar: [
    "00_context.md",
    "02_architecture.md",
    "07_issues/current.md",
  ],
  cerrar_sesion: [
    "README.md",
    "04_tasks/current.md",
    "05_progress/current.md",
    "07_issues/current.md",
    "06_decisions.md",
  ],
};
