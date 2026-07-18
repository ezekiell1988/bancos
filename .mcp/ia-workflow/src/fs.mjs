import fs from "node:fs/promises";
import path from "node:path";
import { rpcError } from "./common.mjs";

export function createIaFileSystem(iaRoot) {
  function safeIaPath(relativePath) {
    const normalized = normalizeRelativePath(relativePath);
    const absolute = path.resolve(iaRoot, normalized);
    const relative = path.relative(iaRoot, absolute);

    if (relative.startsWith("..") || path.isAbsolute(relative)) {
      throw rpcError(-32602, `Ruta fuera de /ia no permitida: ${relativePath}`);
    }

    return absolute;
  }

  async function exists(absolutePath) {
    try {
      await fs.access(absolutePath);
      return true;
    } catch {
      return false;
    }
  }

  async function readText(absolutePath) {
    return fs.readFile(absolutePath, "utf8");
  }

  async function optionalText(relativePath) {
    const absolute = safeIaPath(relativePath);
    return (await exists(absolute)) ? readText(absolute) : "";
  }

  async function listMarkdownFiles(absoluteRoot, relativeRoot) {
    if (!(await exists(absoluteRoot))) {
      return [];
    }

    const stat = await fs.stat(absoluteRoot);
    if (!stat.isDirectory()) {
      return relativeRoot.endsWith(".md") ? [relativeRoot] : [];
    }

    const entries = await fs.readdir(absoluteRoot, { withFileTypes: true });
    const files = [];

    for (const entry of entries) {
      if (entry.name.startsWith(".")) {
        continue;
      }

      const absolute = path.join(absoluteRoot, entry.name);
      const relative = relativeRoot ? `${relativeRoot}/${entry.name}` : entry.name;

      if (entry.isDirectory()) {
        files.push(...(await listMarkdownFiles(absolute, relative)));
      } else if (entry.isFile() && entry.name.endsWith(".md")) {
        files.push(relative);
      }
    }

    return files;
  }

  return {
    exists,
    fromIaUri,
    listMarkdownFiles,
    normalizeRelativePath,
    optionalText,
    readText,
    safeIaPath,
    toIaUri,
  };
}

export function normalizeRelativePath(relativePath) {
  const clean = String(relativePath)
    .replace(/^ia[\\/]/, "")
    .replace(/^\/+/, "");

  if (!clean || clean.includes("\0")) {
    throw rpcError(-32602, `Ruta invalida: ${relativePath}`);
  }

  return path.normalize(clean).replaceAll("\\", "/");
}

export function toIaUri(relativePath) {
  return `ia:///${encodeURI(normalizeRelativePath(relativePath))}`;
}

export function fromIaUri(uri) {
  if (!uri.startsWith("ia:///")) {
    throw rpcError(-32602, `URI no soportada: ${uri}`);
  }

  return decodeURI(uri.slice("ia:///".length));
}
