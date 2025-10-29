import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable, throwError } from 'rxjs';
import { catchError, retry } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class DataServerService {
  private baseUrl = environment.backendApiUrl;

  constructor(private http: HttpClient) {}

  askAssistant(question: string): Observable<string> {
    const url = `${this.baseUrl}/assistants/ask`;
    return this.http
      .post(url, { message: question }, { responseType: 'text' })
      .pipe(
        retry(1),
        catchError((err) => {
          console.error('askAssistant error', err);
          return throwError(() => err || new Error('askAssistant failed'));
        })
      );
  }

  sendContact(payload: { name: string; email: string; message: string }) {
    const url = `${this.baseUrl}/contacts`;
    return this.http.post(url, payload).pipe(
      catchError((err) => {
        console.error('sendContact error', err);
        return throwError(() => err || new Error('sendContact failed'));
      })
    );
  }
}
