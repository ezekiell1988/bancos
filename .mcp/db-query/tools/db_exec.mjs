import { createDbExecDefinition, executeDbExec } from "../src/db-exec.mjs";
import { profile } from "../src/profile.mjs";

export const definition = createDbExecDefinition(profile);
export const execute = executeDbExec;
