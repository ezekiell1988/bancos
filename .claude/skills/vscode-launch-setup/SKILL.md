---
name: vscode-launch-setup
description: >
  Crea o actualiza la configuración de debug de VS Code (.vscode/launch.json) para proyectos
  con backend .NET y frontend Angular. Genera dos launches con ícono de bug: uno para la API
  .NET en un puerto configurable y otro para ng serve con proxy hacia la API. También crea
  proxy.conf.json en el proyecto Angular y silencia el terminal de build en tasks.json.
  Triggers: launch.json, vscode launch, debug config, ng serve proxy, .net debug vscode,
  configurar debug, lanzar api angular, launch vscode, proxy angular dotnet.
---

# VS Code Launch Setup — .NET API + Angular

Configura el entorno de debug local de VS Code para proyectos con backend .NET y frontend
Angular, con las siguientes metas:

- Solo 2 terminales visibles, ambas con ícono de bug (debugger)
- `/api` en el puerto de Angular redirigido automáticamente al .NET
- Build de .NET silencioso (solo aparece si hay error)
- Compound para lanzar ambos procesos con un solo F5

---

## Información que necesitás antes de crear

| Dato | Cómo obtenerlo |
|------|---------------|
| Ruta del `.csproj` | `find src -name "*.csproj"` |
| Target framework | `grep TargetFramework` en el `.csproj` (ej. `net10.0`) |
| Puerto del API | El que usa el proyecto (ej. `8000`) |
| Ruta del `angular.json` | `find src -name "angular.json"` |
| Nombre del proyecto Angular | `cat angular.json \| python3 -c "import json,sys; d=json.load(sys.stdin); print(list(d['projects'].keys())[0])"` |

---

## Archivos a crear / modificar

### 1. `.vscode/launch.json`

Ver plantilla en [examples/launch.json](./examples/launch.json).

Puntos clave:
- `"console": "integratedTerminal"` en ambas configs para que cada una abra su propia pestaña
- `preLaunchTask` del .NET apunta a `"build-dotnet"` (no al task `"build"` general)
- El Angular usa `type: "node"` con `runtimeExecutable: "npx"` y `runtimeArgs: ["ng", "serve", ...]`
- `--proxy-config proxy.conf.json` en los args del ng serve
- El compound `"Full Stack"` usa `"stopAll": true`

### 2. `src/<AngularProject>/proxy.conf.json` (nuevo)

Ver plantilla en [examples/proxy.conf.json](./examples/proxy.conf.json).

Redirige `/api` hacia `https://localhost:<PORT_API>` con `"secure": false` para aceptar
certificados self-signed del .NET dev cert.

### 3. `.vscode/tasks.json` — modificar `build-dotnet`

Agregar `presentation` al task `build-dotnet` para que no abra terminal al compilar:

```json
"presentation": {
  "reveal": "silent",
  "panel": "shared",
  "close": true
}
```

`"reveal": "silent"` — solo muestra la terminal si el build falla.  
`"close": true` — cierra la pestaña automáticamente al terminar con éxito.

Cambiar también el `preLaunchTask` del launch de .NET de `"build"` a `"build-dotnet"` para
evitar que compile Angular innecesariamente antes de levantar el API.

---

## Resultado esperado

Al presionar F5 con el compound **"Full Stack"**:

1. Se compila el .NET en silencio (sin abrir terminal extra)
2. Se abre terminal **API (.NET)** con el debugger adjunto
3. Se abre terminal **Angular** con `ng serve` y el proxy activo
4. `http://localhost:4200/api/*` → `https://localhost:<PORT_API>/api/*`
