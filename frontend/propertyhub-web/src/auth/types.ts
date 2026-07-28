export type Role = "User" | "Admin";

export interface AuthUser {
  id: string;
  fullName: string;
  email: string;
  role: Role;
}

export interface AuthSession {
  accessToken: string;
  tokenType: "Bearer";
  expiresAtUtc: string;
  user: AuthUser;
}

export interface Registration {
  id: string;
  fullName: string;
  email: string;
  role: Role;
  status: string;
}
