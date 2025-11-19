import axios from 'axios';
import keycloak from '../keycloak';
import { User, CreateUserRequest, UpdateUserRequest, Role } from '../types';

const API_BASE_URL = 'http://localhost:5001/api';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
});

// Interceptor to add JWT token to requests
apiClient.interceptors.request.use(
  (config) => {
    if (keycloak.token) {
      config.headers.Authorization = `Bearer ${keycloak.token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Interceptor to refresh token if expired
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        await keycloak.updateToken(30);
        originalRequest.headers.Authorization = `Bearer ${keycloak.token}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        keycloak.login();
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export const userApi = {
  getAll: async (): Promise<User[]> => {
    const response = await apiClient.get<User[]>('/Users');
    return response.data;
  },

  getById: async (id: string): Promise<User> => {
    const response = await apiClient.get<User>(`/Users/${id}`);
    return response.data;
  },

  create: async (data: CreateUserRequest): Promise<User> => {
    const response = await apiClient.post<User>('/Users', data);
    return response.data;
  },

  update: async (id: string, data: UpdateUserRequest): Promise<User> => {
    const response = await apiClient.put<User>(`/Users/${id}`, data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/Users/${id}`);
  },

  assignRole: async (userId: string, roleId: string): Promise<void> => {
    await apiClient.post(`/Users/${userId}/roles/${roleId}`);
  },

  removeRole: async (userId: string, roleId: string): Promise<void> => {
    await apiClient.delete(`/Users/${userId}/roles/${roleId}`);
  },
};

export const roleApi = {
  getAll: async (): Promise<Role[]> => {
    const response = await apiClient.get<Role[]>('/Roles');
    return response.data;
  },
};

export default apiClient;
