import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Button } from 'primeng/button';
import { AuthStore } from '../core/auth.store';
import { CartStore } from '../core/cart.store';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Button, DecimalPipe],
  templateUrl: './shell.html',
})
export class Shell {
  readonly auth = inject(AuthStore);
  readonly cart = inject(CartStore);
  private readonly router = inject(Router);

  signOut(): void {
    this.auth.clear();
    void this.router.navigateByUrl('/login');
  }
}
