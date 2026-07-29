import { getApiBaseUrl } from "./config";

export type PropertyPurpose = "Sale" | "Rent";
export type PropertyType = "House" | "Apartment" | "Plot" | "Shop" | "Office";
export type AreaUnit = "SquareFeet" | "Marla" | "Kanal";
export type ModerationStatus = "Pending" | "Approved" | "Rejected";
export type AvailabilityStatus = "Available" | "Sold" | "Rented";

export interface PropertyCity {
  id: string;
  name: string;
}

export interface PropertyImage {
  id: string;
  url: string;
  sortOrder: number;
  isPrimary: boolean;
  contentType: string;
  fileSizeBytes: number;
}

export interface PropertyImagesResponse {
  propertyId: string;
  images: PropertyImage[];
  moderationStatus: ModerationStatus;
}

export interface PropertySummary {
  id: string;
  title: string;
  city: PropertyCity;
  purpose: PropertyPurpose;
  propertyType: PropertyType;
  price: number;
  area: number;
  areaUnit: AreaUnit;
  bedrooms: number | null;
  bathrooms: number | null;
  primaryImageUrl: string | null;
}

export interface PropertyDetail extends Omit<PropertySummary, "primaryImageUrl"> {
  description: string;
  address: string;
  sellerDisplayName: string;
  images: PropertyImage[];
}

export interface ManagedProperty extends Omit<PropertyDetail, "sellerDisplayName"> {
  contactNumber: string;
  moderationStatus: ModerationStatus;
  availabilityStatus: AvailabilityStatus;
  rejectionReason: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  images: PropertyImage[];
}

export interface PropertyInput {
  title: string;
  description: string;
  purpose: PropertyPurpose;
  propertyType: PropertyType;
  cityId: string;
  address: string;
  price: number;
  area: number;
  areaUnit: AreaUnit;
  bedrooms: number | null;
  bathrooms: number | null;
  contactNumber: string;
}

export interface PropertyFilters {
  cityId?: string;
  purpose?: PropertyPurpose;
  propertyType?: PropertyType;
}

export interface ActiveCity {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
}

interface ProblemDetails {
  title?: string;
  errors?: Record<string, string[]>;
}

export class PropertyApiError extends Error {
  constructor(message: string, readonly status: number) {
    super(message);
  }
}

async function request<T>(
  path: string,
  options: { accessToken?: string; isServer?: boolean; init?: RequestInit } = {}
): Promise<T> {
  const { accessToken, isServer = false, init = {} } = options;
  const response = await fetch(`${getApiBaseUrl(isServer)}${path}`, {
    ...init,
    headers: {
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...(init.body && !(init.body instanceof FormData)
        ? { "Content-Type": "application/json" }
        : {}),
      ...init.headers
    }
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails;
    const validationMessage = Object.values(problem.errors ?? {}).flat()[0];
    throw new PropertyApiError(
      validationMessage ?? problem.title ?? "The property request could not be completed.",
      response.status
    );
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

function propertyQuery(filters: PropertyFilters) {
  const query = new URLSearchParams();
  if (filters.cityId) query.set("cityId", filters.cityId);
  if (filters.purpose) query.set("purpose", filters.purpose);
  if (filters.propertyType) query.set("propertyType", filters.propertyType);
  const value = query.toString();
  return value ? `?${value}` : "";
}

export async function listPublicProperties(filters: PropertyFilters = {}, isServer = false) {
  const response = await request<{ items: PropertySummary[] }>(
    `/api/properties${propertyQuery(filters)}`,
    { isServer }
  );
  return response.items;
}

export function getPublicProperty(propertyId: string, isServer = false) {
  return request<PropertyDetail>(`/api/properties/${propertyId}`, { isServer });
}

export async function listActiveCities(isServer = false) {
  const response = await request<{ items: ActiveCity[] }>("/api/cities", { isServer });
  return response.items;
}

export async function listOwnedProperties(accessToken: string) {
  const response = await request<{ items: ManagedProperty[] }>("/api/users/me/properties", {
    accessToken
  });
  return response.items;
}

export function createProperty(accessToken: string, property: PropertyInput) {
  return request<ManagedProperty>("/api/properties", {
    accessToken,
    init: { method: "POST", body: JSON.stringify(property) }
  });
}

export function updateProperty(accessToken: string, propertyId: string, property: PropertyInput) {
  return request<ManagedProperty>(`/api/properties/${propertyId}`, {
    accessToken,
    init: { method: "PUT", body: JSON.stringify(property) }
  });
}

export function updatePropertyAvailability(
  accessToken: string,
  propertyId: string,
  status: AvailabilityStatus
) {
  return request<ManagedProperty>(`/api/properties/${propertyId}/availability`, {
    accessToken,
    init: { method: "PATCH", body: JSON.stringify({ status }) }
  });
}

export function deleteProperty(accessToken: string, propertyId: string) {
  return request<void>(`/api/properties/${propertyId}`, {
    accessToken,
    init: { method: "DELETE" }
  });
}

export function propertyImageUrl(relativeUrl: string) {
  return `${getApiBaseUrl(false)}${relativeUrl}`;
}

export function uploadPropertyImages(
  accessToken: string,
  propertyId: string,
  images: File[]
) {
  const form = new FormData();
  images.forEach(image => form.append("images", image));
  return request<PropertyImagesResponse>(`/api/properties/${propertyId}/images`, {
    accessToken,
    init: { method: "POST", body: form }
  });
}

export function setPrimaryPropertyImage(
  accessToken: string,
  propertyId: string,
  imageId: string
) {
  return request<PropertyImagesResponse>(
    `/api/properties/${propertyId}/images/${imageId}/primary`,
    { accessToken, init: { method: "PUT" } }
  );
}

export function deletePropertyImage(
  accessToken: string,
  propertyId: string,
  imageId: string
) {
  return request<PropertyImagesResponse>(
    `/api/properties/${propertyId}/images/${imageId}`,
    { accessToken, init: { method: "DELETE" } }
  );
}

export async function getProtectedPropertyImage(accessToken: string, relativeUrl: string) {
  const response = await fetch(propertyImageUrl(relativeUrl), {
    headers: { Authorization: `Bearer ${accessToken}` }
  });
  if (!response.ok) {
    throw new PropertyApiError("The property image could not be loaded.", response.status);
  }
  return response.blob();
}

export async function listPropertiesForModeration(
  accessToken: string,
  moderationStatus?: ModerationStatus
) {
  const query = moderationStatus ? `?moderationStatus=${moderationStatus}` : "";
  const response = await request<{ items: ManagedProperty[] }>(
    `/api/admin/properties${query}`,
    { accessToken }
  );
  return response.items;
}

export function moderateProperty(
  accessToken: string,
  propertyId: string,
  status: "Approved" | "Rejected",
  reason?: string
) {
  return request<ManagedProperty>(`/api/admin/properties/${propertyId}/moderation`, {
    accessToken,
    init: { method: "POST", body: JSON.stringify({ status, reason: reason || null }) }
  });
}
