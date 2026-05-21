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

export type PaymentMethod = 'Stripe' | 'Ach' | 'External' | 'Manual';

export type SubscriptionStatusValue = 'None' | 'Trialing' | 'Active' | 'PastDue' | 'Canceled';

export type BillingMode = 'PerTenant' | 'SharedUnit';

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

export interface NotificationEmail {
  id: string;
  email: string;
}

export interface UserProfile {
  email: string;
  role: string;
  isProfileComplete: boolean;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  notificationEmails: NotificationEmail[];
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
  previewUrl: string;
}

export interface PublicUserProfile {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
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
  tenantId?: string;
  unitId: string;
  monthlyAmount: number;
  dueDayOfMonth: number;
  startDate: string;
  endDate?: string;
  isDeleted: boolean;
  deletedAt?: string;
}

export interface UnitPropertyInfo {
  unitId: string;
  unitNumber: string;
  bedrooms?: number;
  bathrooms?: number;
  squareFeet?: number;
  billingMode: BillingMode;
  propertyId: string;
  propertyName: string;
  propertyAddress: string;
  adminId?: string;
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

export interface SubscriptionStatusResponse {
  status: SubscriptionStatusValue;
  isActive: boolean;
  maxTenants: number | null;
  currentTenantCount: number;
}

export interface AdminRegisterResponse {
  checkoutUrl: string;
  totpSetup: {
    manualEntryKey: string;
    qrCode: string;
  };
}

export interface Property {
  id: string;
  name: string;
  address: string;
  isActive: boolean;
  createdAt: string;
}

export interface Unit {
  id: string;
  propertyId: string;
  unitNumber: string;
  bedrooms?: number;
  bathrooms?: number;
  squareFeet?: number;
  isActive: boolean;
  billingMode: BillingMode;
  currentTenantIds: string[];
}

export interface ConnectStatus {
  isConnected: boolean;
  chargesEnabled: boolean;
  payoutsEnabled: boolean;
  dashboardUrl?: string;
}

export interface ApiError {
  message: string;
  status: number;
}
