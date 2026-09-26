import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/auth.guards';
import { placeholderRoute } from '../../core/layout/placeholder-routes';

const view = { canActivate: [permissionGuard], data: { permission: 'inventory.view' } };
const adjust = { canActivate: [permissionGuard], data: { permission: 'inventory.adjust' } };

// Conteos y consumo llegan en F-07.
export const INVENTORY_ROUTES: Routes = [
  {
    path: 'existencias',
    title: 'Existencias',
    ...view,
    loadComponent: () => import('./pages/stock-page').then((m) => m.StockPage),
  },
  {
    path: 'kardex',
    title: 'Kardex',
    ...view,
    loadComponent: () => import('./pages/kardex-page').then((m) => m.KardexPage),
  },
  {
    path: 'ajustes',
    title: 'Ajustes',
    ...view,
    loadComponent: () => import('./pages/adjustments-list-page').then((m) => m.AdjustmentsListPage),
  },
  {
    path: 'ajustes/nuevo',
    title: 'Nuevo ajuste',
    ...adjust,
    loadComponent: () => import('./pages/adjustment-form-page').then((m) => m.AdjustmentFormPage),
  },
  {
    path: 'ajustes/:id',
    title: 'Ajuste',
    ...view,
    loadComponent: () =>
      import('./pages/adjustment-detail-page').then((m) => m.AdjustmentDetailPage),
  },
  {
    path: 'existencias-iniciales',
    title: 'Existencias iniciales',
    ...adjust,
    loadComponent: () => import('./pages/initial-stock-page').then((m) => m.InitialStockPage),
  },
  placeholderRoute('conteos', 'Conteos físicos', 'inventory.count'),
  placeholderRoute('consumos/nuevo', 'Consumo del día', 'inventory.consumption'),
];
