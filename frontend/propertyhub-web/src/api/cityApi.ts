import { getApiBaseUrl } from "./config";

export interface City {
  id: string;
  name: string;
  isActive: boolean;
  latitude: number;
  longitude: number;
}

export interface CityInput {
  name: string;
  isActive: boolean;
  latitude: number;
  longitude: number;
}

interface CityListResponse {
  items: City[];
}

interface ProblemDetails {
  title?: string;
  errors?: Record<string, string[]>;
}

export class CityApiError extends Error {
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
    throw new CityApiError(
      validationMessage ?? problem.title ?? "The city request could not be completed.",
      response.status
    );
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export async function listCities(accessToken: string) {
  const response = await request<CityListResponse>("/api/admin/cities", accessToken);
  return response.items;
}

export function createCity(accessToken: string, city: CityInput) {
  return request<City>("/api/admin/cities", accessToken, {
    method: "POST",
    body: JSON.stringify(city)
  });
}

export function updateCity(accessToken: string, cityId: string, city: CityInput) {
  return request<City>(`/api/admin/cities/${cityId}`, accessToken, {
    method: "PUT",
    body: JSON.stringify(city)
  });
}

export function deleteCity(accessToken: string, cityId: string) {
  return request<void>(`/api/admin/cities/${cityId}`, accessToken, { method: "DELETE" });
}
