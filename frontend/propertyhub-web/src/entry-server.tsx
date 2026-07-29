import { renderToString } from "react-dom/server";
import { App } from "./App";
import { AuthProvider } from "./auth/AuthContext";
import { loadPublicPageData } from "./ssrData";

export async function render(url: string) {
  const initialPublicData = (await loadPublicPageData(url)) ?? null;
  const html = renderToString(
    <AuthProvider>
      <App url={url} initialPublicData={initialPublicData} />
    </AuthProvider>
  );
  return { html, initialPublicData };
}
