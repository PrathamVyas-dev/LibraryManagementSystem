/**
 * @deprecated - Please import DTOs from '@app/models/dtos/transaction-dtos' 
 * and UI models from '@app/models/ui-models/transaction-ui-models' instead.
 * This file will be removed in a future update.
 */

// Import from the new centralized model location
import { 
  BorrowingTransactionDto,
  BorrowBookDto,
  ReturnBookDto,
} from '../../models/dtos/transaction-dtos';
// Re-export DTOs for backward compatibility
export { 
  BorrowingTransactionDto,
  BorrowBookDto,
  ReturnBookDto,
  UpdateBorrowingTransactionDto
};

/**
 * @deprecated Use TransactionFilterUiModel from ui-models/transaction-ui-models instead
 */
export interface TransactionFilter {
  status?: string;
  bookTitle?: string;
  memberName?: string;
  fromDate?: Date | null;
  toDate?: Date | null;
  overdueOnly?: boolean;
}

/**
 * @deprecated Use TransactionSummaryUiModel from ui-models/transaction-ui-models instead
 */
export interface TransactionSummary {
  totalTransactions: number;
  activeBorrowings: number;
  overdueItems: number;
  returnedItems: number;
  mostBorrowedBooks?: {
    bookId: number;
    title: string;
    borrowCount: number;
  }[];
  activeMembersCount?: number;
}

/**
 * @deprecated Use MemberBorrowingStatusUiModel from ui-models/transaction-ui-models instead
 */
export interface MemberBorrowingStatus {
  memberId: number;
  memberName: string;
  currentBorrowings: number;
  maxAllowedBorrowings: number;
  hasOverdueItems: boolean;
  isActive: boolean;
  canBorrow: boolean;
  reasonCannotBorrow?: string;
}

/**
 * @deprecated Use PaginatedResponseUiModel from ui-models/transaction-ui-models instead
 */
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

/**
 * @deprecated Use ExtendBorrowingUiModel from ui-models/transaction-ui-models instead
 */
export interface ExtendBorrowingDto {
  transactionID: number;
  newDueDate: Date;
}

/**
 * @deprecated Use TransactionSearchParamsUiModel from ui-models/transaction-ui-models instead
 */
export interface TransactionSearchParams {
  memberID?: number;
  bookID?: number;
  status?: string;
  fromDate?: Date;
  toDate?: Date;
  overdue?: boolean;
  page?: number;
  pageSize?: number;
}
