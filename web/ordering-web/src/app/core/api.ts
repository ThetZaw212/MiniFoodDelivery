import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { CartLine, CustomerOrder, LoginResponse, Menu, Restaurant, StockResult } from './models';

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);

  login(email: string, password: string) {
    return this.http.post<LoginResponse>('/auth/api/auth/login', { email, password });
  }

  restaurants() {
    return this.http.get<Restaurant[]>('/catalog/api/restaurants');
  }

  menu(restaurantId: string) {
    return this.http.get<Menu>(`/catalog/api/restaurants/${restaurantId}/menu`);
  }

  checkStock(lines: CartLine[]) {
    return this.http.post<StockResult>('/catalog/api/stock/check', {
      lines: lines.map((line) => ({ menuItemId: line.menuItemId, quantity: line.quantity })),
    });
  }

  placeOrder(body: {
    restaurantId: string;
    customerName: string;
    deliveryAddress: string;
    lines: { menuItemId: string; quantity: number }[];
    paymentMethod: string;
    paymentLabel: string;
  }) {
    return this.http.post<CustomerOrder>('/orders/api/orders', body);
  }

  orders(customer: string) {
    return this.http.get<CustomerOrder[]>('/orders/api/orders', { params: { customer } });
  }
}
