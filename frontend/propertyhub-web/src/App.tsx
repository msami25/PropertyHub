import { useEffect, useState, type ReactNode } from "react";
import { useAuth } from "./auth/AuthContext";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { CityManagementPage } from "./pages/CityManagementPage";
import { MyPropertiesPage } from "./pages/MyPropertiesPage";
import { PropertyDetailPage } from "./pages/PropertyDetailPage";
import { PropertyListPage } from "./pages/PropertyListPage";
import { PropertyModerationPage } from "./pages/PropertyModerationPage";
import type { PublicPageData } from "./ssrData";

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

interface AppProps {
  url?: string;
  initialPublicData?: PublicPageData;
}

export function App({ url = "/", initialPublicData }: Readonly<AppProps>) {
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
    if (pathname === "/" || pathname === "/properties") {
      return (
        <PropertyListPage
          initialItems={initialPublicData?.kind === "property-list" ? initialPublicData.items : undefined}
          navigate={navigate}
        />
      );
    }
    const propertyMatch = /^\/properties\/([^/]+)$/.exec(pathname);
    if (propertyMatch) {
      return (
        <PropertyDetailPage
          propertyId={propertyMatch[1]}
          initialProperty={initialPublicData?.kind === "property-detail"
            ? initialPublicData.property
            : undefined}
        />
      );
    }
    if (pathname === "/login") {
      return <LoginPage returnUrl={currentUrl.searchParams.get("returnUrl")} navigate={navigate} />;
    }
    if (pathname === "/register") return <RegisterPage />;
    if (pathname === "/my" || pathname.startsWith("/my/")) {
      if (!session) return <FoundationPage title="Sign in required" />;
      if (pathname === "/my/properties" || pathname === "/my") {
        return (
          <MyPropertiesPage
            accessToken={session.accessToken}
            onSessionExpired={() => {
              logout();
              navigate("/login?returnUrl=/my/properties");
            }}
          />
        );
      }
      return <FoundationPage title="Page not found" />;
    }
    if (pathname === "/admin" || pathname.startsWith("/admin/")) {
      if (!session) return <FoundationPage title="Sign in required" />;
      if (session.user.role !== "Admin") return <FoundationPage title="Not authorized" />;
      if (pathname === "/admin/cities") {
        return (
          <CityManagementPage
            accessToken={session.accessToken}
            onSessionExpired={() => {
              logout();
              navigate("/login?returnUrl=/admin/cities");
            }}
          />
        );
      }
      if (pathname === "/admin/properties") {
        return (
          <PropertyModerationPage
            accessToken={session.accessToken}
            onSessionExpired={() => {
              logout();
              navigate("/login?returnUrl=/admin/properties");
            }}
          />
        );
      }
      return <FoundationPage title="Administration" />;
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
              <ClientLink href="/my/properties" navigate={navigate}>My properties</ClientLink>
              {session.user.role === "Admin" && (
                <>
                  <ClientLink href="/admin/properties" navigate={navigate}>Moderation</ClientLink>
                  <ClientLink href="/admin/cities" navigate={navigate}>Cities</ClientLink>
                </>
              )}
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
