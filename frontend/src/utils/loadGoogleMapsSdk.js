import { hasGoogleMapsApiKey } from './mapConfig';

const LIBRARIES = 'geometry,places';

/**
 * Đảm bảo google.maps sẵn sàng. Chỉ gọi khi đã có VITE_GOOGLE_MAPS_API_KEY (không dùng key cứng trong repo).
 */
export function loadGoogleMapsSdk() {
  return new Promise((resolve, reject) => {
    if (!hasGoogleMapsApiKey()) {
      reject(new Error('Thiếu VITE_GOOGLE_MAPS_API_KEY'));
      return;
    }

    const KEY = import.meta.env.VITE_GOOGLE_MAPS_API_KEY.trim();

    if (window.google?.maps) {
      resolve();
      return;
    }

    const existing = document.querySelector('script[src*="maps.googleapis.com/maps/api/js"]');
    if (existing) {
      let tries = 0;
      const id = setInterval(() => {
        if (window.google?.maps) {
          clearInterval(id);
          resolve();
        } else if (++tries > 200) {
          clearInterval(id);
          reject(new Error('Google Maps không sẵn sàng sau khi tải script'));
        }
      }, 50);
      return;
    }

    const s = document.createElement('script');
    s.async = true;
    s.defer = true;
    s.src = `https://maps.googleapis.com/maps/api/js?key=${KEY}&libraries=${LIBRARIES}`;
    s.onerror = () => reject(new Error('Không tải được script Google Maps'));
    s.onload = () => {
      if (window.google?.maps) resolve();
      else reject(new Error('Thiếu google.maps sau khi tải script'));
    };
    document.head.appendChild(s);
  });
}
