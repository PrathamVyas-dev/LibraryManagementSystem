import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { 
  BorrowingTransactionDto, 
  BorrowBookDto, 
  ReturnBookDto, 
  UpdateBorrowingTransactionDto 
} from '../../models/dtos/transaction-dtos';
import { 
  TransactionSummaryUiModel, 
  MemberBorrowingStatusUiModel,
  TransactionDisplayUiModel,
  TransactionFilterUiModel
} from '../../models/ui-models/transaction-ui-models';
import { AuthService } from '../auth/auth.service';

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  private apiBaseUrl = environment.apiBaseUrl;
  private endpoints = environment.apiEndpoints.borrowing;
  
  // Default borrowing period in days
  private defaultBorrowingPeriod = 14;
  
  // N/A date marker from backend
  private NA_DATE = '1000-01-01';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  // Get all transactions with optional filtering
  getAll(params?: any): Observable<BorrowingTransactionDto[]> {
    let httpParams = new HttpParams();
    
    if (params) {
      Object.keys(params).forEach(key => {
        if (params[key] !== null && params[key] !== undefined) {
          httpParams = httpParams.set(key, params[key].toString());
        }
      });
    }
    
    return this.http.get<BorrowingTransactionDto[]>(`${this.apiBaseUrl}${this.endpoints.getAll}`, { params: httpParams })
      .pipe(
        map(transactions => this.processTransactionsForDisplay(transactions)),
        catchError(error => this.handleErrorInternal(error))
      );
  }

  // Add the missing getAllTransactions method
  getAllTransactions(filter?: TransactionFilterUiModel): Observable<BorrowingTransactionDto[]> {
    let params = new HttpParams();
    
    if (filter) {
      Object.keys(filter).forEach(key => {
        const value = filter[key as keyof TransactionFilterUiModel];
        if (value !== undefined && value !== null && value !== '') {
          params = params.set(key, value.toString());
        }
      });
    }
    
    return this.http.get<BorrowingTransactionDto[]>(`${this.apiBaseUrl}${this.endpoints.getAll}`, { params })
      .pipe(catchError(this.handleError));
  }

  // Get a specific transaction by ID
  getById(id: number): Observable<BorrowingTransactionDto> {
    return this.http.get<BorrowingTransactionDto>(`${this.apiBaseUrl}${this.endpoints.getById}${id}`)
      .pipe(catchError(error => this.handleErrorInternal(error)));
  }

  // Calculate due date (14 days after borrow date)
  calculateDueDate(borrowDate: string): Date {
    const borrowDateObj = new Date(borrowDate);
    const dueDate = new Date(borrowDateObj);
    dueDate.setDate(dueDate.getDate() + 14); // 14-day loan period
    return dueDate;
  }

  // Add the missing method
  getTransaction(id: number): Observable<BorrowingTransactionDto> {
    return this.http.get<BorrowingTransactionDto>(`${this.apiBaseUrl}/api/Borrowing/${id}`)
      .pipe(catchError(error => this.handleErrorInternal(error)));
  }

  // Create a new borrowing transaction
  borrowBook(borrowDto: BorrowBookDto): Observable<BorrowingTransactionDto> {
    console.log('Borrowing book with data:', borrowDto);
    
    // For users, enforce today's date
    if (!this.canChangeDate()) {
      borrowDto.borrowDate = this.formatDate(new Date());
    }
    
    return this.http.post<BorrowingTransactionDto>(`${this.apiBaseUrl}${this.endpoints.borrow}`, borrowDto)
      .pipe(
        tap(response => {
          console.log('Book borrowed successfully:', response);
        }),
        catchError(error => {
          console.error('Error borrowing book:', error);
          return throwError(() => new Error(`Failed to borrow book: ${error.message || 'Unknown error'}`));
        })
      );
  }

  // Return a borrowed book
  returnBook(returnDto: ReturnBookDto): Observable<BorrowingTransactionDto> {
    // For users, enforce today's date
    if (!this.canChangeDate()) {
      returnDto.returnDate = this.formatDate(new Date());
    }
    
    return this.http.post<BorrowingTransactionDto>(`${this.apiBaseUrl}${this.endpoints.return}`, returnDto)
      .pipe(catchError(error => this.handleErrorInternal(error)));
  }

  // Get all overdue transactions
  getOverdue(): Observable<BorrowingTransactionDto[]> {
    return this.http.get<BorrowingTransactionDto[]>(`${this.apiBaseUrl}${this.endpoints.overdue}`)
      .pipe(
        map(transactions => this.processTransactionsForDisplay(transactions)),
        catchError(error => this.handleErrorInternal(error))
      );
  }

  // Add or modify the getOverdueTransactions method to ensure it fetches overdue transactions
  getOverdueTransactions(): Observable<BorrowingTransactionDto[]> {
    return this.http.get<BorrowingTransactionDto[]>(`${this.apiBaseUrl}${this.endpoints.overdue}`)
      .pipe(
        catchError(this.handleError),
        tap(transactions => {
          // Ensure all transactions are marked as overdue for consistency
          transactions.forEach(t => {
            if (!t.status || t.status !== 'Overdue') {
              t.status = 'Overdue';
            }
          });
        })
      );
  }

  // Ensure the getTransactions method correctly handles the status filter for overdue transactions
  getTransactions(filter?: any): Observable<BorrowingTransactionDto[]> {
    let params = new HttpParams();
    
    if (filter) {
      Object.keys(filter).forEach(key => {
        const value = filter[key];
        if (value !== undefined && value !== null && value !== '') {
          params = params.set(key, value.toString());
        }
      });
    }
    
    return this.http.get<BorrowingTransactionDto[]>(`${this.apiBaseUrl}${this.endpoints.getAll}`, { params })
      .pipe(
        catchError(this.handleError),
        map(transactions => {
          return transactions;
        })
      );
  }

  // // Helper method to update transaction status based on due date
  // private updateOverdueStatus(transactions: BorrowingTransactionDto[]): void {
  //   const today = new Date();
    
  //   transactions.forEach(transaction => {
  //     // If the transaction is still borrowed and past due date, mark as overdue
  //     if (transaction.status === 'Borrowed' && !transaction.returnDate) {
  //       const dueDate = this.calculateDueDate(transaction.borrowDate);
  //       if (dueDate < today) {
  //         transaction.status = 'Overdue';
  //       }
  //     }
  //   });
  // }

  // Get transactions for a specific member
  getMemberTransactions(memberId: number): Observable<BorrowingTransactionDto[]> {
    return this.http.get<BorrowingTransactionDto[]>(`${this.apiBaseUrl}${this.endpoints.memberHistory}${memberId}`)
      .pipe(
        map(transactions => this.processTransactionsForDisplay(transactions)),
        catchError(error => this.handleErrorInternal(error))
      );
  }

  // Calculate transaction statistics
  getTransactionSummary(): Observable<TransactionSummaryUiModel> {
    return this.getAll().pipe(
      map(transactions => {
        const activeBorrowings = transactions.filter(t => t.status !== 'Returned').length;
        const overdueItems = transactions.filter(t => this.isOverdue(t)).length;
        const returnedItems = transactions.filter(t => t.status === 'Returned').length;
        
        return {
          totalTransactions: transactions.length,
          activeBorrowings,
          overdueItems,
          returnedItems
        };
      }),
      catchError(error => {
        console.error('Error getting transaction summary:', error);
        return of({
          totalTransactions: 0,
          activeBorrowings: 0,
          overdueItems: 0,
          returnedItems: 0
        });
      })
    );
  }

  // Check if a member can borrow books
  checkMemberBorrowingStatus(memberId: number, memberName: string = ''): Observable<MemberBorrowingStatusUiModel> {
    return this.getMemberTransactions(memberId).pipe(
      map(transactions => {
        const currentBorrowings = transactions.filter(t => t.status !== 'Returned').length;
        const hasOverdueItems = transactions.some(t => this.isOverdue(t));
        
        // Define business rules for borrowing eligibility
        const maxAllowedBorrowings = 5; // Example limit
        const canBorrow = currentBorrowings < maxAllowedBorrowings && !hasOverdueItems;
        
        let reasonCannotBorrow = '';
        if (currentBorrowings >= maxAllowedBorrowings) {
          reasonCannotBorrow = 'Maximum borrowing limit reached';
        } else if (hasOverdueItems) {
          reasonCannotBorrow = 'Has overdue items';
        }
        
        return {
          memberId,
          memberName: memberName,
          currentBorrowings,
          maxAllowedBorrowings,
          hasOverdueItems,
          hasOutstandingFines: false, // This would need to be determined from FineService
          canBorrow,
          reasonCannotBorrow: canBorrow ? undefined : reasonCannotBorrow
        };
      }),
      catchError(error => {
        console.error('Error checking member borrowing status:', error);
        return of({
          memberId,
          memberName: memberName,
          currentBorrowings: 0,
          maxAllowedBorrowings: 5,
          hasOverdueItems: false,
          hasOutstandingFines: false,
          canBorrow: false,
          reasonCannotBorrow: 'Error checking borrowing status'
        });
      })
    );
  }

  // Helper method to check if a transaction is overdue
  isOverdue(transaction: BorrowingTransactionDto): boolean {
    if (transaction.status === 'Returned') return false;
    
    const dueDate = this.calculateDueDate(transaction.borrowDate);
    const today = new Date();
    
    // Strip time for date comparison
    dueDate.setHours(0, 0, 0, 0);
    today.setHours(0, 0, 0, 0);
    
    return dueDate < today;
  }

  // Calculate days overdue for a transaction
  calculateOverdueDays(transaction: BorrowingTransactionDto): number {
    if (transaction.status === 'Returned' || !this.isOverdue(transaction)) {
      return 0;
    }
    
    const today = new Date();
    const dueDate = this.calculateDueDate(transaction.borrowDate);
    
    // Strip time for accurate day calculation
    dueDate.setHours(0, 0, 0, 0);
    today.setHours(0, 0, 0, 0);
    
    const diffTime = Math.abs(today.getTime() - dueDate.getTime());
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  // Calculate potential fine for an overdue transaction
  calculatePotentialFine(transaction: BorrowingTransactionDto): number {
    const overdueDays = this.calculateOverdueDays(transaction);
    
    if (overdueDays <= 0) {
      return 0;
    }
    
    // Base fine: ₹10 per day, capped at ₹300
    const baseFine = Math.min(overdueDays * 10, 300);
    
    // Additional suspension fee of ₹200 if more than 30 days overdue
    const suspensionFee = overdueDays > 30 ? 200 : 0;
    
    return baseFine + suspensionFee;
  }

  // Helper method to determine if a return date is NA
  isReturnDateNA(returnDateStr: string): boolean {
    return returnDateStr === this.NA_DATE;
  }

  // Update the formatDate method to accept both string and Date
  formatDate(date: string | Date): string {
    if (!date) return 'N/A';
    
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    
    return dateObj.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  // Check if current user can change dates (admin/librarian only)
  canChangeDate(): boolean {
    return this.authService.hasRole('Admin') || this.authService.hasRole('Librarian');
  }
  
  // Add missing deleteTransaction method
  deleteTransaction(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}${this.endpoints.getById}${id}`)
      .pipe(catchError(error => this.handleErrorInternal(error)));
  }

  // Standardize transaction status to ensure it's only Borrowed, Returned, or Overdue
  determineStatus(transaction: BorrowingTransactionDto): string {
    if (transaction.status === 'Returned') {
      return 'Returned';
    }
    
    return this.isOverdue(transaction) ? 'Overdue' : 'Borrowed';
  }

  // Update mapper to properly handle the DTO structure
  mapToTransactionDisplayUiModel(
    transactionDto: BorrowingTransactionDto, 
    memberName?: string
  ): TransactionDisplayUiModel {
    // Calculate due date (14 days from borrow date)
    const borrowDate = new Date(transactionDto.borrowDate);
    const dueDate = new Date(borrowDate);
    dueDate.setDate(dueDate.getDate() + 14);
    
    // Determine if overdue
    const now = new Date();
    const returnDate = transactionDto.returnDate && transactionDto.returnDate !== '0001-01-01' 
      ? new Date(transactionDto.returnDate) 
      : null;
    const isOverdue = !returnDate && now > dueDate;
    
    // Calculate days overdue
    let daysOverdue = 0;
    if (isOverdue) {
      daysOverdue = Math.ceil((now.getTime() - dueDate.getTime()) / (1000 * 60 * 60 * 24));
    } else if (returnDate && returnDate > dueDate) {
      daysOverdue = Math.ceil((returnDate.getTime() - dueDate.getTime()) / (1000 * 60 * 60 * 24));
    }

    const model: TransactionDisplayUiModel = {
      // Core DTO properties
      transactionID: transactionDto.transactionID,
      bookID: transactionDto.bookID,
      bookName: transactionDto.bookName,
      memberID: transactionDto.memberID,
      borrowDate: transactionDto.borrowDate,
      returnDate: transactionDto.returnDate,
      status: transactionDto.status,
      
      // UI aliases for compatibility
      id: transactionDto.transactionID,
      bookId: transactionDto.bookID,
      bookTitle: transactionDto.bookName,
      memberId: transactionDto.memberID,
      memberName: memberName,
      
      // Calculated fields
      dueDate: dueDate.toISOString().split('T')[0],
      isOverdue: isOverdue,
      isReturned: transactionDto.status === 'Returned',
      daysOverdue: daysOverdue,
      
      // Formatted fields
      formattedBorrowDate: this.formatDate(transactionDto.borrowDate),
      formattedDueDate: this.formatDate(dueDate.toISOString().split('T')[0]),
      formattedReturnDate: returnDate ? this.formatDate(transactionDto.returnDate) : undefined,
      
      // Status color for UI
      statusColor: this.getStatusColor(transactionDto.status, isOverdue)
    };
    
    return model;
  }

  private getStatusColor(status: string, isOverdue: boolean): string {
    if (isOverdue) return 'warn';
    switch (status) {
      case 'Active': return 'primary';
      case 'Returned': return 'accent';
      default: return '';
    }
  }

  // Keep the DTO structure for backend communication
  transformTransactions(transactions: BorrowingTransactionDto[]): BorrowingTransactionDto[] {
    return [...transactions];
  }

  // Add separate method for UI transformations
  transformToUiModels(transactions: BorrowingTransactionDto[]): TransactionDisplayUiModel[] {
    return transactions.map(transaction => this.mapToTransactionDisplayUiModel(transaction));
  }

  // Process transactions for display, adding UI-specific properties
  private processTransactionsForDisplay(transactions: BorrowingTransactionDto[]): BorrowingTransactionDto[] {
    return transactions.map(transaction => {
      const isOverdue = this.isOverdue(transaction);
      const daysOverdue = this.calculateOverdueDays(transaction);
      const estimatedFine = this.calculatePotentialFine(transaction);
      const status = this.determineStatus(transaction);
      
      return {
        ...transaction,
        overdueStatus: isOverdue,
        daysOverdue,
        estimatedFine,
        status: status // Ensure status is standardized
      } as TransactionDisplayUiModel;
    });
  }

  // Add this method to get borrowed books (active transactions)
  getBorrowedBooks(): Observable<BorrowingTransactionDto[]> {
    // Get all transactions first
    return this.getAll().pipe(
      map(transactions => {
        // Filter to only include active/borrowed transactions
        return transactions.filter(t => t.status === 'Active' || t.status === 'Borrowed');
      }),
      catchError(error => this.handleErrorInternal(error))
    );
  }

  // Add the handleError method if it doesn't exist
  private handleError(error: any) {
    let errorMessage = 'An unknown error occurred';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else if (error.status) {
      // Server-side error
      switch (error.status) {
        case 400:
          errorMessage = error.error?.message || 'Bad request';
          break;
        case 401:
          errorMessage = 'Unauthorized. Please log in again.';
          break;
        case 403:
          errorMessage = 'You do not have permission to perform this action.';
          break;
        case 404:
          errorMessage = 'The requested resource was not found.';
          break;
        case 500:
          errorMessage = 'Server error. Please try again later.';
          break;
        default:
          errorMessage = `Error ${error.status}: ${error.error?.message || error.statusText}`;
      }
    }
    
    console.error('TransactionService Error:', errorMessage, error);
    return throwError(() => ({ message: errorMessage, original: error }));
  }

  // Renamed to handleErrorInternal to avoid naming conflicts
  private handleErrorInternal(error: any) {
    let errorMessage = 'An unknown error occurred';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else if (error.status) {
      // Server-side error
      switch (error.status) {
        case 400:
          errorMessage = error.error?.message || 'Bad request';
          break;
        case 401:
          errorMessage = 'Unauthorized. Please log in again.';
          break;
        case 403:
          errorMessage = 'You do not have permission to perform this action.';
          break;
        case 404:
          errorMessage = 'The requested resource was not found.';
          break;
        case 500:
          errorMessage = 'Server error. Please try again later.';
          break;
        default:
          errorMessage = `Error ${error.status}: ${error.error?.message || error.statusText}`;
      }
    }
    
    console.error('TransactionService Error:', errorMessage, error);
    return throwError(() => ({ message: errorMessage, original: error }));
  }
}