import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { catchError, Observable, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class BlogService {
  private apiUrl = `${environment.apiUrl}/blogs.json`;

  constructor(private http: HttpClient) { }

  getBlogs(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl).pipe(
      catchError(this.handleError)
    );
  }

  private handleError(error: HttpErrorResponse) {
    let errorMessage = '';

    if (error.error instanceof ErrorEvent) {
      errorMessage = `Client-side error: ${error.error.message}`;
    } else {
      switch (error.status) {
        case 404:
          errorMessage = `Resource not found (404).`;
          break;
        case 500:
          errorMessage = `Server error (500). Please try again later.`;
          break;
        default:
          errorMessage = `Backend returned code ${error.status}, body was: ${error.error}`;
      }
    }
    return throwError(() => new Error(errorMessage || 'Something went wrong with the API! Please try again later.'));
  }
}
