import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TransactionService } from '../transaction.service';
import { ConfirmationDialogService } from '../../../core/confirmation-dialog.service';
import { TransactionDisplayUiModel } from '../../../models/ui-models/transaction-ui-models';
import { BorrowingTransactionDto } from '../../../models/dtos/transaction-dtos';
import { BookService } from '../../books/book.service';
import { MemberService } from '../../members/member.service';
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-transaction-detail',
  templateUrl: './transaction-detail.component.html',
  styleUrls: ['./transaction-detail.component.scss']
})
export class TransactionDetailComponent implements OnInit {
  transaction: TransactionDisplayUiModel | null = null;
  transactionId: number = 0;
  loading = false;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private transactionService: TransactionService,
    private authService: AuthService,
    private snackBar: MatSnackBar,
    private confirmationService: ConfirmationDialogService,
    private bookService: BookService,
    private memberService: MemberService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.transactionId = +id;
        this.loadTransactionDetails();
      } else {
        this.error = 'No transaction ID provided';
      }
    });
  }

  loadTransactionDetails(): void {
    this.loading = true;
    this.error = null;
    
    this.transactionService.getTransaction(this.transactionId).subscribe({
      next: (data) => {
        this.transaction = data;
        this.loading = false;
        
        // Load related book and member details if needed
        if (data.bookID) {
          this.loadBookDetails(data.bookID);
        }
        if (data.memberID) {
          this.loadMemberDetails(data.memberID);
        }
      },
      error: (err) => {
        this.error = 'Failed to load transaction: ' + (err.message || 'Unknown error');
        this.loading = false;
      }
    });
  }

  loadBookDetails(bookId: number): void {
    this.bookService.getBook(bookId).subscribe({
      next: (book) => {
        if (this.transaction) {
          this.transaction.bookTitle = book.title;
        }
      },
      error: () => {
        // Silently fail - not critical
      }
    });
  }

  loadMemberDetails(memberId: number): void {
    this.memberService.getMember(memberId).subscribe({
      next: (member) => {
        if (this.transaction) {
          this.transaction.memberName = member.name;
        }
      },
      error: () => {
        // Silently fail - not critical
      }
    });
  }

  determineStatus(transaction: BorrowingTransactionDto): string {
    if (transaction.status === 'Returned') {
      return 'Returned';
    }

    return this.isOverdue(transaction) ? 'Overdue' : 'Borrowed';
  }

  isOverdue(transaction: BorrowingTransactionDto): boolean {
    return this.transactionService.isOverdue(transaction);
  }

  calculateOverdueDays(transaction: BorrowingTransactionDto): number {
    return this.transactionService.calculateOverdueDays(transaction);
  }

  calculatePotentialFine(transaction: BorrowingTransactionDto): number {
    return this.transactionService.calculatePotentialFine(transaction);
  }

  formatDate(date: string | null): string {
    if (!date) return 'N/A';
    if (date === '1000-01-01') return 'N/A';

    try {
      return new Date(date).toLocaleDateString();
    } catch (e) {
      return 'Invalid Date';
    }
  }

  returnBook(): void {
    if (!this.transaction) return;

    this.confirmationService.confirm('Are you sure you want to return this book?', 'Confirm Return').subscribe(result => {
      if (result) {
        this.router.navigate(['/borrowing-transactions/return'], {
          queryParams: { transactionId: this.transaction?.transactionID }
        });
      }
    });
  }

  deleteTransaction(): void {
    if (!this.transaction) return;

    this.confirmationService.confirmDelete('transaction').subscribe(confirmed => {
      if (confirmed && this.transaction) {
        this.loading = true;
        this.transactionService.deleteTransaction(this.transaction.transactionID).subscribe({
          next: () => {
            this.snackBar.open('Transaction deleted successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/borrowing-transactions']);
          },
          error: (err) => {
            this.error = 'Failed to delete transaction: ' + (err.message || 'Unknown error');
          }
        });
      }
    });
  }

  canDelete(): boolean {
    return this.isAdmin();
  }

  isAdmin(): boolean {
    return this.authService.hasRole('Admin');
  }

  isLibrarian(): boolean {
    return this.authService.hasRole('Librarian');
  }
}
