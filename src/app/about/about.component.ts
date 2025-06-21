import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EducationService } from '../education.service';
import { ExperienceService } from '../experience.service';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './about.component.html',
  styleUrls: ['./about.component.css'] // keep this or remove if you don't want CSS file
})
export class AboutComponent implements OnInit {
  @ViewChild('experienceElement') experienceElement?: ElementRef;
  @ViewChild('educationElement') educationElement?: ElementRef;

  experiences: any[] = [];
  educations: any[] = [];
  
  backgroundImageLoaded = false;

  constructor(
    private educationService: EducationService,
    private experienceService: ExperienceService
  ) {}

  ngOnInit(): void {
    const img = new Image();
    img.src = '/assets/img/background.jpg';
    img.onload = () => {
      this.backgroundImageLoaded = true;
    };

    this.loadExperience();
    this.loadEducation();
  }

  loadExperience(): void {
    this.experienceService.getExperiences().subscribe({
      next: (data) => (this.experiences = data),
      error: (err) => console.error(err),
    });
  }

  loadEducation(): void {
    this.educationService.getEducations().subscribe({
      next: (data) => (this.educations = data),
      error: (err) => console.error(err),
    });
  }

  downloadResume(): void {
    const link = document.createElement('a');
    link.href = 'assets/resume/Kai_Taing_Resume.pdf';
    link.download = 'Kai_Taing_Resume.pdf';
    link.click();
  }

  scrollToElement(element: HTMLElement | undefined, adjustment: number) {
    if (!element) return;

    const targetPosition = element.getBoundingClientRect().top + window.pageYOffset - adjustment;
    const startPosition = window.pageYOffset;
    const distance = targetPosition - startPosition;
    const duration = 600; // ms
    let startTime: number | null = null;

    function animation(currentTime: number) {
      if (startTime === null) startTime = currentTime;
      const timeElapsed = currentTime - startTime;
      const run = ease(timeElapsed, startPosition, distance, duration);
      window.scrollTo(0, run);
      if (timeElapsed < duration) {
        requestAnimationFrame(animation);
      }
    }

    function ease(t: number, b: number, c: number, d: number) {
      // easeInOutCubic easing function
      t /= d / 2;
      if (t < 1) return (c / 2) * t * t * t + b;
      t -= 2;
      return (c / 2) * (t * t * t + 2) + b;
    }

    requestAnimationFrame(animation);
  }

  scrollToExperience(): void {
    this.scrollToElement(this.experienceElement?.nativeElement, 25);
  }

  scrollToEducation(): void {
    this.scrollToElement(this.educationElement?.nativeElement, 0);
  }
}
