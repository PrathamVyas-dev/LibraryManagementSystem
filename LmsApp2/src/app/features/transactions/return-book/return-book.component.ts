import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TransactionService } from '../transaction.service';
import { AuthService } from '../../auth/auth.service';
import { BorrowingTransactionDto, ReturnBookDto } from '../../../models/dtos/transaction-dtos';
import { TransactionDisplayUiModel } from '../../../models/ui-models/transaction-ui-models';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

@Component({
  selector: 'app-return-book',
  templateUrl: './return-book.component.html',
  styleUrls: ['./return-book.component.scss']
})
export class ReturnBookComponent implements OnInit {
  returnForm: FormGroup;
  searchForm: FormGroup;
  returnBookForm: FormGroup;
  
  loading = false;
  loadingTransactions = false;
  submitting = false;
  error: string | null = null;
  success: string | null = null;
  
  transactions: TransactionDisplayUiModel[] = [];
  filteredTransactions: TransactionDisplayUiModel[] = [];
  selectedTransaction: BorrowingTransactionDto | null = null;
  borrowedBooks: TransactionDisplayUiModel[] = [];
  displayedTransactions: TransactionDisplayUiModel[] = [];
  dataSource: { data: TransactionDisplayUiModel[] } = { data: [] };
  
  // Flag to indicate if dates can be changed (admin/librarian only)
  canChangeDate = false;
  today = new Date();
  minDate = new Date(); // Can't return in the past

  constructor(
    private fb: FormBuilder,
    private transactionService: TransactionService,
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar
  ) {
    this.returnForm = this.fb.group({
      transactionID: ['', Validators.required],
      returnDate: [this.today, Validators.required]
    });
    
    this.searchForm = this.fb.group({
      searchTerm: ['']
    });
    
    this.returnBookForm = this.fb.group({
      transactionId: ['', Validators.required],
      returnDate: [this.today, Validators.required],
      notes: ['']
    });
  }

  ngOnInit(): void {
    // Check if user can change dates
    this.canChangeDate = this.transactionService.canChangeDate();
    
    // If user can't change date, disable the date field
    if (!this.canChangeDate) {
      this.returnForm.get('returnDate')?.disable();
      this.returnBookForm.get('returnDate')?.disable();
    }
    
    // Get transaction ID from query parameter if available
    this.route.queryParams.subscribe(params => {
      if (params['transactionId']) {
        const transactionId = +params['transactionId'];
        this.loadTransaction(transactionId);
      } else {
        // Load active transactions that can be returned
        this.loadActiveTransactions();
      }
    });
  }
  
  // Load a specific transaction
  loadTransaction(transactionId: number): void {
    this.loading = true;
    this.transactionService.getById(transactionId).subscribe({
      next: (transaction) => {
        if (transaction.status === 'Returned') {
          this.error = 'This book has already been returned.';
          this.returnForm.get('transactionID')?.setErrors({ 'alreadyReturned': true });
        } else {
          this.selectedTransaction = transaction;
          this.returnBookForm.patchValue({ 
            transactionId: transaction.transactionID
          });
        }
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load transaction: ' + (err.message || 'Unknown error');
        this.loading = false;
      }
    });
  }
  
  // Load all active transactions
  loadActiveTransactions(): void {
    this.loadingTransactions = true;
    
    let activeTransactionsObservable: Observable<BorrowingTransactionDto[]>;
    
    // If regular user, only load their transactions
    if (!this.canChangeDate && this.authService.memberId) {
      activeTransactionsObservable = this.transactionService.getMemberTransactions(this.authService.memberId);
    } else {
      // Admin/librarian can see all transactions
      activeTransactionsObservable = this.transactionService.getAll()
        .pipe(map(transactions => transactions.filter(t => t.status !== 'Returned')));
    }
    
    // Process transactions
    activeTransactionsObservable.pipe(
      catchError(err => {
        this.error = 'Failed to load transactions: ' + (err.message || 'Unknown error');
        return of([]);
      })
    ).subscribe(transactions => {
      this.transactions = transactions as TransactionDisplayUiModel[];
      this.filteredTransactions = [...this.transactions];
      this.loadingTransactions = false;
    });
  }
  
  // Load borrowed books
  loadBorrowedBooks(): void {
    this.loading = true;
    this.error = null;
    
    this.transactionService.getBorrowedBooks().subscribe({
      next: (transactions: BorrowingTransactionDto[]) => {
        // Transform DTOs to UI models
        this.borrowedBooks = transactions
          .filter(t => t.status === 'Borrowed')
          .map(t => this.transactionService.mapToTransactionDisplayUiModel(t));
        
        this.displayedTransactions = [...this.borrowedBooks];
        this.dataSource.data = this.displayedTransactions;
        this.loading = false;
      },
      error: (err: any) => {
        this.error = 'Failed to load borrowed books: ' + (err.message || 'Unknown error');
        this.loading = false;
      }
    });
  }
  
  // Filter borrowed books
  filterBorrowedBooks(term: string): void {
    if (!term.trim()) {
      this.displayedTransactions = [...this.borrowedBooks];
      return;
    }
    
    term = term.toLowerCase().trim();
    
    this.displayedTransactions = this.borrowedBooks.filter(t => 
      (t.bookName && t.bookName.toLowerCase().includes(term)) ||
      (t.memberName && t.memberName.toLowerCase().includes(term)) ||
      (t.transactionID && t.transactionID.toString().includes(term)) ||
      this.getDisplayStatus(t).toLowerCase().includes(term)
    );
  }
  
  // Search transactions
  searchTransactions(): void {
    const term = this.searchForm.get('searchTerm')?.value?.trim();
    this.filterTransactions(term);
  }
  
  // Filter transactions
  filterTransactions(searchTerm: string): void {
    if (!searchTerm) {
      this.filteredTransactions = [...this.transactions];
      return;
    }
    
    const term = searchTerm.toLowerCase().trim();
    
    this.filteredTransactions = this.transactions.filter(t => 
      (t.bookName && t.bookName.toLowerCase().includes(term)) ||
      (t.memberName && t.memberName.toLowerCase().includes(term)) ||
      (t.transactionID && t.transactionID.toString().includes(term)) ||
      this.getDisplayStatus(t).toLowerCase().includes(term)
    );
  }
  
  // Reset search
  resetSearch(): void {
    this.searchForm.reset();
    this.filteredTransactions = [...this.transactions];
  }
  
  // Select a transaction from the list
  selectTransaction(transaction: BorrowingTransactionDto): void {
    this.selectedTransaction = transaction;
    this.returnBookForm.patchValue({ 
      transactionId: transaction.transactionID
    });
  }
  
  // Clear selection and go back to list
  clearSelection(): void {
    this.selectedTransaction = null;
    this.returnBookForm.reset({
      returnDate: this.today
    });
  }
  
  // Combined returnBook method that handles both use cases
  returnBook(transaction?: TransactionDisplayUiModel): void {
    // Case 1: Return a specific transaction (from the displayed transactions list)
    if (transaction) {
      const returnData: ReturnBookDto = {
        transactionID: transaction.transactionID,
        returnDate: new Date().toISOString()
      };
      
      this.loading = true;
      this.transactionService.returnBook(returnData).subscribe({
        next: () => {
          this.snackBar.open('Book returned successfully!', 'Close', { duration: 3000 });
          this.loadBorrowedBooks(); // Refresh the list
          this.loading = false;
        },
        error: (err) => {
          this.error = 'Failed to return book: ' + (err.message || 'Unknown error');
          this.loading = false;
        }
      });
    } 
    // Case 2: Return book from the form submission
    else {
      if (this.returnBookForm.invalid) {
        this.returnBookForm.markAllAsTouched();
        return;
      }
      
      this.submitting = true;
      this.error = null;
      
      // Get form values
      const formValues = this.returnBookForm.getRawValue(); // Include disabled fields
      
      // Create DTO
      const returnDto: ReturnBookDto = {
        transactionID: formValues.transactionId,
        returnDate: this.formatDate(formValues.returnDate)
      };
      
      // Call service
      this.transactionService.returnBook(returnDto).subscribe({
        next: (transaction) => {
          this.success = 'Book returned successfully!';
          
          // Show success message with snackbar
          this.snackBar.open('Book returned successfully!', 'View Transactions', { 
            duration: 1000 
          }).onAction().subscribe(() => {
            this.router.navigate(['/transactions']);
          });          
        },
      });
    }
  }
  
  // Helper to check if a transaction is overdue
  isOverdue(transaction: BorrowingTransactionDto): boolean {
    return this.transactionService.isOverdue(transaction);
  }
  
  // Get CSS class for overdue status
  getOverdueClass(transaction: BorrowingTransactionDto): string {
    return this.isOverdue(transaction) ? 'table-danger' : '';
  }
  
  // Get display status for a transaction
  getDisplayStatus(transaction: TransactionDisplayUiModel): string {
    if (transaction.status === 'Returned') {
      return 'Returned';
    } else if (transaction.isOverdue) {
      return 'Overdue';
    } else {
      return 'Borrowed';
    }
  }
  
  // Format date for display
  formatDate(date: Date | string): string {
    if (!date) return 'N/A';
    
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    
    return dateObj.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
  
  // Calculate due date from borrow date
  calculateDueDate(borrowDate: string): Date {
    return this.transactionService.calculateDueDate(borrowDate);
  }

  // Add a method to properly update form state
  updateFormState(disabled: boolean): void {
    const controls = ['fineAmount', 'fineReason'];
    
    controls.forEach(control => {
      const ctrl = this.returnForm.get(control);
      if (ctrl) {
        disabled ? ctrl.disable() : ctrl.enable();
      }
    });
  }
}
