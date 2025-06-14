/**
 * UI-specific models for fines feature
 * Note: These are different from DTOs and are used only in the frontend
 */

export interface FineFilterUiModel {
  memberName?: string;
  status?: string;
  fromDate?: Date | string;
  toDate?: Date | string;
  minAmount?: number;
  maxAmount?: number;
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface FineDisplayUiModel {
  fineID: number;
  memberID: number;
  memberName?: string;  // UI-only field
  amount: number;
  status: string;
  transactionDate: string;  // Used for both creation and payment date
  formattedTransactionDate?: string; // UI-only formatted version
  daysOverdue?: number;  // UI calculation
}

export interface FineSummaryUiModel {
  totalFines: number;
  pendingFines: number;
  paidFines: number;
  totalAmount: number;
  pendingAmount: number;
  paidAmount: number;
  averageAmount: number;
  // Keep backward compatibility
  averageFineAmount?: number;
}

/**
 * UI model for fine payment form
 * This is used only for the UI form, not for API submission
 */
export interface FinePaymentUiModel {
  fineID: number;
  amount: number;
  paymentMethod?: string;
  paymentDate?: Date;
  receiptNumber?: string;
  notes?: string;
}

export interface OverdueFineApplicationResultUiModel {
  appliedFines: number;
  totalAmount: number;
  members: number;
  books: number;
  finesCreated: number; // Add the missing property
  errors?: string[];
}
