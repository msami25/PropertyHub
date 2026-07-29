import { useCallback, useEffect, useState, type FormEvent } from "react";
import {
  PropertyApiError,
  createProperty,
  deleteProperty,
  listActiveCities,
  listOwnedProperties,
  updateProperty,
  updatePropertyAvailability,
  type ActiveCity,
  type AreaUnit,
  type ManagedProperty,
  type PropertyInput,
  type PropertyPurpose,
  type PropertyType
} from "../api/propertyApi";
import { PropertyImagesManager } from "../components/PropertyImagesManager";

interface MyPropertiesPageProps {
  accessToken: string;
  onSessionExpired(): void;
}

interface FormState {
  title: string;
  description: string;
  purpose: PropertyPurpose;
  propertyType: PropertyType;
  cityId: string;
  address: string;
  price: string;
  area: string;
  areaUnit: AreaUnit;
  bedrooms: string;
  bathrooms: string;
  contactNumber: string;
}

const emptyForm: FormState = {
  title: "",
  description: "",
  purpose: "Sale",
  propertyType: "House",
  cityId: "",
  address: "",
  price: "",
  area: "",
  areaUnit: "Marla",
  bedrooms: "",
  bathrooms: "",
  contactNumber: ""
};

function money(value: number) {
  return new Intl.NumberFormat("en-PK", {
    style: "currency",
    currency: "PKR",
    maximumFractionDigits: 0
  }).format(value);
}

function statusClass(status: string) {
  return `status-badge status-${status.toLowerCase()}`;
}

export function MyPropertiesPage({
  accessToken,
  onSessionExpired
}: Readonly<MyPropertiesPageProps>) {
  const [properties, setProperties] = useState<ManagedProperty[]>([]);
  const [cities, setCities] = useState<ActiveCity[]>([]);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");
  const [formError, setFormError] = useState("");
  const [message, setMessage] = useState("");

  const apiError = useCallback((value: unknown, fallback: string) => {
    if (value instanceof PropertyApiError) {
      if (value.status === 401) onSessionExpired();
      return value.message;
    }
    return fallback;
  }, [onSessionExpired]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError("");
    try {
      const [owned, activeCities] = await Promise.all([
        listOwnedProperties(accessToken),
        listActiveCities()
      ]);
      setProperties(owned);
      setCities(activeCities);
    } catch (value) {
      setError(apiError(value, "Your properties could not be loaded."));
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, apiError]);

  useEffect(() => {
    void load();
  }, [load]);

  function reset() {
    setEditingId(null);
    setForm(emptyForm);
    setFormError("");
  }

  function edit(property: ManagedProperty) {
    setEditingId(property.id);
    setForm({
      title: property.title,
      description: property.description,
      purpose: property.purpose,
      propertyType: property.propertyType,
      cityId: property.city.id,
      address: property.address,
      price: String(property.price),
      area: String(property.area),
      areaUnit: property.areaUnit,
      bedrooms: property.bedrooms === null ? "" : String(property.bedrooms),
      bathrooms: property.bathrooms === null ? "" : String(property.bathrooms),
      contactNumber: property.contactNumber
    });
    setMessage("");
    setFormError("");
  }

  function toInput(): PropertyInput {
    return {
      ...form,
      title: form.title.trim(),
      description: form.description.trim(),
      address: form.address.trim(),
      contactNumber: form.contactNumber.trim(),
      price: Number(form.price),
      area: Number(form.area),
      bedrooms: form.bedrooms === "" ? null : Number(form.bedrooms),
      bathrooms: form.bathrooms === "" ? null : Number(form.bathrooms)
    };
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    setFormError("");
    setMessage("");
    if (!form.cityId) {
      setFormError("Select an active city.");
      return;
    }

    setIsSaving(true);
    try {
      const saved = editingId
        ? await updateProperty(accessToken, editingId, toInput())
        : await createProperty(accessToken, toInput());
      setProperties(current => editingId
        ? current.map(property => property.id === saved.id ? saved : property)
        : [saved, ...current]);
      setMessage(editingId
        ? "Property updated and returned to pending moderation."
        : "Property created and submitted for moderation.");
      reset();
    } catch (value) {
      setFormError(apiError(value, "The property could not be saved."));
    } finally {
      setIsSaving(false);
    }
  }

  async function setAvailability(property: ManagedProperty, status: "Sold" | "Rented") {
    setError("");
    try {
      const updated = await updatePropertyAvailability(accessToken, property.id, status);
      setProperties(current => current.map(item => item.id === updated.id ? updated : item));
      setMessage(`${property.title} was marked ${status.toLowerCase()}.`);
    } catch (value) {
      setError(apiError(value, "Availability could not be changed."));
    }
  }

  async function remove(property: ManagedProperty) {
    setError("");
    try {
      await deleteProperty(accessToken, property.id);
      setProperties(current => current.filter(item => item.id !== property.id));
      setMessage(`${property.title} was deleted.`);
      if (editingId === property.id) reset();
    } catch (value) {
      setError(apiError(value, "The property could not be deleted."));
    }
  }

  function updateImages(
    propertyId: string,
    result: import("../api/propertyApi").PropertyImagesResponse
  ) {
    setProperties(current => current.map(property => property.id === propertyId
      ? {
          ...property,
          images: result.images,
          moderationStatus: result.moderationStatus
        }
      : property));
    setMessage("Property images were updated and moderation is pending.");
  }

  return (
    <main id="main-content" className="owner-page">
      <header className="owner-hero">
        <div className="owner-hero-copy">
          <p className="eyebrow">Owner workspace</p>
          <h1>My properties</h1>
          <p className="lead">
            Create polished listings, manage images, and track every moderation decision.
          </p>
        </div>
        <button
          className="secondary refresh-button"
          type="button"
          onClick={() => void load()}
          disabled={isLoading}
        >
          {isLoading ? "Refreshing..." : "Refresh listings"}
        </button>
      </header>
      <div className="owner-feedback" aria-live="polite">
        {error && <p className="notice notice-error" role="alert">{error}</p>}
        {message && <p className="notice notice-success" role="status">{message}</p>}
      </div>

      <section className="property-form-card" aria-labelledby="property-form-title">
        <div className="section-heading form-card-heading">
          <div>
            <p className="eyebrow">{editingId ? "Update listing" : "New listing"}</p>
            <h2 id="property-form-title">{editingId ? "Edit property" : "Add property"}</h2>
            <p className="hint">
              {editingId
                ? "Material changes return an approved listing to pending moderation."
                : "Create the listing first, then add one to five images from its listing card."}
            </p>
          </div>
          {editingId && <span className="editing-indicator">Editing</span>}
        </div>
        <form className="property-form" onSubmit={submit}>
          <fieldset>
            <legend>Listing overview</legend>
            <div className="form-grid">
              <div className="form-field form-field-wide">
                <label htmlFor="property-title">Title</label>
                <input
                  id="property-title"
                  required
                  minLength={5}
                  maxLength={100}
                  placeholder="e.g. Contemporary family home in DHA"
                  aria-describedby="property-title-hint"
                  value={form.title}
                  onChange={event => setForm(current => ({ ...current, title: event.target.value }))}
                />
                <span className="field-hint" id="property-title-hint">5–100 characters</span>
              </div>
              <div className="form-field form-field-wide">
                <label htmlFor="property-description">Description</label>
                <textarea
                  id="property-description"
                  required
                  minLength={20}
                  maxLength={2000}
                  placeholder="Describe the property, its condition, layout, and nearby amenities."
                  aria-describedby="property-description-hint"
                  value={form.description}
                  onChange={event => setForm(current => ({
                    ...current,
                    description: event.target.value
                  }))}
                />
                <span className="field-hint" id="property-description-hint">
                  20–2,000 characters
                </span>
              </div>
              <div className="form-field">
                <label htmlFor="property-purpose">Purpose</label>
                <select
                  id="property-purpose"
                  value={form.purpose}
                  onChange={event => setForm(current => ({
                    ...current,
                    purpose: event.target.value as PropertyPurpose
                  }))}
                >
                  <option value="Sale">Sale</option>
                  <option value="Rent">Rent</option>
                </select>
              </div>
              <div className="form-field">
                <label htmlFor="property-type">Type</label>
                <select
                  id="property-type"
                  value={form.propertyType}
                  onChange={event => setForm(current => ({
                    ...current,
                    propertyType: event.target.value as PropertyType
                  }))}
                >
                  {["House", "Apartment", "Plot", "Shop", "Office"].map(type =>
                    <option key={type} value={type}>{type}</option>)}
                </select>
              </div>
            </div>
          </fieldset>

          <fieldset>
            <legend>Location</legend>
            <div className="form-grid">
              <div className="form-field">
                <label htmlFor="property-city">City</label>
                <select
                  id="property-city"
                  required
                  value={form.cityId}
                  onChange={event => setForm(current => ({
                    ...current,
                    cityId: event.target.value
                  }))}
                >
                  <option value="">Select a city</option>
                  {cities.map(city => <option key={city.id} value={city.id}>{city.name}</option>)}
                </select>
              </div>
              <div className="form-field">
                <label htmlFor="property-address">Address</label>
                <input
                  id="property-address"
                  required
                  minLength={5}
                  maxLength={250}
                  placeholder="Street, area, or neighbourhood"
                  value={form.address}
                  onChange={event => setForm(current => ({
                    ...current,
                    address: event.target.value
                  }))}
                />
              </div>
            </div>
          </fieldset>

          <fieldset>
            <legend>Pricing and dimensions</legend>
            <div className="form-grid form-grid-compact">
              <div className="form-field">
                <label htmlFor="property-price">Price (PKR)</label>
                <input
                  id="property-price"
                  type="number"
                  min="0.01"
                  step="0.01"
                  required
                  placeholder="25000000"
                  value={form.price}
                  onChange={event => setForm(current => ({
                    ...current,
                    price: event.target.value
                  }))}
                />
              </div>
              <div className="form-field">
                <label htmlFor="property-area">Area</label>
                <input
                  id="property-area"
                  type="number"
                  min="0.01"
                  step="0.01"
                  required
                  placeholder="5"
                  value={form.area}
                  onChange={event => setForm(current => ({
                    ...current,
                    area: event.target.value
                  }))}
                />
              </div>
              <div className="form-field">
                <label htmlFor="property-area-unit">Area unit</label>
                <select
                  id="property-area-unit"
                  value={form.areaUnit}
                  onChange={event => setForm(current => ({
                    ...current,
                    areaUnit: event.target.value as AreaUnit
                  }))}
                >
                  <option value="SquareFeet">Square feet</option>
                  <option value="Marla">Marla</option>
                  <option value="Kanal">Kanal</option>
                </select>
              </div>
              {form.propertyType !== "Plot" && <>
                <div className="form-field">
                  <label htmlFor="property-bedrooms">Bedrooms</label>
                  <input
                    id="property-bedrooms"
                    type="number"
                    min="0"
                    max="100"
                    placeholder="3"
                    value={form.bedrooms}
                    onChange={event => setForm(current => ({
                      ...current,
                      bedrooms: event.target.value
                    }))}
                  />
                </div>
                <div className="form-field">
                  <label htmlFor="property-bathrooms">Bathrooms</label>
                  <input
                    id="property-bathrooms"
                    type="number"
                    min="0"
                    max="100"
                    placeholder="2"
                    value={form.bathrooms}
                    onChange={event => setForm(current => ({
                      ...current,
                      bathrooms: event.target.value
                    }))}
                  />
                </div>
              </>}
            </div>
          </fieldset>

          <fieldset>
            <legend>Owner contact</legend>
            <div className="form-grid">
              <div className="form-field">
                <label htmlFor="property-contact">Contact number</label>
                <input
                  id="property-contact"
                  required
                  minLength={3}
                  maxLength={20}
                  inputMode="tel"
                  autoComplete="tel"
                  placeholder="0300 1234567"
                  aria-describedby="property-contact-hint"
                  value={form.contactNumber}
                  onChange={event => setForm(current => ({
                    ...current,
                    contactNumber: event.target.value
                  }))}
                />
                <span className="field-hint" id="property-contact-hint">
                  Stored with this listing and kept out of public pages.
                </span>
              </div>
            </div>
          </fieldset>

          {formError && <p className="notice notice-error form-error" role="alert">{formError}</p>}
          <div className="form-actions">
            <button className="primary-action" type="submit" disabled={isSaving}>
              <span>{isSaving
                ? "Saving..."
                : editingId
                  ? "Save changes"
                  : "Create property"}</span>
            </button>
            {editingId &&
              <button type="button" className="secondary" onClick={reset} disabled={isSaving}>
                Cancel
              </button>}
          </div>
        </form>
      </section>

      <section className="owned-listings-section" aria-labelledby="owned-properties-title">
        <div className="listings-toolbar">
          <div>
            <p className="eyebrow">Portfolio</p>
            <h2 id="owned-properties-title">Your listings</h2>
          </div>
          {!isLoading &&
            <span className="listing-count">
              {properties.length} {properties.length === 1 ? "listing" : "listings"}
            </span>}
        </div>
        {isLoading ? (
          <div className="state-card loading-state" role="status" aria-live="polite">
            <span className="loading-mark" aria-hidden="true" />
            <div>
              <strong>Loading your properties...</strong>
              <p>Retrieving your latest listings and moderation states.</p>
            </div>
          </div>
        ) : properties.length === 0 ? (
          <div className="state-card empty-state">
            <span className="empty-state-mark" aria-hidden="true">PH</span>
            <div>
              <h3>Your portfolio is ready for its first listing</h3>
              <p>You have not created a property yet.</p>
              <p className="hint">Complete the form above, then upload images from the new card.</p>
            </div>
          </div>
        ) : (
          <div className="owner-listing-grid">
            {properties.map(property => (
              <article className="owner-listing-card" key={property.id}>
                <div className="listing-card-topline">
                  <div className="listing-title-block">
                    <p className="eyebrow">{property.propertyType} for {property.purpose}</p>
                    <h3>{property.title}</h3>
                    <p className="listing-location">{property.city.name} · {property.area} {
                      property.areaUnit === "SquareFeet" ? "sq ft" : property.areaUnit
                    }</p>
                  </div>
                  <strong className="listing-price">{money(property.price)}</strong>
                </div>
                <div className="listing-status-row" aria-label="Listing status">
                  <span className={statusClass(property.moderationStatus)}>
                    {property.moderationStatus}
                  </span>
                  <span className={statusClass(property.availabilityStatus)}>
                    {property.availabilityStatus}
                  </span>
                </div>
                {property.rejectionReason &&
                  <p className="rejection-note">
                    <strong>Rejection reason</strong>
                    <span>{property.rejectionReason}</span>
                  </p>}
                <div className="listing-image-area">
                  <PropertyImagesManager
                    propertyId={property.id}
                    propertyTitle={property.title}
                    accessToken={accessToken}
                    images={property.images}
                    canEdit={property.availabilityStatus === "Available"}
                    onChanged={result => updateImages(property.id, result)}
                    onSessionExpired={onSessionExpired}
                  />
                </div>
                <div className="listing-actions" aria-label={`Actions for ${property.title}`}>
                  <button type="button" className="secondary" onClick={() => edit(property)}>
                    Edit
                  </button>
                  {property.availabilityStatus === "Available" && property.purpose === "Sale" &&
                    <button
                      type="button"
                      className="availability-action"
                      onClick={() => void setAvailability(property, "Sold")}
                    >
                      Mark sold
                    </button>}
                  {property.availabilityStatus === "Available" && property.purpose === "Rent" &&
                    <button
                      type="button"
                      className="availability-action"
                      onClick={() => void setAvailability(property, "Rented")}
                    >
                      Mark rented
                    </button>}
                  <button
                    type="button"
                    className="danger danger-subtle"
                    aria-label={`Delete ${property.title}`}
                    onClick={() => void remove(property)}
                  >
                    Delete
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </main>
  );
}
