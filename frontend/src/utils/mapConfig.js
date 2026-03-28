/** Chỉ dùng Google Maps khi có key trong .env — tránh key mẫu bị Google chặn (màn "Something went wrong"). */
export function hasGoogleMapsApiKey() {
  return Boolean(import.meta.env.VITE_GOOGLE_MAPS_API_KEY?.trim());
}
