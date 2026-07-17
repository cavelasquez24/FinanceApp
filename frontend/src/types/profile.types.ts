export interface UserInfo {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  currencyCode: string;
  paydayDay: number | null;
}

export interface UpdateProfileDto {
  firstName: string;
  lastName: string;
}

export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ChangeCurrencyDto {
  currencyCode: string;
}

export interface UpdatePaydayDto {
  paydayDay: number | null; // null = volver a mes calendario
}