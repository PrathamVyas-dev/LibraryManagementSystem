import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Location } from '@angular/common';
import { TransactionService } from '../transaction.service';
import { MemberService } from '../../members/member.service';
import { BorrowingTransactionDto } from '../../../models/dtos/transaction-dtos';
import { MemberResponseDto } from '../../../models/dtos/member-dtos';
import { AuthService } from '../../auth/auth.service';
import { TransactionDisplayUiModel } from '../../../models/ui-models/transaction-ui-models';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Sort } from '@angular/material/sort';

@Component({
  selector: 'app-member-history',
  templateUrl: './member-history.component.html',
  styleUrls: ['./member-history.component.scss']
})
export class MemberHistoryComponent implements OnInit {
  memberId: number = 0;
  member: MemberResponseDto | null = null;
  transactions: TransactionDisplayUiModel[] = [];
  filteredTransactions: TransactionDisplayUiModel[] = [];
  filterForm: FormGroup;
  
  loading = {
    member: false,
    transactions: false
  };
  
  error: string | null = null;
  
  statuses = ['All', 'Active', 'Overdue', 'Returned'];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private location: Location,
    private fb: FormBuilder,
    private memberService: MemberService,
    private transactionService: TransactionService,
    private authService: AuthService,
    private snackBar: MatSnackBar
  ) {
    this.filterForm = this.fb.group({
      status: ['All'],
      dateFrom: [''],
      dateTo: [''],
      searchTerm: [''],
      showReturned: [true],
      sortBy: ['borrowDate'],
      sortDirection: ['desc']
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.memberId = +id;
        this.loadData();
      } else {
        this.error = 'Member ID is required';
      }
    });
    
    // React to filter changes
    this.filterForm.valueChanges.subscribe(() => {
      this.applyFilters();
    });
  }
  
  loadData(): void {
    // Reset error
    this.error = null;
    
    // Set loading states
    this.loading.member = true;
    this.loading.transactions = true;
    
    // Load member data and transactions in parallel
    forkJoin({
      member: this.memberService.getMember(this.memberId).pipe(
        catchError(err => {
          this.error = 'Failed to load member details: ' + (err.message || 'Unknown error');
          return of(null);
        })
      ),
      transactions: this.memberService.getMemberBorrowings(this.memberId).pipe(
        catchError(err => {
          this.error = 'Failed to load transaction history: ' + (err.message || 'Unknown error');
          return of([]);
        })
      )
    }).subscribe(result => {
      // Update member info
      this.member = result.member;
      this.loading.member = false;
      
      // Process transactions for display
      if (result.transactions.length > 0) {
        this.transactions = result.transactions.map(t => {
          return {
            id: t.transactionID,
            transactionID: t.transactionID,
            bookId: t.bookID,
            bookID: t.bookID,
            bookName: t.bookName || '',
            bookTitle: t.bookName || '',
            memberId: t.memberID,
            memberID: t.memberID,
            memberName: '',
            borrowDate: t.borrowDate,
            dueDate: this.transactionService.calculateDueDate(t.borrowDate).toISOString().split('T')[0],
            returnDate: t.returnDate,
            status: t.status,
            isOverdue: t.status === 'Overdue'
          } as TransactionDisplayUiModel;
        });
        
        this.applyFilters();
      } else {
        this.filteredTransactions = [];
      }
      
      this.loading.transactions = false;
    });
  }
  
  refreshData(): void {
    this.loadData();
  }
  
  applyFilters(): void {
    const filters = this.filterForm.value;
    
    // Start with all transactions
    let filtered = [...this.transactions];
    
    // Filter by status
    if (filters.status !== 'All') {
      if (filters.status === 'Active') {
        filtered = filtered.filter(t => this.calculateStatus(t) === 'Borrowed');
      } else if (filters.status === 'Overdue') {
        filtered = filtered.filter(t => this.calculateStatus(t) === 'Overdue');
      } else if (filters.status === 'Returned') {
        filtered = filtered.filter(t => this.calculateStatus(t) === 'Returned');
      }
    }
    
    // Filter by show/hide returned
    if (!filters.showReturned) {
      filtered = filtered.filter(t => t.status !== 'Returned');
    }
    
    // Filter by date range - from date
    if (filters.dateFrom) {
      const fromDate = new Date(filters.dateFrom);
      filtered = filtered.filter(t => {
        const borrowDate = new Date(t.borrowDate);
        return borrowDate >= fromDate;
      });
    }
    
    // Filter by date range - to date
    if (filters.dateTo) {
      const toDate = new Date(filters.dateTo);
      // Set to end of day
      toDate.setHours(23, 59, 59, 999);
      filtered = filtered.filter(t => {
        const borrowDate = new Date(t.borrowDate);
        return borrowDate <= toDate;
      });
    }
    
    // Filter by search term
    if (filters.searchTerm) {
      const term = filters.searchTerm.toLowerCase().trim();
      filtered = filtered.filter(t => 
        t.bookName.toLowerCase().includes(term)
      );
    }
    
    // Sort results
    filtered = this.sortTransactions(filtered, filters.sortBy, filters.sortDirection);
    
    // Update filtered transactions
    this.filteredTransactions = filtered;
  }
  
  applyFilter(term: string): void {
    term = term.toLowerCase().trim();
    this.filteredTransactions = this.transactions.filter(t => 
      (t.bookName && t.bookName.toLowerCase().includes(term)) ||
      (t.bookTitle && t.bookTitle.toLowerCase().includes(term))
    );
  }
  
  sortTransactions(transactions: TransactionDisplayUiModel[], sortBy: string, direction: 'asc' | 'desc'): TransactionDisplayUiModel[] {
    return [...transactions].sort((a, b) => {
      let comparison = 0;
      
      switch (sortBy) {
        case 'borrowDate':
          comparison = new Date(a.borrowDate).getTime() - new Date(b.borrowDate).getTime();
          break;
        case 'dueDate':
          comparison = new Date(a.dueDate as string).getTime() - new Date(b.dueDate as string).getTime();
          break;
        case 'returnDate':
          if (!a.returnDate && !b.returnDate) {
            comparison = 0;
          } else if (!a.returnDate) {
            comparison = 1;
          } else if (!b.returnDate) {
            comparison = -1;
          } else {
            const aDate = a.returnDate ? new Date(a.returnDate as string).getTime() : Number.MAX_SAFE_INTEGER;
            const bDate = b.returnDate ? new Date(b.returnDate as string).getTime() : Number.MAX_SAFE_INTEGER;
            comparison = aDate - bDate;
          }
          break;
        case 'status':
          const statusA = a.status || '';
          const statusB = b.status || '';
          comparison = statusA.localeCompare(statusB);
          break;
        default:
          comparison = 0;
      }
      
      return direction === 'asc' ? comparison : -comparison;
    });
  }
  
  sortData(sort: Sort): void {
    const data = [...this.filteredTransactions];
    
    if (!sort.active || sort.direction === '') {
      this.filteredTransactions = data;
      return;
    }
    
    this.filteredTransactions = data.sort((a, b) => {
      const isAsc = sort.direction === 'asc';
      let comparison = 0;
      
      switch (sort.active) {
        case 'bookTitle':
          comparison = (((a.bookTitle || a.bookName) || '') as string).localeCompare(
                        ((b.bookTitle || b.bookName) || '') as string);
          break;
        case 'borrowDate':
          comparison = new Date(a.borrowDate as string).getTime() - new Date(b.borrowDate as string).getTime();
          break;
        case 'dueDate':
          comparison = new Date(a.dueDate as string).getTime() - new Date(b.dueDate as string).getTime();
          break;
        case 'returnDate':
          if (!a.returnDate && !b.returnDate) {
            comparison = 0;
          } else if (!a.returnDate) {
            comparison = 1;
          } else if (!b.returnDate) {
            comparison = -1;
          } else {
            const aDate = a.returnDate ? new Date(a.returnDate as string).getTime() : Number.MAX_SAFE_INTEGER;
            const bDate = b.returnDate ? new Date(b.returnDate as string).getTime() : Number.MAX_SAFE_INTEGER;
            comparison = aDate - bDate;
          }
          break;
        case 'status':
          const statusA = a.status || '';
          const statusB = b.status || '';
          comparison = statusA.localeCompare(statusB);
          break;
        default:
          comparison = 0;
      }
      
      return comparison * (isAsc ? 1 : -1);
    });
  }
  
  clearFilters(): void {
    this.filterForm.reset({
      status: 'All',
      dateFrom: '',
      dateTo: '',
      searchTerm: '',
      showReturned: true,
      sortBy: 'borrowDate',
      sortDirection: 'desc'
    });
  }
  
  calculateStatus(transaction: any): string {
    if (transaction.status === 'Returned') {
      return 'Returned';
    }
    
    return this.isOverdue(transaction) ? 'Overdue' : 'Borrowed';
  }
  
  getStatusClass(transaction: BorrowingTransactionDto): string {
    const status = this.calculateStatus(transaction);
    switch (status) {
      case 'Active': return 'primary';
      case 'Returned': return 'success';
      case 'Overdue': return 'danger';
      default: return 'secondary';
    }
  }
  
  isOverdue(transaction: BorrowingTransactionDto): boolean {
    return this.transactionService.isOverdue(transaction);
  }
  
  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    if (date === '1000-01-01') return 'N/A';
    
    try {
      return new Date(date).toLocaleDateString();
    } catch (e) {
      return 'N/A';
    }
  }
  
  viewTransactionDetails(transactionId: number): void {
    this.router.navigate(['/borrowing-transactions/details', transactionId]);
  }
  
  returnBook(transactionId: number): void {
    this.router.navigate(['/borrowing-transactions/return'], { 
      queryParams: { transactionId } 
    });
  }
  
  goBack(): void {
    this.location.back();
  }
  
  isAdminOrLibrarian(): boolean {
    return this.authService.hasRole('Admin') || this.authService.hasRole('Librarian');
  }
}
