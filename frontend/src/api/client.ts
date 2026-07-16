import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';

// URL base de la API — en producción vendrá de variables de entorno
const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5271';

// Instancia de Axios configurada para nuestra API
export const apiClient = axios.create({
  baseURL: `${BASE_URL}/api/v1`,
  headers: {
    'Content-Type': 'application/json',
  },
});

// ── Interceptor de REQUEST ─────────────────────────────────────────────
// Se ejecuta antes de cada request
// Agrega automáticamente el token JWT al header Authorization
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// ── Interceptor de RESPONSE ────────────────────────────────────────────
// Se ejecuta después de cada respuesta
// Maneja automáticamente el refresh token cuando el JWT expira
apiClient.interceptors.response.use(
  // Si la respuesta es exitosa, la dejamos pasar
  (response) => response,

  // Si hay error, intentamos recuperarnos
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retry?: boolean;
    };

    // Si el error es 401 (no autorizado) y no hemos reintentado ya
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = localStorage.getItem('refreshToken');

        if (!refreshToken) {
          // No hay refresh token — redirigir al login
          redirectToLogin();
          return Promise.reject(error);
        }

        // Intentamos renovar el access token
        const response = await axios.post(
          `${BASE_URL}/api/v1/auth/refresh-token`,
          { refreshToken }
        );

        const { accessToken, refreshToken: newRefreshToken } =
          response.data.data;

        // Guardamos los nuevos tokens
        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('refreshToken', newRefreshToken);

        // Reintentamos el request original con el nuevo token
        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        return apiClient(originalRequest);

      } catch {
        // El refresh token también expiró — forzar logout
        redirectToLogin();
        return Promise.reject(error);
      }
    }

    return Promise.reject(error);
  }
);

function redirectToLogin() {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
  window.location.href = '/login';
}