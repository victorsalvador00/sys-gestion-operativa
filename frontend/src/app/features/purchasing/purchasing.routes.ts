import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/auth.guards';
import { placeholderRoute } from '../../core/layout/placeholder-routes';

const view = { canActivate: [permissionGuard], data: { permission: 'purchasing.view' } };
const manageRequisitions = {
  canActivate: [permissionGuard],
  data: { permission: 'purchasing.requisitions.manage' },
};
const manageSuppliers = {
  canActivate: [permissionGuard],
  data: { permission: 'purchasing.suppliers.manage' },
};

// Órdenes de compra y recepciones en F-13.
export const PURCHASING_ROUTES: Routes = [
  {
    path: 'proveedores',
    title: 'Proveedores',
    ...view,
    loadComponent: () => import('./pages/suppliers-list-page').then((m) => m.SuppliersListPage),
  },
  {
    path: 'proveedores/nuevo',
    title: 'Nuevo proveedor',
    ...manageSuppliers,
    loadComponent: () => import('./pages/supplier-form-page').then((m) => m.SupplierFormPage),
  },
  {
    // Quien solo tiene purchasing.view lo ve en solo lectura.
    path: 'proveedores/:id',
    title: 'Proveedor',
    ...view,
    loadComponent: () => import('./pages/supplier-form-page').then((m) => m.SupplierFormPage),
  },
  {
    path: 'requisiciones',
    title: 'Requisiciones',
    ...view,
    loadComponent: () =>
      import('./pages/requisitions-list-page').then((m) => m.RequisitionsListPage),
  },
  {
    path: 'requisiciones/nueva',
    title: 'Nueva requisición',
    ...manageRequisitions,
    loadComponent: () => import('./pages/requisition-form-page').then((m) => m.RequisitionFormPage),
  },
  {
    path: 'requisiciones/:id',
    title: 'Requisición',
    ...view,
    loadComponent: () =>
      import('./pages/requisition-detail-page').then((m) => m.RequisitionDetailPage),
  },
  {
    path: 'requisiciones/:id/editar',
    title: 'Editar requisición',
    ...manageRequisitions,
    loadComponent: () => import('./pages/requisition-form-page').then((m) => m.RequisitionFormPage),
  },
  placeholderRoute('ordenes', 'Órdenes de compra', 'purchasing.view'),
  placeholderRoute('recepciones', 'Recepciones', 'purchasing.view'),
];
