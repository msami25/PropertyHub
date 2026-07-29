import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";
import { AuthProvider } from "../src/auth/AuthContext";

const summary = {
  id: "10000000-0000-4000-8000-000000000010",
  title: "Approved Lahore home",
  city: { id: "10000000-0000-4000-8000-000000000001", name: "Lahore" },
  purpose: "Sale" as const,
  propertyType: "House" as const,
  price: 25000000,
  area: 10,
  areaUnit: "Marla" as const,
  bedrooms: 4,
  bathrooms: 4
};

afterEach(() => vi.unstubAllGlobals());

describe("public property pages", () => {
  it("renders server-provided listings and applies essential filters", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(Response.json({ items: [summary.city] }))
      .mockResolvedValueOnce(Response.json({ items: [] }));
    vi.stubGlobal("fetch", fetchMock);

    render(<AuthProvider><App url="/properties" initialPublicData={{
      kind: "property-list", items: [summary]
    }} /></AuthProvider>);

    expect(screen.getByRole("heading", { name: summary.title })).toBeInTheDocument();
    fireEvent.change(await screen.findByLabelText("City"), { target: { value: summary.city.id } });
    fireEvent.change(screen.getByLabelText("Purpose"), { target: { value: "Sale" } });
    fireEvent.click(screen.getByRole("button", { name: "Apply filters" }));

    await screen.findByText("No approved, available properties match these filters.");
    const requestUrl = String(fetchMock.mock.calls[1][0]);
    expect(requestUrl).toContain(`cityId=${summary.city.id}`);
    expect(requestUrl).toContain("purpose=Sale");
  });

  it("renders public details without a contact number", () => {
    render(<AuthProvider><App url={`/properties/${summary.id}`} initialPublicData={{
      kind: "property-detail",
      property: {
        ...summary,
        description: "A complete public description for this approved property.",
        address: "Model Town",
        sellerDisplayName: "Property Owner"
      }
    }} /></AuthProvider>);

    expect(screen.getByRole("heading", { name: summary.title })).toBeInTheDocument();
    expect(screen.getByText("Property Owner")).toBeInTheDocument();
    expect(screen.queryByText(/contact/i)).not.toBeInTheDocument();
  });

  it("shows a recoverable API error state", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new Error("offline")));
    render(<AuthProvider><App url="/properties" /></AuthProvider>);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Properties could not be loaded"
    );
    await waitFor(() => expect(screen.queryByText("Loading properties...")).not.toBeInTheDocument());
  });
});
