import { fireEvent, render as renderDom, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";
import { AuthProvider } from "../src/auth/AuthContext";
import { render } from "../src/entry-server";

describe("PropertyHub application shell", () => {
  afterEach(() => vi.unstubAllGlobals());

  it.each([
    ["/login", "<h1>Sign in</h1>"],
    ["/register", "<h1>Register</h1>"],
    ["/my/properties", "<h1>Sign in required</h1>"],
    ["/admin/properties", "<h1>Sign in required</h1>"]
  ])("directly server renders %s with JSON-safe empty public data", async (route, heading) => {
    const result = await render(route);

    expect(result.html).toContain(heading);
    expect(result.initialPublicData).toBeNull();
    expect(JSON.stringify(result.initialPublicData)).toBe("null");
  });

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
        bathrooms: 4,
        primaryImageUrl: "/api/properties/property-id/images/image-id"
      }]
    })));

    const { html } = await render("/properties");

    expect(html).toContain("SSR family home");
    expect(html).toContain("Lahore");
    expect(html).toContain("/api/properties/property-id/images/image-id");
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
    expect(screen.getByRole("link", { name: "Properties" })).toHaveAttribute("aria-current", "page");
    const menuButton = screen.getByRole("button", { name: "Open navigation menu" });
    expect(menuButton).toHaveAttribute("aria-expanded", "false");
    fireEvent.click(menuButton);
    expect(screen.getByRole("button", { name: "Close navigation menu" }))
      .toHaveAttribute("aria-expanded", "true");
  });
});
