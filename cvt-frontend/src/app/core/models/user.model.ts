// Reflète exactement AuthService.DTOs.UserResponse (AuthService/DTOs/UserResponse.cs)
export interface User {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  role: string; // Une seule valeur possible côté backend actuel : "ADMIN"
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  lastLoginAt: string | null;
}

// Reflète AuthService.DTOs.LoginRequest
export interface LoginRequest {
  email: string;
  password: string;
}

// Reflète AuthService.DTOs.LoginResponse
export interface LoginResponse {
  token: string;
  user: User;
}

// Reflète AuthService.DTOs.CreateUserRequest
export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

// Reflète AuthService.DTOs.UpdateUserRequest
export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
}

// Reflète AuthService.DTOs.ChangePasswordRequest
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

// Reflète AuthService.DTOs.ResetPasswordRequest
export interface ResetPasswordRequest {
  newPassword: string;
}
