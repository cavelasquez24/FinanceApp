export interface UserInfo {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  currencyCode: string;
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
  currencyCode: string; // "USD" | "EUR" | "COP" | "PEN" | "MXN" | "ARS" | "CLP" | "BRL"
}