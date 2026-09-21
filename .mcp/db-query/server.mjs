#!/usr/bin/env node
// Adaptador delgado sobre el runtime compartido — ver .mcp/_shared/runtime.mjs.
// No se edita al agregar tools: el registry autodescubre tools/*.mjs.
// El mismo binario se registra dos veces (dbQuery/dbQueryClickeat) vía --secrets-file/--server-name.

import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { runServer } from '../_shared/runtime.mjs';
import { getServerName, initConfig } from './src/common.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));

initConfig();

await runServer({ name: getServerName(), version: '1.0.0', toolsDir: path.join(here, 'tools') });
