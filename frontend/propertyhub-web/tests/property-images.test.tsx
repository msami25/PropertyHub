import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { PropertyImagesManager } from "../src/components/PropertyImagesManager";
import type { PropertyImagesResponse } from "../src/api/propertyApi";

const propertyId = "20000000-0000-4000-8000-000000000001";
const image = {
  id: "30000000-0000-4000-8000-000000000001",
  url: `/api/properties/${propertyId}/images/30000000-0000-4000-8000-000000000001`,
  sortOrder: 1,
  isPrimary: true,
  contentType: "image/png",
  fileSizeBytes: 9
};

afterEach(() => vi.unstubAllGlobals());

describe("property image management", () => {
  it("uploads multipart images with authorization and reports the updated set", async () => {
    const result: PropertyImagesResponse = {
      propertyId,
      images: [image],
      moderationStatus: "Pending"
    };
    const fetchMock = vi.fn().mockResolvedValue(Response.json(result));
    vi.stubGlobal("fetch", fetchMock);
    const onChanged = vi.fn();
    render(<PropertyImagesManager
      propertyId={propertyId}
      propertyTitle="Family home"
      accessToken="user-token"
      images={[]}
      canEdit
      onChanged={onChanged}
      onSessionExpired={vi.fn()}
    />);

    const file = new File(
      [new Uint8Array([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])],
      "home.png",
      { type: "image/png" }
    );
    fireEvent.change(screen.getByLabelText(/Add JPEG/), { target: { files: [file] } });
    fireEvent.click(screen.getByRole("button", { name: "Upload images" }));

    await waitFor(() => expect(onChanged).toHaveBeenCalledWith(result));
    const init = fetchMock.mock.calls[0][1] as RequestInit;
    expect(init.method).toBe("POST");
    expect(init.body).toBeInstanceOf(FormData);
    expect(new Headers(init.headers).get("Authorization")).toBe("Bearer user-token");
    expect(new Headers(init.headers).has("Content-Type")).toBe(false);
  });

  it("loads protected thumbnails and prevents deleting the last image", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(
      new Response(new Blob(["image"], { type: "image/png" }))
    ));
    render(<PropertyImagesManager
      propertyId={propertyId}
      propertyTitle="Family home"
      accessToken="user-token"
      images={[image]}
      canEdit
      onChanged={vi.fn()}
      onSessionExpired={vi.fn()}
    />);

    expect(await screen.findByRole("img", { name: "Family home primary image" }))
      .toHaveAttribute("src", "blob:property-image");
    expect(screen.getByRole("button", { name: "Delete image 1 from Family home" }))
      .toBeDisabled();
  });
});
