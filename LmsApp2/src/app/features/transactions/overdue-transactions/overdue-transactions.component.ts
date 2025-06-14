import { Component, OnInit, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TransactionService } from '../transaction.service';
import { BorrowingTransactionDto } from '../../../models/dtos/transaction-dtos';
import { TransactionDisplayUiModel } from '../../../models/ui-models/transaction-ui-models';
import { AuthService } from '../../auth/auth.service';
import { ConfirmationDialogService } from '../../../core/confirmation-dialog.service';

@Component({
  selector: 'app-overdue-transactions',
  templateUrl: './overdue-transactions.component.html',
  styleUrls: ['./overdue-transactions.component.scss']
})
export class OverdueTransactionsComponent implements OnInit {
  dataSource = new MatTableDataSource<TransactionDisplayUiModel>([]);
  displayedColumns = ['transactionID', 'bookName', 'memberID', 'borrowDate', 'dueDate', 'overdueDays', 'potentialFine', 'actions'];
  loading = false;
  error: string | null = null;
  overdueTransactions: TransactionDisplayUiModel[] = [];
  
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  
  constructor(
    private transactionService: TransactionService,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar,
    private confirmationService: ConfirmationDialogService
  ) {}
  
  ngOnInit(): void {
    this.loadOverdueTransactions();
  }
  
  ngAfterViewInit() {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }
  
  loadOverdueTransactions(): void {
    this.loading = true;
    this.error = null;
    
    // Use the dedicated method for overdue transactions
    this.transactionService.getOverdueTransactions().subscribe({
      next: (data) => {
        this.dataSource.data = data;
        this.loading = false;
        // Apply default sorting if needed
        if (this.sort) {
          this.dataSource.sort = this.sort;
        }
        if (this.paginator) {
          this.dataSource.paginator = this.paginator;
        }
      },
      error: (err) => {
        this.error = 'Failed to load overdue transactions: ' + (err.message || 'Unknown error');
        this.loading = false;
      }
    });
  }
  
  transformTransactions(transactions: BorrowingTransactionDto[]): TransactionDisplayUiModel[] {
    return transactions.map(t => {
      // Calculate due date if it's missing
      const dueDateObj = this.transactionService.calculateDueDate(t.borrowDate);
      const dueDateStr = dueDateObj.toISOString().split('T')[0];
      
      // Create a UI model
      const uiModel: TransactionDisplayUiModel = {
        id: t.transactionID,
        transactionID: t.transactionID,
        bookId: t.bookID,
        bookID: t.bookID,
        bookName: t.bookName || '',
        memberId: t.memberID,
        memberID: t.memberID,
        memberName: '',  // Will be populated later
        borrowDate: t.borrowDate,
        dueDate: dueDateStr,
        returnDate: t.returnDate,
        status: t.status,
        isOverdue: t.status === 'Overdue',
        formattedBorrowDate: this.formatDate(t.borrowDate),
        formattedDueDate: this.formatDate(dueDateStr)
      };
      
      // Calculate days overdue
      if (uiModel.isOverdue) {
        const currentDate = new Date();
        const diffTime = Math.abs(currentDate.getTime() - dueDateObj.getTime());
        uiModel.daysOverdue = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
      }
      
      return uiModel;
    });
  }
  
  calculateOverdueDays(transaction: BorrowingTransactionDto): number {
    return this.transactionService.calculateOverdueDays(transaction);
  }
  
  calculatePotentialFine(transaction: BorrowingTransactionDto): number {
    return this.transactionService.calculatePotentialFine(transaction);
  }
  
  returnBook(transactionId: number): void {
    this.confirmationService.confirm(
      'Are you sure you want to return this book?',
      'Confirm Return'
    ).subscribe(result => {
      if (result) {
        this.router.navigate(['/borrowing-transactions/return'], { 
          queryParams: { transactionId } 
        });
      }
    });
  }
  
  notifyMember(transactionId: number): void {
    this.confirmationService.confirm(
      'Send an overdue notification to this member?',
      'Confirm Notification'
    ).subscribe(result => {
      if (result) {
        // Logic to send notification would go here
        this.snackBar.open('Notification sent successfully', 'Close', { duration: 3000 });
      }
    });
  }
  
  viewDetails(transactionId: number): void {
    this.router.navigate(['/borrowing-transactions/details', transactionId]);
  }
  
  formatDate(date: string | Date): string {
    if (!date) return 'N/A';
    
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    
    return dateObj.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
  
  isAdmin(): boolean {
    return this.authService.hasRole('Admin');
  }
  
  isLibrarian(): boolean {
    return this.authService.hasRole('Librarian');
  }
  
  canManage(): boolean {
    return this.isAdmin() || this.isLibrarian();
  }
  
  // Apply a class based on how overdue the transaction is
  getOverdueClass(days: number): string {
    if (days > 30) return 'severe-overdue';
    if (days > 14) return 'moderate-overdue';
    return 'mild-overdue';
  }
}
