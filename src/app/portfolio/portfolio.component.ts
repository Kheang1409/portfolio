import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild } from '@angular/core';

@Component({
  selector: 'app-portfolio',
  imports: [CommonModule],
  templateUrl: './portfolio.component.html',
  styleUrl: './portfolio.component.css'
})
export class PortfolioComponent {

  @ViewChild('slider') slider?: ElementRef;
  @ViewChild('scrollLeftButton') scrollLeftButton?: ElementRef;
  @ViewChild('scrollRightButton') scrollRightButton?: ElementRef;
  @ViewChild('projectsElement') projectsElement?: ElementRef;

  projects = [
    {
      title: 'Project 1',
      description: 'This is project 1',
      image: 'https://images.unsplash.com/photo-1572177812156-58036aae439c?fm=jpg&q=60&w=3000&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8cHJvamVjdHN8ZW58MHx8MHx8fDA%3D',
    },
    {
      title: 'Project 2',
      description: 'This is project 2',
      image: 'https://images.unsplash.com/photo-1572177812156-58036aae439c?fm=jpg&q=60&w=3000&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8cHJvamVjdHN8ZW58MHx8MHx8fDA%3D',
    },
    {
      title: 'Project 3',
      description: 'This is project 2',
      image: 'https://images.unsplash.com/photo-1572177812156-58036aae439c?fm=jpg&q=60&w=3000&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8cHJvamVjdHN8ZW58MHx8MHx8fDA%3D',
    },
    {
      title: 'Project 4',
      description: 'This is project 2',
      image: 'https://images.unsplash.com/photo-1572177812156-58036aae439c?fm=jpg&q=60&w=3000&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8cHJvamVjdHN8ZW58MHx8MHx8fDA%3D',
    }
  ];

  constructor() { }

  ngOnInit(): void {
    if (this.scrollLeftButton) {
      this.scrollLeftButton.nativeElement.classList.add('hidden');
    }
  }

  scrollToProjects(): void {
    if (this.projectsElement)
      this.projectsElement.nativeElement.scrollIntoView({ behavior: 'smooth' });
  }

  scrollLeft() {
    if (this.slider) {
      this.slider.nativeElement.scrollLeft -= 300;
    }
  }

  scrollRight() {
    if (this.slider) {
      this.slider.nativeElement.scrollLeft += 300;
    }
  }
}
