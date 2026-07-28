import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";
import { AuthProvider } from "../src/auth/AuthContext";
import type { AuthSession } from "../src/auth/types";

const userSession: AuthSession = {
  accessToken: "test-token",
  tokenType: "Bearer",
  expiresAtUtc: "2026-07-28T15:00:00Z",
  user: {
    id: "11111111-1111-4111-8111-111111111111",
    fullName: "Test User",
    email: "user@propertyhub.test",
    role: "User"
  }
};

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("authentication routes", () => {
  it("protects owner routes for anonymous visitors", () => {
    render(
      <AuthProvider>
        <App url="/my/properties" />
      </AuthProvider>
    );

    expect(screen.getByRole("heading", { name: "Sign in required" })).toBeInTheDocument();
  });

  it("returns an authorization state for a User visiting Admin routes", () => {
    render(
      <AuthProvider initialSession={userSession}>
        <App url="/admin" />
      </AuthProvider>
    );

    expect(screen.getByRole("heading", { name: "Not authorized" })).toBeInTheDocument();
  });

  it("protects City management from a non-admin User", () => {
    render(
      <AuthProvider initialSession={userSession}>
        <App url="/admin/cities" />
      </AuthProvider>
    );

    expect(screen.getByRole("heading", { name: "Not authorized" })).toBeInTheDocument();
  });

  it("allows an Admin session into the Admin route", () => {
    render(
      <AuthProvider initialSession={{ ...userSession, user: { ...userSession.user, role: "Admin" } }}>
        <App url="/admin" />
      </AuthProvider>
    );

    expect(screen.getByRole("heading", { name: "Administration" })).toBeInTheDocument();
  });

  it("keeps the token in memory and navigates after login", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify(userSession), {
          status: 200,
          headers: { "Content-Type": "application/json" }
        })
      )
    );
    render(
      <AuthProvider>
        <App url="/login?returnUrl=/my" />
      </AuthProvider>
    );

    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "user@propertyhub.test" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "StrongPass!123" } });
    fireEvent.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() =>
      expect(screen.getByRole("heading", { name: "My PropertyHub" })).toBeInTheDocument()
    );
    expect(window.localStorage).toHaveLength(0);
    expect(window.sessionStorage).toHaveLength(0);
  });
});
