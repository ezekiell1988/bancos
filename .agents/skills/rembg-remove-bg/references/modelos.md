# Modelos rembg — Cuándo usar cada uno

## Comparativa

| Modelo | Velocidad | Calidad de bordes | Mejor para |
|---|---|---|---|
| `u2net` | Media | Buena | **Uso general** — personajes, objetos, logos, ilustraciones |
| `u2net_human_seg` | Media | Muy buena en personas | **Personajes humanos o estilizados** (Cleo, avatares) |
| `isnet-general-use` | Lenta | Excelente | Objetos con bordes complejos — pelo, animales, productos |
| `silueta` | Rápida | Regular | Siluetas simples, prototipos rápidos |

---

## Regla de decisión

```
¿Es un personaje animado / estilizado?
  → u2net_human_seg

¿Tiene bordes complejos (pelo, flecos, transparencias)?
  → isnet-general-use

¿Es logo, icono o forma simple?
  → u2net  (default)

¿Solo quiero verificar rápido?
  → silueta
```

---

## Ejemplo: cambiar modelo

```bash
# Default (u2net)
python3 .agents/skills/rembg-remove-bg/scripts/remove_bg.py src/VoiceBot.Web/src/assets/cleo/

# Personajes (mejor para Cleo)
python3 .agents/skills/rembg-remove-bg/scripts/remove_bg.py src/VoiceBot.Web/src/assets/cleo/ \
  --model u2net_human_seg

# Bordes más precisos
python3 .agents/skills/rembg-remove-bg/scripts/remove_bg.py src/VoiceBot.Web/src/assets/cleo/ \
  --model isnet-general-use
```

---

## Primera descarga de modelos

Los modelos se descargan automáticamente en `~/.u2net/` la primera vez:

| Modelo | Tamaño aprox |
|---|---|
| `u2net` | ~170 MB |
| `u2net_human_seg` | ~170 MB |
| `isnet-general-use` | ~380 MB |
| `silueta` | ~43 MB |

Después de la primera descarga, todas las ejecuciones son locales y rápidas.
