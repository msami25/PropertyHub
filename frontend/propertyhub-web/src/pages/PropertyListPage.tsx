import { useEffect, useState, type FormEvent } from "react";
import {
  listActiveCities,
  listPublicProperties,
  propertyImageUrl,
  type ActiveCity,
  type PropertyFilters,
  type PropertyPurpose,
  type PropertySummary,
  type PropertyType
} from "../api/propertyApi";

interface PropertyListPageProps {
  initialItems?: PropertySummary[];
  navigate(path: string): void;
}

function money(value: number) {
  return new Intl.NumberFormat("en-PK", {
    style: "currency",
    currency: "PKR",
    maximumFractionDigits: 0
  }).format(value);
}

export function PropertyListPage({ initialItems, navigate }: Readonly<PropertyListPageProps>) {
  const [items, setItems] = useState<PropertySummary[]>(initialItems ?? []);
  const [cities, setCities] = useState<ActiveCity[]>([]);
  const [filters, setFilters] = useState<PropertyFilters>({});
  const [isLoading, setIsLoading] = useState(initialItems === undefined);
  const [error, setError] = useState("");

  useEffect(() => {
    void listActiveCities().then(setCities).catch(() => setCities([]));
    if (initialItems !== undefined) return;
    void load({});
  }, []);

  async function load(nextFilters: PropertyFilters) {
    setIsLoading(true);
    setError("");
    try {
      setItems(await listPublicProperties(nextFilters));
    } catch {
      setError("Properties could not be loaded. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    void load(filters);
  }

  return (
    <main id="main-content">
      <div className="page-heading">
        <div>
          <h1>Properties</h1>
          <p className="hint">Browse approved properties that are currently available.</p>
        </div>
      </div>

      <form className="filter-form panel" onSubmit={submit}>
        <label htmlFor="property-city-filter">City</label>
        <select
          id="property-city-filter"
          value={filters.cityId ?? ""}
          onChange={event => setFilters(current => ({ ...current, cityId: event.target.value || undefined }))}
        >
          <option value="">All cities</option>
          {cities.map(city => <option key={city.id} value={city.id}>{city.name}</option>)}
        </select>
        <label htmlFor="property-purpose-filter">Purpose</label>
        <select
          id="property-purpose-filter"
          value={filters.purpose ?? ""}
          onChange={event => setFilters(current => ({
            ...current,
            purpose: (event.target.value || undefined) as PropertyPurpose | undefined
          }))}
        >
          <option value="">Sale or rent</option>
          <option value="Sale">Sale</option>
          <option value="Rent">Rent</option>
        </select>
        <label htmlFor="property-type-filter">Property type</label>
        <select
          id="property-type-filter"
          value={filters.propertyType ?? ""}
          onChange={event => setFilters(current => ({
            ...current,
            propertyType: (event.target.value || undefined) as PropertyType | undefined
          }))}
        >
          <option value="">All types</option>
          {["House", "Apartment", "Plot", "Shop", "Office"].map(type =>
            <option key={type} value={type}>{type}</option>)}
        </select>
        <button type="submit" disabled={isLoading}>Apply filters</button>
      </form>

      {error && <p className="error" role="alert">{error}</p>}
      {isLoading ? (
        <p role="status">Loading properties...</p>
      ) : items.length === 0 && !error ? (
        <p className="panel">No approved, available properties match these filters.</p>
      ) : (
        <div className="card-grid">
          {items.map(property => (
            <article className="property-card" key={property.id}>
              {property.primaryImageUrl
                ? <img className="property-card-image" src={propertyImageUrl(property.primaryImageUrl)}
                    alt={`${property.title} primary image`} />
                : <div className="image-placeholder">PropertyHub</div>}
              <div className="card-content">
                <p className="eyebrow">{property.propertyType} for {property.purpose}</p>
                <h2>
                  <a
                    href={`/properties/${property.id}`}
                    onClick={event => { event.preventDefault(); navigate(`/properties/${property.id}`); }}
                  >
                    {property.title}
                  </a>
                </h2>
                <p>{property.city.name}</p>
                <p>{property.area} {property.areaUnit}</p>
                <strong>{money(property.price)}</strong>
              </div>
            </article>
          ))}
        </div>
      )}
    </main>
  );
}
