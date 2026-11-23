import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { Message } from 'primeng/message';
import { Api } from '../../core/api';
import { AuthStore } from '../../core/auth.store';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Button, InputText, Password, Message],
  templateUrl: './login.html',
})
export class LoginPage {
  private readonly api = inject(Api);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  readonly error = signal('');
  readonly busy = signal(false);
  readonly form = this.formBuilder.nonNullable.group({
    email: ['thet@minifood.dev', [Validators.required, Validators.email]],
    password: ['Food1234!', Validators.required],
  });

  constructor() {
    if (this.auth.signedIn()) void this.router.navigateByUrl('/restaurants');
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    this.error.set('');
    const { email, password } = this.form.getRawValue();
    this.api.login(email, password).subscribe({
      next: (response) => {
        this.auth.setSession(response.token, response.expiresAtUtc, response.user);
        void this.router.navigateByUrl('/restaurants');
      },
      error: (error: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(error.error?.detail ?? 'Sign-in failed.');
      },
    });
  }
}
