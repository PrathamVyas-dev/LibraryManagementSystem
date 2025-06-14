/**
 * @deprecated - Please import DTO interfaces from '@app/models/dtos' 
 * and UI models from '@app/models/ui-models/member-ui-models' instead.
 * This file will be removed in a future update.
 */

// NOTE: DO NOT MODIFY THESE DTOs - they match the backend contract

// Re-export these DTOs for backward compatibility
export interface MemberDetailsDto {
  memberID: number;
  name: string;
  email: string;
  phone: string;
  address?: string;
  membershipStatus: string; // "Active" or "Suspended"
  joinDate: string; // ISO date string
  totalBorrowings?: number;
  activeBorrowings?: number;
  overdueItems?: number;
  outstandingFines?: number;
  roles?: string[];
}

export interface MemberCreateDto {
  name: string;
  email: string;
  password: string;
  phone: string;
  address?: string;
  role?: string;
}

export interface MemberUpdateDto {
  memberID: number;
  name: string;
  phone: string;
  address?: string;
}

export interface FineDetailsDto {
  fineID: number;
  transactionID: number;
  bookID: number;
  bookName?: string;
  amount: number;
  status: string; // "Paid" or "Unpaid"
  transactionDate: string; // ISO date string
  dueDate?: string; // ISO date string
}

export interface MemberResponseDto {
  memberID: number;
  name: string;
  email: string;
  membershipStatus: string;
}

export interface MemberSearchParams {
  name?: string;
  email?: string;
  status?: string;
  hasOverdue?: boolean;
  hasFines?: boolean;
}
