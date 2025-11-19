export interface User {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  isActive: boolean;
  createdAt: string;
  keycloakId?: string;
  userRoles?: UserRole[];
}

export interface Role {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface UserRole {
  id: string;
  userId: string;
  roleId: string;
  role: Role;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  password: string;
}

export interface UpdateUserRequest {
  email?: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
  isActive?: boolean;
}
