import { getApiBaseUrl } from "./config";
import type { AuthSession, Registration } from "../auth/types";

interface ProblemDetails {
  title?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly errors: Record<string, string[]> = {}
  ) {
    super(message);
  }
}

async function postJson<T>(path: string, body: unknown): Promise<T> {
  const response = await fetch(`${getApiBaseUrl(false)}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails;
    const errors = problem.errors ?? {};
    const firstValidationMessage = Object.values(errors).flat()[0];
    throw new ApiError(
      firstValidationMessage ?? problem.title ?? "The request could not be completed.",
      response.status,
      errors
    );
  }

  return response.json() as Promise<T>;
}

export function login(email: string, password: string) {
  return postJson<AuthSession>("/api/auth/login", { email, password });
}

export function register(fullName: string, email: string, password: string) {
  return postJson<Registration>("/api/auth/register", { fullName, email, password });
}
