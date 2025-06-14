import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { MemberResponseDto, UpdateMemberDto } from '../../models/dtos/member-dtos';
import { MemberCreateUiModel } from '../../models/ui-models/member-ui-models';
import { FineDetailsDto } from '../../models/dtos/fine-dtos';
import { BorrowingTransactionDto } from '../../models/dtos/transaction-dtos';
import { ServiceMediatorService, EventTypes } from '../../core/services/service-mediator.service';
import { AuthService } from '../auth/auth.service';

@Injectable({
  providedIn: 'root'
})
export class MemberService {
  private apiBaseUrl = environment.apiBaseUrl;
  private endpoints = environment.apiEndpoints.members;

  constructor(
    private http: HttpClient,
    private serviceMediator: ServiceMediatorService,
    private authService: AuthService
    ) 
    {
    // Subscribe to fine events
    this.serviceMediator.on(EventTypes.FINE_CREATED).subscribe(fine => {
      console.log('Fine created, updating member stats', fine);
      // Handle fine creation event
    });
    
    this.serviceMediator.on(EventTypes.FINE_PAID).subscribe(fine => {
      console.log('Fine paid, updating member stats', fine);
      // Handle fine payment event
    });
  }

  // Get all members with optional filtering
  getMembers(params?: any): Observable<MemberResponseDto[]> {
    let httpParams = new HttpParams();
    if (params) {
      Object.keys(params).forEach(key => {
        if (params[key] !== null && params[key] !== undefined) {
          httpParams = httpParams.set(key, params[key].toString());
        }
      });
    }
    
    return this.http.get<MemberResponseDto[]>(`${this.apiBaseUrl}${this.endpoints.getAll}`, { params: httpParams })
      .pipe(catchError(this.handleError));
  }

  // Get a specific member
  getMember(id: number): Observable<MemberResponseDto> {
    return this.http.get<MemberResponseDto>(`${this.apiBaseUrl}${this.endpoints.getById}${id}`)
      .pipe(catchError(this.handleError));
  }

  // Get member by email
  getMemberByEmail(email: string): Observable<MemberResponseDto> {
    return this.http.get<MemberResponseDto>(`${this.apiBaseUrl}${this.endpoints.byEmail}`, { 
      params: { email }
    }).pipe(catchError(this.handleError));
  }

  // Get current logged-in member's profile - redirects to getMember with current user's ID
  getMyProfile(memberId: number): Observable<MemberResponseDto> {
    return this.getMember(memberId);
  }

  /**
   * Get the current logged-in member's profile
   * @returns Observable of the current member's profile data
   */
  getCurrentMemberProfile(): Observable<MemberResponseDto> {
    // Get the current user's member ID from the auth service
    const currentUserId = this.authService.memberId;
    
    if (!currentUserId) {
      return throwError(() => new Error('No authenticated member found'));
    }
    
    // Use the existing getMember method to fetch the profile by ID
    return this.getMember(currentUserId).pipe(
      catchError(error => {
        console.error('Error fetching current member profile:', error);
        return throwError(() => new Error('Failed to load your profile. Please try again later.'));
      })
    );
  }

  // Create a new member - Using UI model instead of DTO
  createMember(member: MemberCreateUiModel): Observable<MemberResponseDto> {
    return this.http.post<MemberResponseDto>(`${this.apiBaseUrl}${this.endpoints.register}`, member)
      .pipe(catchError(this.handleError));
  }

  // Update a member
  updateMember(member: UpdateMemberDto): Observable<MemberResponseDto> {
    const updatedMember$ = this.http.put<MemberResponseDto>(`${this.apiBaseUrl}${this.endpoints.update}${member.memberID}`, member)
      .pipe(catchError(this.handleError));
    
    // Notify other services about member update
    updatedMember$.pipe(
      tap(() => this.serviceMediator.publish(EventTypes.MEMBER_UPDATED, member))
    ).subscribe();

    return updatedMember$;
  }

  // Delete a member
  deleteMember(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}${this.endpoints.delete}${id}`)
      .pipe(catchError(this.handleError));
  }

  // Get member borrowings
  getMemberBorrowings(memberId: number): Observable<BorrowingTransactionDto[]> {
    return this.http.get<BorrowingTransactionDto[]>(
      `${this.apiBaseUrl}${this.endpoints.getMemberBorrowings}${memberId}/borrowings`
    ).pipe(catchError(this.handleError));
  }

  // Get member fines
  getMemberFines(memberId: number): Observable<FineDetailsDto[]> {
    return this.http.get<FineDetailsDto[]>(
      `${this.apiBaseUrl}${this.endpoints.getMemberFines}${memberId}/fines`
    ).pipe(catchError(this.handleError));
  }

  // Check membership status - read-only functionality
  checkMembershipStatus(memberId: number): Observable<{status: string, canBorrow: boolean, reason?: string}> {
    return this.http.get<{status: string, canBorrow: boolean, reason?: string}>(
      `${this.apiBaseUrl}${this.endpoints.checkMembershipStatus}`,
      { params: { memberId: memberId.toString() } }
    ).pipe(catchError(this.handleError));
  }

  // Check if member can be deleted - derived from member data since there's no specific endpoint
  canDeleteMember(id: number): Observable<{canDelete: boolean, reason?: string}> {
    // First get the member's borrowings
    return this.getMemberBorrowings(id).pipe(
      map(borrowings => {
        const activeBorrowings = borrowings.filter(b => b.status !== 'Returned');
        
        if (activeBorrowings.length > 0) {
          return {
            canDelete: false,
            reason: `Member has ${activeBorrowings.length} active borrowings. Return all books before deleting.`
          };
        }
        
        return { canDelete: true };
      }),
      catchError(error => {
        // If we can't get borrowings, assume we can't delete
        return of({
          canDelete: false,
          reason: 'Unable to verify member status. Please try again later.'
        });
      })
    );
  }

  // Helper function to determine if a member has outstanding fines
  hasOutstandingFines(memberId: number): Observable<boolean> {
    return this.getMemberFines(memberId).pipe(
      map(fines => fines.some(fine => fine.status === 'Pending')),
      catchError(() => of(false)) // Default to false on error
    );
  }

  // Helper function to calculate total outstanding fine amount
  getTotalOutstandingFines(memberId: number): Observable<number> {
    return this.getMemberFines(memberId).pipe(
      map(fines => fines
        .filter(fine => fine.status === 'Pending')
        .reduce((total, fine) => total + fine.amount, 0)
      ),
      catchError(() => of(0)) // Default to 0 on error
    );
  }

  // Get count of member's outstanding fines
  getMemberOutstandingFinesCount(memberId: number): Observable<number> {
    return this.getMemberFines(memberId).pipe(
      map(fines => fines.filter(f => f.status === 'Pending').length),
      catchError(error => {
        console.error('Error getting fines count:', error);
        return of(0);
      })
    );
  }

  // Get member with fines
  getMemberWithFines(memberId: number): Observable<any> {
    return this.getMember(memberId).pipe(
      map(member => {
        // Instead of using FineService, we'll leave fines empty
        // Other components can load fines separately
        return { ...member, fines: [] };
      }),
      catchError(err => {
        console.error('Error loading member with fines:', err);
        return of(null);
      })
    );
  }

  /**
   * Search members by name, email, or ID
   * This performs client-side filtering since there's no server search endpoint
   */
  searchMembers(searchTerm: string): Observable<MemberResponseDto[]> {
    if (!searchTerm.trim()) {
      // If empty search, return all members
      return this.getMembers();
    }
    
    // Get all members and filter client-side
    return this.getMembers().pipe(
      map(members => {
        const term = searchTerm.toLowerCase().trim();
        
        return members.filter(member => 
          // Search by name
          (member.name && member.name.toLowerCase().includes(term)) ||
          // Search by email
          (member.email && member.email.toLowerCase().includes(term)) ||
          // Search by ID (convert ID to string for searching)
          member.memberID.toString().includes(term)
        );
      })
    );
  }

  private handleError(error: any) {
    console.error('An error occurred in MemberService', error);
    return throwError(() => error);
  }
}
