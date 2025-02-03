import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ProjectService } from '../project.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-portfolio',
  imports: [CommonModule],
  templateUrl: './portfolio.component.html',
  styleUrl: './portfolio.component.css'
})
export class PortfolioComponent implements OnInit {

  @ViewChild('slider') slider?: ElementRef;
  @ViewChild('scrollLeftButton') scrollLeftButton?: ElementRef;
  @ViewChild('scrollRightButton') scrollRightButton?: ElementRef;
  @ViewChild('projectsElement') projectsElement?: ElementRef;

  projects: any[] = [];

  constructor(private projectService: ProjectService) { }

  ngOnInit(): void {
    if (this.scrollLeftButton) {
      this.scrollLeftButton.nativeElement.classList.add('hidden');
    }
    this.getMessages();
  }

  getMessages() {
    this.projectService.getProjects().subscribe({
      next: (projects) => {
        this.projects = projects;
      },
      error: (error) => {
        console.log(error);
      }
    });
  }

  scrollToProjects(): void {
    if (this.projectsElement)
      this.projectsElement.nativeElement.scrollIntoView({ behavior: 'smooth' });
  }
}
