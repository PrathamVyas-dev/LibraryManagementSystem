/**
 * @deprecated - Please import DTOs from '@app/models/dtos/book-dtos'
 * and UI models from '@app/models/ui-models/book-ui-models' instead.
 * This file will be removed in a future update.
 */

// Import from the new centralized model location
import { 
  BookDetailsDto, 
  CreateBookDto, 
  UpdateBookDto, 
  SearchBooksDto 
} from '../../models/dtos/book-dtos';

// Re-export DTOs for backward compatibility
export { BookDetailsDto, CreateBookDto, UpdateBookDto, SearchBooksDto };

/**
 * @deprecated Use BookUiModel from ui-models instead
 */
export interface Book {
  bookID?: number;
  title: string;
  author: string;
  isbn: string;
  publishedYear: number;
  yearPublished?: number; // Alias for compatibility with existing components
  genre: string;
  totalCopies: number;
  availableCopies: number;
}
