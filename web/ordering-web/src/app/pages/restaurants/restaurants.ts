import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Tag } from 'primeng/tag';
import { Api } from '../../core/api';
import { Restaurant } from '../../core/models';

@Component({
  selector: 'app-restaurants',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Tag],
  templateUrl: './restaurants.html',
})
export class RestaurantsPage {
  private readonly api = inject(Api);
  readonly restaurants = signal<Restaurant[]>([]);
  readonly query = signal('');
  readonly error = signal('');

  constructor() {
    this.api.restaurants().subscribe({
      next: (rows) => this.restaurants.set(rows),
      error: () => this.error.set('The menu board is not reachable. Is the gateway on port 5080?'),
    });
  }

  visible(): Restaurant[] {
    const term = this.query().trim().toLowerCase();
    if (!term) return this.restaurants();
    return this.restaurants().filter((place) => `${place.name} ${place.cuisine} ${place.address}`.toLowerCase().includes(term));
  }
}
