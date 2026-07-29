import { useEffect, useState } from "react";
import { getProtectedPropertyImage } from "../api/propertyApi";

interface ProtectedPropertyImageProps {
  accessToken: string;
  url: string;
  alt: string;
  className?: string;
}

export function ProtectedPropertyImage({
  accessToken,
  url,
  alt,
  className
}: Readonly<ProtectedPropertyImageProps>) {
  const [source, setSource] = useState("");
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    let objectUrl = "";
    let active = true;
    setFailed(false);
    void getProtectedPropertyImage(accessToken, url)
      .then(blob => {
        if (!active) return;
        objectUrl = URL.createObjectURL(blob);
        setSource(objectUrl);
      })
      .catch(() => {
        if (active) setFailed(true);
      });
    return () => {
      active = false;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [accessToken, url]);

  if (failed) return <div className={`image-placeholder ${className ?? ""}`}>Image unavailable</div>;
  if (!source) return <div className={`image-placeholder ${className ?? ""}`} role="status">Loading image...</div>;
  return <img className={className} src={source} alt={alt} />;
}
