import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild } from '@angular/core';

@Component({
  selector: 'app-blogs',
  imports: [CommonModule],
  templateUrl: './blogs.component.html',
  styleUrl: './blogs.component.css'
})
export class BlogsComponent {
  blogs = [
    { title: 'Understanding Angular Directives', description: 'Learn about Angular directives and how to use them effectively.', link: '#' },
    { title: 'Mastering RxJS in Angular', description: 'Explore RxJS operators to handle asynchronous operations in Angular.', link: '#' },
    { title: 'Best Practices for API Integration', description: 'Discover tips and tricks for seamless API integration.', link: '#' },
    // Add more blog entries here
    { title: 'Understanding Angular Directives', description: 'Learn about Angular directives and how to use them effectively.', link: '#' },
    { title: 'Mastering RxJS in Angular', description: 'Explore RxJS operators to handle asynchronous operations in Angular.', link: '#' },
    { title: 'Best Practices for API Integration', description: 'Discover tips and tricks for seamless API integration.', link: '#' },
    { title: 'Understanding Angular Directives', description: 'Learn about Angular directives and how to use them effectively.', link: '#' },
    { title: 'Mastering RxJS in Angular', description: 'Explore RxJS operators to handle asynchronous operations in Angular.', link: '#' },
    { title: 'Best Practices for API Integration', description: 'Discover tips and tricks for seamless API integration.', link: '#' },
  ];

  @ViewChild('scrollUpButton') scrollUpButton!: ElementRef;
  @ViewChild('scrollDownButton') scrollDownButton!: ElementRef;
  @ViewChild('blogElement') blogElement!: ElementRef;

  scrollToBlogs() {
    const blogContainer = this.blogElement.nativeElement;
    blogContainer.scrollIntoView({ behavior: 'smooth' });
  }
}
