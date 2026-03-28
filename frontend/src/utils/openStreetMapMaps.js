import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { isPlausibleVietnamLatLng, normalizeOrderLatLng } from './orderMapCoords.js';

/** Ưu tiên geocode theo địa chỉ chữ; chỉ dùng lat/lng DB khi không geocode được (khớp đơn hàng người nhập). */
async function resolveMapPoint({ lat, lng, address }, bail) {
  const addr = typeof address === 'string' ? address.trim() : '';
  if (addr) {
    const g = await nominatimGeocode(addr);
    if (bail()) return null;
    if (g && isPlausibleVietnamLatLng(g.lat, g.lng)) return [g.lat, g.lng];
  }
  const n = normalizeOrderLatLng(lat, lng);
  return n ? [n.lat, n.lng] : null;
}

const HCMC = [10.762622, 106.660172];

function labelDivIcon(text, bg) {
  return L.divIcon({
    className: 'sonic-osm-pin',
    html: `<div style="background:${bg};color:#fff;width:26px;height:26px;border-radius:50%;border:2px solid #fff;box-shadow:0 1px 4px rgba(0,0,0,.35);display:flex;align-items:center;justify-content:center;font:700 11px system-ui,sans-serif">${text}</div>`,
    iconSize: [26, 26],
    iconAnchor: [13, 13],
  });
}

async function nominatimGeocode(address) {
  if (!address?.trim()) return null;
  let q = address.trim();
  if (!/\b(việt\s*nam|viet\s*nam|vn)\b/i.test(q)) {
    q = `${q}, Việt Nam`;
  }
  const params = new URLSearchParams({
    format: 'json',
    q,
    limit: '8',
    countrycodes: 'vn',
    addressdetails: '1',
  });
  try {
    const url = `https://nominatim.openstreetmap.org/search?${params.toString()}`;
    const r = await fetch(url, { headers: { Accept: 'application/json' } });
    const text = await r.text();
    let data;
    try {
      data = JSON.parse(text);
    } catch {
      return null;
    }
    if (!Array.isArray(data) || data.length === 0) return null;
    for (const row of data) {
      const la = parseFloat(row.lat);
      const lo = parseFloat(row.lon);
      if (isPlausibleVietnamLatLng(la, lo)) return { lat: la, lng: lo };
    }
    const row = data[0];
    return { lat: parseFloat(row.lat), lng: parseFloat(row.lon) };
  } catch {
    /* ignore */
  }
  return null;
}

function addOsmTiles(map) {
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
    maxZoom: 19,
  }).addTo(map);
}

function resetLeafletHost(containerEl) {
  if (!containerEl || containerEl._leaflet_id == null) return;
  try {
    containerEl.innerHTML = '';
    delete containerEl._leaflet_id;
    containerEl.classList.remove(
      'leaflet-container',
      'leaflet-touch',
      'leaflet-fade-anim',
      'leaflet-grab',
      'leaflet-touch-drag',
      'leaflet-touch-zoom'
    );
  } catch {
    /* ignore */
  }
}

/** Khoảng cách đại cương (km) — dùng khi không có Google geometry */
export function haversineKm(lat1, lng1, lat2, lng2) {
  const R = 6371;
  const toRad = (d) => (d * Math.PI) / 180;
  const dLat = toRad(lat2 - lat1);
  const dLng = toRad(lng2 - lng1);
  const a =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLng / 2) ** 2;
  return R * (2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a)));
}

/**
 * Bản đồ trang shipper / đơn đang chọn
 * @param {() => boolean} [opts.whenCancelled] — trả true nếu đã hủy (tránh L.map trùng container khi effect chạy lại giữa chừng geocode)
 */
export async function mountShipperOrderOsmMap(containerEl, order, opts) {
  const { isTracking, currentLocation, whenCancelled } = opts || {};
  const bail = () => Boolean(whenCancelled?.());

  let pickup = await resolveMapPoint(
    {
      lat: order.pickupLat,
      lng: order.pickupLng,
      address: order.senderAddress || order.pickupAddress,
    },
    bail
  );
  if (bail()) return null;
  let delivery = await resolveMapPoint(
    {
      lat: order.deliveryLat,
      lng: order.deliveryLng,
      address: order.receiverAddress || order.deliveryAddress,
    },
    bail
  );
  if (bail()) return null;

  if (bail() || !containerEl) return null;

  resetLeafletHost(containerEl);

  const map = L.map(containerEl, { scrollWheelZoom: true }).setView(HCMC, 12);
  addOsmTiles(map);

  const fg = L.featureGroup().addTo(map);
  const latlngs = [];

  if (pickup) {
    L.marker(pickup, { icon: labelDivIcon('A', '#22c55e') }).addTo(fg).bindPopup('Điểm lấy hàng');
    latlngs.push(pickup);
  }
  if (delivery) {
    L.marker(delivery, { icon: labelDivIcon('B', '#ef4444') }).addTo(fg).bindPopup('Điểm giao hàng');
    latlngs.push(delivery);
  }

  let shipperMarker = null;
  let routeLine = null;

  if (isTracking && currentLocation?.lat != null && currentLocation?.lng != null) {
    shipperMarker = L.marker([currentLocation.lat, currentLocation.lng], {
      icon: labelDivIcon('S', '#6366f1'),
    })
      .addTo(fg)
      .bindPopup('Vị trí của bạn');
    latlngs.push([currentLocation.lat, currentLocation.lng]);
  } else if (order.shipperLat != null && order.shipperLng != null) {
    const sn = normalizeOrderLatLng(order.shipperLat, order.shipperLng);
    if (sn) {
      shipperMarker = L.marker([sn.lat, sn.lng], { icon: labelDivIcon('S', '#6366f1') })
        .addTo(fg)
        .bindPopup('Shipper');
      latlngs.push([sn.lat, sn.lng]);
    }
  }

  if (pickup && delivery) {
    routeLine = L.polyline([pickup, delivery], { color: '#6366f1', weight: 4, opacity: 0.85 }).addTo(fg);
  }

  if (latlngs.length) {
    map.fitBounds(L.latLngBounds(latlngs), { padding: [48, 48] });
  }

  requestAnimationFrame(() => {
    requestAnimationFrame(() => map.invalidateSize());
  });

  return {
    map,
    setShipperLatLng(lat, lng) {
      if (!delivery) return;
      if (!shipperMarker) {
        shipperMarker = L.marker([lat, lng], { icon: labelDivIcon('S', '#6366f1') })
          .addTo(fg)
          .bindPopup('Bạn');
      } else {
        shipperMarker.setLatLng([lat, lng]);
      }
      if (routeLine) {
        routeLine.remove();
        routeLine = null;
      }
      routeLine = L.polyline(
        [
          [lat, lng],
          delivery,
        ],
        { color: '#6366f1', weight: 4, opacity: 0.85 }
      ).addTo(fg);
    },
    destroy() {
      try {
        map.remove();
      } catch {
        /* ignore */
      }
    },
  };
}

/**
 * Bản đồ trang theo dõi live / tra cứu (object tracking hoặc order từ API)
 */
export async function mountLiveTrackingOsmMap(containerEl, t, opts) {
  const { whenCancelled } = opts || {};
  const bail = () => Boolean(whenCancelled?.());

  const pickup = await resolveMapPoint(
    { lat: t.pickupLat, lng: t.pickupLng, address: t.pickupAddress },
    bail
  );
  if (bail()) return null;
  const delivery = await resolveMapPoint(
    { lat: t.deliveryLat, lng: t.deliveryLng, address: t.deliveryAddress },
    bail
  );
  if (bail()) return null;

  const sNorm = normalizeOrderLatLng(t.shipperLat, t.shipperLng);

  if (bail() || !containerEl) return null;

  resetLeafletHost(containerEl);

  const map = L.map(containerEl, { scrollWheelZoom: true }).setView(HCMC, 13);
  addOsmTiles(map);
  const fg = L.featureGroup().addTo(map);
  const latlngs = [];

  if (pickup) {
    L.marker(pickup, { icon: labelDivIcon('A', '#22c55e') }).addTo(fg).bindPopup(t.pickupAddress || 'Lấy hàng');
    latlngs.push(pickup);
  }
  if (delivery) {
    L.marker(delivery, { icon: labelDivIcon('B', '#ef4444') }).addTo(fg).bindPopup(t.deliveryAddress || 'Giao hàng');
    latlngs.push(delivery);
  }

  let shipperMarker = null;
  let routeLine = null;
  const deliveryPt = delivery;

  if (sNorm) {
    shipperMarker = L.marker([sNorm.lat, sNorm.lng], { icon: labelDivIcon('S', '#6366f1') }).addTo(fg).bindPopup('Shipper');
    latlngs.push([sNorm.lat, sNorm.lng]);
    if (deliveryPt) {
      routeLine = L.polyline([[sNorm.lat, sNorm.lng], deliveryPt], { color: '#6366f1', weight: 5, opacity: 0.85 }).addTo(fg);
    }
  }

  if (latlngs.length) {
    map.fitBounds(L.latLngBounds(latlngs), { padding: [64, 64] });
  }

  requestAnimationFrame(() => {
    requestAnimationFrame(() => map.invalidateSize());
  });

  return {
    map,
    setShipperLatLng(lat, lng) {
      if (!deliveryPt) return;
      if (!shipperMarker) {
        shipperMarker = L.marker([lat, lng], { icon: labelDivIcon('S', '#6366f1') }).addTo(fg).bindPopup('Shipper');
      } else {
        shipperMarker.setLatLng([lat, lng]);
      }
      if (routeLine) {
        routeLine.remove();
        routeLine = null;
      }
      routeLine = L.polyline(
        [
          [lat, lng],
          deliveryPt,
        ],
        { color: '#6366f1', weight: 5, opacity: 0.85 }
      ).addTo(fg);
    },
    destroy() {
      try {
        map.remove();
      } catch {
        /* ignore */
      }
    },
  };
}
