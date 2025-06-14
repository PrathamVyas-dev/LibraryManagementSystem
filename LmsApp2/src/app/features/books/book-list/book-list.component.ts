import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { Router } from '@angular/router';
import { BookService } from '../book.service';
import { BookDetailsDto } from '../../../models/dtos/book-dtos';
import { BookSearchUiModel } from '../../../models/ui-models/book-ui-models';
import { AuthService } from '../../auth/auth.service';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { ConfirmationDialogService } from '../../../core/confirmation-dialog.service';

@Component({
  selector: 'app-book-list',
  templateUrl: './book-list.component.html',
  styleUrls: ['./book-list.component.scss']
})
export class BookListComponent implements OnInit, AfterViewInit {
  displayedColumns: string[] = ['title', 'author', 'genre', 'isbn', 'yearPublished', 'availableCopies', 'actions'];
  dataSource = new MatTableDataSource<BookDetailsDto>([]);
  loading = false;
  error: string | null = null;
  searchTerm: string = '';

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  paginationOptions = {
    pageSize: 10,
    pageSizeOptions: [5, 10, 25, 50]
  };

  currentSearchParams: BookSearchUiModel = {};

  constructor(
    private bookService: BookService,
    private authService: AuthService,
    private confirmationService: ConfirmationDialogService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadBooks();
  }

  ngAfterViewInit() {
    // Safely initialize the paginator if it exists
    if (this.paginator) {
      this.dataSource.paginator = this.paginator;
    }

    // Safely initialize the sort if it exists
    if (this.sort) {
      this.dataSource.sort = this.sort;
      
      // Only subscribe to sortChange if sort exists
      this.sort.sortChange.subscribe(() => {
        // Reset paginator when sort changes
        if (this.paginator) {
          this.paginator.pageIndex = 0;
        }
      });
    }

    // Use setTimeout as a safety net to ensure everything is initialized
    setTimeout(() => {
      if (this.paginator && !this.dataSource.paginator) {
        this.dataSource.paginator = this.paginator;
      }
      
      if (this.sort && !this.dataSource.sort) {
        this.dataSource.sort = this.sort;
      }
    });
  }

  loadBooks(): void {
    this.loading = true;
    this.bookService.getBooks().subscribe({
      next: books => {
        this.dataSource.data = books;
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
        this.loading = false;
      },
      error: err => {
        this.error = 'Failed to load books.';
        this.loading = false;
      }
    });
  }

  loadBooksWithFilters(): void {
    this.loading = true;

    const params = {
      pageNumber: this.paginator?.pageIndex + 1 || 1,
      pageSize: this.paginator?.pageSize || this.paginationOptions.pageSize,
      sortBy: this.sort?.active || 'title',
      sortDirection: this.sort?.direction || 'asc',
      ...this.currentSearchParams
    };

    this.bookService.getBooksWithPagination(params).subscribe({
      next: response => {
        this.dataSource.data = response.items;
        this.paginator.length = response.totalCount;
        this.loading = false;
      },
      error: err => {
        this.error = 'Failed to load books.';
        this.loading = false;
      }
    });
  }

  applyFilter(filterValue: string) {
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  onSearch(params: BookSearchUiModel) {
    this.loading = true;
    this.currentSearchParams = params;
    this.bookService.searchBooks(params).subscribe({
      next: books => {
        this.dataSource.data = books;
        this.loading = false;
      },
      error: err => {
        this.error = 'Failed to search books.';
        this.loading = false;
      }
    });
  }

  canEditOrDelete(): boolean {
    return this.authService.hasRole('Admin') || this.authService.hasRole('Librarian');
  }

  isAdminOrLibrarian(): boolean {
    return this.authService.hasRole('Admin') || this.authService.hasRole('Librarian');
  }

  get books() {
    return this.dataSource.filteredData;
  }

  /**
   * Search books with the current search term
   */
  searchBooks(): void {
    if (!this.searchTerm.trim()) {
      this.loadBooks();
      return;
    }
    
    this.loading = true;
    this.error = null;
    
    // Client-side filtering approach
    const searchTermLower = this.searchTerm.toLowerCase();
    const filteredBooks = this.dataSource.data.filter(book => 
      book.title.toLowerCase().includes(searchTermLower) ||
      book.author.toLowerCase().includes(searchTermLower) ||
      book.genre.toLowerCase().includes(searchTermLower) ||
      book.isbn.includes(this.searchTerm)
    );
    
    // Update the filtered data directly
    this.dataSource.data = filteredBooks;
    this.loading = false;
    
    // Make sure pagination is updated
    if (this.paginator) {
      this.dataSource.paginator = this.paginator;
    }
    
    // Make sure sorting is applied
    if (this.sort) {
      this.dataSource.sort = this.sort;
    }
  }

  deleteBook(bookID: number, bookTitle: string) {
    this.confirmationService.confirmDelete(bookTitle).subscribe(confirmed => {
      if (confirmed) {
        this.loading = true;
        this.bookService.deleteBook(bookID).subscribe({
          next: () => {
            this.loadBooks();
          },
          error: () => {
            this.error = 'Failed to delete book.';
            this.loading = false;
          }
        });
      }
    });
  }
  
  borrowBook(book: BookDetailsDto): void {
    if (book.availableCopies <= 0) {
      this.confirmationService.confirm(
        'This book is currently not available for borrowing. Please check back later.',
        'Book Not Available',
        'info'
      );
      return;
    }
    
    if (!this.authService.isAuthenticated()) {
      this.confirmationService.confirm(
        'You need to be logged in to borrow books. Would you like to login now?',
        'Login Required',
        'info'
      ).subscribe(result => {
        if (result) {
          this.router.navigate(['/login'], { 
            queryParams: { returnUrl: this.router.url } 
          });
        }
      });
      return;
    }
    
    this.router.navigate(['/transactions/borrow'], { 
      queryParams: { bookId: book.bookID } 
    });
  }
}
