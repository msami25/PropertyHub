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

  return (
    <main id="main-content">
      <div className="page-heading">
        <div>
          <h1>My properties</h1>
          <p className="hint">Create listings, track moderation, and update availability.</p>
        </div>
        <button type="button" onClick={() => void load()} disabled={isLoading}>Refresh</button>
      </div>
      {error && <p className="error" role="alert">{error}</p>}
      {message && <p className="success" role="status">{message}</p>}

      <section className="panel" aria-labelledby="property-form-title">
        <h2 id="property-form-title">{editingId ? "Edit property" : "Add property"}</h2>
        <form className="property-form" onSubmit={submit}>
          <label htmlFor="property-title">Title</label>
          <input id="property-title" required minLength={5} maxLength={100} value={form.title}
            onChange={event => setForm(current => ({ ...current, title: event.target.value }))} />
          <label htmlFor="property-description">Description</label>
          <textarea id="property-description" required minLength={20} maxLength={2000}
            value={form.description}
            onChange={event => setForm(current => ({ ...current, description: event.target.value }))} />
          <label htmlFor="property-city">City</label>
          <select id="property-city" required value={form.cityId}
            onChange={event => setForm(current => ({ ...current, cityId: event.target.value }))}>
            <option value="">Select a city</option>
            {cities.map(city => <option key={city.id} value={city.id}>{city.name}</option>)}
          </select>
          <label htmlFor="property-address">Address</label>
          <input id="property-address" required minLength={5} maxLength={250} value={form.address}
            onChange={event => setForm(current => ({ ...current, address: event.target.value }))} />
          <label htmlFor="property-purpose">Purpose</label>
          <select id="property-purpose" value={form.purpose}
            onChange={event => setForm(current => ({
              ...current, purpose: event.target.value as PropertyPurpose
            }))}>
            <option value="Sale">Sale</option><option value="Rent">Rent</option>
          </select>
          <label htmlFor="property-type">Type</label>
          <select id="property-type" value={form.propertyType}
            onChange={event => setForm(current => ({
              ...current, propertyType: event.target.value as PropertyType
            }))}>
            {["House", "Apartment", "Plot", "Shop", "Office"].map(type =>
              <option key={type} value={type}>{type}</option>)}
          </select>
          <label htmlFor="property-price">Price (PKR)</label>
          <input id="property-price" type="number" min="0.01" step="0.01" required value={form.price}
            onChange={event => setForm(current => ({ ...current, price: event.target.value }))} />
          <label htmlFor="property-area">Area</label>
          <input id="property-area" type="number" min="0.01" step="0.01" required value={form.area}
            onChange={event => setForm(current => ({ ...current, area: event.target.value }))} />
          <label htmlFor="property-area-unit">Area unit</label>
          <select id="property-area-unit" value={form.areaUnit}
            onChange={event => setForm(current => ({
              ...current, areaUnit: event.target.value as AreaUnit
            }))}>
            <option value="SquareFeet">Square feet</option><option value="Marla">Marla</option>
            <option value="Kanal">Kanal</option>
          </select>
          {form.propertyType !== "Plot" && <>
            <label htmlFor="property-bedrooms">Bedrooms</label>
            <input id="property-bedrooms" type="number" min="0" max="100" value={form.bedrooms}
              onChange={event => setForm(current => ({ ...current, bedrooms: event.target.value }))} />
            <label htmlFor="property-bathrooms">Bathrooms</label>
            <input id="property-bathrooms" type="number" min="0" max="100" value={form.bathrooms}
              onChange={event => setForm(current => ({ ...current, bathrooms: event.target.value }))} />
          </>}
          <label htmlFor="property-contact">Contact number</label>
          <input id="property-contact" required minLength={3} maxLength={20} value={form.contactNumber}
            onChange={event => setForm(current => ({ ...current, contactNumber: event.target.value }))} />
          {formError && <p className="error" role="alert">{formError}</p>}
          <div className="button-row">
            <button type="submit" disabled={isSaving}>{isSaving ? "Saving..." : editingId ? "Save changes" : "Create property"}</button>
            {editingId && <button type="button" className="secondary" onClick={reset}>Cancel</button>}
          </div>
        </form>
      </section>

      <section className="panel" aria-labelledby="owned-properties-title">
        <h2 id="owned-properties-title">Your listings</h2>
        {isLoading ? <p role="status">Loading your properties...</p>
          : properties.length === 0 ? <p>You have not created a property yet.</p>
          : <div className="table-scroll"><table>
            <thead><tr><th>Property</th><th>Moderation</th><th>Availability</th><th>Actions</th></tr></thead>
            <tbody>{properties.map(property => <tr key={property.id}>
              <td><strong>{property.title}</strong><br />{property.city.name}</td>
              <td>{property.moderationStatus}
                {property.rejectionReason && <span className="error"><br />{property.rejectionReason}</span>}</td>
              <td>{property.availabilityStatus}</td>
              <td><div className="button-row">
                <button type="button" className="secondary" onClick={() => edit(property)}>Edit</button>
                {property.availabilityStatus === "Available" && property.purpose === "Sale" &&
                  <button type="button" onClick={() => void setAvailability(property, "Sold")}>Mark sold</button>}
                {property.availabilityStatus === "Available" && property.purpose === "Rent" &&
                  <button type="button" onClick={() => void setAvailability(property, "Rented")}>Mark rented</button>}
                <button type="button" className="danger"
                  aria-label={`Delete ${property.title}`} onClick={() => void remove(property)}>Delete</button>
              </div></td>
            </tr>)}</tbody>
          </table></div>}
      </section>
    </main>
  );
}
