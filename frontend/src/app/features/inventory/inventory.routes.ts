import { Routes } from '@angular/router';
import { placeholderRoute } from '../../core/layout/placeholder-routes';

// Pantallas reales en F-06 (existencias, kardex, ajustes, existencias iniciales) y F-07 (conteos, consumo).
export const INVENTORY_ROUTES: Routes = [
  placeholderRoute('existencias', 'Existencias', 'inventory.view'),
  placeholderRoute('kardex', 'Kardex', 'inventory.view'),
  placeholderRoute('ajustes', 'Ajustes', 'inventory.adjust'),
  placeholderRoute('conteos', 'Conteos físicos', 'inventory.count'),
  placeholderRoute('consumos/nuevo', 'Consumo del día', 'inventory.consumption'),
  placeholderRoute('existencias-iniciales', 'Existencias iniciales', 'inventory.adjust'),
];
