# Responsive Layouts - Common Patterns

Patrones de layout responsivos que funcionan tanto en modo desktop (Bootstrap) como mobile (Ionic).

## Table of Contents

- [Grid Layouts](#grid-layouts)
- [Card Layouts](#card-layouts)
- [Form Layouts](#form-layouts)
- [Navigation Patterns](#navigation-patterns)
- [Modal/Dialog Patterns](#modaldialog-patterns)
- [List Patterns](#list-patterns)

---

## Grid Layouts

### Desktop Grid (Bootstrap)

```html
<!-- 3 columns on desktop, 2 on tablet, 1 on mobile -->
<div class="container-fluid">
  <div class="row row-space-10">
    <div class="col-12 col-md-6 col-lg-4">
      <div class="card">
        <div class="card-body">Item 1</div>
      </div>
    </div>
    <div class="col-12 col-md-6 col-lg-4">
      <div class="card">
        <div class="card-body">Item 2</div>
      </div>
    </div>
    <div class="col-12 col-md-6 col-lg-4">
      <div class="card">
        <div class="card-body">Item 3</div>
      </div>
    </div>
  </div>
</div>
```

### Mobile Grid (Ionic)

```html
<!-- Ionic grid with same responsive behavior -->
<ion-grid>
  <ion-row>
    <ion-col size="12" size-md="6" size-lg="4">
      <ion-card>
        <ion-card-content>Item 1</ion-card-content>
      </ion-card>
    </ion-col>
    <ion-col size="12" size-md="6" size-lg="4">
      <ion-card>
        <ion-card-content>Item 2</ion-card-content>
      </ion-card>
    </ion-col>
    <ion-col size="12" size-md="6" size-lg="4">
      <ion-card>
        <ion-card-content>Item 3</ion-card-content>
      </ion-card>
    </ion-col>
  </ion-row>
</ion-grid>
```

### Adaptive Grid Component

```typescript
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-responsive-grid',
  template: `
    <!-- Desktop version -->
    <div *ngIf="isDesktop" class="container-fluid">
      <div class="row row-space-10">
        <div 
          *ngFor="let item of items" 
          class="col-12 col-md-6 col-lg-4"
        >
          <div class="card">
            <div class="card-body">
              <ng-container *ngTemplateOutlet="itemTemplate; context: {$implicit: item}"></ng-container>
            </div>
          </div>
        </div>
      </div>
    </div>
    
    <!-- Mobile version -->
    <ion-grid *ngIf="!isDesktop">
      <ion-row>
        <ion-col 
          *ngFor="let item of items"
          size="12" 
          size-md="6" 
          size-lg="4"
        >
          <ion-card>
            <ion-card-content>
              <ng-container *ngTemplateOutlet="itemTemplate; context: {$implicit: item}"></ng-container>
            </ion-card-content>
          </ion-card>
        </ion-col>
      </ion-row>
    </ion-grid>
  `
})
export class ResponsiveGridComponent {
  @Input() items: any[] = [];
  @Input() itemTemplate!: TemplateRef<any>;
  @Input() isDesktop = true;
}
```

**Usage:**
```html
<app-responsive-grid 
  [items]="products" 
  [isDesktop]="appSettings.currentMode === 'desktop'"
  [itemTemplate]="productCard"
></app-responsive-grid>

<ng-template #productCard let-product>
  <h5>{{ product.name }}</h5>
  <p>{{ product.price | currency }}</p>
</ng-template>
```

---

## Card Layouts

### Desktop Card

```html
<div class="card shadow-sm">
  <div class="card-header bg-primary text-white">
    <h5 class="card-title mb-0">Card Title</h5>
  </div>
  <div class="card-body">
    <p class="card-text">Card content goes here.</p>
  </div>
  <div class="card-footer bg-light">
    <div class="d-flex justify-content-between">
      <button class="btn btn-sm btn-secondary">Cancel</button>
      <button class="btn btn-sm btn-primary">Save</button>
    </div>
  </div>
</div>
```

### Mobile Card

```html
<ion-card>
  <ion-card-header color="primary">
    <ion-card-title>Card Title</ion-card-title>
  </ion-card-header>
  <ion-card-content>
    <p>Card content goes here.</p>
  </ion-card-content>
  <ion-footer class="ion-no-border">
    <ion-toolbar>
      <ion-buttons slot="start">
        <ion-button fill="clear">Cancel</ion-button>
      </ion-buttons>
      <ion-buttons slot="end">
        <ion-button fill="solid" color="primary">Save</ion-button>
      </ion-buttons>
    </ion-toolbar>
  </ion-footer>
</ion-card>
```

### Card Grid Pattern

```html
<!-- Desktop: 3-column card grid -->
<div class="container-fluid">
  <div class="row row-space-10">
    <div class="col-12 col-md-6 col-xl-4" *ngFor="let item of items">
      <div class="card h-100">
        <div class="card-body d-flex flex-column">
          <h5 class="card-title">{{ item.title }}</h5>
          <p class="card-text flex-grow-1">{{ item.description }}</p>
          <button class="btn btn-primary mt-auto">View Details</button>
        </div>
      </div>
    </div>
  </div>
</div>

<!-- Mobile: Vertical card list -->
<ion-content>
  <ion-card *ngFor="let item of items">
    <ion-card-header>
      <ion-card-title>{{ item.title }}</ion-card-title>
    </ion-card-header>
    <ion-card-content>
      <p>{{ item.description }}</p>
      <ion-button expand="block" color="primary">View Details</ion-button>
    </ion-card-content>
  </ion-card>
</ion-content>
```

---

## Form Layouts

### Desktop Form

```html
<div class="card">
  <div class="card-header">
    <h5 class="mb-0">User Form</h5>
  </div>
  <div class="card-body">
    <form>
      <div class="row mb-3">
        <div class="col-md-6">
          <label class="form-label">First Name</label>
          <input type="text" class="form-control" />
        </div>
        <div class="col-md-6">
          <label class="form-label">Last Name</label>
          <input type="text" class="form-control" />
        </div>
      </div>
      
      <div class="mb-3">
        <label class="form-label">Email</label>
        <input type="email" class="form-control" />
      </div>
      
      <div class="mb-3">
        <label class="form-label">Message</label>
        <textarea class="form-control" rows="4"></textarea>
      </div>
      
      <div class="d-flex justify-content-end gap-2">
        <button type="button" class="btn btn-secondary">Cancel</button>
        <button type="submit" class="btn btn-primary">Submit</button>
      </div>
    </form>
  </div>
</div>
```

### Mobile Form

```html
<ion-content>
  <form>
    <ion-list>
      <ion-item>
        <ion-label position="stacked">First Name</ion-label>
        <ion-input type="text"></ion-input>
      </ion-item>
      
      <ion-item>
        <ion-label position="stacked">Last Name</ion-label>
        <ion-input type="text"></ion-input>
      </ion-item>
      
      <ion-item>
        <ion-label position="stacked">Email</ion-label>
        <ion-input type="email"></ion-input>
      </ion-item>
      
      <ion-item>
        <ion-label position="stacked">Message</ion-label>
        <ion-textarea rows="4"></ion-textarea>
      </ion-item>
    </ion-list>
    
    <div class="ion-padding">
      <ion-button expand="block" color="primary" type="submit">
        Submit
      </ion-button>
      <ion-button expand="block" fill="clear" type="button">
        Cancel
      </ion-button>
    </div>
  </form>
</ion-content>
```

### Inline Form (Search bar)

```html
<!-- Desktop -->
<div class="d-flex gap-2 mb-3">
  <input 
    type="search" 
    class="form-control" 
    placeholder="Search..."
  />
  <button class="btn btn-primary">
    <i class="fa fa-search"></i>
  </button>
</div>

<!-- Mobile -->
<ion-searchbar 
  placeholder="Search..."
  (ionChange)="onSearch($event)"
></ion-searchbar>
```

---

## Navigation Patterns

### Desktop Sidebar + Content

```html
<div class="d-flex vh-100">
  <!-- Sidebar -->
  <div class="bg-dark text-white" style="width: 250px;">
    <div class="p-3">
      <h4>Navigation</h4>
      <ul class="list-unstyled">
        <li class="mb-2">
          <a href="#" class="text-white text-decoration-none">Dashboard</a>
        </li>
        <li class="mb-2">
          <a href="#" class="text-white text-decoration-none">Users</a>
        </li>
        <li class="mb-2">
          <a href="#" class="text-white text-decoration-none">Settings</a>
        </li>
      </ul>
    </div>
  </div>
  
  <!-- Content -->
  <div class="flex-grow-1 p-4 overflow-auto">
    <router-outlet></router-outlet>
  </div>
</div>
```

### Mobile Tabs

```html
<ion-tabs>
  <ion-tab-bar slot="bottom">
    <ion-tab-button tab="dashboard">
      <ion-icon name="home"></ion-icon>
      <ion-label>Dashboard</ion-label>
    </ion-tab-button>
    
    <ion-tab-button tab="users">
      <ion-icon name="people"></ion-icon>
      <ion-label>Users</ion-label>
    </ion-tab-button>
    
    <ion-tab-button tab="settings">
      <ion-icon name="settings"></ion-icon>
      <ion-label>Settings</ion-label>
    </ion-tab-button>
  </ion-tab-bar>
</ion-tabs>
```

### Breadcrumb Navigation

```html
<!-- Desktop -->
<nav aria-label="breadcrumb" class="mb-3">
  <ol class="breadcrumb">
    <li class="breadcrumb-item"><a href="#">Home</a></li>
    <li class="breadcrumb-item"><a href="#">Products</a></li>
    <li class="breadcrumb-item active">Details</li>
  </ol>
</nav>

<!-- Mobile -->
<ion-toolbar>
  <ion-buttons slot="start">
    <ion-back-button defaultHref="/"></ion-back-button>
  </ion-buttons>
  <ion-title>Product Details</ion-title>
</ion-toolbar>
```

---

## Modal/Dialog Patterns

### Desktop Modal (Bootstrap)

```html
<!-- Trigger -->
<button class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#exampleModal">
  Open Modal
</button>

<!-- Modal -->
<div class="modal fade" id="exampleModal" tabindex="-1">
  <div class="modal-dialog modal-dialog-centered">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">Modal Title</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <p>Modal content goes here.</p>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
          Close
        </button>
        <button type="button" class="btn btn-primary">
          Save changes
        </button>
      </div>
    </div>
  </div>
</div>
```

### Mobile Modal (Ionic)

```typescript
// Component
import { ModalController } from '@ionic/angular';

export class ExamplePage {
  constructor(private modalCtrl: ModalController) {}
  
  async openModal() {
    const modal = await this.modalCtrl.create({
      component: ExampleModalComponent
    });
    await modal.present();
  }
}

// Modal Component
@Component({
  template: `
    <ion-header>
      <ion-toolbar>
        <ion-title>Modal Title</ion-title>
        <ion-buttons slot="end">
          <ion-button (click)="dismiss()">Close</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    
    <ion-content>
      <div class="ion-padding">
        <p>Modal content goes here.</p>
      </div>
    </ion-content>
    
    <ion-footer>
      <ion-toolbar>
        <ion-buttons slot="end">
          <ion-button (click)="dismiss()">Close</ion-button>
          <ion-button color="primary" (click)="save()">Save</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-footer>
  `
})
export class ExampleModalComponent {
  constructor(private modalCtrl: ModalController) {}
  
  dismiss() {
    this.modalCtrl.dismiss();
  }
  
  save() {
    this.modalCtrl.dismiss({ saved: true });
  }
}
```

---

## List Patterns

### Desktop Table

```html
<div class="card">
  <div class="card-body">
    <div class="table-responsive">
      <table class="table table-hover table-striped">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Status</th>
            <th class="text-end">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let user of users">
            <td>{{ user.name }}</td>
            <td>{{ user.email }}</td>
            <td>
              <span class="badge" [class.bg-success]="user.active" [class.bg-secondary]="!user.active">
                {{ user.active ? 'Active' : 'Inactive' }}
              </span>
            </td>
            <td class="text-end">
              <button class="btn btn-sm btn-primary me-1">Edit</button>
              <button class="btn btn-sm btn-danger">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</div>
```

### Mobile List

```html
<ion-list>
  <ion-item *ngFor="let user of users">
    <ion-label>
      <h2>{{ user.name }}</h2>
      <p>{{ user.email }}</p>
    </ion-label>
    <ion-badge slot="end" [color]="user.active ? 'success' : 'medium'">
      {{ user.active ? 'Active' : 'Inactive' }}
    </ion-badge>
    <ion-buttons slot="end">
      <ion-button fill="clear" color="primary">
        <ion-icon name="create"></ion-icon>
      </ion-button>
      <ion-button fill="clear" color="danger">
        <ion-icon name="trash"></ion-icon>
      </ion-button>
    </ion-buttons>
  </ion-item>
</ion-list>
```

### List with Swipe Actions (Mobile)

```html
<ion-list>
  <ion-item-sliding *ngFor="let user of users">
    <ion-item>
      <ion-label>
        <h2>{{ user.name }}</h2>
        <p>{{ user.email }}</p>
      </ion-label>
    </ion-item>
    
    <ion-item-options side="end">
      <ion-item-option color="primary" (click)="edit(user)">
        <ion-icon name="create"></ion-icon>
        Edit
      </ion-item-option>
      <ion-item-option color="danger" (click)="delete(user)">
        <ion-icon name="trash"></ion-icon>
        Delete
      </ion-item-option>
    </ion-item-options>
  </ion-item-sliding>
</ion-list>
```

---

## Responsive Dashboard Layout

### Component

```typescript
import { Component, inject } from '@angular/core';
import { AppSettingsService } from '../../service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html'
})
export class DashboardComponent {
  appSettings = inject(AppSettingsService);
  
  stats = [
    { title: 'Total Users', value: 1234, icon: 'people', color: 'primary' },
    { title: 'Revenue', value: '$45,678', icon: 'cash', color: 'success' },
    { title: 'Orders', value: 789, icon: 'cart', color: 'warning' },
    { title: 'Products', value: 456, icon: 'cube', color: 'info' }
  ];
  
  get isDesktop() {
    return this.appSettings.currentMode === 'desktop';
  }
}
```

### Template (Desktop)

```html
<div *ngIf="isDesktop" class="container-fluid p-4">
  <!-- Stats Row -->
  <div class="row row-space-10 mb-4">
    <div class="col-12 col-sm-6 col-lg-3" *ngFor="let stat of stats">
      <div class="card">
        <div class="card-body d-flex align-items-center">
          <div class="flex-grow-1">
            <h6 class="text-muted mb-1">{{ stat.title }}</h6>
            <h3 class="mb-0">{{ stat.value }}</h3>
          </div>
          <div class="fs-36px text-{{ stat.color }}">
            <i class="fa fa-{{ stat.icon }}"></i>
          </div>
        </div>
      </div>
    </div>
  </div>
  
  <!-- Charts Row -->
  <div class="row row-space-10">
    <div class="col-12 col-lg-8">
      <div class="card">
        <div class="card-header">
          <h5 class="mb-0">Sales Chart</h5>
        </div>
        <div class="card-body">
          <!-- Chart component -->
        </div>
      </div>
    </div>
    <div class="col-12 col-lg-4">
      <div class="card">
        <div class="card-header">
          <h5 class="mb-0">Recent Activity</h5>
        </div>
        <div class="card-body">
          <!-- Activity list -->
        </div>
      </div>
    </div>
  </div>
</div>
```

### Template (Mobile)

```html
<ion-content *ngIf="!isDesktop">
  <!-- Stats Grid -->
  <ion-grid>
    <ion-row>
      <ion-col size="6" *ngFor="let stat of stats">
        <ion-card>
          <ion-card-content class="ion-text-center">
            <ion-icon [name]="stat.icon" [color]="stat.color" size="large"></ion-icon>
            <h2>{{ stat.value }}</h2>
            <p class="ion-text-muted">{{ stat.title }}</p>
          </ion-card-content>
        </ion-card>
      </ion-col>
    </ion-row>
  </ion-grid>
  
  <!-- Charts -->
  <ion-card>
    <ion-card-header>
      <ion-card-title>Sales Chart</ion-card-title>
    </ion-card-header>
    <ion-card-content>
      <!-- Chart component -->
    </ion-card-content>
  </ion-card>
  
  <!-- Activity -->
  <ion-card>
    <ion-card-header>
      <ion-card-title>Recent Activity</ion-card-title>
    </ion-card-header>
    <ion-card-content>
      <!-- Activity list -->
    </ion-card-content>
  </ion-card>
</ion-content>
```

---

## Best Practices Summary

### ✅ DO

1. **Use semantic breakpoints**
   ```html
   <div class="col-12 col-md-6 col-lg-4">
   <ion-col size="12" size-md="6" size-lg="4">
   ```

2. **Keep layouts consistent**
   - Same content, different presentation
   - Same functionality, different UI

3. **Use platform-specific components**
   - Desktop: Cards, tables, modals
   - Mobile: Lists, slides, action sheets

4. **Test responsive behavior**
   - Test all breakpoints
   - Test orientation changes (mobile)
   - Test resize across 768px threshold

### ❌ DON'T

1. **Don't duplicate logic**
   ```typescript
   // ❌ Bad
   if (isDesktop) {
     this.loadData();
   } else {
     this.loadData(); // Duplicate
   }
   
   // ✅ Good
   this.loadData();
   ```

2. **Don't hide critical features on mobile**
   - Make all features accessible on both platforms
   - Adapt UI, don't remove functionality

3. **Don't assume screen size**
   - Use breakpoints, not device detection
   - Test on real devices

4. **Don't mix layout systems**
   ```html
   <!-- ❌ Bad -->
   <div class="row">
     <ion-col>Mixed!</ion-col>
   </div>
   
   <!-- ✅ Good -->
   <div class="row">
     <div class="col">Bootstrap</div>
   </div>
   ```

---

## Resources

- [Bootstrap Grid Documentation](https://getbootstrap.com/docs/5.3/layout/grid/)
- [Ionic Grid Documentation](https://ionicframework.com/docs/api/grid)
- [Responsive Design Patterns](https://web.dev/patterns/layout/)
