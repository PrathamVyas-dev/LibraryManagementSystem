# SampleProject
Library Management System

## Models Structure

The project uses a centralized models folder to organize all interfaces:

### DTOs (Data Transfer Objects)

Located in `src/app/models/dtos/`:

- `auth-dtos.ts` - Authentication related DTOs (Login, Register, Token)
- `member-dtos.ts` - Member related DTOs
- `book-dtos.ts` - Book related DTOs
- `transaction-dtos.ts` - Borrowing transaction DTOs
- `fine-dtos.ts` - Fine related DTOs
- `notification-dtos.ts` - Notification DTOs

These DTOs exactly match the backend API contract and should not be modified.

### UI Models

Located in `src/app/models/ui-models/`:

- Frontend-specific interfaces that don't match backend DTOs
- Use the suffix `UiModel` to distinguish from DTOs

### Naming Conventions

- DTOs: Match the backend exactly and use the `Dto` suffix
- UI Models: Use a descriptive name with `UiModel` suffix
- Never use the same name for both to avoid confusion

### Importing Models

Import models using the barrel files:

```typescript
// Import all models
import { LoginMemberDto, BookDetailsDto } from '@app/models';

// Import DTOs
import { BorrowBookDto } from '@app/models/dtos/transaction-dtos';

// Import UI models
import { BookFilterUiModel } from '@app/models/ui-models/book-ui-models';
```

### Migration Notes

Some model interfaces are being migrated from feature-specific files to the centralized models structure. Files with @deprecated notices are being phased out - please use the new imports.
