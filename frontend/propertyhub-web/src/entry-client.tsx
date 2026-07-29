import { StrictMode } from "react";
import { hydrateRoot } from "react-dom/client";
import { App } from "./App";
import { AuthProvider } from "./auth/AuthContext";
import "./styles.css";

const dataElement = document.getElementById("initial-data");
const initialPublicData = dataElement?.textContent
  ? JSON.parse(dataElement.textContent)
  : undefined;

hydrateRoot(
  document.getElementById("root")!,
  <StrictMode>
    <AuthProvider>
      <App url={window.location.href} initialPublicData={initialPublicData} />
    </AuthProvider>
  </StrictMode>
);
