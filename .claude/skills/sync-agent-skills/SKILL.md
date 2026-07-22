---
name: sync-agent-skills
description: >
  Revisar y sincronizar las carpetas de skills entre .agents, .claude y .codex en este
  repo. Base canonica: .agents/skills. Detecta diferencias por hash de contenido, marca
  la version mas reciente por fecha de modificacion y pide aprobacion cuando la mas
  reciente NO esta en la base. Incluye scripts PS1 listos (dry-run, interactivo, modo
  CI). Usar cuando se pida sincronizar skills, copiar skills a claude/codex, detectar
  skills desactualizados o resolver conflictos entre copias. Triggers: sincronizar skills,
  sync skills, copiar skills, skills desactualizados, .claude skills, .codex skills,
  .agents skills, espejo de skills, conflicto de skills, propagar skill, actualizar skills.
---

# Sincronizar skills entre .agents, .claude y .codex

En este repo los skills viven en tres carpetas espejo:

| Carpeta | Consumidor |
|---------|-----------|
| `.agents/skills/` | **Base canónica** — GitHub Copilot (VS Code) |
| `.claude/skills/` | Claude Code |
| `.codex/skills/` | Codex |

Regla: `.agents` es la fuente de verdad. Si una copia en `.claude` o `.codex` tiene
modificación más reciente, se pide aprobación antes de adoptarla como fuente.

## Script principal

[examples/sync-skills.ps1](./examples/sync-skills.ps1) — comparación por hash MD5 de
contenido + espejo con `robocopy /MIR` por skill.

```powershell
# Solo reporte, no modifica nada (dry-run)
.\scripts\sync-skills.ps1

# Interactivo: aplica cambios y pide aprobación en conflictos
.\scripts\sync-skills.ps1 -Apply

# Sin preguntas: siempre gana .agents (útil para CI o post-edición masiva)
.\scripts\sync-skills.ps1 -Apply -AutoBase
```

El script canónico está en `scripts/sync-skills.ps1`; la copia en `examples/` es
referencia si el original se pierde.

### Lógica de decisión por skill

1. **Idéntico en las tres** → no hace nada.
2. **Falta en alguna carpeta** (mismo hash donde existe) → copia desde `.agents`.
3. **Contenido difiere**:
   - Más reciente en `.agents` → sincroniza desde `.agents` sin preguntar.
   - Más reciente en `.claude`/`.codex` → pregunta: `s` = usar esa versión
     (actualiza `.agents` también), `N` = usar `.agents`, `o` = omitir.
4. **Solo existe fuera de `.agents`** → pregunta si adoptarlo en todas o ignorar.

### Salida

Reporte por skill con fecha de última modificación, cantidad de archivos y hash corto,
más resumen final (idénticos / sincronizados / conflictos / omitidos).

## Script simple (copia unidireccional)

Dos scripts para propagar rápido desde `.agents` sin comparación ni preguntas:

**[examples/copy-agent-skills-to-claude.ps1](./examples/copy-agent-skills-to-claude.ps1)**
espejo `.agents/skills` → `.claude/skills`:

```powershell
.\scripts\copy-agent-skills-to-claude.ps1            # espejo (borra extras en destino)
.\scripts\copy-agent-skills-to-claude.ps1 -NoMirror  # copia sin borrar extras
# Destino personalizado (parámetro $Destination):
.\scripts\copy-agent-skills-to-claude.ps1 -Destination ".claude/skills"
```

**[examples/copy-agent-skills-to-codex.ps1](./examples/copy-agent-skills-to-codex.ps1)**
espejo `.agents/skills` → `.codex/skills`:

```powershell
.\scripts\copy-agent-skills-to-codex.ps1            # espejo (borra extras en destino)
.\scripts\copy-agent-skills-to-codex.ps1 -NoMirror  # copia sin borrar extras
# Destino personalizado (parámetro $Destination):
.\scripts\copy-agent-skills-to-codex.ps1 -Destination ".codex/skills"
```

Ambos scripts aceptan `-Source` y `-Destination` para casos fuera de lo estándar.
Para propagar a las **dos** carpetas a la vez, preferir `sync-skills.ps1 -Apply -AutoBase`.

## Precauciones

- `robocopy /MIR` **borra** en destino lo que no existe en la fuente: revisar el
  dry-run antes de `-Apply` si hubo trabajo en progreso en `.claude` o `.codex`.
- Después de crear o editar un skill en `.agents`, correr el sync para que Claude
  Code y Codex vean la misma versión.
- Exit codes de robocopy `>= 8` son error; `0-7` son éxito (el script ya lo maneja).
