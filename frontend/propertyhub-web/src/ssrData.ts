import {
  getPublicProperty,
  listPublicProperties,
  type PropertyDetail,
  type PropertySummary
} from "./api/propertyApi";

export type PublicPageData =
  | { kind: "property-list"; items: PropertySummary[] }
  | { kind: "property-detail"; property: PropertyDetail | null }
  | { kind: "unavailable" };

export async function loadPublicPageData(url: string): Promise<PublicPageData | undefined> {
  const location = new URL(url, "http://propertyhub.local");
  if (location.pathname === "/" || location.pathname === "/properties") {
    try {
      return { kind: "property-list", items: await listPublicProperties({}, true) };
    } catch {
      return { kind: "unavailable" };
    }
  }

  const match = /^\/properties\/([^/]+)$/.exec(location.pathname);
  if (!match) return undefined;
  try {
    return { kind: "property-detail", property: await getPublicProperty(match[1], true) };
  } catch {
    return { kind: "property-detail", property: null };
  }
}
