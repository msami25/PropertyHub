import { renderToString } from "react-dom/server";
import { App } from "./App";
import { AuthProvider } from "./auth/AuthContext";

export function render(url: string) {
  return renderToString(
    <AuthProvider>
      <App url={url} />
    </AuthProvider>
  );
}
