import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { BlogService } from '../blog.service';

@Component({
  selector: 'app-blogs',
  imports: [CommonModule],
  templateUrl: './blogs.component.html',
  styleUrl: './blogs.component.css'
})
export class BlogsComponent implements OnInit {
  blogs: any[] = []

  @ViewChild('scrollUpButton') scrollUpButton!: ElementRef;
  @ViewChild('scrollDownButton') scrollDownButton!: ElementRef;
  @ViewChild('blogElement') blogElement!: ElementRef;

  constructor(private blogService: BlogService) { }

  ngOnInit(): void {
    this.getBlogs();
  }

  scrollToBlogs() {
    const blogContainer = this.blogElement.nativeElement;
    blogContainer.scrollIntoView({ behavior: 'smooth' });
  }

  getBlogs() {
    this.blogService.getBlogs().subscribe({
      next: (blogs) => {
        this.blogs = blogs;
      },
      error: (error) => {
        console.log(error);
      }
    });
  }
}
