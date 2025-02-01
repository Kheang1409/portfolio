import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';

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

  projects = [
    {
      title: 'Web Application Development',
      description: 'A modern web application developed with Angular and .NET Core to manage tasks efficiently.',
      image: 'https://cmsv2-assets.apptegy.net/uploads/10914/file/1041385/27209043-719e-4d60-add5-c204af747480.jpeg',
    },
    {
      title: 'E-Commerce Platform',
      description: 'A fully-fledged e-commerce platform with payment gateway integration, real-time updates, and product management.',
      image: 'https://www.iwdagency.com/cdn/shop/articles/eCommerce_Platform_Pic_2000x.jpg?v=1698957713',
    },
    {
      title: 'Mobile App Design',
      description: 'UI/UX design for a mobile app focused on improving user experience and interactivity.',
      image: 'https://img.freepik.com/free-vector/various-screens-violet-public-transport-mobile-app_23-2148704862.jpg',
    },
    {
      title: 'Machine Learning Model',
      description: 'A machine learning model for classifying images based on different parameters, trained using TensorFlow.',
      image: 'https://img.freepik.com/free-vector/ai-technology-brain-background-vector-digital-transformation-concept_53876-117812.jpg?semt=ais_hybrid',
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
