import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";
import { AuthProvider } from "../src/auth/AuthContext";
import type { AuthSession } from "../src/auth/types";
import type { ManagedProperty } from "../src/api/propertyApi";

const userSession: AuthSession = {
  accessToken: "user-token",
  tokenType: "Bearer",
  expiresAtUtc: "2026-07-29T15:00:00Z",
  user: {
    id: "11111111-1111-4111-8111-111111111111",
    fullName: "Listing Owner",
    email: "owner@propertyhub.test",
    role: "User"
  }
};

const city = {
  id: "10000000-0000-4000-8000-000000000001",
  name: "Lahore",
  latitude: 31.5204,
  longitude: 74.3587
};

function managed(overrides: Partial<ManagedProperty> = {}): ManagedProperty {
  return {
    id: "20000000-0000-4000-8000-000000000001",
    title: "Family house in Lahore",
    description: "A complete description with enough detail for a property listing.",
    city,
    purpose: "Sale",
    propertyType: "House",
    address: "Model Town Lahore",
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
    updatedAtUtc: "2026-07-29T00:00:00Z",
    images: [],
    ...overrides
  };
}

afterEach(() => vi.unstubAllGlobals());

function renderPage() {
  return render(<AuthProvider initialSession={userSession}><App url="/my/properties" /></AuthProvider>);
}

describe("owner property management", () => {
  it("creates, edits, marks sold, and deletes an owned listing", async () => {
    let properties: ManagedProperty[] = [];
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? "GET";
      if (url.endsWith("/api/cities")) return Response.json({ items: [city] });
      if (method === "GET") return Response.json({ items: properties });
      if (method === "POST") {
        const created = managed(JSON.parse(String(init?.body)));
        properties = [created];
        return Response.json(created, { status: 201 });
      }
      if (method === "PUT") {
        properties = [managed({ ...JSON.parse(String(init?.body)), moderationStatus: "Pending" })];
        return Response.json(properties[0]);
      }
      if (method === "PATCH") {
        properties = [{ ...properties[0], availabilityStatus: "Sold" }];
        return Response.json(properties[0]);
      }
      properties = [];
      return new Response(null, { status: 204 });
    });
    vi.stubGlobal("fetch", fetchMock);
    renderPage();
    await screen.findByText("You have not created a property yet.");

    fireEvent.change(screen.getByLabelText("Title"), { target: { value: "Family house in Lahore" } });
    fireEvent.change(screen.getByLabelText("Description"), {
      target: { value: "A complete description with enough detail for a property listing." }
    });
    fireEvent.change(screen.getByLabelText("City"), { target: { value: city.id } });
    fireEvent.change(screen.getByLabelText("Address"), { target: { value: "Model Town Lahore" } });
    fireEvent.change(screen.getByLabelText("Price (PKR)"), { target: { value: "25000000" } });
    fireEvent.change(screen.getByLabelText("Area"), { target: { value: "10" } });
    fireEvent.change(screen.getByLabelText("Contact number"), { target: { value: "03001234567" } });
    fireEvent.click(screen.getByRole("button", { name: "Create property" }));

    expect(await screen.findByText("Property created and submitted for moderation.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Edit" }));
    fireEvent.change(screen.getByLabelText("Title"), { target: { value: "Updated family house" } });
    fireEvent.click(screen.getByRole("button", { name: "Save changes" }));
    expect(await screen.findByText("Updated family house")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Mark sold" }));
    expect(await screen.findByText("Sold")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Delete Updated family house" }));
    await waitFor(() => expect(screen.queryByText("Updated family house")).not.toBeInTheDocument());

    const mutations = fetchMock.mock.calls.filter(([, init]) => init?.method);
    expect(mutations.map(([, init]) => init?.method)).toEqual(["POST", "PUT", "PATCH", "DELETE"]);
    expect(mutations.every(([, init]) =>
      new Headers(init?.headers).get("Authorization") === "Bearer user-token"
    )).toBe(true);
  });
});
