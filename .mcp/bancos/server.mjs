#!/usr/bin/env node
// MCP del registro de movimientos (datos/ del repo). Adaptador delgado sobre _shared/runtime.mjs:
// los tools se autodescubren en tools/. Flag: --datos-dir (default <repo>/datos).
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { runServer } from '../_shared/runtime.mjs';
import { initDatos } from './src/datos.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));

initDatos();

await runServer({ name: 'bancos', version: '2.0.0', toolsDir: path.join(here, 'tools') });
