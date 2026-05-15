export type UserRole = 'SuperAdmin' | 'Admin' | 'Tenant';

export type TransactionStatus = 'Pending' | 'Confirmed' | 'Declined' | 'Overdue';

export type TransactionType =
  | 'Rent'
  | 'Deposit'
  | 'LateFee'
  | 'Maintenance'
  | 'Utility'
  | 'Parking'
  | 'Pet'
  | 'Refund'
  | 'Other';

export type PaymentMethod = 'Stripe' | 'External' | 'Manual';

export type NotificationType =
  | 'PaymentConfirmed'
  | 'PaymentPending'
  | 'PaymentDeclined'
  | 'PaymentOverdue'
  | 'RentReminder'
  | 'InviteCreated'
  | 'ContractUploaded';

export interface User {
  id: string;
  email: string;
  role: UserRole;
  isActive: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
}

export interface TotpValidationRequest {
  temporaryToken: string;
  totpCode: string;
}

export interface Transaction {
  id: string;
  tenantId: string;
  unitId: string;
  type: TransactionType;
  amount: number;
  status: TransactionStatus;
  paymentMethod: PaymentMethod;
  externalMethodNote?: string;
  dueDate?: string;
  paidDate?: string;
  createdAt: string;
  updatedAt: string;
}

export interface Contract {
  id: string;
  tenantId: string;
  fileName: string;
  isCurrent: boolean;
  uploadedAt: string;
  downloadUrl: string;
}

export interface Notification {
  id: string;
  type: NotificationType;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface RentSchedule {
  id: string;
  tenantId: string;
  unitId: string;
  monthlyAmount: number;
  dueDayOfMonth: number;
  startDate: string;
}

export interface ReminderSetting {
  id: string;
  daysBefore: number;
  sendTime: string;
  isActive: boolean;
}

export interface NotificationPreference {
  emailEnabled: boolean;
}

export interface ApiError {
  message: string;
  status: number;
}