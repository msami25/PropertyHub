import express from "express";
import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const isProduction = process.argv.includes("--production");
const port = Number(process.env.PORT ?? 3000);
const app = express();

let vite;
if (isProduction) {
  app.use("/assets", express.static(path.join(root, "dist/client/assets"), { immutable: true, maxAge: "1y" }));
} else {
  const { createServer } = await import("vite");
  vite = await createServer({ root, server: { middlewareMode: true }, appType: "custom" });
  app.use(vite.middlewares);
}

app.get("/health", (_request, response) => {
  response.json({ status: "Healthy" });
});

app.use(async (request, response, next) => {
  try {
    const templatePath = isProduction
      ? path.join(root, "dist/client/index.html")
      : path.join(root, "index.html");
    let template = await fs.readFile(templatePath, "utf8");
    let render;

    if (isProduction) {
      ({ render } = await import("../dist/server/entry-server.js"));
    } else {
      template = await vite.transformIndexHtml(request.originalUrl, template);
      ({ render } = await vite.ssrLoadModule("/src/entry-server.tsx"));
    }

    const appHtml = render(request.originalUrl);
    response.status(200).type("html").send(template.replace("<!--app-html-->", appHtml));
  } catch (error) {
    vite?.ssrFixStacktrace(error);
    next(error);
  }
});

app.listen(port, () => {
  console.log(`PropertyHub SSR listening on http://localhost:${port}`);
});
