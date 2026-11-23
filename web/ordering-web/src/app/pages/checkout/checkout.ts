import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Api } from '../../core/api';
import { AuthStore } from '../../core/auth.store';
import { CartStore } from '../../core/cart.store';
import { PaymentMethod, StockResult } from '../../core/models';

@Component({
  selector: 'app-checkout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, ReactiveFormsModule, RouterLink, Button, InputText, Message],
  templateUrl: './checkout.html',
})
export class CheckoutPage {
  readonly cart = inject(CartStore);
  readonly auth = inject(AuthStore);
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  readonly methods: { id: PaymentMethod; title: string; note: string }[] = [
    { id: 'Cash', title: 'Cash', note: 'Pay the courier' },
    { id: 'Bank', title: 'Bank', note: 'Demo transfer' },
    { id: 'Visa', title: 'Visa', note: 'Demo card' },
    { id: 'Mastercard', title: 'Mastercard', note: 'Demo card' },
    { id: 'MPU', title: 'MPU', note: 'Demo card' },
    { id: 'PayPal', title: 'PayPal', note: 'Demo wallet' },
  ];

  readonly method = signal<PaymentMethod>('Cash');
  readonly stock = signal<StockResult | null>(null);
  readonly error = signal('');
  readonly busy = signal(false);
  readonly form = this.formBuilder.nonNullable.group({
    address: ['12 Strand Road, Yangon', Validators.required],
    bank: ['KBZ'],
    account: [''],
    card: [''],
    expiry: [''],
    cvv: [''],
    paypal: [''],
  });

  checkStock(): void {
    this.error.set('');
    this.api.checkStock(this.cart.lines()).subscribe({
      next: (result) => this.stock.set(result),
      error: () => this.error.set('Stock could not be checked. Try again before paying.'),
    });
  }

  pay(): void {
    const lines = this.cart.lines();
    const restaurantId = this.cart.restaurantId();
    const user = this.auth.user();
    if (!restaurantId || lines.length === 0 || !user) return;
    const problem = this.paymentProblem();
    if (problem) {
      this.error.set(problem);
      return;
    }
    if (this.form.controls.address.invalid) {
      this.error.set('A delivery address is required.');
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.api.checkStock(lines).subscribe({
      next: (result) => {
        this.stock.set(result);
        if (!result.ok) {
          this.busy.set(false);
          this.error.set('The kitchen cannot cover this basket. Lower a quantity and check again.');
          return;
        }
        this.api
          .placeOrder({
            restaurantId,
            customerName: user.name,
            deliveryAddress: this.form.controls.address.value.trim(),
            lines: lines.map((line) => ({ menuItemId: line.menuItemId, quantity: line.quantity })),
            paymentMethod: this.method(),
            paymentLabel: this.paymentLabel(),
          })
          .subscribe({
            next: () => {
              this.cart.clear();
              void this.router.navigateByUrl('/orders');
            },
            error: (error: HttpErrorResponse) => {
              this.busy.set(false);
              this.error.set(error.error?.detail ?? 'The order was not placed. Stock may have changed.');
              this.checkStock();
            },
          });
      },
      error: () => {
        this.busy.set(false);
        this.error.set('Stock could not be checked, so payment stayed closed.');
      },
    });
  }

  private paymentProblem(): string {
    const value = this.form.getRawValue();
    const digits = value.card.replace(/\s/g, '');
    switch (this.method()) {
      case 'Cash':
        return '';
      case 'Bank':
        return /^\d{6,}$/.test(value.account) ? '' : 'Enter a demo account number of at least 6 digits.';
      case 'Visa':
        return /^4\d{15}$/.test(digits) && this.cardExtraOk() ? '' : 'Use a 16-digit demo Visa starting with 4.';
      case 'Mastercard':
        return /^(5[1-5]\d{14}|2[2-7]\d{14})$/.test(digits) && this.cardExtraOk() ? '' : 'Use a 16-digit demo Mastercard.';
      case 'MPU':
        return /^\d{16}$/.test(digits) && this.cardExtraOk() ? '' : 'Use a 16-digit demo MPU number.';
      case 'PayPal':
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.paypal) ? '' : 'Enter the PayPal email for this demo.';
    }
  }

  private cardExtraOk(): boolean {
    const value = this.form.getRawValue();
    return /^(0[1-9]|1[0-2])\/\d{2}$/.test(value.expiry) && /^\d{3}$/.test(value.cvv);
  }

  private paymentLabel(): string {
    const value = this.form.getRawValue();
    const digits = value.card.replace(/\s/g, '');
    switch (this.method()) {
      case 'Cash':
        return 'Cash on delivery';
      case 'Bank':
        return `${value.bank} •••• ${value.account.slice(-4)}`;
      case 'PayPal':
        return `PayPal ${value.paypal}`;
      default:
        return `${this.method()} •••• ${digits.slice(-4)}`;
    }
  }
}
