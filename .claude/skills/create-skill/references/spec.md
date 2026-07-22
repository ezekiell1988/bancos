# Especificación completa de Agent Skills

Fuente: [agentskills.io/specification](https://agentskills.io/specification) y [code.visualstudio.com/docs/copilot/customization/agent-skills](https://code.visualstudio.com/docs/copilot/customization/agent-skills)

---

## Estructura de directorios

```
skill-name/
├── SKILL.md          # Obligatorio: metadata + instrucciones
├── scripts/          # Código ejecutable
├── references/       # Documentación técnica adicional
├── assets/           # Plantillas, datos estáticos
└── examples/         # Ejemplos de código (convención de este repo)
```

---

## Frontmatter — todos los campos

### Estándar agentskills.io

| Campo | Req. | Límite | Descripción |
|-------|------|--------|-------------|
| `name` | Sí | 64 chars | Identificador único. Solo `a-z`, `0-9`, `-`. Sin mayúsculas, espacios, slashes. Debe coincidir con el directorio. |
| `description` | Sí | 1024 chars | Qué hace el skill y cuándo usarlo. Incluir keywords. |
| `license` | No | — | Nombre de licencia o referencia al archivo. |
| `compatibility` | No | 500 chars | Requisitos de entorno (herramientas, sistema, red). |
| `metadata` | No | — | Map de clave-valor para metadata adicional. |
| `allowed-tools` | No | — | Herramientas pre-aprobadas (experimental). Ej: `Bash(git:*) Read` |

### Exclusivos de VS Code / GitHub Copilot

| Campo | Default | Descripción |
|-------|---------|-------------|
| `argument-hint` | — | Texto de ayuda en el input al invocar con `/nombre`. Ej: `[archivo] [opciones]` |
| `user-invocable` | `true` | Si `false`: no aparece en el menú `/` pero el agente lo carga automáticamente. |
| `disable-model-invocation` | `false` | Si `true`: solo se activa manualmente con `/nombre`, nunca automático. |
| `context` | `inline` | `fork`: ejecuta el skill en un subagente separado. Solo su resultado final vuelve al agente principal. |

---

## Combinaciones de `user-invocable` y `disable-model-invocation`

| user-invocable | disable-model-invocation | Menú `/` | Automático | Caso de uso |
|----------------|--------------------------|----------|------------|-------------|
| omitido | omitido | Sí | Sí | Skills de propósito general |
| `false` | omitido | No | Sí | Conocimiento de fondo, carga transparente |
| omitido | `true` | Sí | No | Solo bajo demanda explícita |
| `false` | `true` | No | No | Skill deshabilitado |

---

## `context: fork` — cuándo usarlo

Usar `context: fork` cuando el skill:
- Lee muchos archivos o hace investigaciones largas que no necesitan quedar en el contexto principal
- Produce un resultado enfocado (resumen, reporte, conjunto pequeño de edits)
- No debe influir en el comportamiento del agente principal más allá de su resultado final

Habilitar en VS Code: `github.copilot.chat.skillTool.enabled = true`

---

## Carga progresiva — límites recomendados

| Nivel | Contenido | Límite recomendado |
|-------|-----------|-------------------|
| 1 | `name` + `description` | ~100 tokens, siempre en memoria |
| 2 | Body del `SKILL.md` | < 500 líneas / < 5000 tokens |
| 3 | Archivos referenciados | Archivos pequeños y enfocados; un tema por archivo |

---

## Reglas de referencias a archivos

- Usar rutas **relativas** desde la raíz del skill: `[script](./examples/run.ps1)`
- Un archivo **no se carga** si no está referenciado en el SKILL.md
- Evitar cadenas de referencias profundas (`A → B → C → D`)
- Mantener referencias a un nivel de profundidad desde el SKILL.md

---

## Errores comunes y soluciones

| Síntoma | Causa probable | Solución |
|---------|---------------|----------|
| Skill no aparece en `/` | `name` inválido o no coincide con directorio | Verificar charset y coincidencia exacta |
| Skill no se activa automáticamente | `description` sin keywords relevantes | Agregar más triggers específicos |
| Archivos auxiliares no disponibles | No están referenciados en SKILL.md | Agregar enlaces Markdown con rutas relativas |
| Skill carga pero no usa los ejemplos | Archivos fuera del directorio del skill | Mover archivos dentro del directorio del skill |
| Error silencioso al cargar | `name` con prefijo (`org/skill`) | Eliminar el prefijo del campo `name` |

---

## Ubicaciones de skills reconocidas

**Por proyecto (en el repo):**
- `.github/skills/` — estándar GitHub
- `.claude/skills/` — estándar Claude
- `.agents/skills/` — genérico (usado en este proyecto)

**Personales (perfil de usuario):**
- `~/.copilot/skills/`
- `~/.claude/skills/`
- `~/.agents/skills/`

**Adicionales:** configurar con `chat.agentSkillsLocations` en VS Code settings.

---

## Plantilla SKILL.md completa

```markdown
---
name: nombre-del-skill
description: >
  Una oración que describe la tarea principal. Una segunda oración que explica
  cuándo activarlo. Triggers: keyword-1, keyword-2, keyword-3.
# Campos opcionales:
# argument-hint: "[parámetro] [opciones]"
# user-invocable: false
# disable-model-invocation: true
# context: fork
# compatibility: Requiere PowerShell 7+ y acceso a internet
# metadata:
#   author: itqs
#   version: "1.0"
---

# Nombre del Skill

Descripción breve de para qué sirve.

---

## Cuándo usarlo

- Caso de uso A
- Caso de uso B

## Procedimiento

1. Paso uno
2. Paso dos
3. Paso tres

## Ejemplos

Ver [examples/ejemplo.ps1](./examples/ejemplo.ps1) para implementación completa.

## Notas y advertencias

- Nota importante 1
- Nota importante 2
```
