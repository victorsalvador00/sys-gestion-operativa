import { Routes } from '@angular/router';
import { placeholderRoute } from '../../core/layout/placeholder-routes';

// Pantallas reales en F-04.
export const ADMIN_ROUTES: Routes = [
  placeholderRoute('usuarios', 'Usuarios', 'security.users.manage'),
  placeholderRoute('roles', 'Roles', 'security.roles.manage'),
  placeholderRoute('bitacora', 'Bitácora', 'security.audit.view'),
  placeholderRoute('configuracion', 'Configuración', 'settings.manage'),
];
