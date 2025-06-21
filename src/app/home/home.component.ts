import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']  // Add this line to load scoped CSS
})
export class HomeComponent implements OnInit {

  backgroundImageLoaded = false;

  ngOnInit() {
    const img = new Image();
    img.src = '/assets/img/background.jpg';
    img.onload = () => {
      this.backgroundImageLoaded = true;
    };
  }
}
