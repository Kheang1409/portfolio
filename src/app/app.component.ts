import { Component, HostListener } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './header/header.component';
import { ToTopComponent } from './to-top/to-top.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent, ToTopComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'portfolio';
}
