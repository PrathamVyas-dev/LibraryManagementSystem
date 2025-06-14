import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { MemberService } from '../member.service';
import { TransactionService } from '../../transactions/transaction.service';
import { FineService } from '../../fines/fine.service';
import { AuthService } from '../../auth/auth.service';
import { MemberResponseDto } from '../../../models/dtos/member-dtos';
import { BorrowingTransactionDto } from '../../../models/dtos/transaction-dtos';
import { FineDetailsDto } from '../../../models/dtos/fine-dtos';
import { MemberProfileUiModel } from '../../../models/ui-models/member-ui-models';

@Component({
  selector: 'app-member-profile',
  templateUrl: './member-profile.component.html',
  styleUrls: ['./member-profile.component.scss']
})
export class MemberProfileComponent implements OnInit {
  member: MemberResponseDto | null = null;
  profileData: MemberProfileUiModel | null = null;
  transactions: BorrowingTransactionDto[] = [];
  pendingFines: FineDetailsDto[] = [];
  loading = false;
  loadingTransactions = false;
  loadingFines = false;
  error: string | null = null;
  
  // These properties will be derived from other data
  memberActiveBorrowings = 0;
  memberOverdueItems = 0;
  memberOutstandingFines = 0;
  
  constructor(
    private memberService: MemberService,
    private transactionService: TransactionService,
    private fineService: FineService,
    private authService: AuthService,
    private snackBar: MatSnackBar,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadMemberProfile();
  }

  loadMemberProfile(): void {
    this.loading = true;
    this.error = null;
    
    // Use the getCurrentMemberProfile method from the member service
    this.memberService.getCurrentMemberProfile().subscribe({
      next: (data) => {
        this.member = data;
        this.loading = false;
        
        // Once the profile is loaded, load borrowing history if needed
        if (this.member && this.member.memberID) {
          this.loadTransactions(this.member.memberID);
        }
      },
      error: (err) => {
        this.error = err.message || 'Failed to load profile';
        this.loading = false;
        console.error('Profile loading error:', err);
      }
    });
  }
  
  loadTransactions(memberId: number) {
    this.loadingTransactions = true;
    this.memberService.getMemberBorrowings(memberId).subscribe({
      next: transactions => {
        this.transactions = transactions;
        
        // Calculate active borrowings and overdue items
        this.memberActiveBorrowings = transactions.filter(t => t.status !== 'Returned').length;
        this.memberOverdueItems = transactions.filter(t => this.isOverdue(t)).length;
        
        // Now we can build the profile data with correct statistics
        this.updateProfileData();
        this.loadingTransactions = false;
      },
      error: err => {
        this.error = 'Failed to load transactions: ' + (err.message || 'Unknown error');
        this.loadingTransactions = false;
      }
    });
  }
  
  loadFines(memberId: number) {
    this.loadingFines = true;
    this.memberService.getMemberFines(memberId).subscribe({
      next: fines => {
        this.pendingFines = fines.filter(fine => fine.status === 'Pending');
        // Calculate outstanding fines
        this.memberOutstandingFines = this.getTotalFineAmount();
        
        // Update profile data with fines info
        this.updateProfileData();
        this.loadingFines = false;
      },
      error: err => {
        this.error = 'Failed to load fines: ' + (err.message || 'Unknown error');
        this.loadingFines = false;
      }
    });
  }
  
  // Build or update the profile data UI model with all stats
  private updateProfileData() {
    if (!this.member) return;
    
    // Only update if we have loaded both transactions and fines
    if (!this.loadingTransactions && !this.loadingFines) {
      this.profileData = {
        memberInfo: {
          id: this.member.memberID,
          name: this.member.name,
          email: this.member.email,
          phone: this.member.phone || '',
          address: this.member.address || '',
          status: this.member.membershipStatus
        },
        borrowingStats: {
          totalBorrowed: this.transactions.length,
          currentlyBorrowed: this.memberActiveBorrowings,
          overdue: this.memberOverdueItems,
          fines: this.memberOutstandingFines
        }
      };
      
      this.loading = false;
    }
  }
  
  editProfile() {
    if (!this.member) return;
    this.router.navigate(['/members/edit', this.member.memberID]);
  }
  
  viewAllTransactions() {
    if (!this.member) return;
    
    // Navigate to the member history component in the transactions module
    this.router.navigate(['/transactions/member', this.member.memberID]);
  }
  
  viewFines() {
    if (!this.member) return;
    
    // Navigate to borrowings-fines component with the fines tab active
    this.router.navigate(['/members', this.member.memberID, 'borrowings-fines'], { 
      queryParams: { tab: 'fines' } 
    });
  }
  
  borrowBook() {
    // Navigate to the books list to select a book to borrow
    this.router.navigate(['/books']);
  }
  
  viewMyFines(): void {
    if (this.member?.memberID) {
      this.router.navigate(['/fines/member', this.member.memberID]);
    }
  }

  payFine(fineId: number): void {
    if (this.member?.memberID) {
      this.router.navigate(['/fines/payment'], { 
        queryParams: { 
          fineId: fineId,
          returnUrl: '/members/profile' 
        } 
      });
    }
  }
  
  getTotalFineAmount(): number {
    return this.pendingFines.reduce((total, fine) => total + fine.amount, 0);
  }
  
  getStatusClass(status: string): string {
    return status === 'Returned' ? 'bg-success' : 
           status === 'Overdue' ? 'bg-danger' : 'bg-warning';
  }
  
  isOverdue(transaction: BorrowingTransactionDto): boolean {
    if (transaction.status === 'Returned') return false;
    
    const dueDate = new Date(transaction.returnDate);
    const today = new Date();
    
    dueDate.setHours(0, 0, 0, 0);
    today.setHours(0, 0, 0, 0);
    
    return dueDate < today;
  }
  
  getTransactionStatus(transaction: BorrowingTransactionDto): string {
    if (transaction.status === 'Returned') {
      return 'Returned';
    }
    
    return this.isOverdue(transaction) ? 'Overdue' : 'Active';
  }
}
