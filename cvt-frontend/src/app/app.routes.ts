import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    loadComponent: () =>
      import('./layouts/dashboard-layout/dashboard-layout.component').then(
        (m) => m.DashboardLayoutComponent,
      ),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'documents',
        loadComponent: () =>
          import('./features/documents/documents.component').then((m) => m.DocumentsComponent),
      },
      {
        path: 'assistant',
        loadComponent: () =>
          import('./features/assistant/assistant.component').then((m) => m.AssistantComponent),
      },
      {
        path: 'administration',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/administration/administration.component').then(
            (m) => m.AdministrationComponent,
          ),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
