# Frontend Integration Guide

## Overview

This guide provides complete integration instructions for connecting the Smart Insights Aggregator Next.js frontend with the .NET 8 backend API. The backend follows Clean Architecture and provides 48 RESTful endpoints across 8 controllers.

### Backend Stack
- .NET 8.0 Web API
- PostgreSQL 16 with Entity Framework Core
- JWT Bearer Authentication
- Azure OpenAI (GPT-4) Integration
- Serilog Logging

### Integration Architecture
- **API Client**: Axios with interceptors for JWT handling
- **Authentication**: JWT tokens stored in localStorage/cookies
- **Type Safety**: TypeScript interfaces matching backend DTOs
- **Error Handling**: Centralized error interceptors
- **State Management**: React Context for auth + React Query/SWR for data fetching

---

## 1. Environment Variables Setup

Create a `.env.local` file in your Next.js project root:

```env
# Backend API Configuration
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000/api
NEXT_PUBLIC_API_TIMEOUT=30000

# Authentication
NEXT_PUBLIC_JWT_STORAGE_KEY=smart_insights_token
NEXT_PUBLIC_REFRESH_TOKEN_KEY=smart_insights_refresh

# Feature Flags
NEXT_PUBLIC_ENABLE_ANONYMOUS_FEEDBACK=true
NEXT_PUBLIC_ENABLE_AI_ANALYSIS=true

# Optional: Development
NEXT_PUBLIC_DEBUG_API=true
```

---

## 2. Type Definitions

Create `types/api.ts`:

```typescript
// ============================================================================
// Common Types
// ============================================================================

export interface ApiResponse<T> {
  data: T;
  message: string;
  success: boolean;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ValidationError {
  field: string;
  message: string;
}

// ============================================================================
// Enums (matching backend)
// ============================================================================

export enum Role {
  Admin = 'Admin',
  Student = 'Student'
}

export enum InquiryStatus {
  Draft = 'Draft',
  Sent = 'Sent',
  Closed = 'Closed'
}

export enum InputStatus {
  Pending = 'Pending',
  Reviewed = 'Reviewed',
  Resolved = 'Resolved',
  Archived = 'Archived'
}

export enum InputType {
  General = 'General',
  InquiryLinked = 'InquiryLinked'
}

export enum Sentiment {
  Positive = 'Positive',
  Neutral = 'Neutral',
  Negative = 'Negative'
}

export enum Tone {
  Positive = 'Positive',
  Neutral = 'Neutral',
  Negative = 'Negative'
}

export enum ThemeType {
  Infrastructure = 'Infrastructure',
  Academic = 'Academic',
  Technology = 'Technology',
  Administration = 'Administration',
  StudentServices = 'StudentServices',
  Other = 'Other'
}

export enum RevealStatus {
  NotRequested = 'NotRequested',
  Pending = 'Pending',
  Approved = 'Approved',
  Denied = 'Denied'
}

// ============================================================================
// Auth DTOs
// ============================================================================

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  email: string;
  role: Role;
  userId: string;
  fullName: string;
  expiresAt: string;
}

// ============================================================================
// User DTOs
// ============================================================================

export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  role: Role;
  departmentId?: string;
  departmentName?: string;
  programId?: string;
  programName?: string;
  semesterId?: string;
  semesterName?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  fullName: string;
  role: string;
  departmentId?: string;
  programId?: string;
  semesterId?: string;
  isActive?: boolean;
}

export interface UpdateUserRequest {
  email?: string;
  fullName?: string;
  departmentId?: string;
  programId?: string;
  semesterId?: string;
  isActive?: boolean;
}

export interface UserStatsDto {
  totalUsers: number;
  totalAdmins: number;
  totalStudents: number;
  activeUsers: number;
}

// ============================================================================
// Inquiry DTOs
// ============================================================================

export interface InquiryDto {
  id: string;
  body: string;
  status: InquiryStatus;
  departmentIds: string[];
  departmentNames: string[];
  programIds: string[];
  programNames: string[];
  semesterIds: string[];
  semesterNames: string[];
  createdById: string;
  createdByName: string;
  createdAt: string;
  updatedAt: string;
  inputCount: number;
}

export interface CreateInquiryRequest {
  body: string;
  status?: string;
  departmentIds: string[];
  programIds: string[];
  semesterIds: string[];
}

export interface UpdateInquiryRequest {
  body?: string;
  status?: string;
  departmentIds?: string[];
  programIds?: string[];
  semesterIds?: string[];
}

export interface InquiryStatsDto {
  totalInquiries: number;
  draftInquiries: number;
  sentInquiries: number;
  closedInquiries: number;
  totalResponses: number;
}

// ============================================================================
// Input DTOs
// ============================================================================

export interface InputDto {
  id: string;
  body: string;
  type: InputType;
  status: InputStatus;
  userId?: string;
  userName?: string;
  userEmail?: string;
  inquiryId?: string;
  inquiryBody?: string;
  topicId?: string;
  topicName?: string;
  sentiment?: Sentiment;
  tone?: Tone;
  urgencyScore?: number;
  importanceScore?: number;
  clarityScore?: number;
  qualityScore?: number;
  helpfulnessScore?: number;
  overallScore?: number;
  severityLevel?: number;
  themeType?: ThemeType;
  aiProcessedAt?: string;
  revealStatus: RevealStatus;
  identityRevealedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateInputRequest {
  body: string;
  userId?: string;
  inquiryId?: string;
}

export interface UpdateInputRequest {
  body?: string;
  status?: string;
  topicId?: string;
  sentiment?: string;
  tone?: string;
  urgencyScore?: number;
  importanceScore?: number;
  clarityScore?: number;
  qualityScore?: number;
  helpfulnessScore?: number;
  severityLevel?: number;
  themeType?: string;
}

export interface InputFilterDto {
  type?: InputType;
  inquiryId?: string;
  topicId?: string;
  sentiment?: Sentiment;
  tone?: Tone;
  minQualityScore?: number;
  severityLevel?: number;
  themeType?: ThemeType;
  status?: InputStatus;
  searchTerm?: string;
  sortBy?: string;
  sortDescending?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export interface InputReplyDto {
  id: string;
  inputId: string;
  userId: string;
  userName: string;
  userRole: Role;
  message: string;
  createdAt: string;
}

export interface CreateReplyRequest {
  message: string;
}

export interface InputStatsDto {
  totalInputs: number;
  generalInputs: number;
  inquiryLinkedInputs: number;
  pendingInputs: number;
  reviewedInputs: number;
  resolvedInputs: number;
  positiveCount: number;
  neutralCount: number;
  negativeCount: number;
  averageQualityScore: number;
}

// ============================================================================
// Topic DTOs
// ============================================================================

export interface TopicDto {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  createdAt: string;
  inputCount: number;
}

export interface CreateTopicRequest {
  name: string;
  description: string;
  isActive?: boolean;
}

export interface UpdateTopicRequest {
  name?: string;
  description?: string;
  isActive?: boolean;
}

export interface TopicStatsDto {
  totalTopics: number;
  activeTopics: number;
  totalInputsLinked: number;
}

// ============================================================================
// Department DTOs
// ============================================================================

export interface DepartmentDto {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateDepartmentRequest {
  name: string;
  code: string;
  isActive?: boolean;
}

export interface UpdateDepartmentRequest {
  name?: string;
  code?: string;
  isActive?: boolean;
}

// ============================================================================
// Program DTOs
// ============================================================================

export interface ProgramDto {
  id: string;
  name: string;
  code: string;
  departmentId: string;
  departmentName: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateProgramRequest {
  name: string;
  code: string;
  departmentId: string;
  isActive?: boolean;
}

export interface UpdateProgramRequest {
  name?: string;
  code?: string;
  departmentId?: string;
  isActive?: boolean;
}

// ============================================================================
// Semester DTOs
// ============================================================================

export interface SemesterDto {
  id: string;
  name: string;
  number: number;
  isActive: boolean;
  createdAt: string;
}

export interface CreateSemesterRequest {
  name: string;
  number: number;
  isActive?: boolean;
}

export interface UpdateSemesterRequest {
  name?: string;
  number?: number;
  isActive?: boolean;
}

// ============================================================================
// Theme DTOs
// ============================================================================

export interface ThemeDto {
  id: string;
  name: string;
  type: ThemeType;
  description: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateThemeRequest {
  name: string;
  type: string;
  description: string;
  isActive?: boolean;
}

export interface UpdateThemeRequest {
  name?: string;
  type?: string;
  description?: string;
  isActive?: boolean;
}
```

---

## 3. API Client Setup

Create `lib/api/client.ts`:

```typescript
import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000/api';
const API_TIMEOUT = Number(process.env.NEXT_PUBLIC_API_TIMEOUT) || 30000;
const TOKEN_KEY = process.env.NEXT_PUBLIC_JWT_STORAGE_KEY || 'smart_insights_token';

// ============================================================================
// Create Axios Instance
// ============================================================================

const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: API_TIMEOUT,
  headers: {
    'Content-Type': 'application/json',
  },
});

// ============================================================================
// Request Interceptor (Add JWT Token)
// ============================================================================

apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Get token from localStorage
    const token = localStorage.getItem(TOKEN_KEY);

    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    // Debug logging (only in development)
    if (process.env.NEXT_PUBLIC_DEBUG_API === 'true') {
      console.log(`[API Request] ${config.method?.toUpperCase()} ${config.url}`, {
        params: config.params,
        data: config.data,
      });
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// ============================================================================
// Response Interceptor (Handle Errors)
// ============================================================================

apiClient.interceptors.response.use(
  (response) => {
    // Debug logging
    if (process.env.NEXT_PUBLIC_DEBUG_API === 'true') {
      console.log(`[API Response] ${response.config.url}`, response.data);
    }

    return response;
  },
  (error: AxiosError) => {
    // Handle different error scenarios
    if (error.response) {
      const status = error.response.status;

      switch (status) {
        case 401:
          // Unauthorized - clear token and redirect to login
          localStorage.removeItem(TOKEN_KEY);
          if (typeof window !== 'undefined') {
            window.location.href = '/login';
          }
          break;

        case 403:
          // Forbidden - user doesn't have permission
          console.error('Access forbidden:', error.response.data);
          break;

        case 404:
          // Not found
          console.error('Resource not found:', error.config?.url);
          break;

        case 500:
          // Server error
          console.error('Server error:', error.response.data);
          break;
      }

      // Debug logging
      if (process.env.NEXT_PUBLIC_DEBUG_API === 'true') {
        console.error(`[API Error] ${error.config?.url}`, {
          status,
          data: error.response.data,
        });
      }
    } else if (error.request) {
      // Network error - no response received
      console.error('Network error:', error.message);
    } else {
      // Something else happened
      console.error('API Error:', error.message);
    }

    return Promise.reject(error);
  }
);

// ============================================================================
// Helper Functions
// ============================================================================

export const setAuthToken = (token: string) => {
  localStorage.setItem(TOKEN_KEY, token);
};

export const getAuthToken = (): string | null => {
  return localStorage.getItem(TOKEN_KEY);
};

export const removeAuthToken = () => {
  localStorage.removeItem(TOKEN_KEY);
};

export const isAuthenticated = (): boolean => {
  return !!getAuthToken();
};

// ============================================================================
// Export
// ============================================================================

export default apiClient;
```

---

## 4. Authentication API

Create `lib/api/auth.ts`:

```typescript
import apiClient, { setAuthToken, removeAuthToken } from './client';
import { ApiResponse, LoginRequest, LoginResponse } from '@/types/api';

// ============================================================================
// Auth API Service
// ============================================================================

export const authApi = {
  /**
   * Login user
   */
  login: async (credentials: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<ApiResponse<LoginResponse>>(
      '/auth/login',
      credentials
    );

    // Store token in localStorage
    if (response.data.data.token) {
      setAuthToken(response.data.data.token);
    }

    return response.data.data;
  },

  /**
   * Logout user
   */
  logout: () => {
    removeAuthToken();
  },

  /**
   * Get current user info (if needed)
   */
  getCurrentUser: async (): Promise<LoginResponse> => {
    const response = await apiClient.get<ApiResponse<LoginResponse>>('/auth/me');
    return response.data.data;
  },
};

export default authApi;
```

---

## 5. Users API

Create `lib/api/users.ts`:

```typescript
import apiClient from './client';
import {
  ApiResponse,
  PaginatedResult,
  UserDto,
  CreateUserRequest,
  UpdateUserRequest,
  UserStatsDto,
} from '@/types/api';

// ============================================================================
// Users API Service
// ============================================================================

export const usersApi = {
  /**
   * Get all users (paginated)
   */
  getAll: async (params?: {
    page?: number;
    pageSize?: number;
    role?: string;
    departmentId?: string;
    programId?: string;
    semesterId?: string;
    isActive?: boolean;
    searchTerm?: string;
  }): Promise<PaginatedResult<UserDto>> => {
    const response = await apiClient.get<ApiResponse<PaginatedResult<UserDto>>>(
      '/users',
      { params }
    );
    return response.data.data;
  },

  /**
   * Get user by ID
   */
  getById: async (id: string): Promise<UserDto> => {
    const response = await apiClient.get<ApiResponse<UserDto>>(`/users/${id}`);
    return response.data.data;
  },

  /**
   * Create new user
   */
  create: async (request: CreateUserRequest): Promise<UserDto> => {
    const response = await apiClient.post<ApiResponse<UserDto>>('/users', request);
    return response.data.data;
  },

  /**
   * Update user
   */
  update: async (id: string, request: UpdateUserRequest): Promise<UserDto> => {
    const response = await apiClient.put<ApiResponse<UserDto>>(
      `/users/${id}`,
      request
    );
    return response.data.data;
  },

  /**
   * Delete user
   */
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/users/${id}`);
  },

  /**
   * Get user statistics
   */
  getStats: async (): Promise<UserStatsDto> => {
    const response = await apiClient.get<ApiResponse<UserStatsDto>>('/users/stats');
    return response.data.data;
  },

  /**
   * Import users from CSV
   */
  importFromCsv: async (file: File): Promise<UserDto[]> => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post<ApiResponse<UserDto[]>>(
      '/users/import-csv',
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );
    return response.data.data;
  },

  /**
   * Get CSV template
   */
  downloadCsvTemplate: async (): Promise<Blob> => {
    const response = await apiClient.get('/users/csv-template', {
      responseType: 'blob',
    });
    return response.data;
  },
};

export default usersApi;
```

---

## 6. Inquiries API

Create `lib/api/inquiries.ts`:

```typescript
import apiClient from './client';
import {
  ApiResponse,
  PaginatedResult,
  InquiryDto,
  CreateInquiryRequest,
  UpdateInquiryRequest,
  InquiryStatsDto,
} from '@/types/api';

// ============================================================================
// Inquiries API Service
// ============================================================================

export const inquiriesApi = {
  /**
   * Get all inquiries (paginated)
   */
  getAll: async (params?: {
    page?: number;
    pageSize?: number;
    status?: string;
    departmentId?: string;
    createdById?: string;
  }): Promise<PaginatedResult<InquiryDto>> => {
    const response = await apiClient.get<ApiResponse<PaginatedResult<InquiryDto>>>(
      '/inquiries',
      { params }
    );
    return response.data.data;
  },

  /**
   * Get inquiry by ID
   */
  getById: async (id: string): Promise<InquiryDto> => {
    const response = await apiClient.get<ApiResponse<InquiryDto>>(
      `/inquiries/${id}`
    );
    return response.data.data;
  },

  /**
   * Create new inquiry
   */
  create: async (request: CreateInquiryRequest): Promise<InquiryDto> => {
    const response = await apiClient.post<ApiResponse<InquiryDto>>(
      '/inquiries',
      request
    );
    return response.data.data;
  },

  /**
   * Update inquiry
   */
  update: async (id: string, request: UpdateInquiryRequest): Promise<InquiryDto> => {
    const response = await apiClient.put<ApiResponse<InquiryDto>>(
      `/inquiries/${id}`,
      request
    );
    return response.data.data;
  },

  /**
   * Delete inquiry
   */
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/inquiries/${id}`);
  },

  /**
   * Send inquiry (change status to Sent)
   */
  send: async (id: string): Promise<InquiryDto> => {
    const response = await apiClient.post<ApiResponse<InquiryDto>>(
      `/inquiries/${id}/send`
    );
    return response.data.data;
  },

  /**
   * Close inquiry
   */
  close: async (id: string): Promise<InquiryDto> => {
    const response = await apiClient.post<ApiResponse<InquiryDto>>(
      `/inquiries/${id}/close`
    );
    return response.data.data;
  },

  /**
   * Get inquiries for current student
   */
  getMyInquiries: async (params?: {
    page?: number;
    pageSize?: number;
    status?: string;
  }): Promise<PaginatedResult<InquiryDto>> => {
    const response = await apiClient.get<ApiResponse<PaginatedResult<InquiryDto>>>(
      '/inquiries/my-inquiries',
      { params }
    );
    return response.data.data;
  },

  /**
   * Get inquiry statistics
   */
  getStats: async (): Promise<InquiryStatsDto> => {
    const response = await apiClient.get<ApiResponse<InquiryStatsDto>>(
      '/inquiries/stats'
    );
    return response.data.data;
  },
};

export default inquiriesApi;
```

---

## 7. Inputs (Feedback) API

Create `lib/api/inputs.ts`:

```typescript
import apiClient from './client';
import {
  ApiResponse,
  PaginatedResult,
  InputDto,
  CreateInputRequest,
  UpdateInputRequest,
  InputFilterDto,
  InputReplyDto,
  CreateReplyRequest,
  InputStatsDto,
} from '@/types/api';

// ============================================================================
// Inputs API Service
// ============================================================================

export const inputsApi = {
  /**
   * Get filtered inputs (paginated with complex filtering)
   */
  getFiltered: async (filter: InputFilterDto): Promise<PaginatedResult<InputDto>> => {
    const response = await apiClient.get<ApiResponse<PaginatedResult<InputDto>>>(
      '/inputs/filter',
      { params: filter }
    );
    return response.data.data;
  },

  /**
   * Get input by ID
   */
  getById: async (id: string): Promise<InputDto> => {
    const response = await apiClient.get<ApiResponse<InputDto>>(`/inputs/${id}`);
    return response.data.data;
  },

  /**
   * Create new input (anonymous submission allowed)
   */
  create: async (request: CreateInputRequest): Promise<InputDto> => {
    const response = await apiClient.post<ApiResponse<InputDto>>(
      '/inputs',
      request
    );
    return response.data.data;
  },

  /**
   * Update input
   */
  update: async (id: string, request: UpdateInputRequest): Promise<InputDto> => {
    const response = await apiClient.put<ApiResponse<InputDto>>(
      `/inputs/${id}`,
      request
    );
    return response.data.data;
  },

  /**
   * Delete input
   */
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/inputs/${id}`);
  },

  /**
   * Request identity reveal (Admin only)
   */
  requestReveal: async (id: string): Promise<InputDto> => {
    const response = await apiClient.post<ApiResponse<InputDto>>(
      `/inputs/${id}/reveal-request`
    );
    return response.data.data;
  },

  /**
   * Respond to identity reveal request (Student only)
   */
  respondToReveal: async (id: string, approved: boolean): Promise<InputDto> => {
    const response = await apiClient.post<ApiResponse<InputDto>>(
      `/inputs/${id}/reveal-respond`,
      { approved }
    );
    return response.data.data;
  },

  /**
   * Get my inputs (Student only)
   */
  getMyInputs: async (params?: {
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedResult<InputDto>> => {
    const response = await apiClient.get<ApiResponse<PaginatedResult<InputDto>>>(
      '/inputs/my-inputs',
      { params }
    );
    return response.data.data;
  },

  /**
   * Get input statistics
   */
  getStats: async (): Promise<InputStatsDto> => {
    const response = await apiClient.get<ApiResponse<InputStatsDto>>(
      '/inputs/stats'
    );
    return response.data.data;
  },

  // ============================================================================
  // Replies (Conversation System)
  // ============================================================================

  /**
   * Get replies for an input
   */
  getReplies: async (inputId: string): Promise<InputReplyDto[]> => {
    const response = await apiClient.get<ApiResponse<InputReplyDto[]>>(
      `/inputs/${inputId}/replies`
    );
    return response.data.data;
  },

  /**
   * Create reply to an input
   */
  createReply: async (
    inputId: string,
    request: CreateReplyRequest
  ): Promise<InputReplyDto> => {
    const response = await apiClient.post<ApiResponse<InputReplyDto>>(
      `/inputs/${inputId}/replies`,
      request
    );
    return response.data.data;
  },
};

export default inputsApi;
```

---

## 8. Other Resource APIs

Create `lib/api/topics.ts`:

```typescript
import apiClient from './client';
import {
  ApiResponse,
  PaginatedResult,
  TopicDto,
  CreateTopicRequest,
  UpdateTopicRequest,
  TopicStatsDto,
} from '@/types/api';

export const topicsApi = {
  getAll: async (params?: {
    page?: number;
    pageSize?: number;
    isActive?: boolean;
  }): Promise<PaginatedResult<TopicDto>> => {
    const response = await apiClient.get<ApiResponse<PaginatedResult<TopicDto>>>(
      '/topics',
      { params }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<TopicDto> => {
    const response = await apiClient.get<ApiResponse<TopicDto>>(`/topics/${id}`);
    return response.data.data;
  },

  create: async (request: CreateTopicRequest): Promise<TopicDto> => {
    const response = await apiClient.post<ApiResponse<TopicDto>>(
      '/topics',
      request
    );
    return response.data.data;
  },

  update: async (id: string, request: UpdateTopicRequest): Promise<TopicDto> => {
    const response = await apiClient.put<ApiResponse<TopicDto>>(
      `/topics/${id}`,
      request
    );
    return response.data.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/topics/${id}`);
  },

  getStats: async (): Promise<TopicStatsDto> => {
    const response = await apiClient.get<ApiResponse<TopicStatsDto>>(
      '/topics/stats'
    );
    return response.data.data;
  },
};

export default topicsApi;
```

Create `lib/api/departments.ts`:

```typescript
import apiClient from './client';
import {
  ApiResponse,
  PaginatedResult,
  DepartmentDto,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
} from '@/types/api';

export const departmentsApi = {
  getAll: async (params?: {
    page?: number;
    pageSize?: number;
    isActive?: boolean;
  }): Promise<PaginatedResult<DepartmentDto>> => {
    const response = await apiClient.get<ApiResponse<PaginatedResult<DepartmentDto>>>(
      '/departments',
      { params }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<DepartmentDto> => {
    const response = await apiClient.get<ApiResponse<DepartmentDto>>(
      `/departments/${id}`
    );
    return response.data.data;
  },

  create: async (request: CreateDepartmentRequest): Promise<DepartmentDto> => {
    const response = await apiClient.post<ApiResponse<DepartmentDto>>(
      '/departments',
      request
    );
    return response.data.data;
  },

  update: async (
    id: string,
    request: UpdateDepartmentRequest
  ): Promise<DepartmentDto> => {
    const response = await apiClient.put<ApiResponse<DepartmentDto>>(
      `/departments/${id}`,
      request
    );
    return response.data.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/departments/${id}`);
  },
};

export default departmentsApi;
```

Create `lib/api/programs.ts`:

```typescript
import apiClient from './client';
import {
  ApiResponse,
  PaginatedResult,
  ProgramDto,
  CreateProgramRequest,
  UpdateProgramRequest,
} from '@/types/api';

export const programsApi = {
  getAll: async (params?: {
    page?: number;
    pageSize?: number;
    departmentId?: string;
    isActive?: boolean;
  }): Promise<PaginatedResult<ProgramDto>> => {
    const response = await apiClient.get<ApiResponse<PaginatedResult<ProgramDto>>>(
      '/programs',
      { params }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<ProgramDto> => {
    const response = await apiClient.get<ApiResponse<ProgramDto>>(`/programs/${id}`);
    return response.data.data;
  },

  create: async (request: CreateProgramRequest): Promise<ProgramDto> => {
    const response = await apiClient.post<ApiResponse<ProgramDto>>(
      '/programs',
      request
    );
    return response.data.data;
  },

  update: async (id: string, request: UpdateProgramRequest): Promise<ProgramDto> => {
    const response = await apiClient.put<ApiResponse<ProgramDto>>(
      `/programs/${id}`,
      request
    );
    return response.data.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/programs/${id}`);
  },
};

export default programsApi;
```

Create `lib/api/semesters.ts`:

```typescript
import apiClient from './client';
import {
  ApiResponse,
  PaginatedResult,
  SemesterDto,
  CreateSemesterRequest,
  UpdateSemesterRequest,
} from '@/types/api';

export const semestersApi = {
  getAll: async (params?: {
    page?: number;
    pageSize?: number;
    isActive?: boolean;
  }): Promise<PaginatedResult<SemesterDto>> => {
    const response = await apiClient.get<ApiResponse<PaginatedResult<SemesterDto>>>(
      '/semesters',
      { params }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<SemesterDto> => {
    const response = await apiClient.get<ApiResponse<SemesterDto>>(
      `/semesters/${id}`
    );
    return response.data.data;
  },

  create: async (request: CreateSemesterRequest): Promise<SemesterDto> => {
    const response = await apiClient.post<ApiResponse<SemesterDto>>(
      '/semesters',
      request
    );
    return response.data.data;
  },

  update: async (
    id: string,
    request: UpdateSemesterRequest
  ): Promise<SemesterDto> => {
    const response = await apiClient.put<ApiResponse<SemesterDto>>(
      `/semesters/${id}`,
      request
    );
    return response.data.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/semesters/${id}`);
  },
};

export default semestersApi;
```

Create `lib/api/themes.ts`:

```typescript
import apiClient from './client';
import {
  ApiResponse,
  PaginatedResult,
  ThemeDto,
  CreateThemeRequest,
  UpdateThemeRequest,
} from '@/types/api';

export const themesApi = {
  getAll: async (params?: {
    page?: number;
    pageSize?: number;
    type?: string;
    isActive?: boolean;
  }): Promise<PaginatedResult<ThemeDto>> => {
    const response = await apiClient.get<ApiResponse<PaginatedResult<ThemeDto>>>(
      '/themes',
      { params }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<ThemeDto> => {
    const response = await apiClient.get<ApiResponse<ThemeDto>>(`/themes/${id}`);
    return response.data.data;
  },

  create: async (request: CreateThemeRequest): Promise<ThemeDto> => {
    const response = await apiClient.post<ApiResponse<ThemeDto>>(
      '/themes',
      request
    );
    return response.data.data;
  },

  update: async (id: string, request: UpdateThemeRequest): Promise<ThemeDto> => {
    const response = await apiClient.put<ApiResponse<ThemeDto>>(
      `/themes/${id}`,
      request
    );
    return response.data.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/themes/${id}`);
  },
};

export default themesApi;
```

---

## 9. React Context for Authentication

Create `contexts/AuthContext.tsx`:

```typescript
'use client';

import React, { createContext, useContext, useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { authApi } from '@/lib/api/auth';
import { getAuthToken, removeAuthToken } from '@/lib/api/client';
import { LoginRequest, LoginResponse, Role } from '@/types/api';

// ============================================================================
// Types
// ============================================================================

interface AuthContextType {
  user: LoginResponse | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => void;
  hasRole: (role: Role) => boolean;
}

// ============================================================================
// Context
// ============================================================================

const AuthContext = createContext<AuthContextType | undefined>(undefined);

// ============================================================================
// Provider Component
// ============================================================================

export const AuthProvider: React.FC<{ children: React.Node }> = ({ children }) => {
  const router = useRouter();
  const [user, setUser] = useState<LoginResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Check if user is authenticated on mount
  useEffect(() => {
    const initAuth = async () => {
      const token = getAuthToken();

      if (token) {
        try {
          // Optional: Fetch current user info from backend
          // const currentUser = await authApi.getCurrentUser();
          // setUser(currentUser);

          // Or decode JWT token locally if you prefer
          const payload = JSON.parse(atob(token.split('.')[1]));
          setUser({
            token,
            email: payload.email,
            role: payload.role,
            userId: payload.sub,
            fullName: payload.name,
            expiresAt: new Date(payload.exp * 1000).toISOString(),
          });
        } catch (error) {
          console.error('Failed to restore auth session:', error);
          removeAuthToken();
        }
      }

      setIsLoading(false);
    };

    initAuth();
  }, []);

  // Login function
  const login = async (credentials: LoginRequest) => {
    try {
      const response = await authApi.login(credentials);
      setUser(response);
      router.push('/dashboard');
    } catch (error) {
      console.error('Login failed:', error);
      throw error;
    }
  };

  // Logout function
  const logout = () => {
    authApi.logout();
    setUser(null);
    router.push('/login');
  };

  // Check if user has specific role
  const hasRole = (role: Role): boolean => {
    return user?.role === role;
  };

  const value: AuthContextType = {
    user,
    isLoading,
    isAuthenticated: !!user,
    login,
    logout,
    hasRole,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

// ============================================================================
// Custom Hook
// ============================================================================

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);

  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }

  return context;
};
```

Wrap your app with the provider in `app/layout.tsx`:

```typescript
import { AuthProvider } from '@/contexts/AuthContext';

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>
        <AuthProvider>
          {children}
        </AuthProvider>
      </body>
    </html>
  );
}
```

---

## 10. Custom React Hooks

Create `hooks/useApi.ts`:

```typescript
import { useState, useEffect } from 'react';

interface UseApiOptions<T> {
  immediate?: boolean;
  onSuccess?: (data: T) => void;
  onError?: (error: Error) => void;
}

interface UseApiReturn<T, P extends any[]> {
  data: T | null;
  error: Error | null;
  isLoading: boolean;
  execute: (...args: P) => Promise<void>;
  reset: () => void;
}

/**
 * Generic hook for API calls with loading/error states
 */
export function useApi<T, P extends any[] = []>(
  apiFunc: (...args: P) => Promise<T>,
  options: UseApiOptions<T> = {}
): UseApiReturn<T, P> {
  const { immediate = false, onSuccess, onError } = options;

  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<Error | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const execute = async (...args: P) => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await apiFunc(...args);
      setData(result);

      if (onSuccess) {
        onSuccess(result);
      }
    } catch (err) {
      const error = err instanceof Error ? err : new Error('An error occurred');
      setError(error);

      if (onError) {
        onError(error);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const reset = () => {
    setData(null);
    setError(null);
    setIsLoading(false);
  };

  useEffect(() => {
    if (immediate) {
      execute();
    }
  }, []);

  return { data, error, isLoading, execute, reset };
}
```

Create `hooks/usePagination.ts`:

```typescript
import { useState } from 'react';
import { PaginatedResult } from '@/types/api';

interface UsePaginationReturn<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  setPage: (page: number) => void;
  nextPage: () => void;
  previousPage: () => void;
  setPageSize: (size: number) => void;
  setData: (result: PaginatedResult<T>) => void;
}

/**
 * Hook for managing paginated data
 */
export function usePagination<T>(
  initialPageSize: number = 10
): UsePaginationReturn<T> {
  const [data, setDataState] = useState<T[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  const setData = (result: PaginatedResult<T>) => {
    setDataState(result.items);
    setPageNumber(result.pageNumber);
    setTotalCount(result.totalCount);
    setTotalPages(result.totalPages);
  };

  const setPage = (page: number) => {
    if (page >= 1 && page <= totalPages) {
      setPageNumber(page);
    }
  };

  const nextPage = () => {
    if (pageNumber < totalPages) {
      setPageNumber(pageNumber + 1);
    }
  };

  const previousPage = () => {
    if (pageNumber > 1) {
      setPageNumber(pageNumber - 1);
    }
  };

  return {
    data,
    pageNumber,
    pageSize,
    totalCount,
    totalPages,
    hasNextPage: pageNumber < totalPages,
    hasPreviousPage: pageNumber > 1,
    setPage,
    nextPage,
    previousPage,
    setPageSize,
    setData,
  };
}
```

---

## 11. Component Integration Examples

### Example 1: Login Page

```typescript
'use client';

import { useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { LoginRequest } from '@/types/api';

export default function LoginPage() {
  const { login } = useAuth();
  const [formData, setFormData] = useState<LoginRequest>({
    email: '',
    password: '',
  });
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      await login(formData);
      // Router will redirect to dashboard automatically
    } catch (err) {
      setError('Invalid email or password');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center">
      <form onSubmit={handleSubmit} className="w-full max-w-md space-y-4">
        <h1 className="text-2xl font-bold">Login</h1>

        {error && (
          <div className="bg-red-100 text-red-700 p-3 rounded">
            {error}
          </div>
        )}

        <input
          type="email"
          placeholder="Email"
          value={formData.email}
          onChange={(e) => setFormData({ ...formData, email: e.target.value })}
          className="w-full p-2 border rounded"
          required
        />

        <input
          type="password"
          placeholder="Password"
          value={formData.password}
          onChange={(e) => setFormData({ ...formData, password: e.target.value })}
          className="w-full p-2 border rounded"
          required
        />

        <button
          type="submit"
          disabled={isLoading}
          className="w-full bg-blue-600 text-white p-2 rounded hover:bg-blue-700 disabled:opacity-50"
        >
          {isLoading ? 'Logging in...' : 'Login'}
        </button>
      </form>
    </div>
  );
}
```

### Example 2: Feedback Submission (Anonymous)

```typescript
'use client';

import { useState } from 'react';
import { inputsApi } from '@/lib/api/inputs';
import { CreateInputRequest } from '@/types/api';
import { useAuth } from '@/contexts/AuthContext';

export default function FeedbackForm({ inquiryId }: { inquiryId?: string }) {
  const { user } = useAuth();
  const [body, setBody] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setSuccess(false);

    try {
      const request: CreateInputRequest = {
        body,
        userId: user?.userId, // Optional - can be undefined for anonymous
        inquiryId, // Optional - only if responding to specific inquiry
      };

      await inputsApi.create(request);
      setSuccess(true);
      setBody('');
    } catch (error) {
      console.error('Failed to submit feedback:', error);
      alert('Failed to submit feedback');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <h2 className="text-xl font-bold">Submit Feedback</h2>

      {success && (
        <div className="bg-green-100 text-green-700 p-3 rounded">
          Feedback submitted successfully!
        </div>
      )}

      <textarea
        value={body}
        onChange={(e) => setBody(e.target.value)}
        placeholder="Share your feedback..."
        className="w-full p-2 border rounded min-h-[150px]"
        required
      />

      {!user && (
        <p className="text-sm text-gray-600">
          You're submitting anonymously. Login to track your feedback.
        </p>
      )}

      <button
        type="submit"
        disabled={isLoading}
        className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:opacity-50"
      >
        {isLoading ? 'Submitting...' : 'Submit Feedback'}
      </button>
    </form>
  );
}
```

### Example 3: Feedback List with Filtering

```typescript
'use client';

import { useState, useEffect } from 'react';
import { inputsApi } from '@/lib/api/inputs';
import { InputDto, InputFilterDto, InputType, Sentiment } from '@/types/api';
import { usePagination } from '@/hooks/usePagination';

export default function FeedbackList() {
  const pagination = usePagination<InputDto>(10);
  const [filters, setFilters] = useState<InputFilterDto>({
    pageNumber: 1,
    pageSize: 10,
  });
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    loadFeedback();
  }, [filters.pageNumber, filters.type, filters.sentiment]);

  const loadFeedback = async () => {
    setIsLoading(true);
    try {
      const result = await inputsApi.getFiltered({
        ...filters,
        pageNumber: pagination.pageNumber,
        pageSize: pagination.pageSize,
      });
      pagination.setData(result);
    } catch (error) {
      console.error('Failed to load feedback:', error);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Feedback</h1>

      {/* Filters */}
      <div className="flex gap-4">
        <select
          value={filters.type || ''}
          onChange={(e) => setFilters({ ...filters, type: e.target.value as InputType })}
          className="p-2 border rounded"
        >
          <option value="">All Types</option>
          <option value={InputType.General}>General</option>
          <option value={InputType.InquiryLinked}>Inquiry Linked</option>
        </select>

        <select
          value={filters.sentiment || ''}
          onChange={(e) => setFilters({ ...filters, sentiment: e.target.value as Sentiment })}
          className="p-2 border rounded"
        >
          <option value="">All Sentiments</option>
          <option value={Sentiment.Positive}>Positive</option>
          <option value={Sentiment.Neutral}>Neutral</option>
          <option value={Sentiment.Negative}>Negative</option>
        </select>
      </div>

      {/* List */}
      {isLoading ? (
        <div>Loading...</div>
      ) : (
        <div className="space-y-4">
          {pagination.data.map((input) => (
            <div key={input.id} className="border rounded p-4">
              <p className="text-gray-600 text-sm">
                {new Date(input.createdAt).toLocaleDateString()}
              </p>
              <p className="mt-2">{input.body}</p>
              <div className="mt-2 flex gap-2">
                {input.sentiment && (
                  <span className="text-sm bg-gray-100 px-2 py-1 rounded">
                    {input.sentiment}
                  </span>
                )}
                {input.overallScore && (
                  <span className="text-sm bg-blue-100 px-2 py-1 rounded">
                    Score: {(input.overallScore * 100).toFixed(0)}%
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Pagination */}
      <div className="flex justify-between items-center">
        <button
          onClick={pagination.previousPage}
          disabled={!pagination.hasPreviousPage}
          className="px-4 py-2 bg-gray-200 rounded disabled:opacity-50"
        >
          Previous
        </button>

        <span>
          Page {pagination.pageNumber} of {pagination.totalPages}
        </span>

        <button
          onClick={pagination.nextPage}
          disabled={!pagination.hasNextPage}
          className="px-4 py-2 bg-gray-200 rounded disabled:opacity-50"
        >
          Next
        </button>
      </div>
    </div>
  );
}
```

### Example 4: User Management (Admin)

```typescript
'use client';

import { useState, useEffect } from 'react';
import { usersApi } from '@/lib/api/users';
import { UserDto, CreateUserRequest } from '@/types/api';
import { useApi } from '@/hooks/useApi';
import { usePagination } from '@/hooks/usePagination';

export default function UserManagement() {
  const pagination = usePagination<UserDto>(10);
  const [isLoading, setIsLoading] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);

  useEffect(() => {
    loadUsers();
  }, [pagination.pageNumber]);

  const loadUsers = async () => {
    setIsLoading(true);
    try {
      const result = await usersApi.getAll({
        page: pagination.pageNumber,
        pageSize: pagination.pageSize,
      });
      pagination.setData(result);
    } catch (error) {
      console.error('Failed to load users:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure?')) return;

    try {
      await usersApi.delete(id);
      loadUsers();
    } catch (error) {
      console.error('Failed to delete user:', error);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold">User Management</h1>
        <button
          onClick={() => setShowCreateModal(true)}
          className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
        >
          Add User
        </button>
      </div>

      {isLoading ? (
        <div>Loading...</div>
      ) : (
        <table className="w-full border">
          <thead className="bg-gray-100">
            <tr>
              <th className="p-2 text-left">Name</th>
              <th className="p-2 text-left">Email</th>
              <th className="p-2 text-left">Role</th>
              <th className="p-2 text-left">Department</th>
              <th className="p-2 text-left">Actions</th>
            </tr>
          </thead>
          <tbody>
            {pagination.data.map((user) => (
              <tr key={user.id} className="border-t">
                <td className="p-2">{user.fullName}</td>
                <td className="p-2">{user.email}</td>
                <td className="p-2">{user.role}</td>
                <td className="p-2">{user.departmentName || '-'}</td>
                <td className="p-2">
                  <button
                    onClick={() => handleDelete(user.id)}
                    className="text-red-600 hover:text-red-800"
                  >
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {/* Pagination controls here */}
    </div>
  );
}
```

---

## 12. Protected Routes

Create `middleware.ts` in your Next.js root:

```typescript
import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

const TOKEN_KEY = process.env.NEXT_PUBLIC_JWT_STORAGE_KEY || 'smart_insights_token';

export function middleware(request: NextRequest) {
  const token = request.cookies.get(TOKEN_KEY)?.value;
  const { pathname } = request.nextUrl;

  // Public routes
  const publicRoutes = ['/login', '/register', '/feedback'];
  const isPublicRoute = publicRoutes.some((route) => pathname.startsWith(route));

  // If accessing protected route without token
  if (!isPublicRoute && !token) {
    return NextResponse.redirect(new URL('/login', request.url));
  }

  // If accessing login with valid token
  if (pathname === '/login' && token) {
    return NextResponse.redirect(new URL('/dashboard', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico).*)'],
};
```

---

## 13. Mock Data Replacement Guide

### Step-by-Step Migration

1. **Identify Mock Data Locations**
   - Search for files with mock data arrays/objects
   - Common locations: `lib/data/`, `mocks/`, `constants/`

2. **Replace Static Lists**

   **Before** (mock data):
   ```typescript
   const mockFeedback = [
     { id: 1, body: 'Test feedback', sentiment: 'Positive' },
     // ...
   ];
   ```

   **After** (API integration):
   ```typescript
   const [feedback, setFeedback] = useState<InputDto[]>([]);

   useEffect(() => {
     const loadFeedback = async () => {
       const result = await inputsApi.getFiltered({ pageSize: 10 });
       setFeedback(result.items);
     };
     loadFeedback();
   }, []);
   ```

3. **Replace Mock API Functions**

   **Before**:
   ```typescript
   export const getMockUsers = () => {
     return Promise.resolve(mockUsers);
   };
   ```

   **After**:
   ```typescript
   // Just use usersApi.getAll() directly
   ```

4. **Update Form Submissions**

   **Before**:
   ```typescript
   const handleSubmit = (data) => {
     console.log('Would submit:', data);
     // Mock success
   };
   ```

   **After**:
   ```typescript
   const handleSubmit = async (data) => {
     try {
       await usersApi.create(data);
       // Show success message
     } catch (error) {
       // Handle error
     }
   };
   ```

5. **Remove Mock Files**
   - After replacing all usage, delete mock data files
   - Remove mock API service files
   - Clean up unused imports

---

## 14. Error Handling Best Practices

### Global Error Handler Component

Create `components/ErrorBoundary.tsx`:

```typescript
'use client';

import React from 'react';

interface Props {
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Error caught by boundary:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return (
        this.props.fallback || (
          <div className="min-h-screen flex items-center justify-center">
            <div className="text-center">
              <h1 className="text-2xl font-bold text-red-600 mb-4">
                Something went wrong
              </h1>
              <p className="text-gray-600 mb-4">
                {this.state.error?.message || 'An unexpected error occurred'}
              </p>
              <button
                onClick={() => window.location.reload()}
                className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
              >
                Reload Page
              </button>
            </div>
          </div>
        )
      );
    }

    return this.props.children;
  }
}
```

### API Error Handling Utility

Create `lib/utils/errorHandler.ts`:

```typescript
import { AxiosError } from 'axios';

export interface ApiError {
  message: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export const handleApiError = (error: unknown): ApiError => {
  if (error instanceof AxiosError) {
    const response = error.response;

    if (response) {
      return {
        message: response.data?.message || 'An error occurred',
        status: response.status,
        errors: response.data?.errors,
      };
    }

    // Network error
    return {
      message: 'Network error. Please check your connection.',
    };
  }

  // Unknown error
  return {
    message: error instanceof Error ? error.message : 'An unexpected error occurred',
  };
};

export const getErrorMessage = (error: unknown): string => {
  const apiError = handleApiError(error);
  return apiError.message;
};
```

---

## 15. Testing the Integration

### Manual Testing Checklist

1. **Authentication Flow**
   - [ ] Login with admin credentials
   - [ ] Login with student credentials
   - [ ] Invalid credentials show error
   - [ ] Token persists after page reload
   - [ ] Logout clears token

2. **Anonymous Feedback**
   - [ ] Submit feedback without login
   - [ ] Feedback appears in admin dashboard
   - [ ] User identity is hidden

3. **CRUD Operations**
   - [ ] Create user
   - [ ] Update user
   - [ ] Delete user
   - [ ] List users with pagination

4. **Inquiry Flow**
   - [ ] Admin creates inquiry
   - [ ] Admin sends inquiry
   - [ ] Student sees targeted inquiry
   - [ ] Student submits response

5. **AI Analysis**
   - [ ] Submit feedback and check AI processing
   - [ ] View sentiment analysis
   - [ ] View quality scores
   - [ ] Check topic assignment

### API Testing with curl

```bash
# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@kfueit.edu.pk","password":"Admin123!"}'

# Get users (with token)
curl -X GET http://localhost:5000/api/users \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"

# Submit anonymous feedback
curl -X POST http://localhost:5000/api/inputs \
  -H "Content-Type: application/json" \
  -d '{"body":"This is test feedback"}'
```

---

## 16. Troubleshooting

### Common Issues

#### 1. CORS Errors

**Problem**: Browser blocks API requests with CORS error

**Solution**: Ensure backend CORS is configured correctly in `appsettings.json`:
```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"]
  }
}
```

#### 2. 401 Unauthorized

**Problem**: API returns 401 even with valid token

**Solutions**:
- Check token is stored correctly: `localStorage.getItem('smart_insights_token')`
- Verify token format: Should be `Bearer <token>`
- Check token expiration
- Ensure backend JWT settings match

#### 3. Network Error

**Problem**: Cannot connect to backend

**Solutions**:
- Verify backend is running: `dotnet run --project src/SmartInsights.API`
- Check API URL in `.env.local`
- Try accessing Swagger: `http://localhost:5000`

#### 4. Type Errors

**Problem**: TypeScript errors with API responses

**Solutions**:
- Ensure types match backend DTOs exactly
- Check for nullable fields (use `?` for optional)
- Verify enum values match backend

#### 5. Anonymous Submissions Not Working

**Problem**: Error when submitting without auth

**Solutions**:
- Check `[AllowAnonymous]` attribute on InputsController.Create
- Ensure userId is optional in CreateInputRequest
- Verify no auth middleware blocking the endpoint

---

## 17. Next Steps

### Recommended Enhancements

1. **React Query / SWR Integration**
   ```bash
   npm install @tanstack/react-query
   # or
   npm install swr
   ```
   - Better caching
   - Automatic refetching
   - Optimistic updates

2. **Form Validation**
   ```bash
   npm install react-hook-form zod
   ```
   - Client-side validation
   - Better UX
   - Type-safe forms

3. **Toast Notifications**
   ```bash
   npm install react-hot-toast
   ```
   - Success/error messages
   - Better feedback

4. **Loading States**
   - Add skeleton loaders
   - Progress indicators
   - Optimistic UI updates

5. **WebSocket Integration**
   - Real-time notifications
   - Live feedback updates
   - Chat functionality

---

## 18. API Endpoint Reference

### Complete Endpoint List

#### Auth
- `POST /api/auth/login` - Login

#### Users
- `GET /api/users` - Get all users (paginated)
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user
- `GET /api/users/stats` - Get statistics
- `POST /api/users/import-csv` - Import from CSV
- `GET /api/users/csv-template` - Download CSV template
- `GET /api/users/filter` - Filter users

#### Inquiries
- `GET /api/inquiries` - Get all inquiries
- `GET /api/inquiries/{id}` - Get inquiry by ID
- `POST /api/inquiries` - Create inquiry
- `PUT /api/inquiries/{id}` - Update inquiry
- `DELETE /api/inquiries/{id}` - Delete inquiry
- `POST /api/inquiries/{id}/send` - Send inquiry
- `POST /api/inquiries/{id}/close` - Close inquiry
- `GET /api/inquiries/my-inquiries` - Get my inquiries (student)
- `GET /api/inquiries/stats` - Get statistics

#### Inputs (Feedback)
- `GET /api/inputs/filter` - Get filtered inputs
- `GET /api/inputs/{id}` - Get input by ID
- `POST /api/inputs` - Create input (anonymous allowed)
- `PUT /api/inputs/{id}` - Update input
- `DELETE /api/inputs/{id}` - Delete input
- `POST /api/inputs/{id}/reveal-request` - Request identity reveal
- `POST /api/inputs/{id}/reveal-respond` - Respond to reveal request
- `GET /api/inputs/my-inputs` - Get my inputs (student)
- `GET /api/inputs/stats` - Get statistics
- `GET /api/inputs/{id}/replies` - Get replies
- `POST /api/inputs/{id}/replies` - Create reply

#### Topics
- `GET /api/topics` - Get all topics
- `GET /api/topics/{id}` - Get topic by ID
- `POST /api/topics` - Create topic
- `PUT /api/topics/{id}` - Update topic
- `DELETE /api/topics/{id}` - Delete topic
- `GET /api/topics/stats` - Get statistics

#### Departments
- `GET /api/departments` - Get all departments
- `GET /api/departments/{id}` - Get department by ID
- `POST /api/departments` - Create department
- `PUT /api/departments/{id}` - Update department
- `DELETE /api/departments/{id}` - Delete department

#### Programs
- `GET /api/programs` - Get all programs
- `GET /api/programs/{id}` - Get program by ID
- `POST /api/programs` - Create program
- `PUT /api/programs/{id}` - Update program
- `DELETE /api/programs/{id}` - Delete program

#### Semesters
- `GET /api/semesters` - Get all semesters
- `GET /api/semesters/{id}` - Get semester by ID
- `POST /api/semesters` - Create semester
- `PUT /api/semesters/{id}` - Update semester
- `DELETE /api/semesters/{id}` - Delete semester

#### Themes
- `GET /api/themes` - Get all themes
- `GET /api/themes/{id}` - Get theme by ID
- `POST /api/themes` - Create theme
- `PUT /api/themes/{id}` - Update theme
- `DELETE /api/themes/{id}` - Delete theme

---

## 19. Useful Commands

### Backend Commands

```bash
# Run backend
cd src/SmartInsights.API
dotnet run

# Run migrations
dotnet ef migrations add InitialCreate --project src/SmartInsights.Infrastructure --startup-project src/SmartInsights.API
dotnet ef database update --project src/SmartInsights.Infrastructure --startup-project src/SmartInsights.API

# Seed database
# (Add DbSeeder call to Program.cs)
```

### Frontend Commands

```bash
# Install dependencies
npm install

# Run development server
npm run dev

# Build for production
npm run build

# Run production build
npm start

# Type check
npm run type-check

# Lint
npm run lint
```

---

## 20. Support & Resources

### Backend Documentation
- Swagger UI: `http://localhost:5000` (when running)
- Backend README: `README.md` in backend repo
- API documentation: Available in Swagger

### Frontend Resources
- Next.js Docs: https://nextjs.org/docs
- React Docs: https://react.dev
- TypeScript Docs: https://www.typescriptlang.org/docs
- Axios Docs: https://axios-http.com/docs/intro

### Contact
For issues or questions:
1. Check this integration guide
2. Review backend README.md
3. Test endpoints in Swagger
4. Check browser console for errors
5. Review backend logs in `logs/` directory

---

## Appendix: Full File Structure

```
frontend/
├── .env.local
├── types/
│   └── api.ts
├── lib/
│   ├── api/
│   │   ├── client.ts
│   │   ├── auth.ts
│   │   ├── users.ts
│   │   ├── inquiries.ts
│   │   ├── inputs.ts
│   │   ├── topics.ts
│   │   ├── departments.ts
│   │   ├── programs.ts
│   │   ├── semesters.ts
│   │   └── themes.ts
│   └── utils/
│       └── errorHandler.ts
├── contexts/
│   └── AuthContext.tsx
├── hooks/
│   ├── useApi.ts
│   └── usePagination.ts
├── components/
│   └── ErrorBoundary.tsx
├── app/
│   ├── layout.tsx
│   ├── login/
│   │   └── page.tsx
│   ├── dashboard/
│   │   └── page.tsx
│   └── feedback/
│       └── page.tsx
└── middleware.ts
```

---

**End of Integration Guide**

This guide provides everything needed to integrate the Smart Insights Aggregator backend with a Next.js frontend. All code examples are production-ready and follow best practices.
