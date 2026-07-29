import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";
import { AuthProvider } from "../src/auth/AuthContext";
import type { AdminDashboard, AdminUser } from "../src/api/adminApi";
import type { AuthSession } from "../src/auth/types";

const adminSession: AuthSession = {
  accessToken: "admin-token",
  tokenType: "Bearer",
  expiresAtUtc: "2026-07-29T15:00:00Z",
  user: {
    id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    fullName: "Test Administrator",
    email: "admin@propertyhub.test",
    role: "Admin"
  }
};

const dashboard: AdminDashboard = {
  asOfUtc: "2026-07-29T07:30:00Z",
  users: { total: 2, registered: 1, active: 2, disabled: 0 },
  properties: { total: 6, pending: 2, approved: 3, rejected: 1 },
  totalCities: 8
};

const adminUser: AdminUser = {
  id: adminSession.user.id,
  fullName: adminSession.user.fullName,
  email: adminSession.user.email,
  role: "Admin",
  status: "Active",
  propertyCount: 0,
  createdAtUtc: "2026-07-28T10:00:00Z",
  version: "AAAAAA=="
};

const managedUser: AdminUser = {
  id: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
  fullName: "Managed User",
  email: "managed@propertyhub.test",
  role: "User",
  status: "Active",
  propertyCount: 3,
  createdAtUtc: "2026-07-29T10:00:00Z",
  version: "AQAAAA=="
};

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("Admin dashboard", () => {
  it("renders live metrics and protects self and Admin status actions", async () => {
    vi.stubGlobal("fetch", createInitialFetch());

    renderAdmin();

    expect((await screen.findByText("Total accounts")).closest("article")).toHaveTextContent(
      "Total accounts2"
    );
    expect(screen.getByText("Pending properties").parentElement).toHaveTextContent("2");
    expect(screen.getByText("Test Administrator", { selector: "strong" })).toBeInTheDocument();
    expect(screen.getByText("Managed User", { selector: "strong" })).toBeInTheDocument();
    expect(screen.getByLabelText("Role for Test Administrator")).toBeDisabled();
    expect(screen.getByLabelText("Status-change reason for Test Administrator")).toBeDisabled();

    const adminRow = screen.getByText(
      "Test Administrator",
      { selector: "strong" }
    ).closest("tr")!;
    expect(within(adminRow).getByRole("button", { name: "Disable" })).toBeDisabled();
    expect(within(adminRow).getByText("You cannot demote yourself.")).toBeInTheDocument();
    expect(within(adminRow).getByText("Admin status is protected.")).toBeInTheDocument();
  });

  it("changes a User role with the current version and refreshes metrics", async () => {
    const promoted = { ...managedUser, role: "Admin" as const, version: "AgAAAA==" };
    const fetchMock = createInitialFetch((url, init) => {
      if (url.includes(`/users/${managedUser.id}/role`) && init?.method === "PATCH") {
        return jsonResponse(promoted);
      }
      return undefined;
    });
    vi.stubGlobal("fetch", fetchMock);
    renderAdmin();

    const roleSelect = await screen.findByLabelText("Role for Managed User");
    fireEvent.change(roleSelect, { target: { value: "Admin" } });
    const row = screen.getByText("Managed User", { selector: "strong" }).closest("tr")!;
    fireEvent.click(within(row).getByRole("button", { name: "Save role" }));

    expect(await screen.findByRole("status")).toHaveTextContent(
      "Managed User is now assigned the Admin role."
    );
    const patchCall = fetchMock.mock.calls.find(call =>
      String(call[0]).includes(`/users/${managedUser.id}/role`));
    expect(patchCall).toBeDefined();
    expect((patchCall![1] as RequestInit).headers).toMatchObject({
      Authorization: "Bearer admin-token",
      "If-Match": `"${managedUser.version}"`
    });
    expect((patchCall![1] as RequestInit).body).toBe(JSON.stringify({ role: "Admin" }));
  });

  it("requires a reason and disables a User through the protected API", async () => {
    const disabled = {
      ...managedUser,
      status: "Disabled" as const,
      version: "AgAAAA=="
    };
    const fetchMock = createInitialFetch((url, init) => {
      if (url.includes(`/users/${managedUser.id}/status`) && init?.method === "PATCH") {
        return jsonResponse(disabled);
      }
      return undefined;
    });
    vi.stubGlobal("fetch", fetchMock);
    renderAdmin();

    const row = (await screen.findByText(
      "Managed User",
      { selector: "strong" }
    )).closest("tr")!;
    fireEvent.click(within(row).getByRole("button", { name: "Disable" }));
    expect(screen.getByRole("alert")).toHaveTextContent(
      "Enter a reason containing at least 5 characters."
    );

    fireEvent.change(screen.getByLabelText("Status-change reason for Managed User"), {
      target: { value: "Repeated unsafe listings" }
    });
    fireEvent.click(within(row).getByRole("button", { name: "Disable" }));

    await waitFor(() =>
      expect(screen.getByRole("status")).toHaveTextContent("Managed User is now disabled.")
    );
    expect(within(row).getByRole("button", { name: "Enable" })).toBeInTheDocument();
    const patchCall = fetchMock.mock.calls.find(call =>
      String(call[0]).includes(`/users/${managedUser.id}/status`));
    expect((patchCall![1] as RequestInit).body).toBe(JSON.stringify({
      status: "Disabled",
      reason: "Repeated unsafe listings"
    }));
  });

  it("shows an authorization-safe error returned by the Admin API", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(
      jsonResponse({ title: "Forbidden" }, 403)
    ));

    renderAdmin();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "You are not authorized to manage users."
    );
    expect(screen.queryByText("Managed User", { selector: "strong" })).not.toBeInTheDocument();
  });
});

function renderAdmin() {
  render(
    <AuthProvider initialSession={adminSession}>
      <App url="/admin" />
    </AuthProvider>
  );
}

function createInitialFetch(
  override?: (url: string, init?: RequestInit) => Response | undefined
) {
  return vi.fn(async (input: string | URL | Request, init?: RequestInit) => {
    const url = String(input);
    const overridden = override?.(url, init);
    if (overridden) return overridden;
    if (url.includes("/api/admin/dashboard")) return jsonResponse(dashboard);
    if (url.includes("/api/admin/users?")) {
      return jsonResponse({
        items: [adminUser, managedUser],
        page: 1,
        pageSize: 20,
        totalCount: 2
      });
    }
    throw new Error(`Unexpected request: ${url}`);
  });
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" }
  });
}
