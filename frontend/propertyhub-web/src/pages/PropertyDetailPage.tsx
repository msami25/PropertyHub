import { useEffect, useState } from "react";
import {
  getPublicProperty,
  propertyImageUrl,
  type PropertyDetail
} from "../api/propertyApi";
import { PropertyWeather } from "../components/PropertyWeather";

interface PropertyDetailPageProps {
  propertyId: string;
  initialProperty?: PropertyDetail | null;
}

export function PropertyDetailPage({
  propertyId,
  initialProperty
}: Readonly<PropertyDetailPageProps>) {
  const [property, setProperty] = useState<PropertyDetail | null>(initialProperty ?? null);
  const [isLoading, setIsLoading] = useState(initialProperty === undefined);
  const [error, setError] = useState(initialProperty === null ? "Property not found." : "");

  useEffect(() => {
    if (initialProperty !== undefined) return;
    void getPublicProperty(propertyId)
      .then(setProperty)
      .catch(() => setError("Property not found or no longer publicly available."))
      .finally(() => setIsLoading(false));
  }, [propertyId, initialProperty]);

  if (isLoading) return <main id="main-content"><p role="status">Loading property...</p></main>;
  if (!property) return <main id="main-content"><p className="error" role="alert">{error}</p></main>;

  return (
    <main id="main-content">
      <p className="eyebrow">{property.propertyType} for {property.purpose}</p>
      <h1>{property.title}</h1>
      <p className="lead">{property.address}, {property.city.name}</p>
      <div className="property-gallery">
        {property.images.map(image => <img
          key={image.id}
          className={image.isPrimary ? "detail-image primary-image" : "detail-image"}
          src={propertyImageUrl(image.url)}
          alt={`${property.title}${image.isPrimary ? " primary image" : " property image"}`}
        />)}
      </div>
      <section className="panel property-details" aria-labelledby="property-description">
        <h2 id="property-description">Property details</h2>
        <p>{property.description}</p>
        <dl>
          <div><dt>Price</dt><dd>PKR {property.price.toLocaleString("en-PK")}</dd></div>
          <div><dt>Area</dt><dd>{property.area} {property.areaUnit}</dd></div>
          {property.bedrooms !== null && <div><dt>Bedrooms</dt><dd>{property.bedrooms}</dd></div>}
          {property.bathrooms !== null && <div><dt>Bathrooms</dt><dd>{property.bathrooms}</dd></div>}
          <div><dt>Listed by</dt><dd>{property.sellerDisplayName}</dd></div>
        </dl>
      </section>
      <PropertyWeather propertyId={property.id} cityName={property.city.name} />
    </main>
  );
}
