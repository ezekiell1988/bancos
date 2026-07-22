---
name: create-skill
description: >
  Guía completa para crear Agent Skills para GitHub Copilot y otros agentes compatibles
  (VS Code, Copilot CLI, Copilot cloud agent). Usar cuando se pida crear un skill,
  convertir conocimiento en skill reutilizable, mejorar un SKILL.md existente, depurar
  por qué un skill no se carga, o diseñar la estructura de archivos de un skill.
  Triggers: create skill, nuevo skill, SKILL.md, agent skill, /create-skill,
  skill no carga, skill no se activa, diseñar skill, empaquetar conocimiento como skill.
---

# Guía para crear Agent Skills

Los Agent Skills son la forma estándar de empaquetar conocimiento especializado, scripts y
ejemplos para que GitHub Copilot (y otros agentes compatibles) los carguen automáticamente
cuando son relevantes. Siguen el estándar abierto de [agentskills.io](https://agentskills.io).

Ver la referencia técnica completa en [references/spec.md](./references/spec.md).

---

## Flujo para crear un skill

### 1. Definir el skill

Antes de escribir código, responder:

- **¿Qué tarea especializada resuelve?** — Sé específico, no genérico.
- **¿Cuándo debe activarse automáticamente?** — Identifica los triggers (palabras clave, contexto).
- **¿Qué archivos auxiliares necesita?** — Scripts, ejemplos, referencias.

### 2. Crear la estructura de directorios

```
.agents/skills/
└── nombre-del-skill/          # nombre en minúsculas, solo letras, números y guiones
    ├── SKILL.md               # OBLIGATORIO — metadata + instrucciones
    ├── examples/              # Scripts/código de ejemplo (referenciados en SKILL.md)
    ├── references/            # Documentación técnica detallada
    └── assets/                # Plantillas, esquemas, datos estáticos
```

**Ubicaciones válidas en el repo:**
- `.github/skills/` — estándar GitHub
- `.agents/skills/` — este proyecto usa esta ubicación
- `~/.copilot/skills/` — skills personales del usuario

### 3. Escribir el SKILL.md

#### Frontmatter (obligatorio)

```yaml
---
name: nombre-del-skill          # debe coincidir EXACTAMENTE con el nombre del directorio
description: >
  Descripción clara de qué hace y cuándo usarlo. Incluir palabras clave
  para que el agente lo active automáticamente. Máximo 1024 caracteres.
---
```

**Reglas críticas del `name`:**
- Solo minúsculas, números y guiones (`a-z`, `0-9`, `-`)
- Sin mayúsculas, espacios, puntos, slashes ni prefijos (`org/skill` falla silenciosamente)
- Sin guiones al inicio/final ni consecutivos (`--`)
- Máximo 64 caracteres
- **Debe coincidir con el nombre del directorio padre** — si no coincide, el skill falla sin error visible

**Frontmatter opcional (VS Code específico):**
```yaml
argument-hint: "[archivo] [opciones]"    # texto de ayuda al invocar con /nombre-del-skill
user-invocable: false                    # ocultar del menú / pero activar automáticamente
disable-model-invocation: true           # solo manual, no automático
context: fork                            # ejecutar en subagente separado (experimental)
```

#### Cuerpo (Markdown libre)

Incluir:
- Qué resuelve el skill y cuándo usarlo
- Procedimiento paso a paso
- Ejemplos de input/output esperado
- Referencias a archivos auxiliares con **rutas relativas**

```markdown
Ver la referencia completa en [references/REFERENCE.md](./references/REFERENCE.md).
Ejecutar el script: [examples/run.ps1](./examples/run.ps1)
```

> **Regla clave**: los archivos auxiliares solo se cargan al contexto si están
> **referenciados en el SKILL.md** con rutas relativas. Sin referencia = no se cargan.

### 4. Tamaño y progresión de carga

El agente carga el skill en 3 niveles progresivos:

| Nivel | Qué se carga | Cuándo | Recomendación |
|-------|-------------|--------|---------------|
| 1 — Descubrimiento | `name` + `description` (~100 tokens) | Al inicio, siempre | Descripción precisa con keywords |
| 2 — Instrucciones | Body completo del `SKILL.md` | Al activarse el skill | Mantener < 500 líneas / < 5000 tokens |
| 3 — Recursos | Archivos referenciados (`examples/`, `references/`) | Solo cuando se necesitan | Archivos pequeños y enfocados |

### 5. Verificar que el skill carga

Síntomas de que un skill **no se carga**:
- `name` con caracteres inválidos (mayúsculas, slash, punto)
- `name` no coincide con el directorio padre
- Archivos auxiliares no referenciados en el SKILL.md
- El skill está fuera de las ubicaciones reconocidas

Para depurar: en VS Code, menú `...` del Chat → **Show Agent Debug Logs**.

---

## Ejemplo completo mínimo

```
mi-skill/
├── SKILL.md
└── examples/
    └── ejemplo.ps1
```

```yaml
---
name: mi-skill
description: >
  Realiza X tarea especializada. Usar cuando el usuario pida Y o Z.
  Triggers: keyword-a, keyword-b, hacer-X.
---

# Mi Skill

## Cuándo usarlo
Activar cuando el usuario mencione X, Y o Z.

## Procedimiento
1. Paso uno...
2. Paso dos...

## Ejemplo
Ver [examples/ejemplo.ps1](./examples/ejemplo.ps1) para una implementación completa.
```

---

## Portabilidad entre agentes

Los skills creados en VS Code funcionan también en:
- **GitHub Copilot CLI** — accesible desde terminal
- **GitHub Copilot cloud agent** — tareas automatizadas
- **Claude Code** y otros agentes compatibles con agentskills.io

El campo `compatibility` del frontmatter sirve para documentar restricciones de entorno.
