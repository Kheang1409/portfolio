import {
  Component,
  ElementRef,
  OnInit,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProjectService } from '../project.service';

@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './portfolio.component.html',
  styleUrl: './portfolio.component.css'
})
export class PortfolioComponent implements OnInit {
  @ViewChild('slider') slider?: ElementRef;
  @ViewChild('projectsElement') projectsElement?: ElementRef;

  projects: any[] = [];

  backgroundImageLoaded = false;

  constructor(private projectService: ProjectService) {}

  ngOnInit(): void {
    this.backgroundImageLoaded = true;
    this.getProjects();
  }

  getProjects(): void {
    this.projectService.getProjects().subscribe({
      next: (projects) => {
        this.projects = projects;
      },
      error: (err) => console.error(err)
    });
  }

  scrollToProjects(): void {
    this.scrollToElement(this.projectsElement?.nativeElement, 90);
  }

  private scrollToElement(element: HTMLElement | undefined, adjustment : number): void {
    if (!element) return;

    const targetPosition = element.getBoundingClientRect().top + window.pageYOffset - adjustment;
    const startPosition = window.pageYOffset;
    const distance = targetPosition - startPosition;
    const duration = 600;
    let startTime: number | null = null;

    function animation(currentTime: number) {
      if (startTime === null) startTime = currentTime;
      const timeElapsed = currentTime - startTime;
      const run = easeInOutCubic(timeElapsed, startPosition, distance, duration);
      window.scrollTo(0, run);
      if (timeElapsed < duration) requestAnimationFrame(animation);
    }

    function easeInOutCubic(t: number, b: number, c: number, d: number) {
      t /= d / 2;
      if (t < 1) return (c / 2) * t * t * t + b;
      t -= 2;
      return (c / 2) * (t * t * t + 2) + b;
    }

    requestAnimationFrame(animation);
  }
}
