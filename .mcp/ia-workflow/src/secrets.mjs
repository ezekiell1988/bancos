export function createSecretScanner({ iaRoot, listMarkdownFiles, readText, safeIaPath }) {
  async function scanForPotentialSecrets() {
    const files = await listMarkdownFiles(iaRoot, "");
    const findings = [];

    for (const file of files) {
      const text = await readText(safeIaPath(file));
      findings.push(...scanTextForSecrets(text, file));
    }

    return findings.slice(0, 20);
  }

  return { scanForPotentialSecrets };
}

export function scanTextForSecrets(text, filePath) {
  const patterns = [
    ["api key", /\b(api[_-]?key|apikey)\b\s*[:=]\s*['"]?[A-Za-z0-9_\-]{16,}/i],
    ["connection string", /\b(Server|Data Source|Initial Catalog|User ID|Password)\s*=/i],
    ["private key", /-----BEGIN [A-Z ]*PRIVATE KEY-----/],
    ["bearer token", /\bBearer\s+[A-Za-z0-9._\-]{20,}/i],
    ["secret", /\b(secret|password|token)\b\s*[:=]\s*['"]?[A-Za-z0-9_\-]{16,}/i],
  ];
  const findings = [];
  const lines = text.split(/\r?\n/);

  lines.forEach((line, index) => {
    for (const [name, pattern] of patterns) {
      if (pattern.test(line)) {
        findings.push({ path: filePath, line: index + 1, pattern: name });
      }
    }
  });

  return findings;
}
