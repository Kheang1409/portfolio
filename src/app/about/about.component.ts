import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Experience } from '../experience';
import { Education } from '../education';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-about',
  imports: [CommonModule],
  templateUrl: './about.component.html',
  styleUrl: './about.component.css'
})
export class AboutComponent implements OnInit {
  @ViewChild('experienceElement') experienceElement?: ElementRef;
  @ViewChild('educationElement') educationElement?: ElementRef;


  experiences: Experience[] = [
    new Experience('1', 'Senior Core Banking Officer', 'Sahakrinpheap Microfinance PLC', 'Developed and maintained software applications for core banking systems, including windows and web platforms.'),
    new Experience('2', 'Web Developer', 'Pathmazing Inc.', 'Implemented Odoo ERP Web Portal and Web API for enterprise clients using Python (Odoo framework) and RESTful APIs.'),
    new Experience('3', 'Web Developer', 'Anakut Digital Solution Co. Ltd.', 'Developed a Point of Sale (POS) web application system using PHP (CodeIgniter), providing businesses with streamlined inventory and sales management tools tailored to their needs.')
  ];

  educations: Education[] = [
    new Education('1', 'Master of Science in Computer Science', 'Maharishi International University', 'Pursuing a Master\'s degree in Computer Science, focusing on advanced topics in software development, data science, and artificial intelligence.'),
    new Education('2', 'Bachelor of Science in Computer Science', 'Royal University of Phnom Penh', 'Completed a Bachelor\'s degree in Computer Science, gaining a solid foundation in programming languages, data structures, algorithms, and software engineering principles.')
  ];

  ngOnInit(): void {

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
