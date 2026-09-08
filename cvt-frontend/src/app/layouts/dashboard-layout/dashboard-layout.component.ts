import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

interface NavItem {
  label: string;
  path: string;
  adminOnly?: boolean;
  icon: 'dashboard' | 'documents' | 'assistant' | 'administration';
}

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './dashboard-layout.component.html',
})
export class DashboardLayoutComponent {
  protected readonly authService = inject(AuthService);

  readonly isSidebarCollapsed = signal(false);
  readonly isMobileMenuOpen = signal(false);
  readonly isProfileMenuOpen = signal(false);

  readonly navItems: NavItem[] = [
    { label: 'Dashboard', path: '/dashboard', icon: 'dashboard' },
    { label: 'Documents', path: '/documents', icon: 'documents' },
    { label: 'Assistant IA', path: '/assistant', icon: 'assistant' },
    { label: 'Administration', path: '/administration', adminOnly: true, icon: 'administration' },
  ];

  toggleSidebar(): void {
    this.isSidebarCollapsed.update((value) => !value);
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen.update((value) => !value);
  }

  closeMobileMenu(): void {
    this.isMobileMenuOpen.set(false);
  }

  toggleProfileMenu(): void {
    this.isProfileMenuOpen.update((value) => !value);
  }

  logout(): void {
    this.authService.logout();
  }

  visibleNavItems(): NavItem[] {
    const isAdmin = this.authService.isAdmin();
    return this.navItems.filter((item) => !item.adminOnly || isAdmin);
  }

  initials(): string {
    const user = this.authService.currentUser();
    if (!user) {
      return '?';
    }
    return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  }
}
