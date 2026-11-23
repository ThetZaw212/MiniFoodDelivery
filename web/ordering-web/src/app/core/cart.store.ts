import { Injectable, computed, effect, signal } from '@angular/core';
import { CartLine, Restaurant } from './models';

const storageKey = 'minifood.cart';

@Injectable({ providedIn: 'root' })
export class CartStore {
  readonly restaurantId = signal<string | null>(null);
  readonly restaurantName = signal('');
  readonly lines = signal<CartLine[]>([]);
  readonly count = computed(() => this.lines().reduce((sum, line) => sum + line.quantity, 0));
  readonly total = computed(() => this.lines().reduce((sum, line) => sum + line.quantity * line.price, 0));

  constructor() {
    const saved = sessionStorage.getItem(storageKey);
    if (saved) {
      try {
        const parsed = JSON.parse(saved) as { restaurantId: string | null; restaurantName: string; lines: CartLine[] };
        this.restaurantId.set(parsed.restaurantId);
        this.restaurantName.set(parsed.restaurantName);
        this.lines.set(parsed.lines ?? []);
      } catch {
        sessionStorage.removeItem(storageKey);
      }
    }

    effect(() => {
      sessionStorage.setItem(
        storageKey,
        JSON.stringify({
          restaurantId: this.restaurantId(),
          restaurantName: this.restaurantName(),
          lines: this.lines(),
        }),
      );
    });
  }

  add(restaurant: Restaurant, item: { id: string; name: string; price: number; stock: number }): void {
    if (this.restaurantId() && this.restaurantId() !== restaurant.id) {
      this.lines.set([]);
    }
    this.restaurantId.set(restaurant.id);
    this.restaurantName.set(restaurant.name);
    this.lines.update((lines) => {
      const existing = lines.find((line) => line.menuItemId === item.id);
      if (!existing) {
        return item.stock < 1 ? lines : [...lines, { menuItemId: item.id, name: item.name, price: item.price, quantity: 1, stock: item.stock }];
      }
      return lines.map((line) =>
        line.menuItemId === item.id
          ? { ...line, stock: item.stock, quantity: Math.min(line.quantity + 1, item.stock) }
          : line,
      );
    });
  }

  setQuantity(menuItemId: string, quantity: number): void {
    this.lines.update((lines) =>
      lines
        .map((line) => (line.menuItemId === menuItemId ? { ...line, quantity: Math.min(Math.max(quantity, 0), line.stock) } : line))
        .filter((line) => line.quantity > 0),
    );
  }

  clear(): void {
    this.lines.set([]);
    this.restaurantId.set(null);
    this.restaurantName.set('');
  }
}
