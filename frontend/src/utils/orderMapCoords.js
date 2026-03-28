/**
 * Phạm vi gần đúng Việt Nam (đất liền + đảo lớn) — dùng để bỏ qua tọa độ DB sai / geocode lệch nước ngoài.
 */
const VN_MIN_LAT = 8.15;
const VN_MAX_LAT = 23.5;
const VN_MIN_LNG = 102.0;
const VN_MAX_LNG = 109.65;

export function isPlausibleVietnamLatLng(lat, lng) {
  return (
    Number.isFinite(lat) &&
    Number.isFinite(lng) &&
    lat >= VN_MIN_LAT &&
    lat <= VN_MAX_LAT &&
    lng >= VN_MIN_LNG &&
    lng <= VN_MAX_LNG
  );
}

/**
 * @returns {{ lat: number, lng: number } | null} — null nếu không tin cậy (sẽ geocode theo địa chỉ)
 */
export function normalizeOrderLatLng(lat, lng) {
  const a = lat != null && lat !== '' ? Number(lat) : NaN;
  const b = lng != null && lng !== '' ? Number(lng) : NaN;
  if (!Number.isFinite(a) || !Number.isFinite(b)) return null;
  if (isPlausibleVietnamLatLng(a, b)) return { lat: a, lng: b };
  if (isPlausibleVietnamLatLng(b, a)) return { lat: b, lng: a };
  return null;
}
