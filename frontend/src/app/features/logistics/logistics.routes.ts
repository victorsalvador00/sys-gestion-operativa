import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/auth.guards';
import { placeholderRoute } from '../../core/layout/placeholder-routes';

const view = { canActivate: [permissionGuard], data: { permission: 'logistics.view' } };
const dispatch = {
  canActivate: [permissionGuard],
  data: { permission: 'logistics.transfers.dispatch' },
};
const receive = {
  canActivate: [permissionGuard],
  data: { permission: 'logistics.transfers.receive' },
};

// Pedidos llegan en F-14.
export const LOGISTICS_ROUTES: Routes = [
  placeholderRoute('pedidos', 'Pedidos', 'logistics.view'),
  {
    path: 'traspasos',
    title: 'Traspasos',
    ...view,
    loadComponent: () => import('./pages/transfers-list-page').then((m) => m.TransfersListPage),
  },
  {
    path: 'traspasos/nuevo',
    title: 'Nuevo traspaso',
    ...dispatch,
    loadComponent: () => import('./pages/transfer-form-page').then((m) => m.TransferFormPage),
  },
  {
    path: 'traspasos/:id',
    title: 'Traspaso',
    ...view,
    loadComponent: () => import('./pages/transfer-detail-page').then((m) => m.TransferDetailPage),
  },
  {
    path: 'traspasos/:id/editar',
    title: 'Editar traspaso',
    ...dispatch,
    loadComponent: () => import('./pages/transfer-form-page').then((m) => m.TransferFormPage),
  },
  {
    path: 'traspasos/:id/recibir',
    title: 'Recibir traspaso',
    ...receive,
    loadComponent: () => import('./pages/transfer-receive-page').then((m) => m.TransferReceivePage),
  },
];
