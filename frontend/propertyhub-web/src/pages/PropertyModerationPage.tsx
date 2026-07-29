import { useCallback, useEffect, useState } from "react";
import {
  PropertyApiError,
  listPropertiesForModeration,
  moderateProperty,
  type ManagedProperty,
  type ModerationStatus
} from "../api/propertyApi";
import { ProtectedPropertyImage } from "../components/ProtectedPropertyImage";

interface PropertyModerationPageProps {
  accessToken: string;
  onSessionExpired(): void;
}

export function PropertyModerationPage({
  accessToken,
  onSessionExpired
}: Readonly<PropertyModerationPageProps>) {
  const [properties, setProperties] = useState<ManagedProperty[]>([]);
  const [filter, setFilter] = useState<ModerationStatus | "">("Pending");
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const describeError = useCallback((value: unknown, fallback: string) => {
    if (value instanceof PropertyApiError) {
      if (value.status === 401) onSessionExpired();
      if (value.status === 403) return "You are not authorized to moderate properties.";
      return value.message;
    }
    return fallback;
  }, [onSessionExpired]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError("");
    try {
      setProperties(await listPropertiesForModeration(accessToken, filter || undefined));
    } catch (value) {
      setError(describeError(value, "Properties could not be loaded."));
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, describeError, filter]);

  useEffect(() => {
    void load();
  }, [load]);

  async function moderate(property: ManagedProperty, status: "Approved" | "Rejected") {
    const reason = reasons[property.id]?.trim() ?? "";
    if (status === "Rejected" && !reason) {
      setError("Enter a rejection reason before rejecting a property.");
      return;
    }
    setError("");
    setMessage("");
    try {
      const updated = await moderateProperty(accessToken, property.id, status, reason);
      if (filter === "Pending") {
        setProperties(current => current.filter(item => item.id !== property.id));
      } else {
        setProperties(current => current.map(item => item.id === updated.id ? updated : item));
      }
      setMessage(`${property.title} was ${status.toLowerCase()}.`);
    } catch (value) {
      setError(describeError(value, "The moderation decision could not be saved."));
    }
  }

  return (
    <main id="main-content">
      <div className="page-heading">
        <div>
          <h1>Property moderation</h1>
          <p className="hint">Review submitted listings before they become public.</p>
        </div>
        <button type="button" onClick={() => void load()} disabled={isLoading}>Refresh</button>
      </div>
      <label htmlFor="moderation-filter">Moderation status</label>
      <select id="moderation-filter" value={filter}
        onChange={event => setFilter(event.target.value as ModerationStatus | "")}>
        <option value="">All statuses</option>
        <option value="Pending">Pending</option>
        <option value="Approved">Approved</option>
        <option value="Rejected">Rejected</option>
      </select>
      {error && <p className="error" role="alert">{error}</p>}
      {message && <p className="success" role="status">{message}</p>}

      <section className="panel" aria-labelledby="moderation-list-title">
        <h2 id="moderation-list-title">Listings</h2>
        {isLoading ? <p role="status">Loading properties...</p>
          : properties.length === 0 ? <p>No properties match this moderation status.</p>
          : <div className="moderation-grid">{properties.map(property =>
            <article className="property-card" key={property.id}>
              <div className="image-strip moderation-images">
                {property.images.length === 0
                  ? <p className="error">No images uploaded. This listing cannot be approved.</p>
                  : property.images.map(image => <ProtectedPropertyImage
                      key={image.id}
                      accessToken={accessToken}
                      url={image.url}
                      alt={`${property.title}${image.isPrimary ? " primary image" : " property image"}`}
                    />)}
              </div>
              <div className="card-content">
                <p className="eyebrow">{property.moderationStatus} · {property.availabilityStatus}</p>
                <h3>{property.title}</h3>
                <p>{property.description}</p>
                <p><strong>{property.city.name}</strong> · {property.address}</p>
                <label htmlFor={`reason-${property.id}`}>Rejection reason</label>
                <textarea id={`reason-${property.id}`} maxLength={500}
                  value={reasons[property.id] ?? ""}
                  onChange={event => setReasons(current => ({
                    ...current, [property.id]: event.target.value
                  }))} />
                <div className="button-row">
                  <button type="button" onClick={() => void moderate(property, "Approved")}>Approve</button>
                  <button type="button" className="danger"
                    onClick={() => void moderate(property, "Rejected")}>Reject</button>
                </div>
              </div>
            </article>)}</div>}
      </section>
    </main>
  );
}
