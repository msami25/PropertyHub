import { useState } from "react";
import {
  PropertyApiError,
  deletePropertyImage,
  setPrimaryPropertyImage,
  uploadPropertyImages,
  type PropertyImage,
  type PropertyImagesResponse
} from "../api/propertyApi";
import { ProtectedPropertyImage } from "./ProtectedPropertyImage";

interface PropertyImagesManagerProps {
  propertyId: string;
  propertyTitle: string;
  accessToken: string;
  images: PropertyImage[];
  canEdit: boolean;
  onChanged(result: PropertyImagesResponse): void;
  onSessionExpired(): void;
}

export function PropertyImagesManager({
  propertyId,
  propertyTitle,
  accessToken,
  images,
  canEdit,
  onChanged,
  onSessionExpired
}: Readonly<PropertyImagesManagerProps>) {
  const [selected, setSelected] = useState<File[]>([]);
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState("");

  function handleError(value: unknown) {
    if (value instanceof PropertyApiError) {
      if (value.status === 401) onSessionExpired();
      setError(value.message);
    } else {
      setError("The image change could not be completed.");
    }
  }

  async function upload() {
    setError("");
    if (selected.length === 0) {
      setError("Select at least one image.");
      return;
    }
    setIsBusy(true);
    try {
      onChanged(await uploadPropertyImages(accessToken, propertyId, selected));
      setSelected([]);
    } catch (value) {
      handleError(value);
    } finally {
      setIsBusy(false);
    }
  }

  async function makePrimary(imageId: string) {
    setError("");
    setIsBusy(true);
    try {
      onChanged(await setPrimaryPropertyImage(accessToken, propertyId, imageId));
    } catch (value) {
      handleError(value);
    } finally {
      setIsBusy(false);
    }
  }

  async function remove(imageId: string) {
    setError("");
    setIsBusy(true);
    try {
      onChanged(await deletePropertyImage(accessToken, propertyId, imageId));
    } catch (value) {
      handleError(value);
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <section className="image-manager" aria-label={`Images for ${propertyTitle}`}>
      <div className="image-strip">
        {images.length === 0 && <p className="hint">Add an image before this listing can be approved.</p>}
        {images.map(image => (
          <figure key={image.id}>
            <ProtectedPropertyImage
              accessToken={accessToken}
              url={image.url}
              alt={`${propertyTitle}${image.isPrimary ? " primary image" : " property image"}`}
            />
            <figcaption>{image.isPrimary ? "Primary" : `Image ${image.sortOrder}`}</figcaption>
            {canEdit && <div className="button-row">
              {!image.isPrimary && <button type="button" className="secondary"
                disabled={isBusy} onClick={() => void makePrimary(image.id)}>Make primary</button>}
              <button type="button" className="danger" disabled={isBusy || images.length === 1}
                aria-label={`Delete image ${image.sortOrder} from ${propertyTitle}`}
                onClick={() => void remove(image.id)}>Delete image</button>
            </div>}
          </figure>
        ))}
      </div>
      {canEdit && images.length < 5 && <div className="image-upload">
        <label htmlFor={`images-${propertyId}`}>Add JPEG, PNG, or WebP images (5 MB each)</label>
        <input id={`images-${propertyId}`} type="file" multiple
          accept="image/jpeg,image/png,image/webp"
          onChange={event => setSelected(Array.from(event.target.files ?? []))} />
        <button type="button" disabled={isBusy || selected.length === 0}
          onClick={() => void upload()}>{isBusy ? "Saving images..." : "Upload images"}</button>
      </div>}
      {error && <p className="error" role="alert">{error}</p>}
    </section>
  );
}
