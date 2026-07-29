import { render as renderDom, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";
import { AuthProvider } from "../src/auth/AuthContext";
import { render } from "../src/entry-server";

describe("PropertyHub application shell", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("server renders public property data without private contact data", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json({
      items: [{
        id: "property-id",
        title: "SSR family home",
        city: { id: "city-id", name: "Lahore" },
        purpose: "Sale",
        propertyType: "House",
        price: 30000000,
        area: 10,
        areaUnit: "Marla",
        bedrooms: 4,
        bathrooms: 4
      }]
    })));

    const { html } = await render("/properties");

    expect(html).toContain("SSR family home");
    expect(html).toContain("Lahore");
    expect(html).not.toContain("accessToken");
    expect(html).not.toContain("contactNumber");
  });

  it("renders accessible primary navigation", () => {
    renderDom(
      <AuthProvider>
        <App />
      </AuthProvider>
    );

    expect(screen.getByRole("navigation", { name: "Primary navigation" })).toBeInTheDocument();
  });
});
