import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";
import { AuthProvider } from "../src/auth/AuthContext";
import type { AuthSession } from "../src/auth/types";

const adminSession: AuthSession = {
  accessToken: "admin-token",
  tokenType: "Bearer",
  expiresAtUtc: "2026-07-29T15:00:00Z",
  user: {
    id: "11111111-1111-4111-8111-111111111111",
    fullName: "Administrator",
    email: "admin@propertyhub.test",
    role: "Admin"
  }
};

const property = {
  id: "20000000-0000-4000-8000-000000000001",
  title: "Pending family house",
  description: "A complete description with enough detail for moderation.",
  city: { id: "city-id", name: "Lahore" },
  purpose: "Sale",
  propertyType: "House",
  address: "Model Town",
  price: 25000000,
  area: 10,
  areaUnit: "Marla",
  bedrooms: 4,
  bathrooms: 4,
  contactNumber: "03001234567",
  moderationStatus: "Pending",
  availabilityStatus: "Available",
  rejectionReason: null,
  createdAtUtc: "2026-07-29T00:00:00Z",
  updatedAtUtc: "2026-07-29T00:00:00Z"
};

afterEach(() => vi.unstubAllGlobals());

function renderPage() {
  return render(<AuthProvider initialSession={adminSession}>
    <App url="/admin/properties" />
  </AuthProvider>);
}

describe("property moderation", () => {
  it("approves pending properties with an Admin token", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(Response.json({ items: [property] }))
      .mockResolvedValueOnce(Response.json({ ...property, moderationStatus: "Approved" }));
    vi.stubGlobal("fetch", fetchMock);
    renderPage();

    await screen.findByRole("heading", { name: property.title });
    fireEvent.click(screen.getByRole("button", { name: "Approve" }));
    expect(await screen.findByText("Pending family house was approved.")).toBeInTheDocument();
    expect(new Headers(fetchMock.mock.calls[1][1]?.headers).get("Authorization"))
      .toBe("Bearer admin-token");
  });

  it("requires a reason before rejecting", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json({ items: [property] })));
    renderPage();
    await screen.findByRole("heading", { name: property.title });

    fireEvent.click(screen.getByRole("button", { name: "Reject" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Enter a rejection reason");
  });

  it("keeps moderation routes unavailable to regular users", () => {
    render(<AuthProvider initialSession={{ ...adminSession, user: { ...adminSession.user, role: "User" } }}>
      <App url="/admin/properties" />
    </AuthProvider>);
    expect(screen.getByRole("heading", { name: "Not authorized" })).toBeInTheDocument();
  });
});
