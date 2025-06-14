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
} from './dtos/transaction-dtos';

// Re-export DTOs for backward compatibility
export { BorrowingTransactionDto, BorrowBookDto, ReturnBookDto };

/**
 * @deprecated Use BorrowBookDto from dtos/transaction-dtos instead
 */
export interface CreateBorrowingDto {
    bookID: number;
    memberID: number;
    borrowDate: Date;
    dueDate: Date;
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
