import { Component, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Router, ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FineService } from '../fine.service';
import { AuthService } from '../../auth/auth.service';
import { ConfirmationDialogService } from '../../../core/confirmation-dialog.service';
import { FineDetailsDto } from '../../../models/dtos/fine-dtos';
import { FineFilterUiModel, FineSummaryUiModel } from '../../../models/ui-models/fine-ui-models';
import { MemberService } from '../../members/member.service';
import { MemberResponseDto } from '../../../models/dtos/member-dtos';

@Component({
  selector: 'app-fine-list',
  templateUrl: './fine-list.component.html',
  styleUrls: ['./fine-list.component.scss']
})
export class FineListComponent implements OnInit {
  dataSource = new MatTableDataSource<FineDetailsDto>([]);
  displayedColumns: string[] = ['fineID', 'memberID', 'amount', 'status', 'transactionDate', 'actions'];
  filterForm: FormGroup;
  loading = false;
  error: string | null = null;
  
  // Summary data
  summaryData: FineSummaryUiModel = {
    totalFines: 0,
    pendingFines: 0,
    paidFines: 0,
    totalAmount: 0,
    pendingAmount: 0,
    paidAmount: 0,
    averageAmount: 0 // Changed from averageFineAmount
  };

  // Report functionality
  showReportPanel = false;
  reportForm: FormGroup;
  generatingReport = false;
  reportFormats = [
    { value: 'csv', label: 'CSV' },
    { value: 'pdf', label: 'PDF' },
    { value: 'excel', label: 'Excel' }
  ];
  reportTypes = [
    { value: 'summary', label: 'Summary Report' },
    { value: 'detailed', label: 'Detailed Report' },
    { value: 'overdue', label: 'Overdue Fines' },
    { value: 'member', label: 'Member-wise Report' }
  ];
  
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  // Member-specific mode
  memberId: number | null = null;
  memberDetails: MemberResponseDto | null = null;
  isMemberSpecific = false;

  constructor(
    private fineService: FineService,
    private authService: AuthService,
    private memberService: MemberService,
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar,
    private confirmationService: ConfirmationDialogService
  ) {
    this.filterForm = this.fb.group({
      memberName: [''],
      status: [''],
      fromDate: [null],
      toDate: [null],
      minAmount: [''],
      maxAmount: ['']
    });

    // Initialize report form
    this.reportForm = this.fb.group({
      reportType: ['summary'],
      reportFormat: ['pdf'],
      includeCharts: [true],
      startDate: [this.getDefaultStartDate()],
      endDate: [new Date()],
      includeDetails: [true]
    });
  }

  ngOnInit(): void {
    // Check if we're in member-specific mode
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.memberId = +params['id'];
        this.isMemberSpecific = true;
        this.loadMemberDetails();
        this.loadMemberFines();
      } else {
        this.loadFines();
      }
    });

    // Subscribe to filter form changes
    this.filterForm.valueChanges.subscribe(() => {
      this.applyFilters();
    });
  }
  
  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }
  
  loadFines(): void {
    this.loading = true;
    this.error = null;
    
    this.fineService.getAllFines().subscribe({
      next: (fines) => {
        this.dataSource.data = fines;
        this.loading = false;
        this.calculateSummary(fines);
      },
      error: (err) => {
        this.error = 'Failed to load fines: ' + (err.message || 'Unknown error');
        this.loading = false;
      }
    });
  }
  
  loadMemberDetails(): void {
    if (!this.memberId) return;
    
    this.memberService.getMember(this.memberId).subscribe({
      next: (member) => {
        this.memberDetails = member;
      },
      error: (err) => {
        this.error = 'Failed to load member details: ' + (err.message || 'Unknown error');
      }
    });
  }
  
  loadMemberFines(): void {
    if (!this.memberId) return;
    
    this.loading = true;
    this.error = null;
    
    this.fineService.getMemberFines(this.memberId).subscribe({
      next: (fines) => {
        this.dataSource.data = fines;
        this.loading = false;
        this.calculateSummary(fines);
      },
      error: (err) => {
        this.error = 'Failed to load member fines: ' + (err.message || 'Unknown error');
        this.loading = false;
      }
    });
  }
  
  calculateSummary(fines: FineDetailsDto[]): void {
    const summary: FineSummaryUiModel = {
      totalFines: 0,
      pendingFines: 0,
      paidFines: 0,
      totalAmount: 0,
      pendingAmount: 0,
      paidAmount: 0,
      averageAmount: 0 // Changed from averageFineAmount
    };
    
    fines.forEach(fine => {
      summary.totalAmount += fine.amount;
      
      if (fine.status === 'Pending') {
        summary.pendingFines++;
        summary.pendingAmount += fine.amount;
      } else if (fine.status === 'Paid') {
        summary.paidFines++;
        summary.paidAmount += fine.amount;
      }
    });
    
    summary.averageAmount = summary.totalFines > 0 ? 
      summary.totalAmount / summary.totalFines : 0;
    
    this.summaryData = summary;
  }
  
  applyFilters(): void {
    const filterValues = this.filterForm.value as FineFilterUiModel;
    
    this.dataSource.filterPredicate = (data: FineDetailsDto, filter: string) => {
      // Member name filter (client-side)
      if (filterValues.memberName && 
          !(data.memberID.toString().includes(filterValues.memberName.toLowerCase()))) {
        return false;
      }
      
      // Status filter
      if (filterValues.status && data.status !== filterValues.status) {
        return false;
      }
      
      // Amount range filters
      if (filterValues.minAmount && data.amount < +filterValues.minAmount) {
        return false;
      }
      
      if (filterValues.maxAmount && data.amount > +filterValues.maxAmount) {
        return false;
      }
      
      // Date range filters
      if (filterValues.fromDate) {
        const fromDate = new Date(filterValues.fromDate);
        const transactionDate = new Date(data.transactionDate);
        if (transactionDate < fromDate) {
          return false;
        }
      }
      
      if (filterValues.toDate) {
        const toDate = new Date(filterValues.toDate);
        toDate.setHours(23, 59, 59, 999); // End of day
        const transactionDate = new Date(data.transactionDate);
        if (transactionDate > toDate) {
          return false;
        }
      }
      
      return true;
    };
    
    this.dataSource.filter = 'APPLY_FILTERS';
  }
  
  resetFilters(): void {
    this.filterForm.reset({
      memberName: '',
      status: '',
      fromDate: null,
      toDate: null,
      minAmount: '',
      maxAmount: ''
    });
    
    this.dataSource.filter = '';
  }
  
  payFine(fineId: number): void {
    this.confirmationService.confirm(
      'Are you sure you want to mark this fine as paid?',
      'Confirm Payment'
    ).subscribe(result => {
      if (result) {
        this.router.navigate(['/fines/payment'], { 
          queryParams: { fineId: fineId } 
        });
      }
    });
  }
  
  deleteFine(fineId: number): void {
    this.confirmationService.confirmDelete('fine').subscribe(result => {
      if (result) {
        this.fineService.deleteFine(fineId).subscribe({
          next: () => {
            this.snackBar.open('Fine deleted successfully', 'Close', { duration: 3000 });
            this.loadFines();
          },
          error: (err) => {
            this.error = 'Failed to delete fine: ' + (err.message || 'Unknown error');
          }
        });
      }
    });
  }
  
  applyOverdueFines(): void {
    this.confirmationService.confirm(
      'Are you sure you want to apply fines for all overdue books?',
      'Confirm Apply Fines'
    ).subscribe(result => {
      if (result) {
        this.loading = true;
        this.fineService.applyOverdueFines().subscribe({
          next: (result) => {
            this.snackBar.open(
              `Applied ${result.finesCreated} new fines totaling ₹${result.totalAmount}`,
              'Close',
              { duration: 5000 }
            );
            this.loadFines();
            this.loading = false;
          },
          error: (err) => {
            this.error = 'Failed to apply overdue fines: ' + (err.message || 'Unknown error');
            this.loading = false;
          }
        });
      }
    });
  }
  
  viewFineDetails(fineId: number): void {
    this.router.navigate(['/fines', fineId]);
  }
  
  goToMemberDetails(): void {
    if (this.memberId) {
      this.router.navigate(['/members', this.memberId]);
    }
  }
  
  formatDate(date: string): string {
    return this.fineService.formatDate(date);
  }
  
  formatCurrency(amount: number): string {
    return '₹' + amount.toFixed(2);
  }
  
  getStatusClass(status: string): string {
    return status === 'Paid' ? 'status-paid' : 'status-pending';
  }
  
  isAdmin(): boolean {
    return this.authService.hasRole('Admin');
  }
  
  isLibrarian(): boolean {
    return this.authService.hasRole('Librarian');
  }
  
  canManageFines(): boolean {
    return this.fineService.canManageFines();
  }
  
  // Report generation methods
  toggleReportPanel(): void {
    this.showReportPanel = !this.showReportPanel;
  }
  
  generateReport(): void {
    if (this.reportForm.invalid) {
      return;
    }
    
    this.generatingReport = true;
    
    // Simulate report generation (in a real app, this would call a backend service)
    setTimeout(() => {
      this.generatingReport = false;
      this.snackBar.open('Report generated successfully', 'Close', { duration: 3000 });
      
      // If CSV format is selected, use the exportToCSV method
      if (this.reportForm.value.reportFormat === 'csv') {
        this.exportToCSV();
      }
    }, 1500);
  }
  
  getDefaultStartDate(): Date {
    const date = new Date();
    date.setMonth(date.getMonth() - 1); // One month ago
    return date;
  }
  
  getReportPreviewData(): any {
    // This would typically fetch data based on report type
    // For now, return the current summary data
    return {
      title: 'Fine Report',
      data: this.summaryData
    };
  }
  
  printReport(): void {
    window.print();
  }
  
  // Export functionality
  exportToCSV(): void {
    const data = this.dataSource.filteredData;
    if (data.length === 0) {
      this.snackBar.open('No data to export', 'Close', { duration: 3000 });
      return;
    }
    
    const headers = ['Fine ID', 'Member ID', 'Amount', 'Status', 'Transaction Date'];
    const csvData = data.map(item => [
      item.fineID,
      item.memberID,
      item.amount,
      item.status,
      this.formatDate(item.transactionDate)
    ]);
    
    csvData.unshift(headers);
    const csvString = csvData.map(row => row.join(',')).join('\n');
    
    const blob = new Blob([csvString], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const date = new Date().toISOString().slice(0, 10);
    const fileName = `fines_${date}.csv`;
    
    const url = URL.createObjectURL(blob);
    link.href = url;
    link.download = fileName;
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    this.snackBar.open('Export successful', 'Close', { duration: 3000 });
  }
}
