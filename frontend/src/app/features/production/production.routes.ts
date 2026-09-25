import { Routes } from '@angular/router';
import { placeholderRoute } from '../../core/layout/placeholder-routes';

// Pantallas reales en F-09 (recetas) y F-10 (órdenes de producción).
export const PRODUCTION_ROUTES: Routes = [
  placeholderRoute('recetas', 'Recetas', 'production.view'),
  placeholderRoute('ordenes', 'Órdenes de producción', 'production.view'),
];
