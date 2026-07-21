# TASK-EBC-DB-06 — Auditoría de formatos de carga: verificar IBAN, banco y responsable por plantilla

**Estado:** Lista
**Autor:** ezekiell1988 `<ezekiell1988@hotmail.com>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-20 20:38 CR
**Fecha cierre:** —
**Área:** DB
**Prioridad:** alta
**Riesgo:** bajo
**Aprobación:** aprobada

---

## Título

Auditoría de formatos de carga: verificar IBAN, banco y responsable por plantilla

## Contexto

Cada formato de importación (plantilla de carga) debe tener asociado un IBAN o número de cuenta, un banco y un responsable (Ezequiel o Karen). Se quiere auditar que ninguna plantilla esté incompleta en estos tres campos.

## Objetivo

Confirmar que cada formato de carga registrado tiene IBAN/cuenta, banco y responsable asignado; corregir los que estén incompletos.

## Alcance permitido

* Consulta DB de plantillas/formatos de importación
* Corrección de registros incompletos vía UI o SQL directo acordado con el usuario
* Revisión interactiva con el usuario para asignar responsable donde falte

## Fuera de alcance

* Cambios al parser o lógica de importación
* Creación de nuevos formatos

## Criterios de aceptación

* [ ] Todos los formatos de carga tienen IBAN/cuenta asignado
* [ ] Todos los formatos tienen banco asignado
* [ ] Todos los formatos tienen responsable = Ezequiel o Karen

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `dbo.AccountAuxiliaries` — creación de auxiliares por cuenta real
* `dbo.Owners` — creación de owners Ezequiel Baltodano y Karen Soto
* `dbo.Imports` — reasignación de `AccountAuxiliaryId` a los auxiliares correctos

## Hallazgos de auditoría (2026-07-20)

### Estado actual en DB
Solo existen 2 `AccountAuxiliaries` genéricos (sin IBAN, sin owner real):
- `Cuenta bancaria` → Activo
- `Créditos y financiamientos` → Pasivo

Todos los `Imports` apuntan a uno de estos dos genéricos.

### Auxiliares que deben crearse

**DÉBITO (4 cuentas):**

| IBAN | Banco | Nombre | Moneda | Responsable |
|---|---|---|---|---|
| `CR06015107220020012339` | BN | BN Débito USD | USD (genera FX diferencial mensual) | Ezequiel |
| `CR07015202001294229652` | BCR | BCR Débito Compartida | CRC | Karen |
| `CR73010200009497305680` | BAC | BAC Débito CRC | CRC | Ezequiel |
| `CR86015100020019688637` | BN | BN Débito CRC | CRC | Ezequiel |

**CRÉDITO BAC — 2 auxiliares por tarjeta (CRC + USD), 8 en total:**

| Tarjeta | IBAN CRC | IBAN USD | Responsable |
|---|---|---|---|
| `4027-51**-****-1593` | `CR12010202421400516643` | `CR63010202418540751831` | Ezequiel |
| `3777-13**-****-8052` | `CR13010202321157328803` | `CR64010202312918989651` | Ezequiel |
| `5491-94**-****-6515` | `CR17010202526537778556` | `CR69010202510369031047` | Ezequiel |
| `5466-37**-****-8608` | `CR18010202522447454214` | `CR48010202514509181545` | Ezequiel |

**PRÉSTAMO (1):**

| IBAN | Banco | Nombre | Responsable |
|---|---|---|---|
| `CR05081302810003488995` | Coopealianza | Coopealianza Préstamo | Ezequiel |

### Owners que deben crearse
- Ezequiel Baltodano (reemplaza a "Propietario predeterminado")
- Karen Soto

## Plan técnico

1. Crear owners: Ezequiel Baltodano y Karen Soto
2. Crear 13 `AccountAuxiliaries` (4 débito + 8 crédito BAC + 1 préstamo) con IBAN, banco y owner correctos
3. Actualizar `Imports.AccountAuxiliaryId` al auxiliar correcto por template/IBAN
4. Validar que ningún auxiliar tenga IBAN nulo ni owner genérico

## Pasos

1. ~~Consultar DB: SELECT de formatos con IBAN, banco, responsable~~ ✓ completado
2. ~~Listar al usuario los campos vacíos/nulos~~ ✓ completado
3. ~~Acordar estructura con el usuario~~ ✓ completado — 2 auxiliares por tarjeta BAC (CRC+USD)
4. Generar y revisar SQL de inserción (owners + auxiliares)
5. Aplicar SQL con aprobación del usuario
6. Actualizar Imports.AccountAuxiliaryId
7. Validar que COUNT(*) incompletos = 0

## Salida esperada

Cero formatos con campos IBAN, banco o responsable vacíos/nulos.

## Validación

* [ ] SELECT COUNT(*) WHERE iban IS NULL OR banco IS NULL OR responsable IS NULL retorna 0

## Rollback

Correcciones de datos son reversibles manualmente si se documenta el estado anterior.

## Dependencias

* ninguna

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Pendiente de revisión: Se agregó la identidad bancaria del auxiliar y una resolución de auxiliar basada en contenido (IBAN o tarjeta+moneda), sin usar nombre ni ruta de archivo.

* Aprobada por ezekiell1988 el 2026-07-20 20:57 CR.

Sin notas adicionales.

## Issues vinculados

* ninguno
