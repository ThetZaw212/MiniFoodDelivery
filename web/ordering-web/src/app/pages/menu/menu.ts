import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Tag } from 'primeng/tag';
import { Api } from '../../core/api';
import { CartStore } from '../../core/cart.store';
import { Menu, MenuItem, Restaurant } from '../../core/models';

@Component({
  selector: 'app-menu',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, RouterLink, Button, Tag],
  templateUrl: './menu.html',
})
export class MenuPage {
  readonly id = input.required<string>();
  private readonly api = inject(Api);
  readonly cart = inject(CartStore);
  readonly menu = signal<Menu | null>(null);
  readonly error = signal('');

  constructor() {
    effect(() => {
      const id = this.id();
      if (id) this.load(id);
    });
  }

  load(id = this.id()): void {
    this.api.menu(id).subscribe({
      next: (menu) => this.menu.set(menu),
      error: () => this.error.set('That restaurant could not be loaded.'),
    });
  }

  restaurant(): Restaurant {
    const menu = this.menu();
    return {
      id: menu?.restaurantId ?? this.id(),
      name: menu?.restaurantName ?? '',
      cuisine: '',
      address: '',
      isOpen: true,
    };
  }

  add(item: MenuItem): void {
    if (!item.isAvailable || item.stock < 1) return;
    this.cart.add(this.restaurant(), item);
  }

  stockLabel(item: MenuItem): string {
    if (!item.isAvailable || item.stock < 1) return 'Sold out';
    if (item.stock <= 8) return `${item.stock} left`;
    return `${item.stock} in the kitchen`;
  }
}
