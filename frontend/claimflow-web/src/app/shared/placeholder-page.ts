import { Component, input } from '@angular/core';

// Temporary page. The heading comes from the route's data.
@Component({
  selector: 'app-placeholder-page',
  template: `
    <h1>{{ heading() }}</h1>
    <p class="card">This page is not available yet.</p>
  `,
})
export class PlaceholderPage {
  readonly heading = input.required<string>();
}
