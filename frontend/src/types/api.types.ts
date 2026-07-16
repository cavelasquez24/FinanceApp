// Formato estándar de respuesta — igual al backend
export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string | null;
  error: ApiError | null;
}

export interface ApiError {
  code: string;
  message: string;
  details: ApiErrorDetail[];
}

export interface ApiErrorDetail {
  field: string;
  message: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}