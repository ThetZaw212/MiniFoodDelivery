import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Tag } from 'primeng/tag';
import { Api } from '../../core/api';
import { AuthStore } from '../../core/auth.store';
import { CustomerOrder } from '../../core/models';

@Component({
  selector: 'app-orders',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, Tag],
  templateUrl: './orders.html',
})
export class OrdersPage {
  private readonly api = inject(Api);
  private readonly auth = inject(AuthStore);
  readonly orders = signal<CustomerOrder[]>([]);

  constructor() {
    const name = this.auth.user()?.name;
    if (!name) return;
    this.api.orders(name).subscribe({
      next: (rows) => this.orders.set(rows),
    });
  }
}
