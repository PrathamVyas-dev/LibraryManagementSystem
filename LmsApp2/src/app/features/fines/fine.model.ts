/**
 * @deprecated - Please import DTOs from '@app/models/dtos/fine-dtos' 
 * and UI models from '@app/models/ui-models/fine-ui-models' instead.
 * This file will be removed in a future update.
 */

// Re-export from DTOs and UI models
export {
  // DTO models
  CreateFineDto,
  UpdateFineDto,
  FineDetailsDto,
  PayFineDto
  // Remove the conflicting FinePaymentDto export
} from '../../models/dtos/fine-dtos';

export {
  // UI models
  FineFilterUiModel,
  FineDisplayUiModel,
  FineSummaryUiModel,
  FinePaymentUiModel
} from '../../models/ui-models/fine-ui-models';

/**
 * @deprecated Use CreateFineDto from dtos/fine-dtos instead
 */
export interface FineCreateDto {
  memberID: number;
  amount: number;
  status: string; // Required in backend
  transactionDate: string; // ISO date string, required in backend
  bookID?: number;
  reason?: string;
}

/**
 * @deprecated Use UpdateFineDto from dtos/fine-dtos instead
 */
export interface FineUpdateDto {
  fineID: number;
  memberID: number;
  amount: number;
  status: string;
  transactionDate: string; // ISO date string
  bookID?: number;
  reason?: string;
}

/**
 * @deprecated Use PayFineDto with FinePaymentUiModel from ui-models/fine-ui-models instead
 */
export interface FinePaymentDto {
  fineID: number;
  amount: number;
  paymentMethod?: string;
  notes?: string;
}

/**
 * @deprecated Use FineFilterUiModel from ui-models/fine-ui-models instead
 */
export interface FineFilter {
  memberID?: number;
  memberName?: string;
  status?: string;
  fromDate?: Date | null;
  toDate?: Date | null;
  minAmount?: number;
  maxAmount?: number;
}

/**
 * @deprecated Use OverdueFineApplicationResultUiModel from ui-models/fine-ui-models instead
 */
export interface OverdueFineApplicationResultDto {
  totalProcessed: number;
  finesCreated: number;
  finesUpdated: number;
  noActionNeeded: number;
  errorCount: number;
  totalAmount: number;
}

/**
 * @deprecated Use FineSummaryUiModel from ui-models/fine-ui-models instead
 */
export interface FineSummary {
  totalFines: number;
  pendingFines: number;
  paidFines: number;
  totalAmount: number;
  pendingAmount: number;
  paidAmount: number;
  averageFineAmount: number;
}
