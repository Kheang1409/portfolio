import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { DataServerService } from '../services/data-server.service';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './contact.component.html',
  styleUrl: './contact.component.css',
})
export class ContactComponent {
  private dataServer = inject(DataServerService);

  contactForm: FormGroup;
  submitted = false;
  loading = false;
  successMessage = '';
  errorMessage = '';

  constructor() {
    this.contactForm = new FormGroup({
      name: new FormControl('', [Validators.required, Validators.minLength(2)]),
      email: new FormControl('', [Validators.required, Validators.email]),
      message: new FormControl('', [
        Validators.required,
        Validators.minLength(10),
      ]),
    });
  }

  get name() {
    return this.contactForm.get('name');
  }
  get email() {
    return this.contactForm.get('email');
  }
  get message() {
    return this.contactForm.get('message');
  }

  onSubmit(): void {
    console.log('Form submission triggered.');
    this.submitted = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.contactForm.markAllAsTouched();

    if (this.contactForm.invalid) {
      console.warn('Form is invalid:', this.contactForm.value);
      return;
    }

    this.loading = true;

    this.dataServer.sendContact(this.contactForm.value).subscribe({
      next: () => {
        this.successMessage = 'Your message has been sent successfully!';
        this.contactForm.reset();
        this.submitted = false;
      },
      error: () => {
        this.errorMessage = 'Failed to send message.';
      },
      complete: () => (this.loading = false),
    });
  }
}
