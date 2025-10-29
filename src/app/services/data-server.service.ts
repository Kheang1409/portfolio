import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class DataServerService {
  private baseUrl = environment.backendApiUrl;

  constructor(private http: HttpClient) {}

  askAssistant(question: string): Observable<string> {
    const url = `${this.baseUrl}/assistants/ask`;
    return this.http.post(url, { message: question }, { responseType: 'text' });
  }

  sendContact(payload: { name: string; email: string; message: string }) {
    const url = `${this.baseUrl}/contacts`;
    return this.http.post(url, payload);
  }
}
