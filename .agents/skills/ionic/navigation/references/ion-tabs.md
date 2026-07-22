# Reference: ion-tabs

## Uso básico

```html
<!-- Tabs con contenido inline (sin router) -->
<ion-tabs>
  <ion-tab tab="home">
    <div class="ion-page">
      <ion-header>
        <ion-toolbar><ion-title>Inicio</ion-title></ion-toolbar>
      </ion-header>
      <ion-content class="ion-padding">Contenido de Inicio</ion-content>
    </div>
  </ion-tab>

  <ion-tab tab="settings">
    <div class="ion-page">
      <ion-header>
        <ion-toolbar><ion-title>Configuración</ion-title></ion-toolbar>
      </ion-header>
      <ion-content class="ion-padding">Contenido de Config</ion-content>
    </div>
  </ion-tab>

  <ion-tab-bar slot="bottom">
    <ion-tab-button tab="home">
      <ion-icon name="home"></ion-icon>
      Inicio
    </ion-tab-button>
    <ion-tab-button tab="settings">
      <ion-icon name="settings"></ion-icon>
      Config
    </ion-tab-button>
  </ion-tab-bar>
</ion-tabs>
```

```html
<!-- Tabs con Angular Router (patrón más común — NO usar ion-tab) -->
<ion-tabs #tabs>
  <ion-tab-bar slot="bottom">
    <ion-tab-button tab="home" href="/tabs/home">
      <ion-icon name="home"></ion-icon>
      Inicio
    </ion-tab-button>
    <ion-tab-button tab="discover" href="/tabs/discover">
      <ion-icon name="search"></ion-icon>
      Descubrir
    </ion-tab-button>
    <ion-tab-button tab="profile" href="/tabs/profile">
      <ion-icon name="person"></ion-icon>
      Perfil
    </ion-tab-button>
  </ion-tab-bar>
</ion-tabs>
```

```html
<!-- Tab bar en la parte superior -->
<ion-tabs>
  <ion-tab-bar slot="top">
    <ion-tab-button tab="home"><ion-icon name="home"></ion-icon>Inicio</ion-tab-button>
    <ion-tab-button tab="news"><ion-icon name="newspaper"></ion-icon>Noticias</ion-tab-button>
  </ion-tab-bar>
</ion-tabs>
```

## Component TS

```typescript
import { ViewChild } from '@angular/core';
import { IonTabs } from '@ionic/angular/standalone';

@Component({
  imports: [IonTabs, IonTabBar, IonTabButton, IonIcon, IonContent, IonHeader, IonTitle, IonToolbar]
})
export class TabsPage {
  @ViewChild('tabs') tabs!: IonTabs;

  selectTab(tab: string) {
    this.tabs.select(tab); // Navegación programática
  }
}
```

## Notas

- `slot="bottom"` (default) o `slot="top"` en `ion-tab-bar`.
- `ion-tab-button [tab]` debe coincidir con el nombre de la ruta lazy-loaded (Angular Router).
- `badge="3"` en `ion-tab-button` muestra un badge numérico sobre el icono.
- Eventos: `(ionTabsDidChange)`, `(ionTabsWillChange)` en `ion-tabs`.
- **Imports TS (sin router)**: `IonTabs, IonTab, IonTabBar, IonTabButton, IonIcon, IonContent, IonHeader, IonTitle, IonToolbar`
- **Imports TS (con router)**: `IonTabs, IonTabBar, IonTabButton, IonIcon`
