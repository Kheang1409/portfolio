import { CommonModule } from '@angular/common';
import { Component, HostListener } from '@angular/core';

@Component({
  selector: 'app-to-top',
  imports: [CommonModule],
  templateUrl: './to-top.component.html',
  styleUrl: './to-top.component.css'
})
export class ToTopComponent {


  showBackToTop = false;

  @HostListener('window:scroll', ['$event'])
  onScroll(event: any) {
    if (window.scrollY > 200) {
      this.showBackToTop = true;
    } else {
      this.showBackToTop = false;
    }
  }

  backToTop(): void {
  const scrollDuration = 600;
  const scrollStep = -window.scrollY / (scrollDuration / 15);
  const scrollInterval = setInterval(() => {
    if (window.scrollY !== 0) {
      window.scrollBy(0, scrollStep);
    } else {
      clearInterval(scrollInterval);
    }
  }, 15);
}
}
