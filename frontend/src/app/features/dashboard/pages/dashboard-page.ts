import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

/** Tablero (se implementa en F-15). Por ahora muestra el estado vacío del shell. */
@Component({
  selector: 'app-dashboard-page',
  imports: [MatCardModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="sgo-page sgo-stack">
      <h1 class="title">Tablero</h1>
      <mat-card appearance="outlined">
        <mat-card-content class="empty">
          <mat-icon aria-hidden="true">dashboard</mat-icon>
          <p>Aquí verás los indicadores de tu ubicación.</p>
        </mat-card-content>
      </mat-card>
    </section>
  `,
  styles: `
    .title {
      margin: 0;
      font: var(--mat-sys-headline-small);
    }
    .empty {
      display: flex;
      align-items: center;
      gap: var(--sgo-space-3);
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class DashboardPage {}
