/**
 * @deprecated - Please import from '@app/models/dtos/auth-dtos' or '@app/models/ui-models/auth-ui-models' instead
 * This file will be removed in a future update.
 */

// Import from the new centralized model location
import { LoginMemberDto, RegisterMemberDto, TokenResponseDto } from './dtos/auth-dtos';
import { MemberResponseDto } from './dtos/member-dtos';
import { AuthUserUiModel } from './ui-models/auth-ui-models';

// Re-export for backward compatibility
export { LoginMemberDto, RegisterMemberDto, TokenResponseDto, MemberResponseDto };

// Legacy interfaces - marked as deprecated
/**
 * @deprecated Use TokenResponseDto instead
 */
export interface AuthResponse {
    token: string;
    expiration?: string;
    email: string; 
    roles: string[];
    userId?: string;
}

/**
 * @deprecated Use AuthUserUiModel from ui-models instead
 */
export interface User {
    username: string;
    roles: string[];
    token: string;
    userId?: string;
}
