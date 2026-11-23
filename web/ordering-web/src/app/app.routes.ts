import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then((module) => module.LoginPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./shell/shell').then((module) => module.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'restaurants' },
      {
        path: 'restaurants',
        loadComponent: () => import('./pages/restaurants/restaurants').then((module) => module.RestaurantsPage),
      },
      {
        path: 'restaurants/:id',
        loadComponent: () => import('./pages/menu/menu').then((module) => module.MenuPage),
      },
      {
        path: 'checkout',
        loadComponent: () => import('./pages/checkout/checkout').then((module) => module.CheckoutPage),
      },
      {
        path: 'orders',
        loadComponent: () => import('./pages/orders/orders').then((module) => module.OrdersPage),
      },
    ],
  },
];
