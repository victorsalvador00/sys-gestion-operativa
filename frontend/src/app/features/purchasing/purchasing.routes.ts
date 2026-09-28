import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/auth.guards';
import { placeholderRoute } from '../../core/layout/placeholder-routes';

const view = { canActivate: [permissionGuard], data: { permission: 'purchasing.view' } };
const manageSuppliers = {
  canActivate: [permissionGuard],
  data: { permission: 'purchasing.suppliers.manage' },
};

// Requisiciones, órdenes y recepciones en F-12 y F-13.
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
  placeholderRoute('requisiciones', 'Requisiciones', 'purchasing.view'),
  placeholderRoute('ordenes', 'Órdenes de compra', 'purchasing.view'),
  placeholderRoute('recepciones', 'Recepciones', 'purchasing.view'),
];
