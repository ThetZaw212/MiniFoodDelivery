export interface Restaurant {
  id: string;
  name: string;
  cuisine: string;
  address: string;
  isOpen: boolean;
}

export interface MenuItem {
  id: string;
  name: string;
  description: string;
  price: number;
  isAvailable: boolean;
  stock: number;
}

export interface Menu {
  restaurantId: string;
  restaurantName: string;
  items: MenuItem[];
}

export interface CartLine {
  menuItemId: string;
  name: string;
  price: number;
  quantity: number;
  stock: number;
}

export interface StockLineResult {
  menuItemId: string;
  name: string;
  requested: number;
  available: number;
  ok: boolean;
}

export interface StockResult {
  ok: boolean;
  lines: StockLineResult[];
}

export interface SessionUser {
  id: string;
  name: string;
  email: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  user: SessionUser;
}

export interface OrderLine {
  menuItemId: string;
  name: string;
  unitPrice: number;
  quantity: number;
}

export interface CustomerOrder {
  id: string;
  restaurantId: string;
  restaurantName: string;
  customerName: string;
  deliveryAddress: string;
  status: string;
  total: number;
  createdAtUtc: string;
  deliveryStatus: string;
  courierName: string | null;
  paymentMethod: string;
  paymentLabel: string;
  lines: OrderLine[];
}

export type PaymentMethod = 'Cash' | 'Bank' | 'Visa' | 'Mastercard' | 'MPU' | 'PayPal';
