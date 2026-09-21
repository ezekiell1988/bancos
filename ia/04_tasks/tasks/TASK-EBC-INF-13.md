# TASK-EBC-INF-13 — Video regalo: muñeca 3D tierna (imagen Azure AI Foundry + Veo 3.1)

**Estado:** En progreso
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Rama:** `main`
**Fecha inicio:** 2026-09-20 CR
**Fecha actualización:** 2026-09-20 CR
**Fecha cierre:** —
**Área:** INF
**Prioridad:** media
**Riesgo:** Medio
**Aprobación:** aprobada

---

## Contexto

Proyecto personal (no financiero) para crear un video corto de regalo para ella. Se convierte su rostro y físico en una "3D doll" tierna (estilo figura/vinyl toy) y se anima una escena corta. Usa la imagen de una persona real: requiere su foto de referencia, y el video es solo de uso privado como regalo.

Herramientas:
- **Imágenes:** Azure AI Foundry con `gpt-image-2.5-flare` y `gpt-image-2.5-sunburst` (a comparar). MCP a traer desde otro proyecto.
- **Video:** Google Veo 3.1 (imagen a video, primer/último frame, imágenes de referencia). MCP de Google a traer aparte.

## Objetivo

Video de 15–30 s: la muñeca se toca la cabeza, se enoja (incluso gira dándole la espalda por el enojo), luego se le muestra un cono de helado de máquina en espiral (soft serve), voltea a verlo y se pone toda alegre.

## Guion (storyboard)

| Plano | Acción | Duración aprox. |
|---|---|---|
| 1 | Muñeca de pie, idle tierno, parpadeo suave. | 3 s |
| 2 | Se toca la cabeza (como si le hubieran dado un golpecito) con gesto de sorpresa. | 3 s |
| 3 | Se enoja: cejas fruncidas, mejillas infladas, brazos cruzados o puños; gira dándole la espalda. | 4–5 s |
| 4 | Entra en cuadro un cono de helado de máquina en espiral (mano/cámara subjetiva). | 3 s |
| 5 | Voltea lentamente, ve el helado, ojos brillantes, sonrisa enorme, saltito de alegría. | 4–5 s |
| 6 | Cierre: plano medio feliz + mensaje/dedicatoria opcional. | 2–3 s |

## Fases

### Fase 0 — Preparación y consentimiento
- Reunir 3–6 fotos de referencia de ella (frente, 3/4, perfil, cuerpo completo, buena luz).
- Confirmar que el video es solo regalo privado; no publicar sin su permiso.
- Guardar las fotos fuera del repo (ignoradas por git); no versionar imágenes ni claves.
- Definir estilo: proporción chibi/vinyl toy, ojos grandes, piel suave, ropa y peinado reales, fondo pastel simple.

### Fase 1 — Infraestructura MCP
- Traer el MCP de Azure AI Foundry desde el otro proyecto y registrarlo en la configuración de Claude Code.
- Verificar los deployments `gpt-image-2.5-flare` y `gpt-image-2.5-sunburst` (endpoint, versión de API, edición de imágenes con referencia).
- Traer y registrar el MCP de Google para Veo 3.1; validar cuota, modelo, duración y resolución máximas.
- Credenciales solo en archivos locales ignorados por git; nunca en el repo ni en la salida.
- Definir carpeta de trabajo y nomenclatura de assets (`fotos/`, `personaje/`, `keyframes/`, `clips/`, `final/`), fuera del control de versiones.

### Fase 1b — Unificación de MCP Node con runtime compartido
Alcance agregado al aprobar (2026-09-21): alinear los MCP de Bancos con el proyecto clickeat/voice-bot.
- Copiar `.mcp/_shared/` (runtime, registry, protocolo, resultados) y usarlo desde todos los MCP Node.
- Reemplazar `db-query` e `ia-workflow` de Bancos por las versiones más recientes de clickeat (1 tool = 1 archivo en `tools/`, autodescubiertas).
- Un solo `.mcp/package.json` y un solo `.mcp/node_modules/` (sin `package.json` ni `node_modules` por carpeta).
- `ia-workflow` en modo `--runtime studio` con `.local-secrets/ia-workflow.json` (autor, iniciales, email, plataforma).
- `dbQuery` con `--secrets-file db.json`; conservar `db_reset_schemas` (exclusivo de Bancos) portada al nuevo formato de tools.
- Registrar `azureAi` y `googleAi` en `.mcp.json`; secretos de Google en `.local-secrets/`.
- Validar con los `tests/smoke.mjs` de cada MCP (db-query requiere Docker/BD arriba para su check `SELECT 1`).
- Riesgo: se reemplazan MCP existentes; el código anterior sigue recuperable en git (HEAD).

### Fase 2 — Diseño del personaje (hoja de personaje)
- Generar con imagen-a-imagen (edits con fotos de referencia) la muñeca 3D en pose neutra.
- Comparar `flare` vs `sunburst` con el mismo prompt; elegir por parecido, ternura y consistencia.
- Iterar hasta lograr parecido con su rostro y físico; fijar prompt base ("prompt de identidad").
- Producir hoja de personaje: frente, 3/4, espalda (necesaria para el giro), cuerpo completo.
- Fondo liso para facilitar reutilización.

### Fase 2.5 — Objetos de escena
- Generar el cono de helado de máquina en espiral (vainilla/soft serve), en estilo consistente con la muñeca, y una referencia de la mano que lo sostiene.

### Fase 3 — Keyframes
Generar imágenes fijas coherentes con la misma identidad:
- K1 idle, K2 tocándose la cabeza, K3 enojada de frente, K4 enojada de espaldas/giro, K5 helado en cuadro, K6 volteando, K7 feliz con brillos en los ojos.
- Mantener mismo fondo, luz y encuadre entre keyframes.
- Selección y retoque de los mejores.

### Fase 4 — Animación con Veo 3.1
- Clips por plano usando primer/último frame (K1→K2, K2→K3, K3→K4, K5→K7, etc.) o imágenes de referencia.
- Prompts de movimiento: cámara estática o leve dolly, movimiento suave estilo stop-motion/juguete.
- Audio opcional: sonidos sutiles (gruñidito, "¡ooh!", risita) o dejar sin audio y poner música.
- Generar varias tomas por plano y elegir la mejor; revisar consistencia del rostro.
- Extender/interpolar si hace falta para el giro de enojo.

### Fase 5 — Montaje y postproducción
- Unir clips (ffmpeg o editor), ajustar cortes, ritmo y transiciones.
- Música cálida de fondo y efectos; texto/dedicatoria final opcional.
- Corrección de color uniforme, exportar 1080p vertical (9:16) y horizontal (16:9) si se desea.

### Fase 6 — Revisión y entrega
- Revisar parecido, ternura y continuidad; regenerar planos débiles.
- Exportar versión final y respaldar en almacenamiento privado.
- Entrega como regalo (archivo directo o enlace privado).

## Incluye
- Configuración de MCPs, generación de imágenes y clips, montaje del video final.

## No incluye
- Publicación en redes.
- Desarrollo dentro de Bancos.Api/Bancos.Mcp ni datos financieros.
- Versionar fotos, claves o el video en el repositorio.

## Criterios de aceptación
- La muñeca se reconoce como ella y se ve tierna.
- Secuencia completa: toque de cabeza → enojo con giro → helado → alegría.
- Video final de 15–30 s, sin artefactos graves en rostro/manos.
- Ninguna credencial ni foto queda en el repo.

## Archivos probables
- Configuración local de MCP (Azure Foundry y Google).
- Carpeta de assets fuera del repo.
- `.mcp/` (`_shared`, `azure-ai`, `google-ai`, `db-query`, `ia-workflow`, `package.json`), `.mcp.json`, `.gitignore`.
- Sin cambios en Bancos.Api/Bancos.Mcp ni en la BD.

## Plan técnico
Ver Fases 0–6. Orden: MCPs → hoja de personaje → keyframes → clips Veo → montaje.

## Riesgos
- Uso de la imagen de una persona real: solo con su consentimiento y uso privado.
- Deriva de identidad entre planos (mitigar con hoja de personaje y primer/último frame).
- Costo/cuota de generación en Azure y Veo 3.1; filtros de contenido con rostros reales.
- Vista de espaldas y giro son difíciles de mantener consistentes.

## Validación
- Revisión visual de cada fase con el usuario antes de avanzar.
- Prueba de humo de cada MCP (una imagen, un clip corto).

## Rollback
- Descartar assets generados; no hay cambios en código ni en base de datos.

## Historial de eventos V2

```jsonl
{"schemaVersion":2,"eventId":"14bf2abf-6a9d-45c9-98d6-e4037459245d","taskId":"TASK-EBC-INF-13","event":"approved","occurredAt":"2026-09-21T10:34:30.214-06:00","actor":"MCP iaWorkflow","reason":"aprobación explícita"}
{"schemaVersion":2,"eventId":"30fceeec-521b-4252-975d-dd9a9b36360b","taskId":"TASK-EBC-INF-13","event":"started","occurredAt":"2026-09-21T10:34:57.266-06:00","actor":"MCP iaWorkflow","reason":"Inicio de implementación autorizado."}
```
