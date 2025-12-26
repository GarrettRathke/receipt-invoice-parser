import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HelloWorldComponent } from './components/hello-world/hello-world.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HelloWorldComponent],
  template: `
    <app-hello-world></app-hello-world>
    <router-outlet />
  `,
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('hello-world-app');
}

