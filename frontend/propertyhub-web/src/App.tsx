function FoundationPage({ title }: Readonly<{ title: string }>) {
  return (
    <main id="main-content">
      <h1>{title}</h1>
      <p>PropertyHub foundation is ready for its first vertical feature slice.</p>
    </main>
  );
}

function getPageTitle(pathname: string) {
  if (pathname === "/") return "Find your next property";
  if (pathname === "/properties") return "Properties";
  if (/^\/properties\/[^/]+$/.test(pathname)) return "Property details";
  if (pathname === "/login") return "Sign in";
  if (pathname === "/register") return "Register";
  if (pathname.startsWith("/my/")) return "My PropertyHub";
  if (pathname.startsWith("/admin/")) return "Administration";
  return "Page not found";
}

export function App({ url = "/" }: Readonly<{ url?: string }>) {
  const pathname = new URL(url, "http://propertyhub.local").pathname;

  return (
    <>
      <header>
        <a className="brand" href="/">PropertyHub</a>
        <nav aria-label="Primary navigation">
          <a href="/properties">Properties</a>
        </nav>
      </header>
      <FoundationPage title={getPageTitle(pathname)} />
    </>
  );
}
