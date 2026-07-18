import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './app.css',
  template: `<main><header><a routerLink="/imports" class="brand">Bancos</a><nav aria-label="Principal"><a routerLink="/imports" routerLinkActive="active">Importaciones</a><a routerLink="/review" routerLinkActive="active">Revisión</a></nav></header><router-outlet /></main>`
})
export class App {}
