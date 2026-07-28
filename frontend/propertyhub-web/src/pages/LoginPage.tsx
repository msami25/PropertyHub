import { useState, type FormEvent } from "react";
import { ApiError } from "../api/authApi";
import { useAuth } from "../auth/AuthContext";

interface LoginPageProps {
  returnUrl: string | null;
  navigate(path: string): void;
}

function safeReturnUrl(value: string | null, fallback: string) {
  return value?.startsWith("/") && !value.startsWith("//") ? value : fallback;
}

export function LoginPage({ returnUrl, navigate }: Readonly<LoginPageProps>) {
  const { login } = useAuth();
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setSubmitting(true);
    const data = new FormData(event.currentTarget);

    try {
      const session = await login(String(data.get("email")), String(data.get("password")));
      const fallback = session.user.role === "Admin" ? "/admin" : "/my";
      navigate(safeReturnUrl(returnUrl, fallback));
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : "Unable to sign in.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main id="main-content">
      <h1>Sign in</h1>
      {error && <p className="error" role="alert">{error}</p>}
      <form onSubmit={handleSubmit}>
        <label htmlFor="login-email">Email</label>
        <input id="login-email" name="email" type="email" autoComplete="email" required />
        <label htmlFor="login-password">Password</label>
        <input id="login-password" name="password" type="password" autoComplete="current-password" required />
        <button disabled={submitting} type="submit">
          {submitting ? "Signing in…" : "Sign in"}
        </button>
      </form>
    </main>
  );
}
