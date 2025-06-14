import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MemberService } from '../member.service';
import { TransactionService } from '../../transactions/transaction.service';
import { FineService } from '../../fines/fine.service';
import { MemberResponseDto } from '../../../models/dtos/member-dtos';
import { BorrowingTransactionDto } from '../../../models/dtos/transaction-dtos';
import { FineDetailsDto } from '../../../models/dtos/fine-dtos';
import { MatTableDataSource } from '@angular/material/table';

@Component({
  selector: 'app-member-borrowings-fines',
  templateUrl: './member-borrowings-fines.component.html',
  styleUrls: ['./member-borrowings-fines.component.scss']
})
export class MemberBorrowingsFinesComponent implements OnInit {
  member: MemberResponseDto | null = null;
  memberId: number | null = null;
  
  // Added properties and states
  loading = false;
  error: string | null = null;
  
  // Borrowings table
  borrowings: BorrowingTransactionDto[] = [];
  borrowingsDataSource = new MatTableDataSource<BorrowingTransactionDto>();
  borrowingsColumns: string[] = ['bookID', 'borrowDate', 'dueDate', 'status', 'actions'];
  
  // Fines table
  fines: FineDetailsDto[] = [];
  finesDataSource = new MatTableDataSource<FineDetailsDto>();
  finesColumns: string[] = ['fineID', 'amount', 'status', 'transactionDate', 'actions'];
  
  // Tabs
  activeTab = 'borrowings';  // 'borrowings' or 'fines'

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private memberService: MemberService,
    private transactionService: TransactionService,
    private fineService: FineService
  ) { }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.memberId = parseInt(idParam, 10);
      this.loadMemberDetails(this.memberId);
    } else {
      this.error = 'No member ID provided';
    }
  }
  
  // Add missing methods
  loadMemberDetails(memberId: number): void {
    this.loading = true;
    this.error = null;
    
    this.memberService.getMember(memberId).subscribe({
      next: (data) => {
        this.member = data;
        this.loadBorrowingHistory();
        this.loadMemberFines();
      },
      error: (err) => {
        this.error = `Failed to load member details: ${err.message || 'Unknown error'}`;
        this.loading = false;
      }
    });
  }
  
  loadBorrowingHistory(): void {
    if (!this.memberId) return;
    
    this.memberService.getMemberBorrowings(this.memberId).subscribe({
      next: (data) => {
        this.borrowings = data;
        this.borrowingsDataSource.data = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = `Failed to load borrowing history: ${err.message || 'Unknown error'}`;
        this.loading = false;
      }
    });
  }
  
  loadMemberFines(): void {
    if (!this.memberId) return;
    
    this.fineService.getMemberFines(this.memberId).subscribe({
      next: (data) => {
        this.fines = data;
        this.finesDataSource.data = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = `Failed to load fines: ${err.message || 'Unknown error'}`;
        this.loading = false;
      }
    });
  }
  
  // Helper methods for templates
  formatDate(date: string): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString();
  }
  
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
  }
  
  switchTab(tab: string): void {
    this.activeTab = tab;
  }
  
  viewTransactionDetails(transactionId: number): void {
    this.router.navigate(['/transactions', transactionId]);
  }
  
  viewFineDetails(fineId: number): void {
    this.router.navigate(['/fines', fineId]);
  }
  
  renewBook(transactionId: number): void {
    // Implementation for renewal
  }
  
  payFine(fineId: number): void {
    if (this.memberId) {
      this.router.navigate(['/fines/payment'], {
        queryParams: {
          fineId: fineId,
          memberId: this.memberId,
          returnUrl: `/members/${this.memberId}`
        }
      });
    }
  }
  
  goToFinesPage(): void {
    if (this.memberId) {
      this.router.navigate(['/fines/member', this.memberId]);
    }
  }
}
