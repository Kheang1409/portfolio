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
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}
