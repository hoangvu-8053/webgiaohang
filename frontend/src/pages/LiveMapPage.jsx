import { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { liveMapApi } from '../api/endpoints';
import useSignalR from '../hooks/useSignalR';
import { API_BASE } from '../api/client';
import { loadGoogleMapsSdk } from '../utils/loadGoogleMapsSdk';
import { hasGoogleMapsApiKey } from '../utils/mapConfig';
import { mountLiveTrackingOsmMap, haversineKm } from '../utils/openStreetMapMaps';
import { normalizeOrderLatLng } from '../utils/orderMapCoords';

export default function LiveMapPage() {
  const { orderId } = useParams();
  const navigate = useNavigate();

  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [connectionStatus, setConnectionStatus] = useState('connecting');
  const [lastUpdate, setLastUpdate] = useState(null);
  const [distanceKm, setDistanceKm] = useState(null);
  const mapRef = useRef(null);
  const googleMap = useRef(null);
  const markers = useRef({});
  const routePath = useRef(null);
  const directionsRendererRef = useRef(null);
  const directionsSvcRef = useRef(null);
  const updateShipperOnMapRef = useRef(null);
  const osmCtrlRef = useRef(null);
  const [mapInfo, setMapInfo] = useState('');

  // SignalR for real-time location
  const { connection, isConnected } = useSignalR(`${API_BASE}/locationHub`);

  useEffect(() => {
    fetchTrackingData();
  }, [orderId]);

  const fetchTrackingData = async () => {
    setLoading(true);
    try {
      const res = await liveMapApi.track(orderId);
      if (res.data.success) {
        setOrder(res.data.tracking);
        setDistanceKm(res.data.tracking.shipperLocation?.distanceToDeliveryKm);
      }
    } catch (err) {
      setError('Không thể tải thông tin theo dõi');
    } finally {
      setLoading(false);
    }
  };

  // Real-time updates via SignalR
  useEffect(() => {
    if (order?.id && connection && isConnected) {
      setConnectionStatus('connected');

      connection.invoke('SubscribeToOrder', order.id);

      connection.on('LocationUpdated', (data) => {
        if (data.orderId === order.id) {
          setOrder(prev => ({
            ...prev,
            shipperLat: data.lat,
            shipperLng: data.lng,
            currentLocation: data.address || prev.currentLocation,
            shipperLocationUpdatedAt: data.timestamp
          }));
          setLastUpdate(new Date());
          updateShipperOnMapRef.current?.(data.lat, data.lng);
        }
      });

      return () => {
        connection.off('LocationUpdated');
        connection.invoke('LeaveOrderGroup', order.id);
        setConnectionStatus('disconnected');
      };
    } else {
      setConnectionStatus('connecting');
    }
  }, [order?.id, connection, isConnected]);

  // Google Maps Initialization
  useEffect(() => {
    if (!order) return;

    let cancelled = false;

    function initMap() {
      if (!mapRef.current) return;
      const g = window.google.maps;

      const centerLat = order.deliveryLat || order.pickupLat || 10.762622;
      const centerLng = order.deliveryLng || order.pickupLng || 106.660172;

      if (!googleMap.current) {
        googleMap.current = new g.Map(mapRef.current, {
          center: { lat: parseFloat(centerLat), lng: parseFloat(centerLng) },
          zoom: 15,
          mapTypeControl: false,
          fullscreenControl: true,
        });
        directionsSvcRef.current = new g.DirectionsService();
        directionsRendererRef.current = new g.DirectionsRenderer({
          map: googleMap.current,
          suppressMarkers: true,
          polylineOptions: { strokeColor: '#6366f1', strokeOpacity: 0.85, strokeWeight: 5 },
        });
      }

      // Cleanup markers (keep map)
      Object.values(markers.current).forEach(m => m.setMap(null));
      markers.current = {};
      if (routePath.current) { routePath.current.setMap(null); routePath.current = null; }
      if (directionsRendererRef.current) directionsRendererRef.current.setMap(null);

      // --- Marker A: Pickup ---
      if (order.pickupLat && order.pickupLng) {
        const m = new g.Marker({
          position: { lat: parseFloat(order.pickupLat), lng: parseFloat(order.pickupLng) },
          map: googleMap.current,
          title: 'A - Điểm lấy hàng',
          icon: { path: g.SymbolPath.CIRCLE, scale: 18, fillColor: '#22c55e', fillOpacity: 1, strokeColor: '#fff', strokeWeight: 3 },
          label: { text: 'A', color: 'white', fontWeight: 'bold', fontSize: '11px' },
          zIndex: 1,
        });
        m.addListener('click', () => new g.InfoWindow({ content: `<div style="padding:8px"><strong>📦 A - Điểm lấy hàng</strong><br/>${order.pickup?.address || order.pickupAddress || '—'}</div>` }).open(googleMap.current, m));
        markers.current.pickup = m;
      }

      // --- Marker B: Delivery ---
      if (order.deliveryLat && order.deliveryLng) {
        const m = new g.Marker({
          position: { lat: parseFloat(order.deliveryLat), lng: parseFloat(order.deliveryLng) },
          map: googleMap.current,
          title: 'B - Điểm giao hàng',
          icon: { path: g.SymbolPath.CIRCLE, scale: 18, fillColor: '#ef4444', fillOpacity: 1, strokeColor: '#fff', strokeWeight: 3 },
          label: { text: 'B', color: 'white', fontWeight: 'bold', fontSize: '11px' },
          zIndex: 1,
        });
        m.addListener('click', () => new g.InfoWindow({ content: `<div style="padding:8px"><strong>🏠 B - Điểm giao hàng</strong><br/>${order.delivery?.address || order.deliveryAddress || '—'}</div>` }).open(googleMap.current, m));
        markers.current.delivery = m;
      }

      // --- Marker S: Shipper ---
      const sLat = parseFloat(order.shipperLat);
      const sLng = parseFloat(order.shipperLng);
      if (order.shipperLat && order.shipperLng && !isNaN(sLat) && !isNaN(sLng)) {
        const m = new g.Marker({
          position: { lat: sLat, lng: sLng },
          map: googleMap.current,
          title: '🚚 Vị trí shipper',
          icon: { path: g.SymbolPath.CIRCLE, scale: 22, fillColor: '#6366f1', fillOpacity: 1, strokeColor: '#fff', strokeWeight: 3 },
          label: { text: 'S', color: 'white', fontWeight: 'bold', fontSize: '11px' },
          zIndex: 200,
        });
        markers.current.shipper = m;

        // Đường đi thực tế: shipper → điểm giao
        drawRoute(sLat, sLng);

        // Tính khoảng cách
        if (order.deliveryLat && order.deliveryLng) {
          const d = new g.LatLng(sLat, sLng);
          const e = new g.LatLng(parseFloat(order.deliveryLat), parseFloat(order.deliveryLng));
          const dist = g.geometry.spherical.computeDistanceBetween(d, e) / 1000;
          setDistanceKm(dist);
        }
      }

      // Fit bounds
      const bounds = new g.LatLngBounds();
      let hasPoint = false;
      if (order.pickupLat && order.pickupLng) { bounds.extend({ lat: parseFloat(order.pickupLat), lng: parseFloat(order.pickupLng) }); hasPoint = true; }
      if (order.deliveryLat && order.deliveryLng) { bounds.extend({ lat: parseFloat(order.deliveryLat), lng: parseFloat(order.deliveryLng) }); hasPoint = true; }
      if (order.shipperLat && order.shipperLng) { bounds.extend({ lat: sLat, lng: sLng }); hasPoint = true; }
      if (hasPoint) googleMap.current.fitBounds(bounds, { padding: 80 });

      requestAnimationFrame(() => {
        requestAnimationFrame(() => {
          if (!googleMap.current || !window.google?.maps) return;
          window.google.maps.event.trigger(googleMap.current, 'resize');
          if (hasPoint) googleMap.current.fitBounds(bounds, { padding: 80 });
        });
      });
    }

    // Vẽ đường đi thực tế bằng Google Directions API
    function drawRoute(sLat, sLng) {
      const g = window.google.maps;
      if (!order.deliveryLat || !order.deliveryLng || !directionsSvcRef.current) return;
      directionsRendererRef.current.setMap(googleMap.current);
      directionsSvcRef.current.route(
        {
          origin: new g.LatLng(sLat, sLng),
          destination: new g.LatLng(parseFloat(order.deliveryLat), parseFloat(order.deliveryLng)),
          travelMode: g.TravelMode.DRIVING,
        },
        (result, status) => {
          if (status === 'OK') {
            directionsRendererRef.current.setDirections(result);
          } else {
            // Fallback: đường thẳng
            if (routePath.current) routePath.current.setMap(null);
            routePath.current = new g.Polyline({
              path: [new g.LatLng(sLat, sLng), new g.LatLng(parseFloat(order.deliveryLat), parseFloat(order.deliveryLng))],
              geodesic: true,
              strokeColor: '#6366f1', strokeOpacity: 0.7, strokeWeight: 4,
              map: googleMap.current,
            });
          }
        }
      );
    }

    // Cập nhật marker + route khi nhận SignalR (không clear toàn bộ map)
    function updateShipperMarkerOnMap(sLat, sLng) {
      const g = window.google.maps;
      if (!googleMap.current) return;

      if (markers.current.shipper) {
        markers.current.shipper.setPosition(new g.LatLng(sLat, sLng));
      } else {
        const m = new g.Marker({
          position: { lat: sLat, lng: sLng },
          map: googleMap.current,
          title: '🚚 Vị trí shipper',
          icon: { path: g.SymbolPath.CIRCLE, scale: 22, fillColor: '#6366f1', fillOpacity: 1, strokeColor: '#fff', strokeWeight: 3 },
          label: { text: 'S', color: 'white', fontWeight: 'bold', fontSize: '11px' },
          zIndex: 200,
        });
        markers.current.shipper = m;
      }

      drawRoute(sLat, sLng);

      // Cập nhật khoảng cách
      if (order.deliveryLat && order.deliveryLng) {
        const d = new g.LatLng(sLat, sLng);
        const e = new g.LatLng(parseFloat(order.deliveryLat), parseFloat(order.deliveryLng));
        const dist = g.geometry.spherical.computeDistanceBetween(d, e) / 1000;
        setDistanceKm(dist);
      }
    }

    function animateMarker(marker, newPosition) {
      const startPos = marker.getPosition();
      const endPos = new window.google.maps.LatLng(newPosition.lat, newPosition.lng);
      const duration = 1000;
      const startTime = Date.now();

      function animate() {
        const elapsed = Date.now() - startTime;
        const progress = Math.min(elapsed / duration, 1);
        const lat = startPos.lat() + (endPos.lat() - startPos.lat()) * progress;
        const lng = startPos.lng() + (endPos.lng() - startPos.lng()) * progress;
        marker.setPosition(new window.google.maps.LatLng(lat, lng));

        if (progress < 1) {
          requestAnimationFrame(animate);
        }
      }
      animate();
    }

    (async () => {
      osmCtrlRef.current?.destroy();
      osmCtrlRef.current = null;
      updateShipperOnMapRef.current = null;

      try {
        if (!hasGoogleMapsApiKey()) {
          setMapInfo(
            'Đang dùng OpenStreetMap. Thêm VITE_GOOGLE_MAPS_API_KEY vào .env nếu cần Google Maps + chỉ đường lái xe.'
          );
          if (mapRef.current) mapRef.current.innerHTML = '';
          googleMap.current = null;
          directionsSvcRef.current = null;
          directionsRendererRef.current = null;
          routePath.current = null;
          markers.current = {};

          if (!mapRef.current) return;
          const ctrl = await mountLiveTrackingOsmMap(mapRef.current, order, {
            whenCancelled: () => cancelled,
          });
          if (cancelled || !ctrl) {
            ctrl?.destroy?.();
            return;
          }
          osmCtrlRef.current = ctrl;
          updateShipperOnMapRef.current = (sLat, sLng) => {
            if (cancelled) return;
            ctrl.setShipperLatLng(sLat, sLng);
            const dest = normalizeOrderLatLng(order.deliveryLat, order.deliveryLng);
            if (dest) {
              setDistanceKm(haversineKm(sLat, sLng, dest.lat, dest.lng));
            }
          };
          return;
        }

        setMapInfo('');
        await loadGoogleMapsSdk();
        if (cancelled || !mapRef.current) return;
        updateShipperOnMapRef.current = updateShipperMarkerOnMap;
        initMap();
      } catch (e) {
        console.error('[LiveMap]', e);
      }
    })();

    return () => {
      cancelled = true;
      osmCtrlRef.current?.destroy();
      osmCtrlRef.current = null;
      updateShipperOnMapRef.current = null;
    };
  }, [order]);

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '4rem' }}>
        <div className="spinner" style={{ margin: '0 auto' }}></div>
        <p style={{ marginTop: '1.5rem', color: 'var(--text-muted)', fontSize: '1.1rem' }}>Đang tải bản đồ...</p>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div style={{ textAlign: 'center', padding: '4rem' }}>
        <div style={{ fontSize: '4rem', marginBottom: '1rem' }}>😕</div>
        <p style={{ color: 'var(--error)', fontSize: '1.1rem', marginBottom: '1.5rem' }}>{error || 'Không tìm thấy đơn hàng'}</p>
        <button className="btn btn-primary" onClick={() => navigate('/orders')}>
          Quay lại danh sách đơn hàng
        </button>
      </div>
    );
  }

  const statusLabel = {
    Pending: 'Chờ lấy hàng',
    Shipping: 'Đang giao hàng',
    Delivered: 'Đã giao hàng',
    Cancelled: 'Đã hủy'
  };

  const statusColor = {
    Pending: '#f59e0b',
    Shipping: '#3b82f6',
    Delivered: '#10b981',
    Cancelled: '#ef4444'
  };

  const isShipping = order.status === 'Shipping';
  const hasLocation = order.shipperLat && order.shipperLng;

  return (
    <div className="animate-fade" style={{ maxWidth: '1400px', margin: '0 auto' }}>
      {/* Header */}
      <div style={{ marginBottom: '2rem' }}>
        <button className="btn" onClick={() => navigate(-1)} style={{ marginBottom: '1rem', background: '#f8fafc', padding: '0.5rem 1.25rem' }}>
          ← Quay lại
        </button>

        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
          <div>
            <h1 style={{ fontSize: '1.75rem', fontWeight: 900, color: 'var(--primary)' }}>
              📍 Theo dõi vị trí giao hàng
            </h1>
            <p style={{ color: '#94a3b8', marginTop: '0.25rem' }}>
              Mã vận đơn: <strong style={{ color: 'var(--primary)' }}>#{order.trackingNumber}</strong>
            </p>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
            {/* Connection Status */}
            <div style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem',
              padding: '0.5rem 1rem',
              borderRadius: '999px',
              background: connectionStatus === 'connected' ? '#ecfdf5' : '#fef2f2',
              color: connectionStatus === 'connected' ? '#10b981' : '#f59e0b',
              fontSize: '0.85rem',
              fontWeight: 700
            }}>
              <span style={{
                width: '8px',
                height: '8px',
                borderRadius: '50%',
                background: connectionStatus === 'connected' ? '#10b981' : '#f59e0b',
                animation: connectionStatus === 'connected' ? 'pulse 2s infinite' : 'none'
              }}></span>
              {connectionStatus === 'connected' ? 'LIVE' : 'Đang kết nối...'}
            </div>

            <div style={{
              background: `${statusColor[order.status] || '#e2e8f0'}20`,
              color: statusColor[order.status],
              padding: '0.5rem 1.25rem',
              borderRadius: '999px',
              fontWeight: 800,
              fontSize: '0.9rem'
            }}>
              {statusLabel[order.status] || order.status}
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 400px', gap: '1.5rem' }}>
        {/* Bản đồ */}
        <div className="card" style={{ padding: 0, overflow: 'hidden', height: '600px', position: 'relative' }}>
          <div ref={mapRef} style={{ width: '100%', height: '100%' }}></div>
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

          {/* GPS Status Banner */}
          <div style={{
            position: 'absolute',
            bottom: '20px',
            left: '20px',
            right: '20px',
            background: hasLocation ? 'rgba(16, 185, 129, 0.95)' : 'rgba(245, 158, 11, 0.95)',
            color: 'white',
            padding: '1rem 1.25rem',
            borderRadius: '12px',
            display: 'flex',
            alignItems: 'center',
            gap: '0.75rem',
            fontSize: '0.9rem',
            fontWeight: 600,
            boxShadow: '0 4px 20px rgba(0,0,0,0.2)'
          }}>
            <span style={{ fontSize: '1.25rem' }}>{hasLocation ? '📍' : '⏳'}</span>
            <div style={{ flex: 1 }}>
              <div>{hasLocation ? 'GPS đang hoạt động' : isShipping ? 'Chờ shipper bật GPS' : 'Chưa có vị trí'}</div>
              {hasLocation && lastUpdate && (
                <div style={{ fontSize: '0.8rem', opacity: 0.9 }}>
                  Cập nhật: {lastUpdate.toLocaleTimeString('vi-VN')}
                </div>
              )}
            </div>
            {distanceKm !== null && (
              <div style={{ textAlign: 'right', fontWeight: 800 }}>
                <div style={{ fontSize: '0.7rem', opacity: 0.8 }}>Cách điểm giao</div>
                <div style={{ fontSize: '1.1rem' }}>{distanceKm.toFixed(1)} km</div>
              </div>
            )}
          </div>
        </div>

        {/* Sidebar */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {/* Thông tin shipper */}
          {order.shipper && (
            <div className="card" style={{ padding: '1.25rem' }}>
              <h3 style={{ fontSize: '0.9rem', fontWeight: 800, marginBottom: '1rem', color: 'var(--primary)' }}>
                THÔNG TIN SHIPPER
              </h3>
              <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                <div style={{
                  width: '50px',
                  height: '50px',
                  borderRadius: '50%',
                  background: 'linear-gradient(135deg, #6366f1, #8b5cf6)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '1.25rem',
                  color: 'white',
                  fontWeight: 800
                }}>
                  {order.shipper.name?.charAt(0) || 'S'}
                </div>
                <div>
                  <div style={{ fontWeight: 800, fontSize: '1rem' }}>{order.shipper.name}</div>
                  {order.shipper.phone && (
                    <div
                      style={{ color: '#6366f1', cursor: 'pointer', fontSize: '0.85rem' }}
                      onClick={() => window.open(`tel:${order.shipper.phone}`)}
                    >
                      📞 {order.shipper.phone}
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* Vị trí hiện tại */}
          <div className="card" style={{ padding: '1.25rem' }}>
            <h3 style={{ fontSize: '0.9rem', fontWeight: 800, marginBottom: '1rem', color: 'var(--primary)' }}>
              VỊ TRÍ HIỆN TẠI
            </h3>
            {hasLocation ? (
              <div>
                <div style={{
                  background: '#f0fdf4',
                  padding: '1rem',
                  borderRadius: '12px',
                  fontSize: '0.85rem',
                  marginBottom: '0.75rem',
                  border: '1px solid #bbf7d0'
                }}>
                  📍 {order.currentLocation || 'Đang cập nhật địa chỉ...'}
                </div>
                <div style={{ fontSize: '0.75rem', color: '#64748b' }}>
                  Tọa độ: {parseFloat(order.shipperLat).toFixed(6)}, {parseFloat(order.shipperLng).toFixed(6)}
                </div>
                {order.shipperLocationUpdatedAt && (
                  <div style={{ fontSize: '0.75rem', color: '#64748b', marginTop: '0.25rem' }}>
                    Cập nhật: {new Date(order.shipperLocationUpdatedAt).toLocaleString('vi-VN')}
                  </div>
                )}
              </div>
            ) : (
              <div style={{
                background: '#fffbeb',
                padding: '1rem',
                borderRadius: '12px',
                textAlign: 'center',
                color: '#f59e0b',
                fontSize: '0.9rem'
              }}>
                {isShipping ? '⏳ Shipper chưa bật GPS' : 'Chưa có vị trí'}
              </div>
            )}
          </div>

          {/* Lộ trình */}
          <div className="card" style={{ padding: '1.25rem' }}>
            <h3 style={{ fontSize: '0.9rem', fontWeight: 800, marginBottom: '1rem', color: 'var(--primary)' }}>
              LỘ TRÌNH GIAO HÀNG
            </h3>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
              {/* Điểm lấy */}
              <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'flex-start' }}>
                <div style={{
                  width: '28px',
                  height: '28px',
                  borderRadius: '50%',
                  background: '#22c55e',
                  color: 'white',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontWeight: 800,
                  fontSize: '0.75rem',
                  flexShrink: 0
                }}>A</div>
                <div style={{ flex: 1, fontSize: '0.85rem' }}>
                  <div style={{ color: '#64748b', fontSize: '0.7rem', fontWeight: 600 }}>ĐIỂM LẤY HÀNG</div>
                  <div style={{ fontWeight: 600 }}>{order.pickup?.address || order.pickupAddress}</div>
                </div>
              </div>

              {/* Shipper */}
              {hasLocation && (
                <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'flex-start' }}>
                  <div style={{
                    width: '28px',
                    height: '28px',
                    borderRadius: '50%',
                    background: '#6366f1',
                    color: 'white',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontSize: '0.9rem',
                    flexShrink: 0
                  }}>🚚</div>
                  <div style={{ flex: 1, fontSize: '0.85rem' }}>
                    <div style={{ color: '#64748b', fontSize: '0.7rem', fontWeight: 600 }}>VỊ TRÍ SHIPPER</div>
                    <div style={{ fontWeight: 600 }}>{order.currentLocation || 'Đang di chuyển...'}</div>
                  </div>
                </div>
              )}

              {/* Điểm giao */}
              <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'flex-start' }}>
                <div style={{
                  width: '28px',
                  height: '28px',
                  borderRadius: '50%',
                  background: '#ef4444',
                  color: 'white',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontWeight: 800,
                  fontSize: '0.75rem',
                  flexShrink: 0
                }}>B</div>
                <div style={{ flex: 1, fontSize: '0.85rem' }}>
                  <div style={{ color: '#64748b', fontSize: '0.7rem', fontWeight: 600 }}>ĐIỂM GIAO HÀNG</div>
                  <div style={{ fontWeight: 600 }}>{order.delivery?.address || order.deliveryAddress}</div>
                </div>
              </div>
            </div>
          </div>

          {/* Thông tin đơn hàng */}
          <div className="card" style={{ padding: '1.25rem' }}>
            <h3 style={{ fontSize: '0.9rem', fontWeight: 800, marginBottom: '1rem', color: 'var(--primary)' }}>
              THÔNG TIN ĐƠN HÀNG
            </h3>
            <div style={{ fontSize: '0.85rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: '#64748b' }}>Hàng hóa:</span>
                <strong>{order.product}</strong>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: '#64748b' }}>Người nhận:</span>
                <span>{order.receiverName}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontWeight: 700, color: '#10b981' }}>
                <span>Tổng tiền (COD):</span>
                <span>{order.totalAmount?.toLocaleString()} VND</span>
              </div>
            </div>
          </div>

          {/* Nút hành động */}
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            {order.shipper?.phone && (
              <button
                className="btn btn-primary"
                style={{ flex: 1 }}
                onClick={() => window.open(`tel:${order.shipper.phone}`)}
              >
                📞 Gọi Shipper
              </button>
            )}
            <button
              className="btn"
              style={{ flex: 1, background: '#f0fdf4', color: '#10b981' }}
              onClick={() => navigate(`/orders/${orderId}`)}
            >
              Chi tiết đơn
            </button>
          </div>
        </div>
      </div>

      {/* CSS Animation */}
      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.4; }
        }
      `}</style>
    </div>
  );
}
