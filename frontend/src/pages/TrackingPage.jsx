import { useState, useEffect, useRef } from 'react';
import { trackingApi } from '../api/endpoints';
import { API_BASE } from '../api/client';
import useSignalR from '../hooks/useSignalR';
import { hasGoogleMapsApiKey } from '../utils/mapConfig';
import { mountLiveTrackingOsmMap, haversineKm } from '../utils/openStreetMapMaps';

const GOOGLE_MAPS_API_KEY = import.meta.env.VITE_GOOGLE_MAPS_API_KEY || '';

export default function TrackingPage() {
  const [trackingNumber, setTrackingNumber] = useState('');
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [mapInfo, setMapInfo] = useState('');
  const mapRef = useRef(null);
  const googleMap = useRef(null);
  const markers = useRef({});
  const osmCtrlRef = useRef(null);
  const updateShipperOnMapRef = useRef(null);

  const { connection, isConnected } = useSignalR(`${API_BASE}/locationHub`);

  const handleTrack = async (e) => {
    e?.preventDefault();
    if (!trackingNumber.trim()) return;
    setLoading(true);
    setError('');
    try {
      const res = await trackingApi.getByTracking(trackingNumber);
      if (res.data.success) {
        setOrder(res.data.order);
      } else {
        setError(res.data.message || 'Không tìm thấy vận đơn.');
      }
    } catch (err) {
      setError(err.response?.data?.message || 'Có lỗi xảy ra khi tra cứu.');
    } finally {
      setLoading(false);
    }
  };

  // SignalR: UpdateLocation (khách hàng dùng event này)
  useEffect(() => {
    if (!order?.id || !connection || !isConnected) return;

    connection.invoke('SubscribeToOrder', order.id);

    connection.on('UpdateLocation', (data) => {
      if (data.orderId === order.id) {
        setOrder((prev) => ({
          ...prev,
          shipperLat: data.lat,
          shipperLng: data.lng,
          currentLocation: data.address || prev.currentLocation,
        }));
        updateShipperOnMapRef.current?.(data.lat, data.lng);
      }
    });

    return () => {
      connection.off('UpdateLocation');
      connection.invoke('LeaveOrderGroup', order.id);
    };
  }, [order?.id, connection, isConnected]);

  // Bản đồ
  useEffect(() => {
    if (!order || !mapRef.current) return;

    let cancelled = false;

    async function initGoogleMap() {
      const bounds = new window.google.maps.LatLngBounds();
      const mapOptions = {
        center: { lat: parseFloat(order.pickupLat) || 10.762622, lng: parseFloat(order.pickupLng) || 106.660172 },
        zoom: 13,
        styles: [
          { featureType: 'poi', elementType: 'labels', stylers: [{ visibility: 'off' }] },
          { featureType: 'transit', stylers: [{ visibility: 'off' }] },
        ],
      };

      if (!googleMap.current) {
        googleMap.current = new window.google.maps.Map(mapRef.current, mapOptions);
      }

      Object.values(markers.current).forEach((m) => m.setMap(null));
      markers.current = {};

      if (order.pickupLat && order.pickupLng) {
        const m = new window.google.maps.Marker({
          position: { lat: parseFloat(order.pickupLat), lng: parseFloat(order.pickupLng) },
          map: googleMap.current,
          title: 'Điểm lấy hàng',
          icon: 'http://maps.google.com/mapfiles/ms/icons/green-dot.png',
        });
        markers.current.pickup = m;
        bounds.extend(m.getPosition());
      }

      if (order.deliveryLat && order.deliveryLng) {
        const m = new window.google.maps.Marker({
          position: { lat: parseFloat(order.deliveryLat), lng: parseFloat(order.deliveryLng) },
          map: googleMap.current,
          title: 'Điểm giao hàng',
          icon: 'http://maps.google.com/mapfiles/ms/icons/red-dot.png',
        });
        markers.current.delivery = m;
        bounds.extend(m.getPosition());
      }

      if (order.shipperLat && order.shipperLng) {
        const m = new window.google.maps.Marker({
          position: { lat: parseFloat(order.shipperLat), lng: parseFloat(order.shipperLng) },
          map: googleMap.current,
          title: 'Shipper đang ở đây',
          icon: {
            url: 'https://cdn-icons-png.flaticon.com/512/2972/2972185.png',
            scaledSize: new window.google.maps.Size(40, 40),
          },
        });
        markers.current.shipper = m;
        bounds.extend(m.getPosition());
      }

      if (!bounds.isEmpty()) {
        googleMap.current.fitBounds(bounds);
      }
    }

    function updateShipperOnMap(sLat, sLng) {
      if (!window.google?.maps || !googleMap.current) return;
      const g = window.google.maps;
      if (markers.current.shipper) {
        markers.current.shipper.setPosition(new g.LatLng(sLat, sLng));
      } else {
        const m = new g.Marker({
          position: new g.LatLng(sLat, sLng),
          map: googleMap.current,
          title: 'Shipper đang ở đây',
          icon: {
            url: 'https://cdn-icons-png.flaticon.com/512/2972/2972185.png',
            scaledSize: new g.Size(40, 40),
          },
        });
        markers.current.shipper = m;
      }
    }

    (async () => {
      osmCtrlRef.current?.destroy();
      osmCtrlRef.current = null;
      updateShipperOnMapRef.current = null;

      try {
        if (!hasGoogleMapsApiKey()) {
          setMapInfo(
            'Đang dùng OpenStreetMap. Thêm VITE_GOOGLE_MAPS_API_KEY vào .env nếu cần Google Maps.'
          );
          if (mapRef.current) mapRef.current.innerHTML = '';
          googleMap.current = null;
          markers.current = {};

          if (!mapRef.current || cancelled) return;
          const ctrl = await mountLiveTrackingOsmMap(mapRef.current, order, {
            whenCancelled: () => cancelled,
          });
          if (cancelled || !ctrl) {
            ctrl?.destroy?.();
            return;
          }
          osmCtrlRef.current = ctrl;
          updateShipperOnMapRef.current = (sLat, sLng) => ctrl.setShipperLatLng(sLat, sLng);
          return;
        }

        setMapInfo('');
        if (!window.google) {
          await new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = `https://maps.googleapis.com/maps/api/js?key=${GOOGLE_MAPS_API_KEY}`;
            script.async = true;
            script.defer = true;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
          });
        }
        if (cancelled || !mapRef.current) return;
        updateShipperOnMapRef.current = updateShipperOnMap;
        initGoogleMap();
      } catch {
        // fallback OSM on error
        setMapInfo('Không tải được Google Maps — đang dùng OpenStreetMap.');
        if (mapRef.current) mapRef.current.innerHTML = '';
        googleMap.current = null;
        markers.current = {};
        if (!mapRef.current || cancelled) return;
        const ctrl = await mountLiveTrackingOsmMap(mapRef.current, order, {
          whenCancelled: () => cancelled,
        });
        if (cancelled || !ctrl) {
          ctrl?.destroy?.();
          return;
        }
        osmCtrlRef.current = ctrl;
        updateShipperOnMapRef.current = (sLat, sLng) => ctrl.setShipperLatLng(sLat, sLng);
      }
    })();

    return () => {
      cancelled = true;
      osmCtrlRef.current?.destroy();
      osmCtrlRef.current = null;
      updateShipperOnMapRef.current = null;
    };
  }, [order]);

  const statusLabel = { Pending: 'Chờ lấy hàng', Shipping: 'Đang giao', Delivered: 'Hoàn tất', Cancelled: 'Đã hủy' };
  const statusColor = { Pending: '#fbbf24', Shipping: '#3b82f6', Delivered: '#10b981', Cancelled: '#ef4444' };

  return (
    <div className="animate-fade" style={{ maxWidth: '1000px', margin: '0 auto' }}>
      <div style={{ padding: '2rem 0', textAlign: 'center' }}>
        <h1 className="brand-name" style={{ fontSize: '3rem', fontWeight: 900, marginBottom: '1rem' }}>TRA CỨU VẬN ĐƠN 👋</h1>
        <p style={{ color: 'var(--text-muted)', fontSize: '1.1rem', fontWeight: 600 }}>Cập nhật vị trí đơn hàng của bạn theo thời gian thực.</p>
      </div>

      <div className="card" style={{ padding: '1.5rem', background: 'white', display: 'flex', gap: '1rem', border: '4px solid var(--primary)', borderRadius: '999px', marginBottom: '2rem', boxShadow: '0 10px 25px -5px rgba(0,0,0,0.1)' }}>
        <input
          value={trackingNumber}
          onChange={(e) => setTrackingNumber(e.target.value)}
          onKeyPress={(e) => e.key === 'Enter' && handleTrack()}
          className="input"
          placeholder="Nhập mã vận đơn (VD: GH123456)..."
          style={{ border: 'none', background: 'none', fontSize: '1.15rem', fontWeight: 700, paddingLeft: '1rem' }}
        />
        <button
          className="btn btn-primary"
          onClick={handleTrack}
          disabled={loading}
          style={{ padding: '0.75rem 2.5rem', borderRadius: '999px' }}
        >
          {loading ? 'ĐANG TÌM...' : 'TRA CỨU ✨'}
        </button>
      </div>

      {error && (
        <div className="alert-error" style={{ textAlign: 'center', borderRadius: '16px', padding: '1rem', marginBottom: '2rem' }}>
          {error}
        </div>
      )}

      {order && (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 350px', gap: '2rem' }}>
          <div className="card" style={{ padding: '0', overflow: 'hidden', height: '500px', position: 'relative' }}>
            <div ref={mapRef} style={{ width: '100%', height: '100%' }} />
            {mapInfo && (
              <div
                style={{
                  position: 'absolute',
                  top: 0,
                  left: 0,
                  right: 0,
                  padding: '0.5rem 1rem',
                  background: 'rgba(239,246,255,0.95)',
                  color: '#1d4ed8',
                  fontSize: '0.75rem',
                  fontWeight: 600,
                  borderBottom: '1px solid #bfdbfe',
                  zIndex: 500,
                }}
              >
                {mapInfo}
              </div>
            )}
            {!order.shipperLat && order.status === 'Shipping' && (
              <div style={{ position: 'absolute', bottom: '20px', left: '20px', right: '20px', background: 'rgba(255,255,255,0.9)', padding: '1rem', borderRadius: '12px', textAlign: 'center', fontSize: '0.9rem', fontWeight: 700 }}>
                ⚠️ Shipper chưa bật định vị GPS.
              </div>
            )}
          </div>

          <div className="card animate-fade" style={{ borderTop: `10px solid ${statusColor[order.status] || '#e2e8f0'}`, padding: '2rem' }}>
            <div style={{ marginBottom: '1.5rem' }}>
              <div style={{ color: 'var(--text-muted)', fontSize: '0.75rem', fontWeight: 800, textTransform: 'uppercase' }}>Mã vận đơn</div>
              <div style={{ fontSize: '1.5rem', fontWeight: 900, color: 'var(--primary)' }}>#{order.trackingNumber}</div>
            </div>

            <div style={{ marginBottom: '1.5rem', background: `${statusColor[order.status] || '#e2e8f0'}20`, color: statusColor[order.status], padding: '0.5rem 1rem', borderRadius: '999px', fontWeight: 900, fontSize: '0.85rem', textAlign: 'center' }}>
              ● {statusLabel[order.status] || order.status}
            </div>

            <hr style={{ margin: '1.5rem 0', borderColor: '#f1f5f9' }} />

            <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
              <div>
                <div style={{ color: '#94a3b8', fontSize: '0.7rem', fontWeight: 800, marginBottom: '0.25rem' }}>NGƯỜI GỬI</div>
                <div style={{ fontSize: '1rem', fontWeight: 800 }}>{order.senderName}</div>
                <p style={{ color: 'var(--text-muted)', fontSize: '0.8rem', lineHeight: '1.4' }}>{order.pickupAddress}</p>
              </div>

              <div>
                <div style={{ color: '#94a3b8', fontSize: '0.7rem', fontWeight: 800, marginBottom: '0.25rem' }}>NGƯỜI NHẬN</div>
                <div style={{ fontSize: '1rem', fontWeight: 800 }}>{order.receiverName}</div>
                <p style={{ color: 'var(--text-muted)', fontSize: '0.8rem', lineHeight: '1.4' }}>{order.deliveryAddress}</p>
              </div>

              <div style={{ background: '#f8fafc', padding: '1rem', borderRadius: '12px', border: '1px solid #e2e8f0' }}>
                <div style={{ color: '#64748b', fontSize: '0.7rem', fontWeight: 800 }}>VỊ TRÍ HIỆN TẠI</div>
                <div style={{ fontSize: '0.9rem', fontWeight: 800, color: 'var(--primary)', marginTop: '0.25rem' }}>
                  {order.currentLocation || (order.status === 'Shipping' ? 'Đang trên đường...' : 'Chờ lấy hàng')}
                </div>
              </div>
            </div>

            <div style={{ marginTop: '2rem', fontSize: '0.75rem', color: '#94a3b8', textAlign: 'center' }}>
              Cập nhật: {new Date(order.actualDeliveryDate || order.orderDate).toLocaleString('vi-VN')}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
