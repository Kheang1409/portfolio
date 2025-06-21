import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { BlogService } from '../blog.service';

@Component({
  selector: 'app-blogs',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './blogs.component.html',
  styleUrl: './blogs.component.css'
})
export class BlogsComponent implements OnInit {
  blogs: any[] = [];
  paginatedBlogs: any[] = [];
  currentPage: number = 1;
  itemsPerPage: number = 3;
  totalPages: number = 1;

  @ViewChild('blogElement') blogElement!: ElementRef;

  backgroundImageLoaded = false;

  constructor(private blogService: BlogService) {}

  ngOnInit(): void {
    this.backgroundImageLoaded = true;
    this.getBlogs();
  }

  getBlogs() {
    this.blogService.getBlogs().subscribe({
      next: (blogs) => {
        this.blogs = blogs;
        this.totalPages = Math.ceil(blogs.length / this.itemsPerPage);
        this.updatePagination();
      },
      error: (err) => console.error(err),
    });
  }

  updatePagination() {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    const end = start + this.itemsPerPage;
    this.paginatedBlogs = this.blogs.slice(start, end);
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.updatePagination();
    this.scrollToBlogs();
  }

  scrollToBlogs() {
    const blogContainer = this.blogElement.nativeElement;
    const offset = 90;
    const target = blogContainer.getBoundingClientRect().top + window.pageYOffset - offset;
    const start = window.pageYOffset;
    const distance = target - start;
    const duration = 600;
    let startTime: number | null = null;

    function animation(currentTime: number) {
      if (startTime === null) startTime = currentTime;
      const timeElapsed = currentTime - startTime;
      const run = easeInOutCubic(timeElapsed, start, distance, duration);
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