import { Injectable, computed, signal } from '@angular/core';
import { SessionUser } from './models';

const storageKey = 'minifood.session';

interface SavedSession {
  token: string;
  expiresAtUtc: string;
  user: SessionUser;
}

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly session = signal<SavedSession | null>(this.read());
  readonly user = computed(() => this.session()?.user ?? null);
  readonly token = computed(() => (this.isExpired() ? null : (this.session()?.token ?? null)));
  readonly signedIn = computed(() => this.token() !== null);

  constructor() {
    if (this.isExpired()) this.clear();
  }

  setSession(token: string, expiresAtUtc: string, user: SessionUser): void {
    const next = { token, expiresAtUtc, user };
    sessionStorage.setItem(storageKey, JSON.stringify(next));
    this.session.set(next);
  }

  clear(): void {
    sessionStorage.removeItem(storageKey);
    this.session.set(null);
  }

  private isExpired(): boolean {
    const current = this.session();
    if (!current) return true;
    return Date.parse(current.expiresAtUtc) <= Date.now();
  }

  private read(): SavedSession | null {
    const raw = sessionStorage.getItem(storageKey);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as SavedSession;
    } catch {
      return null;
    }
  }
}
