import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";
import { AuthProvider } from "../src/auth/AuthContext";
import type { AuthSession } from "../src/auth/types";
import type { City } from "../src/api/cityApi";

const adminSession: AuthSession = {
  accessToken: "admin-token",
  tokenType: "Bearer",
  expiresAtUtc: "2026-07-28T15:00:00Z",
  user: {
    id: "11111111-1111-4111-8111-111111111111",
    fullName: "Test Administrator",
    email: "admin@propertyhub.test",
    role: "Admin"
  }
};

afterEach(() => {
  vi.unstubAllGlobals();
});

function renderPage() {
  return render(
    <AuthProvider initialSession={adminSession}>
      <App url="/admin/cities" />
    </AuthProvider>
  );
}

describe("city management", () => {
  it("shows loading then renders cities returned by the Admin API", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(
      Response.json({
        items: [{
          id: "10000000-0000-4000-8000-000000000001",
          name: "Lahore",
          isActive: true,
          latitude: 31.5204,
          longitude: 74.3587
        }]
      })
    ));

    renderPage();

    expect(screen.getByText("Loading cities…")).toBeInTheDocument();
    expect(await screen.findByRole("cell", { name: "Lahore" })).toBeInTheDocument();
  });

  it("supports creating, editing, and deleting a city", async () => {
    let cities: City[] = [];
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const method = init?.method ?? "GET";
      if (method === "GET") return Response.json({ items: cities });
      if (method === "POST") {
        const created = {
          id: "20000000-0000-4000-8000-000000000001",
          ...(JSON.parse(String(init?.body)) as Omit<City, "id">)
        };
        cities = [created];
        return Response.json(created, { status: 201 });
      }
      if (method === "PUT") {
        const updated = {
          id: cities[0].id,
          ...(JSON.parse(String(init?.body)) as Omit<City, "id">)
        };
        cities = [updated];
        return Response.json(updated);
      }
      cities = [];
      return new Response(null, { status: 204 });
    });
    vi.stubGlobal("fetch", fetchMock);
    renderPage();
    expect(await screen.findByText("No cities have been created.")).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Sialkot" } });
    fireEvent.change(screen.getByLabelText("Latitude"), { target: { value: "32.4945" } });
    fireEvent.change(screen.getByLabelText("Longitude"), { target: { value: "74.5229" } });
    fireEvent.click(screen.getByRole("button", { name: "Add city" }));

    expect(await screen.findByRole("cell", { name: "Sialkot" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Edit" }));
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Sialkot Updated" } });
    fireEvent.click(screen.getByRole("button", { name: "Save changes" }));

    expect(await screen.findByRole("cell", { name: "Sialkot Updated" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Delete Sialkot Updated" }));
    await waitFor(() =>
      expect(screen.queryByRole("cell", { name: "Sialkot Updated" })).not.toBeInTheDocument()
    );
    const mutationCalls = fetchMock.mock.calls.filter(([, init]) => init?.method);
    expect(mutationCalls).toHaveLength(3);
    expect(mutationCalls.every(([, init]) =>
      new Headers(init?.headers).get("Authorization") === "Bearer admin-token"
    )).toBe(true);
  });

  it("displays API validation and conflict errors", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(Response.json({ items: [] }))
      .mockResolvedValueOnce(Response.json(
        { title: "A city with this name already exists" },
        { status: 409 }
      ));
    vi.stubGlobal("fetch", fetchMock);
    renderPage();
    await screen.findByText("No cities have been created.");

    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Lahore" } });
    fireEvent.change(screen.getByLabelText("Latitude"), { target: { value: "31.5204" } });
    fireEvent.change(screen.getByLabelText("Longitude"), { target: { value: "74.3587" } });
    fireEvent.click(screen.getByRole("button", { name: "Add city" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "A city with this name already exists"
    );
  });

  it("shows a retryable server-error state", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(
      Response.json({ title: "An unexpected error occurred." }, { status: 500 })
    ));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("An unexpected error occurred.");
    expect(screen.getByRole("button", { name: "Refresh" })).toBeInTheDocument();
  });
});
