# Ejemplo: Componente Presentacional (card)

Componente privado de la feature — solo accesible desde `features/customers/`.

## customer-card.ts

**Archivo:** `src/app/features/customers/components/customer-card/customer-card.ts`

```typescript
import {
  Component,
  ChangeDetectionStrategy,
  input,
  computed,
} from '@angular/core';
import { Customer } from '../../data-access/customer.model';

@Component({
  selector: 'app-customer-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    'class': 'customer-card',
    '[class.customer-card--active]':   'isActive()',
    '[class.customer-card--blocked]':  'isBlocked()',
    '[class.customer-card--inactive]': 'isInactive()',
  },
  templateUrl: './customer-card.html',
  styles: `
    :host {
      display: block;
      background: var(--color-surface);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-md);
      padding: var(--space-4);
      cursor: pointer;
      transition: box-shadow var(--transition-fast);
    }
    :host:hover { box-shadow: var(--shadow-md); }
    :host.customer-card--active   { border-left: 3px solid var(--color-success); }
    :host.customer-card--blocked  { border-left: 3px solid var(--color-danger); }
    :host.customer-card--inactive { border-left: 3px solid var(--color-text-disabled); }
  `,
})
export class CustomerCard {
  customer = input.required<Customer>();

  isActive   = computed(() => this.customer().status === 'active');
  isBlocked  = computed(() => this.customer().status === 'blocked');
  isInactive = computed(() => this.customer().status === 'inactive');

  statusLabel = computed(() => ({
    active:   'Activo',
    inactive: 'Inactivo',
    blocked:  'Bloqueado',
  }[this.customer().status]));
}
```

---

## customer-card.html

**Archivo:** `src/app/features/customers/components/customer-card/customer-card.html`

```html
<div class="card-name">{{ customer().name }}</div>
<div class="card-email">{{ customer().email }}</div>
<div class="card-meta">
  <span class="status-badge" [class]="'status-badge--' + customer().status">
    {{ statusLabel() }}
  </span>
  <span class="card-purchases">
    {{ customer().totalPurchases | currency:'CRC':'symbol':'1.0-0' }} en compras
  </span>
</div>
```

**Reglas de componentes presentacionales:**
- Solo `input()` — nunca `inject()` de servicios en un componente "dumb".
- `host: {}` para clases dinámicas en el elemento raíz — nunca `[class]` en el template raíz.
- `computed()` para labels, flags booleanos y cualquier valor derivado de inputs.
- Estilos inline (en `styles:`) para componentes pequeños; archivo `.scss` separado para componentes con >20 líneas de estilos.
- Template separado (`.html`) cuando el template supera ~30 líneas.
- Colores siempre via `var(--color-*)` — jamás hex hardcodeados.
