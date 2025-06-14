import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import {
  BookDetailsDto,
  CreateBookDto,
  UpdateBookDto
} from '../../models/dtos/book-dtos';
import { PaginatedResponseUiModel } from '../../models/ui-models/transaction-ui-models';
import { BookSearchUiModel } from '../../models/ui-models/book-ui-models';
import { environment } from '../../../environments/environment';

export interface BookPaginationParams {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: string;
  title?: string;
  author?: string;
  genre?: string;
  isbn?: string;
  availableCopiesGreaterThanZero?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class BookService {
  private apiUrl = `${environment.apiBaseUrl}/api/Books`;

  constructor(private http: HttpClient) {}

  getBooks(): Observable<BookDetailsDto[]> {
    return this.http.get<BookDetailsDto[]>(this.apiUrl)
      .pipe(catchError(this.handleError));
  }

  getBook(id: number): Observable<BookDetailsDto> {
    return this.http.get<BookDetailsDto>(`${this.apiUrl}/${id}`)
      .pipe(
        catchError(error => {
          console.error(`Error fetching book with ID ${id}:`, error);
          return throwError(() => new Error(`Failed to fetch book: ${error.message || 'Unknown error'}`));
        })
      );
  }

  addBook(book: CreateBookDto): Observable<BookDetailsDto> {
    return this.http.post<BookDetailsDto>(this.apiUrl, book)
      .pipe(catchError(this.handleError));
  }

  updateBook(book: UpdateBookDto): Observable<BookDetailsDto> {
    return this.http.put<BookDetailsDto>(this.apiUrl, book)
      .pipe(catchError(this.handleError));
  }

  deleteBook(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`)
      .pipe(catchError(this.handleError));
  }

  searchBooks(params: BookSearchUiModel = {}): Observable<BookDetailsDto[]> {
    let httpParams = new HttpParams();
    
    // Standard DTO parameters
    if (params.title) httpParams = httpParams.set('title', params.title);
    if (params.author) httpParams = httpParams.set('author', params.author);
    if (params.genre) httpParams = httpParams.set('genre', params.genre);
    if (params.isbn) httpParams = httpParams.set('isbn', params.isbn);
    if (params.availableCopiesGreaterThanZero !== undefined) {
      httpParams = httpParams.set('availableCopiesGreaterThanZero', params.availableCopiesGreaterThanZero.toString());
    }
    
    // Extended UI-specific parameters 
    if (params.searchTerm) httpParams = httpParams.set('searchTerm', params.searchTerm);
    if (params.yearPublished) httpParams = httpParams.set('yearPublished', params.yearPublished.toString());
    if (params.availableOnly !== undefined) {
      httpParams = httpParams.set('availableOnly', params.availableOnly.toString());
    }
    
    return this.http.get<BookDetailsDto[]>(`${this.apiUrl}/search`, { params: httpParams })
      .pipe(catchError(this.handleError));
  }

  getBooksWithPagination(params: BookPaginationParams): Observable<PaginatedResponseUiModel<BookDetailsDto>> {
    let httpParams = new HttpParams();
    
    // Add pagination parameters
    if (params.pageNumber) httpParams = httpParams.set('pageNumber', params.pageNumber.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDirection) httpParams = httpParams.set('sortDirection', params.sortDirection);
    
    // Add search parameters
    if (params.title) httpParams = httpParams.set('title', params.title);
    if (params.author) httpParams = httpParams.set('author', params.author);
    if (params.genre) httpParams = httpParams.set('genre', params.genre);
    if (params.isbn) httpParams = httpParams.set('isbn', params.isbn);
    if (params.availableCopiesGreaterThanZero !== undefined) {
      httpParams = httpParams.set('availableCopiesGreaterThanZero', params.availableCopiesGreaterThanZero.toString());
    }
    
    return this.http.get<PaginatedResponseUiModel<BookDetailsDto>>(`${this.apiUrl}/paginated`, { params: httpParams })
      .pipe(catchError(this.handleError));
  }

  /**
   * Search books by title on the client-side
   * @param searchTerm The title search term
   * @returns Observable of filtered books
   */
  searchBooksByTitleClientSide(searchTerm: string): Observable<BookDetailsDto[]> {
    if (!searchTerm || searchTerm.trim() === '') {
      return this.getBooks();
    }
    
    // Get all books and filter on client-side by title
    return this.getBooks().pipe(
      map(books => books.filter(book => 
        book.title.toLowerCase().includes(searchTerm.toLowerCase())
      ))
    );
  }

  private handleError(error: any) {
    // Optionally log error here
    return throwError(() => error);
  }
}
