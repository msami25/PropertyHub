import { StrictMode } from "react";
import { hydrateRoot } from "react-dom/client";
import { App } from "./App";
import { AuthProvider } from "./auth/AuthContext";
import "./styles.css";

hydrateRoot(
  document.getElementById("root")!,
  <StrictMode>
    <AuthProvider>
      <App url={window.location.href} />
    </AuthProvider>
  </StrictMode>
);
