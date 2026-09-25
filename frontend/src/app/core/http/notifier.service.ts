import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/** Avisos breves (toasts) en la parte inferior de la pantalla. */
@Injectable({ providedIn: 'root' })
export class Notifier {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, 'Cerrar', { duration: 4000, panelClass: 'sgo-toast-success' });
  }

  error(message: string): void {
    this.snackBar.open(message, 'Cerrar', { duration: 8000, panelClass: 'sgo-toast-error' });
  }
}
