---
name: clickeat-avances
description: >
  Genera el reporte HTML de avance diario del proyecto ClickEat Voice Bot para presentar
  a gerencia no técnica (Yafit Ohaya Beladel). Usar cuando se pida crear un reporte de avance,
  documentar lo que se hizo hoy, resumir el progreso del proyecto, o generar el archivo
  docs/avances/YYYYMMDD_HHMM.html. Incluye template HTML con diseño de marca ClickEat y script
  PS1 para extraer contexto automáticamente desde ia/05_progress/current.md e ia/04_tasks/current.md.
  El agente SIEMPRE lee el reporte anterior antes de generar uno nuevo para evitar duplicar
  items: lo ya reportado va en «estado anterior», no en «lo realizado hoy».
  Triggers: reporte de avance, avance del día, informe para Yafit, docs/avances, generar avance,
  qué se hizo hoy, progress report, estado del proyecto, resumen ejecutivo, avance gerencia,
  crear html avance, 20260602.html, informe no técnico.
---

# Skill: clickeat-avances

Genera el reporte HTML diario de avance del proyecto ClickEat Voice Bot.
El reporte está dirigido a **Yafit Ohaya Beladel** (gerencia, perfil no técnico).
El archivo de salida se guarda en `docs/avances/YYYYMMDD_HHMM.html`
(fecha + hora de inicio de la sesión, ej: `20260602_1430.html`).

> **Regla anti-duplicados:** Antes de generar un reporte nuevo, el agente DEBE leer el
> último archivo HTML en `docs/avances/` para identificar qué ya fue reportado.
> Lo que ya aparece en ese reporte va en la sección **«¿Cómo estaba el proyecto hasta la última sesión reportada?»**,
> **nunca** en **«¿Qué se realizó a partir de esta jornada?»**.

---

## Cuándo usar este skill

- Usuario pide "crea el avance de hoy"
- Usuario pide "genera el reporte para Yafit"
- Usuario pide "documenta lo que se hizo esta sesión"
- Fin de una sesión de trabajo importante con cambios significativos

---

## Archivos del skill

- Template HTML: [assets/template.html](./assets/template.html)
- Script de extracción: [examples/generar-avance.ps1](./examples/generar-avance.ps1)

---

## Procedimiento paso a paso

### 0. Leer el reporte anterior (OBLIGATORIO — anti-duplicados)

Antes de leer ningún otro archivo, ejecutar:

```
docs/avances/        ← listar archivos, ordenar por nombre desc, tomar el primero
```

Leer ese HTML y extraer **todos los ítems que ya fueron reportados** (secciones
«¿Qué se realizó?» y «¿Qué se trabajó internamente?»).  
Guardar la lista como **«ya reportado»**.

**Regla de clasificación:**

| Si el ítem… | Va en… |
|-------------|--------|
| **No aparece** en el reporte anterior | **«¿Qué se realizó a partir de esta jornada?»** (novedad) |
| **Ya aparece** en el reporte anterior | **«¿Cómo estaba el proyecto hasta la última sesión reportada?»** (estado previo) |

Si no existe ningún reporte anterior, omitir este paso.

### 1. Recopilar contexto

Leer en paralelo los siguientes archivos del proyecto:

| Archivo | Qué extraer |
|---------|------------|
| `ia/05_progress/current.md` | Estado actual ejecutivo: qué se hizo recientemente |
| `ia/05_progress/archive/2026-06.md` | Sesiones del día: detalle completo, problemas resueltos, archivos modificados |
| `ia/04_tasks/current.md` | Tareas en progreso (`🔄`) y pendientes (`⏳`) |
| `ia/04_tasks/done/2026-06.md` | Tareas completadas este mes (`✅`) |
| `ia/03_plan.md` | Fases completadas vs pendientes (para las barras de progreso) |
| `ia/07_issues/open.md` | Issues abiertos (índice) — para sección «qué falta» |
| `ia/07_issues/archive/2026-06.md` | Bugs resueltos este mes — para sección «lo que se trabajó internamente» |

Alternativamente, ejecutar el script PS1 para extraer el contexto automáticamente:
```powershell
pwsh .agents/skills/clickeat-avances/examples/generar-avance.ps1
```

### 2. Identificar las 4 secciones clave del reporte

| Sección | Fuente | Descripción para gerencia |
|---------|--------|--------------------------|
| **Estado anterior (última sesión reportada)** | `ia/05_progress/current.md` + `ia/03_plan.md` | Lo que estaba listo y lo que estaba pendiente *antes* de esta jornada |
| **Lo nuevo incluido hoy** | `ia/05_progress/archive/2026-06.md` (sesiones del día) + `ia/04_tasks/done/2026-06.md` | Funcionalidades nuevas o correcciones entregadas durante la sesión |
| **Lo que se trabajó internamente** | `ia/07_issues/archive/2026-06.md` — bugs resueltos, cambios de código sin feature visible | Mejoras técnicas que el usuario no ve directamente pero que hacen el sistema más robusto |
| **Qué falta / próximos pasos** | `ia/04_tasks/current.md` (tareas `⏳` y `🔄`) + `ia/07_issues/open.md` | Lo que queda por construir antes de poder salir a producción |

### 3. Determinar el % de avance por área

Calcular visualmente (no tiene que ser exacto, debe ser honesto):

| Área | Cómo calcularlo |
|------|----------------|
| Voice Bot | Si la fase 1–8 en `03_plan.md` están ✅ → 100% |
| Cleo Chat — Consultas | Si todos los CHAT-STEP consulta están ✅ → 100% |
| Cleo Chat — Compra (backend) | Contar CHAT-STEP compra ✅ vs total |
| Cleo Chat — Pago (backend) | CHAT-PAY-01..06 completados → 100% backend |
| Cleo Chat — Pantallas de pago (UI) | CHAT-PAY-FE-01..03 completados → 100% |
| Deploy / Producción | TASK-OPS-02 completado → 100% |

### 4. Adaptar el lenguaje

**Reglas de comunicación para el reporte:**

- ❌ No usar: "backend", "endpoint", "SP", "EF Core", "Redis", "TypeScript", "build"
- ✅ Usar: "la plataforma", "el sistema", "Cleo", "el chat", "el proceso de pago", "lo que el cliente ve"
- ❌ No decir: "CS0103", "null reference", "state machine", "DI", "TCS"
- ✅ Decir: "se encontró y corrigió un error que...", "el sistema no avanzaba al siguiente paso porque..."
- Los bugs son "errores corregidos", no "fixes"
- Las tareas técnicas de infraestructura se explican por su impacto en el usuario final

**Ejemplos de traducción técnico → gerencial:**

| Técnico | Gerencial |
|---------|-----------|
| `ctx.Email` era null → Onvopay devolvía 400 | El correo del cliente no se enviaba correctamente a la plataforma de pagos, lo que impedía generar el link |
| `output<void>()` → siempre index 0 | Al tocar cualquier promo que no fuera la primera, el sistema seleccionaba la primera por error |
| `ServiceExtensions.cs` — 3 estados sin registrar | El sistema no sabía qué hacer después de que el cliente pagaba y mostraba un mensaje de error genérico |
| Túnel Cloudflare para webhook local | Para probar el pago de extremo a extremo, se necesita una conexión temporal que permita a la plataforma de pagos comunicarse con el servidor de desarrollo |

### 5. Generar el HTML

Usar el template de [assets/template.html](./assets/template.html).

Reemplazar todas las variables `{{VARIABLE}}` con el contenido real:

| Variable | Valor |
|----------|-------|
| `{{FECHA_LARGA}}` | Ej: `2 de junio de 2026` |
| `{{FECHA_YYYYMMDD}}` | Ej: `20260602` |
| `{{HORA_INICIO}}` | Ej: `6:00 pm` (hora real de inicio de la sesión de trabajo; en este proyecto suele iniciar a las 6 pm) |
| `{{FECHA_ULTIMA_SESION}}` | Ej: `2 de junio de 2026` |
| `{{ESTADO_AYER_ITEMS}}` | HTML: lista de `<li>` con el estado anterior |
| `{{LO_NUEVO_HOY_ITEMS}}` | HTML: lista de `<li>` de funcionalidades entregadas hoy |
| `{{BUGS_RESUELTOS}}` | HTML: secciones `<h3>` + `<p>` por bug, o vacío si no hubo |
| `{{PROGRESO_BARS}}` | HTML: bloques `.progress-wrap` con % real |
| `{{PENDIENTE_ITEMS}}` | HTML: lista de `<li>` con lo que falta |
| `{{RESUMEN_EJECUTIVO}}` | Párrafo corto de 2–3 oraciones: dónde está el proyecto hoy |

### 6. Guardar y confirmar

Nombrar el archivo con **fecha + hora de inicio** de la sesión:
```
docs/avances/YYYYMMDD_HHMM.html
# ej: docs/avances/20260602_1430.html
```

Si no se conoce la hora exacta de inicio, usar la hora actual al generar el reporte.

Informar al usuario la ruta del archivo generado.

---

## Estructura del reporte HTML (secciones)

1. **Encabezado** — proyecto, fecha, de/para
2. **¿De qué trata el proyecto?** — breve contexto (1 vez, no cambiar sesión a sesión)
3. **¿Cómo estaba el proyecto hasta la última sesión reportada?** — estado anterior
4. **¿Qué se realizó a partir de las 6 pm?** — dividido por mejora/corrección, con ejemplos visuales si aplica
5. **Progreso general** — barras por área funcional
6. **¿Qué falta por completar?** — próximos pasos claros
7. **Footer** — fecha + equipo

---

## Referencia de colores de marca ClickEat

| Uso | Valor |
|-----|-------|
| Rojo primario | `#D32F2F` |
| Rojo hover | `#b71c1c` |
| Rosa claro (fondo) | `#fee2e2` |
| Amarillo advertencia | `#fef9c3` / `#facc15` |
| Verde completado | `#d1fae5` / `#065f46` |
| Azul pendiente | `#e0e7ff` / `#3730a3` |
| Texto principal | `#1a1a2e` |
| Texto secundario | `#444` |
