import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './header/header.component';
import { ToTopComponent } from './to-top/to-top.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent, ToTopComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'portfolio';
}
