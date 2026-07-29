import { createContext, useContext, useMemo, useState, type PropsWithChildren } from "react";
import * as authApi from "../api/authApi";
import type { AuthSession } from "./types";

interface AuthContextValue {
  session: AuthSession | null;
  login(email: string, password: string): Promise<AuthSession>;
  logout(): void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

interface AuthProviderProps extends PropsWithChildren {
  initialSession?: AuthSession | null;
}

export function AuthProvider({ children, initialSession = null }: Readonly<AuthProviderProps>) {
  const [session, setSession] = useState<AuthSession | null>(initialSession);
  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      async login(email, password) {
        const authenticatedSession = await authApi.login(email, password);
        setSession(authenticatedSession);
        return authenticatedSession;
      },
      logout() {
        setSession(null);
      }
    }),
    [session]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider.");
  }

  return context;
}
