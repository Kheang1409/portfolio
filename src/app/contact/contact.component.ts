import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-contact',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './contact.component.html',
  styleUrl: './contact.component.css'
})
export class ContactComponent {
  contactForm: FormGroup;

  constructor() {
    // Initialize the form with controls and validation rules
    this.contactForm = new FormGroup({
      name: new FormControl('', Validators.required),
      email: new FormControl('', [Validators.required, Validators.email]),
      message: new FormControl('', Validators.required)
    });
  }

  ngOnInit(): void { }

  // Handle form submission
  onSubmit(): void {
    if (this.contactForm.valid) {
      console.log('Form submitted successfully:', this.contactForm.value);
      // Add your form submission logic here, such as calling an API to send the data

      // Optionally reset the form after submission
      this.contactForm.reset();
    } else {
      console.log('Form is invalid');
    }
  }

  // Getters to easily access form controls in the template
  get name() {
    return this.contactForm.get('name');
  }

  get email() {
    return this.contactForm.get('email');
  }

  get message() {
    return this.contactForm.get('message');
  }
}
