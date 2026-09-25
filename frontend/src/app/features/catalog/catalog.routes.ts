import { Routes } from '@angular/router';
import { placeholderRoute } from '../../core/layout/placeholder-routes';

// Pantallas reales en F-05.
export const CATALOG_ROUTES: Routes = [
  placeholderRoute('articulos', 'Artículos', 'catalog.view'),
  placeholderRoute('categorias', 'Categorías', 'catalog.view'),
  placeholderRoute('unidades', 'Unidades', 'catalog.view'),
  placeholderRoute('ubicaciones', 'Ubicaciones', 'locations.view'),
];
