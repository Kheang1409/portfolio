import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MarkdownModule } from 'ngx-markdown';

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule, MarkdownModule],
  templateUrl: './chatbot.component.html',
  styleUrls: ['./chatbot.component.css']
})
export class ChatbotComponent {
  isOpen = false;
  userInput = '';
  loading = false;
  messages: { sender: 'user' | 'bot'; text: string }[] = [];

  constructor(private http: HttpClient) {}

  toggleChat() {
    this.isOpen = !this.isOpen;
  }

  sendMessage() {
    if (!this.userInput.trim()) return;

    const question = this.userInput;
    this.messages.push({ sender: 'user', text: question });
    this.userInput = '';
    this.loading = true;

    this.http.post('https://contactformapi-j6hw.onrender.com/api/assistants/ask', { question }, { responseType: 'text' })
      .subscribe({
        next: (res) => {
          this.loading = false;
          this.messages.push({ sender: 'bot', text: res || 'No response.' });
        },
        error: (err) => {
          this.loading = false;
          console.error(err);
          this.messages.push({ sender: 'bot', text: 'Error getting response.' });
        }
      });
  }
}