import { Routes } from '@angular/router';
import { placeholderRoute } from '../../core/layout/placeholder-routes';

// Pantallas reales en F-11 a F-13.
export const PURCHASING_ROUTES: Routes = [
  placeholderRoute('proveedores', 'Proveedores', 'purchasing.view'),
  placeholderRoute('requisiciones', 'Requisiciones', 'purchasing.view'),
  placeholderRoute('ordenes', 'Órdenes de compra', 'purchasing.view'),
  placeholderRoute('recepciones', 'Recepciones', 'purchasing.view'),
];
