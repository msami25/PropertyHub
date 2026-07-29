import { getApiBaseUrl } from "./config";
import type { Role } from "../auth/types";

export type AccountStatus = "Active" | "Disabled";

export interface AdminDashboard {
  asOfUtc: string;
  users: {
    total: number;
    registered: number;
    active: number;
    disabled: number;
  };
  properties: {
    total: number;
    pending: number;
    approved: number;
    rejected: number;
  };
  totalCities: number;
}

export interface AdminUser {
  id: string;
  fullName: string;
  email: string;
  role: Role;
  status: AccountStatus;
  propertyCount: number;
  createdAtUtc: string;
  version: string;
}

export interface AdminUserList {
  items: AdminUser[];
  page: number;
  pageSize: number;
  totalCount: number;
}

interface ProblemDetails {
  title?: string;
  errors?: Record<string, string[]>;
}

export class AdminApiError extends Error {
  constructor(message: string, readonly status: number) {
    super(message);
  }
}

async function request<T>(
  path: string,
  accessToken: string,
  init: RequestInit = {}
): Promise<T> {
  const response = await fetch(`${getApiBaseUrl(false)}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${accessToken}`,
      ...(init.body ? { "Content-Type": "application/json" } : {}),
      ...init.headers
    }
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails;
    const validationMessage = Object.values(problem.errors ?? {}).flat()[0];
    throw new AdminApiError(
      validationMessage ?? problem.title ?? "The Admin request could not be completed.",
      response.status
    );
  }

  return response.json() as Promise<T>;
}

export function getDashboard(accessToken: string) {
  return request<AdminDashboard>("/api/admin/dashboard", accessToken);
}

export function listUsers(
  accessToken: string,
  search: string,
  page: number,
  pageSize = 20
) {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (search.trim()) query.set("search", search.trim());
  return request<AdminUserList>(`/api/admin/users?${query}`, accessToken);
}

export function changeUserRole(
  accessToken: string,
  user: AdminUser,
  role: Role
) {
  return request<AdminUser>(`/api/admin/users/${user.id}/role`, accessToken, {
    method: "PATCH",
    headers: { "If-Match": `"${user.version}"` },
    body: JSON.stringify({ role })
  });
}

export function changeUserStatus(
  accessToken: string,
  user: AdminUser,
  status: AccountStatus,
  reason: string
) {
  return request<AdminUser>(`/api/admin/users/${user.id}/status`, accessToken, {
    method: "PATCH",
    headers: { "If-Match": `"${user.version}"` },
    body: JSON.stringify({ status, reason })
  });
}
