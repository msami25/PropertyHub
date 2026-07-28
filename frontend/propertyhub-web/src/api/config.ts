export function getApiBaseUrl(isServer: boolean) {
  if (isServer) {
    return process.env.API_INTERNAL_BASE_URL ?? "http://localhost:8080";
  }

  return import.meta.env.VITE_PUBLIC_API_BASE_URL ?? "http://localhost:8080";
}
