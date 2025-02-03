import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EducationService } from '../education.service';
import { ExperienceService } from '../experience.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-about',
  imports: [CommonModule],
  templateUrl: './about.component.html',
  styleUrl: './about.component.css'
})
export class AboutComponent implements OnInit {
  @ViewChild('experienceElement') experienceElement?: ElementRef;
  @ViewChild('educationElement') educationElement?: ElementRef;


  experiences: any[] = [];

  educations: any[] = [];

  constructor(private educationService: EducationService, private experienceService: ExperienceService) { }

  ngOnInit(): void {
    this.getExperience();
    this.getEducation();
  }

  getEducation() {
    this.educationService.getEducations().subscribe({
      next: (educations) => {
        this.educations = educations;
      },
      error: (error) => {
        console.log(error);
      }
    });
  }

  getExperience() {
    this.experienceService.getExperiences().subscribe({
      next: (experiences) => {
        this.experiences = experiences;
      },
      error: (error) => {
        console.log(error);
      }
    });
  }

  downloadResume(): void {
    const resumeUrl = 'assets/resume/Kai_Taing_Resume.pdf';
    const link = document.createElement('a');
    link.href = resumeUrl;
    link.download = 'Kai_Taing_Resume.pdf';

    link.click();
  }

  scrollToExperience(): void {
    if (this.experienceElement) {
      this.experienceElement.nativeElement.scrollIntoView({ behavior: 'smooth' });
    }
  }

  scrollToEducation(): void {
    if (this.educationElement) {
      this.educationElement.nativeElement.scrollIntoView({ behavior: 'smooth' });
    }
  }
}
