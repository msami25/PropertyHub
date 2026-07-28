import { useEffect, useState, type ReactNode } from "react";
import { useAuth } from "./auth/AuthContext";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";

function FoundationPage({ title }: Readonly<{ title: string }>) {
  return (
    <main id="main-content">
      <h1>{title}</h1>
      <p>PropertyHub foundation is ready for its first vertical feature slice.</p>
    </main>
  );
}

function ClientLink({
  href,
  navigate,
  children
}: Readonly<{ href: string; navigate(path: string): void; children: ReactNode }>) {
  return (
    <a
      href={href}
      onClick={(event) => {
        if (!event.ctrlKey && !event.metaKey && !event.shiftKey) {
          event.preventDefault();
          navigate(href);
        }
      }}
    >
      {children}
    </a>
  );
}

export function App({ url = "/" }: Readonly<{ url?: string }>) {
  const initialUrl = new URL(url, "http://propertyhub.local");
  const [location, setLocation] = useState(`${initialUrl.pathname}${initialUrl.search}`);
  const { session, logout } = useAuth();
  const currentUrl = new URL(location, "http://propertyhub.local");

  useEffect(() => {
    const updateLocation = () => setLocation(`${window.location.pathname}${window.location.search}`);
    window.addEventListener("popstate", updateLocation);
    return () => window.removeEventListener("popstate", updateLocation);
  }, []);

  function navigate(path: string) {
    if (typeof window !== "undefined") {
      window.history.pushState({}, "", path);
    }
    setLocation(path);
  }

  function renderPage() {
    const pathname = currentUrl.pathname;
    if (pathname === "/") return <FoundationPage title="Find your next property" />;
    if (pathname === "/properties") return <FoundationPage title="Properties" />;
    if (/^\/properties\/[^/]+$/.test(pathname)) return <FoundationPage title="Property details" />;
    if (pathname === "/login") {
      return <LoginPage returnUrl={currentUrl.searchParams.get("returnUrl")} navigate={navigate} />;
    }
    if (pathname === "/register") return <RegisterPage />;
    if (pathname === "/my" || pathname.startsWith("/my/")) {
      return session
        ? <FoundationPage title="My PropertyHub" />
        : <FoundationPage title="Sign in required" />;
    }
    if (pathname === "/admin" || pathname.startsWith("/admin/")) {
      if (!session) return <FoundationPage title="Sign in required" />;
      return session.user.role === "Admin"
        ? <FoundationPage title="Administration" />
        : <FoundationPage title="Not authorized" />;
    }
    return <FoundationPage title="Page not found" />;
  }

  return (
    <>
      <header>
        <ClientLink href="/" navigate={navigate}>PropertyHub</ClientLink>
        <nav aria-label="Primary navigation">
          <ClientLink href="/properties" navigate={navigate}>Properties</ClientLink>
          {session ? (
            <>
              <ClientLink href={session.user.role === "Admin" ? "/admin" : "/my"} navigate={navigate}>
                {session.user.fullName}
              </ClientLink>
              <button className="link-button" onClick={() => { logout(); navigate("/"); }} type="button">
                Sign out
              </button>
            </>
          ) : (
            <>
              <ClientLink href="/login" navigate={navigate}>Sign in</ClientLink>
              <ClientLink href="/register" navigate={navigate}>Register</ClientLink>
            </>
          )}
        </nav>
      </header>
      {renderPage()}
    </>
  );
}
