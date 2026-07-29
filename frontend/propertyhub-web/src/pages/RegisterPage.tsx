import { useState, type FormEvent } from "react";
import { ApiError, register } from "../api/authApi";

export function RegisterPage() {
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setSuccess("");
    setSubmitting(true);
    const data = new FormData(event.currentTarget);

    try {
      const registration = await register(
        String(data.get("fullName")),
        String(data.get("email")),
        String(data.get("password"))
      );
      setSuccess(`${registration.fullName}, your account is ready. You can now sign in.`);
      event.currentTarget.reset();
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : "Unable to register.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main id="main-content">
      <h1>Register</h1>
      {error && <p className="error" role="alert">{error}</p>}
      {success && <p className="success" role="status">{success}</p>}
      <form onSubmit={handleSubmit}>
        <label htmlFor="register-name">Full name</label>
        <input id="register-name" name="fullName" autoComplete="name" minLength={2} maxLength={100} required />
        <label htmlFor="register-email">Email</label>
        <input id="register-email" name="email" type="email" autoComplete="email" required />
        <label htmlFor="register-password">Password</label>
        <input
          id="register-password"
          name="password"
          type="password"
          autoComplete="new-password"
          minLength={8}
          required
        />
        <p className="hint">Use uppercase, lowercase, a number, and a symbol.</p>
        <button disabled={submitting} type="submit">
          {submitting ? "Creating account…" : "Create account"}
        </button>
      </form>
    </main>
  );
}
