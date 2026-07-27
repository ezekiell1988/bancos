import { spawn } from "node:child_process";

export async function runSmoke({ serverPath, projectRoot, serverName, requiresApply }) {
  const child = spawn(process.execPath, [serverPath, "--project-root", projectRoot], { stdio: ["pipe", "pipe", "pipe"] });
  let buffer = "";
  let stderr = "";
  let nextId = 1;
  let failures = 0;
  const pending = new Map();
  child.stdout.on("data", (chunk) => {
    buffer += chunk.toString();
    let end;
    while ((end = buffer.indexOf("\n")) !== -1) {
      const line = buffer.slice(0, end).trim();
      buffer = buffer.slice(end + 1);
      if (!line) continue;
      const message = JSON.parse(line);
      pending.get(message.id)?.(message);
      pending.delete(message.id);
    }
  });
  child.stderr.on("data", (chunk) => { stderr += chunk.toString(); });
  const check = (name, condition, detail = "") => {
    process.stdout.write(`${condition ? "OK  " : "FAIL"} ${name}${detail ? ` - ${detail}` : ""}\n`);
    if (!condition) failures += 1;
  };
  const rpc = (method, params = {}, timeoutMs = 20000) => {
    const id = nextId++;
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => { pending.delete(id); reject(new Error(`timeout ${method}: ${stderr}`)); }, timeoutMs);
      pending.set(id, (message) => { clearTimeout(timer); resolve(message); });
      child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", id, method, params })}\n`);
    });
  };

  try {
    const initialize = await rpc("initialize", { protocolVersion: "2025-06-18", capabilities: {}, clientInfo: { name: "smoke", version: "1" } });
    check("initialize responde", initialize.result?.serverInfo?.name === serverName);
    const future = await rpc("initialize", { protocolVersion: "9999-99-99", capabilities: {}, clientInfo: { name: "smoke", version: "1" } });
    check("versión futura no se refleja", future.result?.protocolVersion !== "9999-99-99");
    const listed = await rpc("tools/list");
    const tools = listed.result?.tools ?? [];
    const dbExec = tools.find((item) => item.name === "db_exec");
    check("tools/list expone las herramientas esperadas", tools.length === 1 && dbExec);
    check("db_exec publica schema estricto", dbExec?.inputSchema?.additionalProperties === false);
    if (requiresApply) {
      const response = await rpc("tools/call", { name: "db_exec", arguments: { blocks: [{ name: "preview", sql: "DELETE FROM dbo.X" }], reportName: "db-exec-preview" } });
      const payload = response.result?.structuredContent;
      check("producción exige apply", payload?.requiresApply === true && payload.applied === false);
      check("producción detecta operación", payload?.blocks?.[0]?.operations?.includes("DELETE") === true);
    }
    process.stdout.write(failures === 0 ? "\nSMOKE TEST: TODO OK\n" : `\nSMOKE TEST: ${failures} fallo(s)\n`);
    process.exitCode = failures === 0 ? 0 : 1;
  } catch (error) {
    process.stderr.write(`SMOKE TEST ERROR: ${error.stack ?? error.message}\n${stderr}`);
    process.exitCode = 1;
  } finally {
    child.kill();
  }
}