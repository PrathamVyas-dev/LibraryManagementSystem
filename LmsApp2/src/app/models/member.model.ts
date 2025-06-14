/**
 * @deprecated - Please import DTO interfaces from '@app/models/dtos' 
 * and UI models from '@app/models/ui-models/member-ui-models' instead.
 * This file will be removed in a future update.
 */

// Import UI models from the new location
import {
  MemberUiModel,
  MemberDetailsUiModel,
  MemberSearchParamsUiModel
} from './ui-models/member-ui-models';

// NOTE: DO NOT MODIFY THESE DTOs - they match the backend contract

// Re-export these DTOs for backward compatibility
export interface MemberResponseDto {
    memberID: number;
    name: string;
    email: string;
    phone?: string;
    address?: string;
    status: string;
}

export interface MemberDetailsDto {
    memberID: number;
    name: string; // Make name required
    firstName?: string;
    lastName?: string;
    email: string;
    phone: string; // Make this required to match the feature module
    address?: string;
    status: string;
    membershipStatus?: string;
    roles?: string[];
    overdueItems?: number;
    outstandingFines?: number;
}

export interface MemberCreateDto {
    name: string;
    email: string;
    phone?: string;
    address?: string;
    password?: string;
    role?: string;
}

export interface MemberUpdateDto {
    memberID: number;
    name: string;
    email: string;
    phone?: string;
    address?: string;
    status?: string;
}

export interface MemberSearchParams {
    name?: string;
    email?: string;
    status?: string;
    hasOverdue?: boolean;
    hasFines?: boolean;
    page?: number;
    pageSize?: number;
}
