import { useEffect, useState, type ReactNode } from "react";
import { useAuth } from "./auth/AuthContext";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { AdminDashboardPage } from "./pages/AdminDashboardPage";
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
  className,
  isActive = false,
  children
}: Readonly<{
  href: string;
  navigate(path: string): void;
  className?: string;
  isActive?: boolean;
  children: ReactNode;
}>) {
  return (
    <a
      href={href}
      className={className}
      aria-current={isActive ? "page" : undefined}
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
  initialPublicData?: PublicPageData | null;
}

export function App({ url = "/", initialPublicData }: Readonly<AppProps>) {
  const initialUrl = new URL(url, "http://propertyhub.local");
  const [location, setLocation] = useState(`${initialUrl.pathname}${initialUrl.search}`);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
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
    setIsMenuOpen(false);
  }

  const isPropertyRoute = currentUrl.pathname === "/" ||
    currentUrl.pathname === "/properties" ||
    currentUrl.pathname.startsWith("/properties/");
  const isOwnerRoute = currentUrl.pathname === "/my" ||
    currentUrl.pathname.startsWith("/my/");

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
      if (pathname === "/admin" || pathname === "/admin/users") {
        return (
          <AdminDashboardPage
            accessToken={session.accessToken}
            currentUserId={session.user.id}
            onSessionExpired={() => {
              logout();
              navigate("/login?returnUrl=/admin");
            }}
          />
        );
      }
      return <FoundationPage title="Page not found" />;
    }
    return <FoundationPage title="Page not found" />;
  }

  return (
    <>
      <a className="skip-link" href="#main-content">Skip to content</a>
      <header className="site-header">
        <div className="site-header-inner">
          <ClientLink className="brand" href="/" navigate={navigate}>PropertyHub</ClientLink>
          <button
            className="menu-toggle"
            type="button"
            aria-controls="primary-navigation"
            aria-expanded={isMenuOpen}
            aria-label={isMenuOpen ? "Close navigation menu" : "Open navigation menu"}
            onClick={() => setIsMenuOpen(current => !current)}
          >
            <span aria-hidden="true" />
            <span aria-hidden="true" />
            <span aria-hidden="true" />
          </button>
          <nav
            id="primary-navigation"
            className="site-nav"
            data-open={isMenuOpen}
            aria-label="Primary navigation"
          >
            <ClientLink
              className="nav-link"
              href="/properties"
              navigate={navigate}
              isActive={isPropertyRoute}
            >
              Properties
            </ClientLink>
            {session ? (
              <>
                <ClientLink
                  className="nav-link"
                  href="/my/properties"
                  navigate={navigate}
                  isActive={isOwnerRoute}
                >
                  My properties
                </ClientLink>
                {session.user.role === "Admin" && (
                  <>
                    <ClientLink
                      className="nav-link"
                      href="/admin"
                      navigate={navigate}
                      isActive={currentUrl.pathname === "/admin" ||
                        currentUrl.pathname === "/admin/users"}
                    >
                      Admin dashboard
                    </ClientLink>
                    <ClientLink
                      className="nav-link"
                      href="/admin/properties"
                      navigate={navigate}
                      isActive={currentUrl.pathname === "/admin/properties"}
                    >
                      Moderation
                    </ClientLink>
                    <ClientLink
                      className="nav-link"
                      href="/admin/cities"
                      navigate={navigate}
                      isActive={currentUrl.pathname === "/admin/cities"}
                    >
                      Cities
                    </ClientLink>
                  </>
                )}
                <span className="nav-divider" aria-hidden="true" />
                <ClientLink
                  className="account-link"
                  href={session.user.role === "Admin" ? "/admin" : "/my"}
                  navigate={navigate}
                >
                  <span className="account-dot" aria-hidden="true" />
                  {session.user.fullName}
                </ClientLink>
                <button
                  className="sign-out-button"
                  onClick={() => { logout(); navigate("/"); }}
                  type="button"
                >
                  Sign out
                </button>
              </>
            ) : (
              <>
                <ClientLink
                  className="nav-link"
                  href="/login"
                  navigate={navigate}
                  isActive={currentUrl.pathname === "/login"}
                >
                  Sign in
                </ClientLink>
                <ClientLink
                  className="nav-cta"
                  href="/register"
                  navigate={navigate}
                  isActive={currentUrl.pathname === "/register"}
                >
                  Register
                </ClientLink>
              </>
            )}
          </nav>
        </div>
      </header>
      {renderPage()}
    </>
  );
}
