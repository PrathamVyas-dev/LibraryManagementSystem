/**
 * @deprecated - Please import DTOs from '@app/models/dtos/notification-dtos' 
 * and UI models from '@app/models/ui-models/notification-ui-models' instead.
 * This file will be removed in a future update.
 */

// Import from the new centralized model location
import { 
  NotificationDetailsDto,
  CreateNotificationDto
} from './dtos/notification-dtos';

import {
  NotificationFilterUiModel
} from './ui-models/notification-ui-models';

// Re-export DTOs for backward compatibility
export { NotificationDetailsDto, CreateNotificationDto };

/**
 * @deprecated Use NotificationFilterUiModel from ui-models/notification-ui-models instead
 */
export interface NotificationFilter {
  memberId?: number;
  memberName?: string;
  fromDate?: Date;
  toDate?: Date;
}
