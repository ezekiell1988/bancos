#!/usr/bin/env node
// Adaptador delgado sobre el runtime compartido — ver .mcp/_shared/runtime.mjs.
// No se edita al agregar tools: el registry autodescubre tools/*.mjs.

import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { runServer } from '../_shared/runtime.mjs';
import { initProjectRoot } from './src/common.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));

initProjectRoot();

await runServer({ name: 'googleAi', version: '1.0.0', toolsDir: path.join(here, 'tools') });
