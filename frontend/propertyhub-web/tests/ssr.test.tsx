import { render as renderDom, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { App } from "../src/App";
import { AuthProvider } from "../src/auth/AuthContext";
import { render } from "../src/entry-server";

describe("PropertyHub application shell", () => {
  it("server renders the requested public route", () => {
    const html = render("/properties");

    expect(html).toContain("<h1>Properties</h1>");
    expect(html).not.toContain("accessToken");
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
