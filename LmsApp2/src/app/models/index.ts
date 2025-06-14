// Main barrel file for exporting all models

// Export all DTOs
export * from './dtos/auth-dtos';
export * from './dtos/member-dtos';
export * from './dtos/book-dtos';
export * from './dtos/transaction-dtos';
export * from './dtos/fine-dtos';
export * from './dtos/notification-dtos';

// Export any frontend-specific models
export * from './ui-models/transaction-ui-models';
export * from './ui-models/book-ui-models';
export * from './ui-models/member-ui-models';
export * from './ui-models/notification-ui-models';
export * from './ui-models/fine-ui-models';
export * from './ui-models/auth-ui-models';

// Temporary re-exports for backward compatibility
// These will be removed in future versions
export * from './auth.model';
export * from './book.model';
// Explicitly re-export with renamed conflicting member
export { MemberResponseDto as LegacyMemberResponseDto, /* include other exports here */ } from './member.model';
export * from './transaction.model';
export * from './notification.model';
