import { Routes } from '@angular/router';
import { placeholderRoute } from '../../core/layout/placeholder-routes';

// Pantallas reales en F-08 (traspasos) y F-14 (pedidos).
export const LOGISTICS_ROUTES: Routes = [
  placeholderRoute('pedidos', 'Pedidos', 'logistics.view'),
  placeholderRoute('traspasos', 'Traspasos', 'logistics.view'),
];
