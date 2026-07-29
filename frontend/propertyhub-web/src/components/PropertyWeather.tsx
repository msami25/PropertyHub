import { useEffect, useState } from "react";
import {
  getPropertyWeather,
  type PropertyWeather as PropertyWeatherData
} from "../api/propertyApi";

interface PropertyWeatherProps {
  propertyId: string;
  cityName: string;
}

export function PropertyWeather({
  propertyId,
  cityName
}: Readonly<PropertyWeatherProps>) {
  const [weather, setWeather] = useState<PropertyWeatherData | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let active = true;
    setIsLoading(true);
    void getPropertyWeather(propertyId)
      .then(result => {
        if (active) setWeather(result);
      })
      .catch(() => {
        if (active) setWeather(null);
      })
      .finally(() => {
        if (active) setIsLoading(false);
      });
    return () => {
      active = false;
    };
  }, [propertyId]);

  return (
    <section className="panel weather-panel" aria-labelledby="current-weather-title">
      <h2 id="current-weather-title">Current weather in {cityName}</h2>
      {isLoading ? (
        <p role="status">Loading current weather...</p>
      ) : !weather?.isAvailable ? (
        <p className="hint">
          Current weather is temporarily unavailable. Property information is still available.
        </p>
      ) : (
        <>
          <p className="weather-condition">{weather.condition}</p>
          <dl>
            <div>
              <dt>Temperature</dt>
              <dd>{weather.temperatureCelsius} °C</dd>
            </div>
            <div>
              <dt>Humidity</dt>
              <dd>{weather.relativeHumidityPercent}%</dd>
            </div>
            <div>
              <dt>Wind</dt>
              <dd>{weather.windSpeedKilometresPerHour} km/h</dd>
            </div>
          </dl>
          {weather.observedAtUtc && (
            <p className="hint">
              Updated {new Date(weather.observedAtUtc).toLocaleString("en-PK", {
                timeZone: "UTC",
                dateStyle: "medium",
                timeStyle: "short"
              })} UTC
            </p>
          )}
        </>
      )}
    </section>
  );
}
