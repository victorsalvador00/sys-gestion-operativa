import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/auth.guards';
import { placeholderRoute } from '../../core/layout/placeholder-routes';

const view = { canActivate: [permissionGuard], data: { permission: 'production.view' } };
const manageRecipes = {
  canActivate: [permissionGuard],
  data: { permission: 'production.recipes.manage' },
};

// Órdenes de producción llegan en F-10.
export const PRODUCTION_ROUTES: Routes = [
  {
    path: 'recetas',
    title: 'Recetas',
    ...view,
    loadComponent: () => import('./pages/recipes-list-page').then((m) => m.RecipesListPage),
  },
  {
    path: 'recetas/nueva',
    title: 'Nueva receta',
    ...manageRecipes,
    loadComponent: () => import('./pages/recipe-page').then((m) => m.RecipePage),
  },
  {
    // Quien solo tiene production.view la ve en solo lectura.
    path: 'recetas/:id',
    title: 'Receta',
    ...view,
    loadComponent: () => import('./pages/recipe-page').then((m) => m.RecipePage),
  },
  placeholderRoute('ordenes', 'Órdenes de producción', 'production.view'),
];
