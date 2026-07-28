import { useCallback, useEffect, useState, type FormEvent } from "react";
import {
  CityApiError,
  createCity,
  deleteCity,
  listCities,
  updateCity,
  type City,
  type CityInput
} from "../api/cityApi";

interface CityManagementPageProps {
  accessToken: string;
  onSessionExpired(): void;
}

interface CityFormState {
  name: string;
  latitude: string;
  longitude: string;
  isActive: boolean;
}

const emptyForm: CityFormState = {
  name: "",
  latitude: "",
  longitude: "",
  isActive: true
};

export function CityManagementPage({
  accessToken,
  onSessionExpired
}: Readonly<CityManagementPageProps>) {
  const [cities, setCities] = useState<City[]>([]);
  const [form, setForm] = useState<CityFormState>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState("");
  const [formError, setFormError] = useState("");
  const [statusMessage, setStatusMessage] = useState("");

  const handleApiError = useCallback((error: unknown, fallback: string) => {
    if (error instanceof CityApiError) {
      if (error.status === 401) {
        onSessionExpired();
        return "Your session expired. Please sign in again.";
      }
      if (error.status === 403) return "You are not authorized to manage cities.";
      return error.message;
    }
    return fallback;
  }, [onSessionExpired]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setPageError("");
    try {
      setCities(await listCities(accessToken));
    } catch (error) {
      setPageError(handleApiError(error, "Cities could not be loaded. Please try again."));
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, handleApiError]);

  useEffect(() => {
    void load();
  }, [load]);

  function startEditing(city: City) {
    setEditingId(city.id);
    setForm({
      name: city.name,
      latitude: String(city.latitude),
      longitude: String(city.longitude),
      isActive: city.isActive
    });
    setFormError("");
    setStatusMessage("");
  }

  function resetForm() {
    setEditingId(null);
    setForm(emptyForm);
    setFormError("");
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError("");
    setStatusMessage("");
    const input: CityInput = {
      name: form.name.trim(),
      latitude: Number(form.latitude),
      longitude: Number(form.longitude),
      isActive: form.isActive
    };
    if (input.name.length < 2) {
      setFormError("City name must contain at least 2 characters.");
      return;
    }
    if (!Number.isFinite(input.latitude) || !Number.isFinite(input.longitude)) {
      setFormError("Latitude and longitude are required.");
      return;
    }

    setIsSaving(true);
    try {
      if (editingId) {
        const updated = await updateCity(accessToken, editingId, input);
        setCities(current => current.map(city => city.id === updated.id ? updated : city));
        setStatusMessage(`${updated.name} was updated.`);
      } else {
        const created = await createCity(accessToken, input);
        setCities(current => [...current, created].sort((left, right) =>
          left.name.localeCompare(right.name)));
        setStatusMessage(`${created.name} was created.`);
      }
      resetForm();
    } catch (error) {
      setFormError(handleApiError(error, "The city could not be saved. Please try again."));
    } finally {
      setIsSaving(false);
    }
  }

  async function remove(city: City) {
    setPageError("");
    setStatusMessage("");
    try {
      await deleteCity(accessToken, city.id);
      setCities(current => current.filter(item => item.id !== city.id));
      if (editingId === city.id) resetForm();
      setStatusMessage(`${city.name} was deleted.`);
    } catch (error) {
      setPageError(handleApiError(error, "The city could not be deleted. Please try again."));
    }
  }

  return (
    <main id="main-content">
      <div className="page-heading">
        <div>
          <h1>City management</h1>
          <p className="hint">Manage the locations available to property listings.</p>
        </div>
        <button type="button" onClick={() => void load()} disabled={isLoading}>Refresh</button>
      </div>

      {pageError && <p className="error" role="alert">{pageError}</p>}
      {statusMessage && <p className="success" role="status">{statusMessage}</p>}

      <section aria-labelledby="city-form-title" className="panel">
        <h2 id="city-form-title">{editingId ? "Edit city" : "Add city"}</h2>
        <form onSubmit={submit}>
          <label htmlFor="city-name">Name</label>
          <input
            id="city-name"
            maxLength={100}
            minLength={2}
            required
            value={form.name}
            onChange={event => setForm(current => ({ ...current, name: event.target.value }))}
          />

          <label htmlFor="city-latitude">Latitude</label>
          <input
            id="city-latitude"
            type="number"
            min="-90"
            max="90"
            step="0.000001"
            required
            value={form.latitude}
            onChange={event => setForm(current => ({ ...current, latitude: event.target.value }))}
          />

          <label htmlFor="city-longitude">Longitude</label>
          <input
            id="city-longitude"
            type="number"
            min="-180"
            max="180"
            step="0.000001"
            required
            value={form.longitude}
            onChange={event => setForm(current => ({ ...current, longitude: event.target.value }))}
          />

          <label className="checkbox-label" htmlFor="city-active">
            <input
              id="city-active"
              type="checkbox"
              checked={form.isActive}
              onChange={event => setForm(current => ({ ...current, isActive: event.target.checked }))}
            />
            Active in property forms and filters
          </label>

          {formError && <p className="error" role="alert">{formError}</p>}
          <div className="button-row">
            <button type="submit" disabled={isSaving}>
              {isSaving ? "Saving…" : editingId ? "Save changes" : "Add city"}
            </button>
            {editingId && <button className="secondary" type="button" onClick={resetForm}>Cancel</button>}
          </div>
        </form>
      </section>

      <section aria-labelledby="city-list-title" className="panel">
        <h2 id="city-list-title">Cities</h2>
        {isLoading ? (
          <p role="status">Loading cities…</p>
        ) : cities.length === 0 && !pageError ? (
          <p>No cities have been created.</p>
        ) : cities.length > 0 ? (
          <div className="table-scroll">
            <table>
              <thead>
                <tr>
                  <th scope="col">Name</th>
                  <th scope="col">Coordinates</th>
                  <th scope="col">Status</th>
                  <th scope="col">Actions</th>
                </tr>
              </thead>
              <tbody>
                {cities.map(city => (
                  <tr key={city.id}>
                    <td>{city.name}</td>
                    <td>{city.latitude}, {city.longitude}</td>
                    <td>{city.isActive ? "Active" : "Inactive"}</td>
                    <td>
                      <div className="button-row">
                        <button className="secondary" type="button" onClick={() => startEditing(city)}>
                          Edit
                        </button>
                        <button
                          className="danger"
                          type="button"
                          aria-label={`Delete ${city.name}`}
                          onClick={() => void remove(city)}
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
      </section>
    </main>
  );
}
