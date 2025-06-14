import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TransactionService } from '../transaction.service';
import { BorrowingTransactionDto } from '../../../models/dtos/transaction-dtos';
import { TransactionDisplayUiModel, TransactionFilterUiModel } from '../../../models/ui-models/transaction-ui-models';
import { AuthService } from '../../auth/auth.service';
import { ConfirmationDialogService } from '../../../core/confirmation-dialog.service';

@Component({
  selector: 'app-transaction-list',
  templateUrl: './transaction-list.component.html',
  styleUrls: ['./transaction-list.component.scss']
})
export class TransactionListComponent implements OnInit, AfterViewInit {
  dataSource = new MatTableDataSource<TransactionDisplayUiModel>([]);
  filterForm!: FormGroup;
  loading = false;
  error: string | null = null;
  
  // Dashboard summary data
  summaryData = {
    total: 0,
    active: 0,
    overdue: 0,
    returned: 0
  };
  
  // Columns for the table
  displayedColumns: string[] = [
    'transactionID', 
    'bookName', 
    'memberID', 
    'borrowDate', 
    'dueDate', 
    'returnDate', 
    'status', 
    'actions'
  ];
  
  // Current columns being displayed (may change based on screen size)
  currentDisplayColumns: string[] = [...this.displayedColumns];
  
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  transactions: TransactionDisplayUiModel[] = [];

  constructor(
    private transactionService: TransactionService,
    private authService: AuthService,
    private fb: FormBuilder,
    private router: Router,
    private snackBar: MatSnackBar,
    private confirmationService: ConfirmationDialogService
  ) {
    this.initFilterForm();
  }

  ngOnInit(): void {
    this.loadTransactions();
    this.loadSummaryData();
    
    // Adjust displayed columns for smaller screens
    this.adjustColumnsForScreenSize();
    window.addEventListener('resize', () => this.adjustColumnsForScreenSize());
  }
  
  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
    
    // Force overdue status update after a small delay
    setTimeout(() => {
      this.updateOverdueStatus();
    }, 100);
  }
  
  // Initialize the filter form
  private initFilterForm(): void {
    this.filterForm = this.fb.group({
      status: [''],
      bookTitle: [''],
      fromDate: [null],
      toDate: [null]
    });
    
    // Subscribe to form changes for filtering
    this.filterForm.valueChanges.subscribe(() => {
      this.applyFilters();
    });
  }
  
  // Load all transactions
  loadTransactions(filters: any = {}): void {
    this.loading = true;
    this.error = null;
    
    this.transactionService.getTransactions(filters).subscribe({
      next: (data: BorrowingTransactionDto[]) => {
        // Transform DTOs to UI models
        this.transactions = data.map(t => this.transactionService.mapToTransactionDisplayUiModel(t));
        this.dataSource.data = this.transactions;
        this.updateOverdueStatus(); // Call this explicitly to update overdue status
        this.loading = false;
      },
      error: (err: any) => {
        this.error = 'Failed to load transactions: ' + (err.message || 'Unknown error');
        this.loading = false;
      }
    });
  }
  
  // Transform transactions for display
  transformTransactions(transactions: BorrowingTransactionDto[]): TransactionDisplayUiModel[] {
    return transactions.map(t => {
      return this.transactionService.mapToTransactionDisplayUiModel(t);
    });
  }
  
  // Load summary data for dashboard
  loadSummaryData(): void {
    this.transactionService.getTransactionSummary().subscribe({
      next: (summary) => {
        this.summaryData = {
          total: summary.totalTransactions,
          active: summary.activeBorrowings,
          overdue: summary.overdueItems,
          returned: summary.returnedItems
        };
      },
      error: () => {
        // In case of error, we'll keep the defaults (all zeros)
      }
    });
  }
  
  // Determine transaction status (Borrowed, Returned, Overdue)
  determineStatus(transaction: BorrowingTransactionDto): string {
    if (transaction.status === 'Returned') {
      return 'Returned';
    }
    
    return this.transactionService.isOverdue(transaction) ? 'Overdue' : 'Borrowed';
  }
  
  // Add a new method to safely determine status
  safelyDetermineStatus(data: any): string {
    // If it's a UI model with 'status' property, use that
    if (data.status) {
      return data.status;
    }
    
    // If it's a DTO, convert it properly
    if (data.transactionID && typeof data.transactionID === 'number') {
      return this.determineStatus(data as BorrowingTransactionDto);
    }
    
    // Default fallback
    return 'Unknown';
  }
  
  // Calculate due date from borrow date
  calculateDueDate(borrowDate: string): string {
    const dueDate = this.transactionService.calculateDueDate(borrowDate);
    return this.transactionService.formatDate(dueDate);
  }
  
  // Make sure the isOverdue method is properly implemented
  isOverdue(transaction: any): boolean {
    if (transaction.status === 'Returned' || transaction.returnDate) {
      return false; // Already returned, can't be overdue
    }
    
    const today = new Date();
    const dueDate = new Date(transaction.dueDate);
    
    // For debugging - log to see if dates are being compared correctly
    console.log(`Transaction ID: ${transaction.transactionID}, Status: ${transaction.status}`);
    console.log(`Due date: ${dueDate}, Today: ${today}, Is overdue: ${dueDate < today}`);
    
    return dueDate < today;
  }

  // Add this method to force update transaction rows with overdue status
  updateOverdueStatus(): void {
    if (this.dataSource && this.dataSource.data) {
      const updatedData = this.dataSource.data.map(transaction => {
        // Create a new object to ensure change detection
        const updatedTransaction = {...transaction};
        
        // If borrowed and past due date, set status to Overdue
        if (transaction.status === 'Borrowed' && this.isOverdue(transaction)) {
          updatedTransaction.status = 'Overdue';
        }
        
        return updatedTransaction;
      });
      
      // Update the data source with the modified transactions
      this.dataSource.data = updatedData;
    }
  }

  // Apply filters to the table
  applyFilters(): void {
    const filters = this.filterForm.value;
    
    // Create filter object for the API call
    const apiFilters: any = {};
    
    if (filters.status) {
      apiFilters.status = filters.status;
    }
    
    if (filters.bookTitle) {
      apiFilters.bookTitle = filters.bookTitle;
    }
    
    if (filters.fromDate) {
      apiFilters.fromDate = filters.fromDate;
    }
    
    if (filters.toDate) {
      apiFilters.toDate = filters.toDate;
    }
    
    // Apply filters
    this.loadTransactions(apiFilters);
  }
  
  // Reset all filters
  resetFilters(): void {
    this.filterForm.reset({
      status: '',
      bookTitle: '',
      fromDate: null,
      toDate: null
    });
    
    this.dataSource.filter = '';
  }
  
  // Get CSS class for status badge
  getStatusClass(status: string): string {
    switch (status) {
      case 'Borrowed': return 'status-borrowed';
      case 'Returned': return 'status-returned';
      case 'Overdue': return 'status-overdue';
      default: return '';
    }
  }
  
  // Format date for display
  formatDate(date: string | null): string {
    if (!date) return 'N/A';
    if (date === '1000-01-01') return 'N/A';
    
    try {
      return new Date(date).toLocaleDateString();
    } catch (e) {
      return 'Invalid Date';
    }
  }
  
  // Add missing formatStatus method
  formatStatus(status: string): string {
    switch (status) {
      case 'Active':
        return 'Borrowed';
      case 'Overdue':
        return 'Overdue';
      case 'Returned':
        return 'Returned';
      default:
        return status;
    }
  }
  
  // Return a book
  returnBook(transactionId: number): void {
    this.confirmationService.confirm('Are you sure you want to return this book?', 'Confirm Return').subscribe(result => {
      if (result) {
        this.router.navigate(['/borrowing-transactions/return'], { 
          queryParams: { transactionId } 
        });
      }
    });
  }
  
  // Delete a transaction
  delete(transactionId: number): void {
    this.confirmationService.confirmDelete('transaction').subscribe(result => {
      if (result) {
        this.loading = true;
        this.transactionService.deleteTransaction(transactionId).subscribe({
          next: () => {
            this.snackBar.open('Transaction deleted successfully', 'Close', { duration: 3000 });
            this.loadTransactions();
            this.loadSummaryData();
          },
          error: (err) => {
            this.error = 'Failed to delete transaction: ' + (err.message || 'Unknown error');
            this.loading = false;
          }
        });
      }
    });
  }
  
  // Export transactions to CSV
  exportToCSV(): void {
    // Get current filtered data
    const data = this.dataSource.filteredData;
    if (data.length === 0) {
      this.snackBar.open('No data to export', 'Close', { duration: 3000 });
      return;
    }
    
    // Define columns to export
    const headers = [
      'Transaction ID',
      'Book',
      'Member',
      'Borrow Date',
      'Due Date',
      'Return Date',
      'Status'
    ];
    
    // Map data to CSV format
    const csvData = data.map(item => this.exportRowData(item));
    
    // Add headers
    csvData.unshift(headers);
    
    // Convert to CSV string
    const csvString = csvData.map(row => row.join(',')).join('\n');
    
    // Create a download link
    const blob = new Blob([csvString], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    
    // Create file name with current date
    const date = new Date().toISOString().slice(0, 10);
    const fileName = `transactions_${date}.csv`;
    
    // Trigger download
    const url = URL.createObjectURL(blob);
    link.href = url;
    link.download = fileName;
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    this.snackBar.open('Export successful', 'Close', { duration: 3000 });
  }
  
  exportRowData(item: TransactionDisplayUiModel): any[] {
    return [
      item.transactionID,
      item.bookName || 'N/A',
      item.memberName || `Member #${item.memberID}`,
      this.formatStatus(item.status),
      this.formatDate(item.borrowDate),
      this.formatDate(item.dueDate || ''),
      item.returnDate && item.returnDate !== '1000-01-01' ? this.formatDate(item.returnDate) : 'Not returned'
    ];
  }
  
  // Check role permissions
  isAdmin(): boolean {
    return this.authService.hasRole('Admin');
  }
  
  isLibrarian(): boolean {
    return this.authService.hasRole('Librarian');
  }
  
  // Adjust columns based on screen size
  private adjustColumnsForScreenSize(): void {
    const width = window.innerWidth;
    
    if (width < 768) {
      // For smaller screens, show fewer columns
      this.currentDisplayColumns = [
        'transactionID',
        'bookName',
        'borrowDate',
        'status',
        'actions'
      ];
    } else if (width < 992) {
      // For medium screens
      this.currentDisplayColumns = [
        'transactionID',
        'bookName',
        'memberID',
        'borrowDate',
        'dueDate',
        'status',
        'actions'
      ];
    } else {
      // For large screens, show all columns
      this.currentDisplayColumns = [...this.displayedColumns];
    }
  }
}
