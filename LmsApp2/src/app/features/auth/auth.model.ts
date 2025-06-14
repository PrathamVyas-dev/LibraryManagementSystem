/**
 * @deprecated - Please import from '@app/models/dtos/auth-dtos' or '@app/models/ui-models/auth-ui-models' instead
 */

// Import from the new centralized model location
import { LoginMemberDto, RegisterMemberDto, TokenResponseDto } from '../../models/dtos/auth-dtos';
import { AuthUserUiModel } from '../../models/ui-models/auth-ui-models';
import { MemberResponseDto } from '../../models/dtos/member-dtos';

// Re-export for backward compatibility
export { LoginMemberDto, RegisterMemberDto, TokenResponseDto, MemberResponseDto };

// Legacy interfaces - marked as deprecated
/**
 * @deprecated Use LoginMemberDto instead
 */
export interface LoginRequest {
    email: string; 
    password: string;
}

/**
 * @deprecated Use RegisterMemberDto instead
 */
export interface RegisterRequest {
    name : string;
    email: string; 
    password: string;
    phone : number;
    address : string;
}

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
