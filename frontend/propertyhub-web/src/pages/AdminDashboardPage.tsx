import { useCallback, useEffect, useState, type FormEvent } from "react";
import {
  AdminApiError,
  changeUserRole,
  changeUserStatus,
  getDashboard,
  listUsers,
  type AdminDashboard,
  type AdminUser
} from "../api/adminApi";
import type { Role } from "../auth/types";

interface AdminDashboardPageProps {
  accessToken: string;
  currentUserId: string;
  onSessionExpired(): void;
}

export function AdminDashboardPage({
  accessToken,
  currentUserId,
  onSessionExpired
}: Readonly<AdminDashboardPageProps>) {
  const [dashboard, setDashboard] = useState<AdminDashboard | null>(null);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [selectedRoles, setSelectedRoles] = useState<Record<string, Role>>({});
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const [busyUserId, setBusyUserId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const pageSize = 20;

  const describeError = useCallback((value: unknown, fallback: string) => {
    if (value instanceof AdminApiError) {
      if (value.status === 401) {
        onSessionExpired();
        return "Your session expired. Please sign in again.";
      }
      if (value.status === 403) return "You are not authorized to manage users.";
      if (value.status === 412) return "This account changed. Refresh before trying again.";
      return value.message;
    }
    return fallback;
  }, [onSessionExpired]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError("");
    try {
      const [metrics, userPage] = await Promise.all([
        getDashboard(accessToken),
        listUsers(accessToken, search, page, pageSize)
      ]);
      setDashboard(metrics);
      setUsers(userPage.items);
      setTotalCount(userPage.totalCount);
      setSelectedRoles(Object.fromEntries(userPage.items.map(user => [user.id, user.role])));
    } catch (value) {
      setError(describeError(value, "Admin data could not be loaded. Please try again."));
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, describeError, page, search]);

  useEffect(() => {
    void load();
  }, [load]);

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPage(1);
    setSearch(searchInput.trim());
  }

  async function refreshDashboard() {
    setDashboard(await getDashboard(accessToken));
  }

  async function saveRole(user: AdminUser) {
    const role = selectedRoles[user.id] ?? user.role;
    setBusyUserId(user.id);
    setError("");
    setMessage("");
    try {
      const updated = await changeUserRole(accessToken, user, role);
      setUsers(current => current.map(item => item.id === updated.id ? updated : item));
      setSelectedRoles(current => ({ ...current, [updated.id]: updated.role }));
      await refreshDashboard();
      setMessage(`${updated.fullName} is now assigned the ${updated.role} role.`);
    } catch (value) {
      setError(describeError(value, "The role could not be changed."));
    } finally {
      setBusyUserId(null);
    }
  }

  async function saveStatus(user: AdminUser) {
    const reason = reasons[user.id]?.trim() ?? "";
    if (reason.length < 5) {
      setError("Enter a reason containing at least 5 characters.");
      return;
    }

    setBusyUserId(user.id);
    setError("");
    setMessage("");
    try {
      const status = user.status === "Active" ? "Disabled" : "Active";
      const updated = await changeUserStatus(accessToken, user, status, reason);
      setUsers(current => current.map(item => item.id === updated.id ? updated : item));
      setReasons(current => ({ ...current, [updated.id]: "" }));
      await refreshDashboard();
      setMessage(`${updated.fullName} is now ${updated.status.toLowerCase()}.`);
    } catch (value) {
      setError(describeError(value, "The account status could not be changed."));
    } finally {
      setBusyUserId(null);
    }
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <main id="main-content">
      <div className="page-heading">
        <div>
          <h1>Admin dashboard</h1>
          <p className="hint">Live platform metrics and account access management.</p>
        </div>
        <button type="button" onClick={() => void load()} disabled={isLoading}>Refresh</button>
      </div>

      {error && <p className="error" role="alert">{error}</p>}
      {message && <p className="success" role="status">{message}</p>}
      {isLoading && !dashboard ? <p role="status">Loading Admin dashboard...</p> : null}

      {dashboard && (
        <section aria-labelledby="metrics-title">
          <div className="section-heading">
            <h2 id="metrics-title">Platform metrics</h2>
            <p className="hint">
              As of {new Date(dashboard.asOfUtc).toLocaleString()} · live database data
            </p>
          </div>
          <div className="metric-grid">
            <article className="metric-card"><span>Total accounts</span><strong>{dashboard.users.total}</strong></article>
            <article className="metric-card"><span>Registered users</span><strong>{dashboard.users.registered}</strong></article>
            <article className="metric-card"><span>Active accounts</span><strong>{dashboard.users.active}</strong></article>
            <article className="metric-card"><span>Disabled accounts</span><strong>{dashboard.users.disabled}</strong></article>
            <article className="metric-card"><span>Total properties</span><strong>{dashboard.properties.total}</strong></article>
            <article className="metric-card"><span>Pending properties</span><strong>{dashboard.properties.pending}</strong></article>
            <article className="metric-card"><span>Approved properties</span><strong>{dashboard.properties.approved}</strong></article>
            <article className="metric-card"><span>Rejected properties</span><strong>{dashboard.properties.rejected}</strong></article>
            <article className="metric-card"><span>Total cities</span><strong>{dashboard.totalCities}</strong></article>
          </div>
        </section>
      )}

      <section className="panel" aria-labelledby="user-management-title">
        <div className="section-heading">
          <div>
            <h2 id="user-management-title">User management</h2>
            <p className="hint">Role and status changes invalidate existing access tokens immediately.</p>
          </div>
          <form className="inline-form" role="search" onSubmit={submitSearch}>
            <label htmlFor="admin-user-search">Search users</label>
            <input
              id="admin-user-search"
              maxLength={100}
              value={searchInput}
              onChange={event => setSearchInput(event.target.value)}
            />
            <button type="submit">Search</button>
          </form>
        </div>

        {!isLoading && users.length === 0 && !error ? (
          <p>No users match this search.</p>
        ) : users.length > 0 ? (
          <>
            <div className="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th scope="col">User</th>
                    <th scope="col">Status</th>
                    <th scope="col">Properties</th>
                    <th scope="col">Role</th>
                    <th scope="col">Account status</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map(user => {
                    const isSelf = user.id === currentUserId;
                    const isAdmin = user.role === "Admin";
                    const isBusy = busyUserId === user.id;
                    return (
                      <tr key={user.id}>
                        <td>
                          <strong>{user.fullName}</strong>
                          <span className="table-subtext">{user.email}</span>
                        </td>
                        <td>{user.status}</td>
                        <td>{user.propertyCount}</td>
                        <td>
                          <div className="admin-action">
                            <label className="sr-only" htmlFor={`role-${user.id}`}>
                              Role for {user.fullName}
                            </label>
                            <select
                              id={`role-${user.id}`}
                              value={selectedRoles[user.id] ?? user.role}
                              disabled={isSelf || isBusy}
                              onChange={event => setSelectedRoles(current => ({
                                ...current,
                                [user.id]: event.target.value as Role
                              }))}
                            >
                              <option value="User">User</option>
                              <option value="Admin">Admin</option>
                            </select>
                            <button
                              className="secondary"
                              type="button"
                              disabled={isSelf || isBusy
                                || (selectedRoles[user.id] ?? user.role) === user.role}
                              onClick={() => void saveRole(user)}
                            >
                              Save role
                            </button>
                          </div>
                          {isSelf && <span className="table-subtext">You cannot demote yourself.</span>}
                        </td>
                        <td>
                          <div className="admin-action">
                            <label className="sr-only" htmlFor={`reason-${user.id}`}>
                              Status-change reason for {user.fullName}
                            </label>
                            <input
                              id={`reason-${user.id}`}
                              placeholder="Reason"
                              maxLength={500}
                              disabled={isSelf || isAdmin || isBusy}
                              value={reasons[user.id] ?? ""}
                              onChange={event => setReasons(current => ({
                                ...current,
                                [user.id]: event.target.value
                              }))}
                            />
                            <button
                              className={user.status === "Active" ? "danger" : "secondary"}
                              type="button"
                              disabled={isSelf || isAdmin || isBusy}
                              onClick={() => void saveStatus(user)}
                            >
                              {user.status === "Active" ? "Disable" : "Enable"}
                            </button>
                          </div>
                          {isAdmin && <span className="table-subtext">Admin status is protected.</span>}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <div className="pagination" aria-label="User list pagination">
              <button
                className="secondary"
                type="button"
                disabled={page <= 1 || isLoading}
                onClick={() => setPage(current => current - 1)}
              >
                Previous
              </button>
              <span>Page {page} of {totalPages}</span>
              <button
                className="secondary"
                type="button"
                disabled={page >= totalPages || isLoading}
                onClick={() => setPage(current => current + 1)}
              >
                Next
              </button>
            </div>
          </>
        ) : null}
      </section>
    </main>
  );
}
