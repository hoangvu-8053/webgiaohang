import { useState, useEffect, useRef } from 'react';
import { shipperApi } from '../api/endpoints';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import useSignalR from '../hooks/useSignalR';
import { API_BASE } from '../api/client';
import { loadGoogleMapsSdk } from '../utils/loadGoogleMapsSdk';
import { hasGoogleMapsApiKey } from '../utils/mapConfig';
import { mountShipperOrderOsmMap } from '../utils/openStreetMapMaps';
import { normalizeOrderLatLng } from '../utils/orderMapCoords';

export default function ShipperOrdersPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState('');
  const [isTracking, setIsTracking] = useState(false);
  const [currentLocation, setCurrentLocation] = useState(null);
  const [selectedOrderId, setSelectedOrderId] = useState(null);
  const [showLocationModal, setShowLocationModal] = useState(false);
  const [mapError, setMapError] = useState('');
  const [mapInfo, setMapInfo] = useState('');
  const watchId = useRef(null);
  const lastUpdateTime = useRef(0);
  const mapElRef = useRef(null);
  const gMapRef = useRef(null);
  const gMarkersRef = useRef({});
  const gRouteRef = useRef(null);
  const routeRendererRef = useRef(null); // DirectionsService renderer
  const osmCtrlRef = useRef(null);

  // SignalR: nhận vị trí cập nhật từ server
  const { connection, isConnected } = useSignalR(`${API_BASE}/locationHub`);

  // Lắng nghe LocationUpdated từ SignalR để cập nhật map live
  useEffect(() => {
    if (!connection || !isConnected || !selectedOrderId) return;

    connection.invoke('SubscribeToOrder', selectedOrderId);

    connection.on('LocationUpdated', (data) => {
      if (data.orderId === selectedOrderId) {
        const newLoc = { lat: data.lat, lng: data.lng, accuracy: 0 };
        setCurrentLocation(newLoc);

        // Cập nhật marker trên bản đồ
        const g = window.google?.maps;
        if (g && gMapRef.current) {
          updateShipperMarker(g, data.lat, data.lng);
        }
        osmCtrlRef.current?.setShipperLatLng?.(data.lat, data.lng);
      }
    });

    return () => {
      connection.off('LocationUpdated');
      connection.invoke('LeaveOrderGroup', selectedOrderId).catch(() => {});
    };
  }, [connection, isConnected, selectedOrderId]);

  // Hàm cập nhật / tạo marker vị trí shipper trên bản đồ
  const updateShipperMarker = (g, lat, lng) => {
    const key = 'Sme';
    const existing = gMarkersRef.current[key];

    if (existing) {
      // Di chuyển marker tới vị trí mới (smooth)
      const newPos = new g.LatLng(lat, lng);
      existing.setPosition(newPos);

      // Vẽ route thực tế từ vị trí mới tới điểm giao
      drawLiveRoute(g, lat, lng);
    } else {
      // Tạo marker mới nếu chưa có
      const marker = new g.Marker({
        position: new g.LatLng(lat, lng),
        map: gMapRef.current,
        title: 'Vị trí của bạn',
        icon: {
          path: g.SymbolPath.CIRCLE,
          scale: 22,
          fillColor: '#6366f1',
          fillOpacity: 1,
          strokeColor: '#fff',
          strokeWeight: 3,
        },
        label: { text: 'S', color: 'white', fontWeight: 'bold', fontSize: '11px' },
        zIndex: 200,
      });
      gMarkersRef.current[key] = marker;
      drawLiveRoute(g, lat, lng);
    }
  };

  // Vẽ đường đi thực tế (Google Directions) từ vị trí shipper tới điểm giao
  const drawLiveRoute = (g, shipperLat, shipperLng) => {
    const order = orders.find((o) => o.id === selectedOrderId);
    if (!order) return;

    // Xóa route cũ
    if (routeRendererRef.current) {
      routeRendererRef.current.setMap(null);
      routeRendererRef.current = null;
    }

    // Cần tọa độ điểm giao
    const dest = normalizeOrderLatLng(order.deliveryLat, order.deliveryLng);
    if (!dest) return;
    const destLat = dest.lat;
    const destLng = dest.lng;

    const ds = new g.DirectionsService();
    const dr = new g.DirectionsRenderer({
      map: gMapRef.current,
      suppressMarkers: true, // đã có marker riêng
      polylineOptions: {
        strokeColor: '#6366f1',
        strokeOpacity: 0.85,
        strokeWeight: 5,
      },
    });
    routeRendererRef.current = dr;

    ds.route(
      {
        origin: new g.LatLng(shipperLat, shipperLng),
        destination: new g.LatLng(destLat, destLng),
        travelMode: g.TravelMode.DRIVING,
      },
      (result, status) => {
        if (status === 'OK') {
          dr.setDirections(result);
        } else {
          // Fallback: vẽ đường thẳng
          const shipperPos = new g.LatLng(shipperLat, shipperLng);
          const deliveryPos = new g.LatLng(destLat, destLng);
          if (gRouteRef.current) gRouteRef.current.setMap(null);
          gRouteRef.current = new g.Polyline({
            path: [shipperPos, deliveryPos],
            geodesic: true,
            strokeColor: '#6366f1',
            strokeOpacity: 0.7,
            strokeWeight: 4,
            map: gMapRef.current,
          });
        }
      }
    );
  };

  useEffect(() => {
    fetchOrders();

    // Cleanup on unmount
    return () => {
      if (watchId.current !== null) {
        navigator.geolocation.clearWatch(watchId.current);
      }
    };
  }, []);

  // Auto-update location when tracking is enabled
  useEffect(() => {
    if (isTracking && selectedOrderId) {
      startTracking();
    } else if (!isTracking && watchId.current !== null) {
      stopTracking();
    }
  }, [isTracking, selectedOrderId]);

  // Bản đồ đơn đang chọn (điểm lấy / giao / vị trí shipper)
  useEffect(() => {
    const order = orders.find((o) => o.id === selectedOrderId);
    const el = mapElRef.current;
    if (!order || !el) return;

    let cancelled = false;

    const clearMapOverlays = () => {
      Object.values(gMarkersRef.current).forEach((m) => {
        try {
          m?.setMap?.(null);
        } catch {
          /* ignore */
        }
      });
      gMarkersRef.current = {};
      if (gRouteRef.current) {
        try {
          gRouteRef.current.setMap(null);
        } catch {
          /* ignore */
        }
        gRouteRef.current = null;
      }
      if (routeRendererRef.current) {
        try {
          routeRendererRef.current.setMap(null);
        } catch {
          /* ignore */
        }
        routeRendererRef.current = null;
      }
    };

    const run = async () => {
      osmCtrlRef.current?.destroy();
      osmCtrlRef.current = null;

      try {
        setMapError('');
        setMapInfo('');

        if (!hasGoogleMapsApiKey()) {
          const hadGoogle = !!gMapRef.current;
          clearMapOverlays();
          gMapRef.current = null;
          // Chỉ xóa DOM khi từng dùng Google — tránh innerHTML giữa chừng khiến Leaflet lỗi / ô xám
          if (hadGoogle && mapElRef.current) mapElRef.current.innerHTML = '';

          setMapInfo(
            'Đang dùng OpenStreetMap (miễn phí, không cần API key). Đường vẽ là nét thẳng. Để dùng Google Maps và chỉ đường chi tiết, thêm VITE_GOOGLE_MAPS_API_KEY vào .env của frontend.'
          );

          const ctrl = await mountShipperOrderOsmMap(mapElRef.current, order, {
            isTracking,
            currentLocation,
            whenCancelled: () => cancelled,
          });
          if (cancelled) {
            ctrl?.destroy?.();
            return;
          }
          if (!ctrl || !mapElRef.current) {
            ctrl?.destroy?.();
            if (!cancelled) {
              setMapError(
                'Không hiển thị được bản đồ OpenStreetMap. Thử F5 tải lại trang; kiểm tra mạng hoặc extension chặn tile *.openstreetmap.org.'
              );
            }
            return;
          }
          osmCtrlRef.current = ctrl;
          setMapError('');
          requestAnimationFrame(() => {
            requestAnimationFrame(() => osmCtrlRef.current?.map?.invalidateSize());
          });
          return;
        }

        await loadGoogleMapsSdk();
        if (cancelled || !mapElRef.current) return;

        const g = window.google.maps;
        const defaultCenter = { lat: 10.762622, lng: 106.660172 };

        if (!gMapRef.current) {
          gMapRef.current = new g.Map(mapElRef.current, {
            center: defaultCenter,
            zoom: 13,
            mapTypeControl: false,
            fullscreenControl: true,
          });
        }

        clearMapOverlays();

        const bounds = new g.LatLngBounds();
        let hasPoint = false;

        const addMarker = (lat, lng, label, color, title) => {
          const pos = { lat: Number(lat), lng: Number(lng) };
          const marker = new g.Marker({
            position: pos,
            map: gMapRef.current,
            title: title || '',
            label: { text: label, color: 'white', fontWeight: 'bold', fontSize: '11px' },
            icon: {
              path: g.SymbolPath.CIRCLE,
              scale: title === 'me' || title === 'shipper' ? 22 : 18,
              fillColor: color,
              fillOpacity: 1,
              strokeColor: '#fff',
              strokeWeight: 2,
            },
          });
          gMarkersRef.current[label + title] = marker;
          bounds.extend(pos);
          hasPoint = true;
          return pos;
        };

        let pickupPos = null;
        let deliveryPos = null;

        const geocoder = new g.Geocoder();
        const pickupAddr = order.senderAddress || order.pickupAddress;
        const deliveryAddr = order.receiverAddress || order.deliveryAddress;

        const geocodeToPos = (address) =>
          new Promise((resolve) => {
            if (!address?.trim()) {
              resolve(null);
              return;
            }
            geocoder.geocode({ address: address.trim(), componentRestrictions: { country: 'vn' } }, (results, status) => {
              if (!cancelled && status === 'OK' && results[0]) {
                const loc = results[0].geometry.location;
                resolve({ lat: loc.lat(), lng: loc.lng() });
              } else resolve(null);
            });
          });

        const gPick = await geocodeToPos(pickupAddr);
        if (gPick) pickupPos = addMarker(gPick.lat, gPick.lng, 'A', '#22c55e', 'pickup');
        else {
          const pNorm = normalizeOrderLatLng(order.pickupLat, order.pickupLng);
          if (pNorm) pickupPos = addMarker(pNorm.lat, pNorm.lng, 'A', '#22c55e', 'pickup');
        }

        const gDel = await geocodeToPos(deliveryAddr);
        if (gDel) deliveryPos = addMarker(gDel.lat, gDel.lng, 'B', '#ef4444', 'delivery');
        else {
          const dNorm = normalizeOrderLatLng(order.deliveryLat, order.deliveryLng);
          if (dNorm) deliveryPos = addMarker(dNorm.lat, dNorm.lng, 'B', '#ef4444', 'delivery');
        }

        if (isTracking && currentLocation?.lat != null && currentLocation?.lng != null) {
          addMarker(currentLocation.lat, currentLocation.lng, 'S', '#6366f1', 'me');
        } else if (order.shipperLat != null && order.shipperLng != null) {
          const sNorm = normalizeOrderLatLng(order.shipperLat, order.shipperLng);
          if (sNorm) addMarker(sNorm.lat, sNorm.lng, 'S', '#6366f1', 'shipper');
        }

        if (pickupPos && deliveryPos) {
          gRouteRef.current = new g.Polyline({
            path: [pickupPos, deliveryPos],
            geodesic: true,
            strokeColor: '#6366f1',
            strokeOpacity: 0.85,
            strokeWeight: 4,
            map: gMapRef.current,
          });
        }

        if (hasPoint) {
          gMapRef.current.fitBounds(bounds, { top: 56, right: 56, bottom: 56, left: 56 });
        } else {
          gMapRef.current.setCenter(defaultCenter);
          gMapRef.current.setZoom(12);
        }

        // Tránh ô xám: container đôi khi chưa có kích thước cuối khi Map khởi tạo
        requestAnimationFrame(() => {
          requestAnimationFrame(() => {
            if (cancelled || !gMapRef.current) return;
            g.event.trigger(gMapRef.current, 'resize');
            if (hasPoint) {
              gMapRef.current.fitBounds(bounds, { top: 56, right: 56, bottom: 56, left: 56 });
            }
          });
        });
      } catch (e) {
        console.error('[ShipperOrders] map:', e);
        if (hasGoogleMapsApiKey()) {
          setMapError(
            'Không hiển thị được bản đồ Google. Kiểm tra: (1) mạng, (2) VITE_GOOGLE_MAPS_API_KEY, (3) Maps JavaScript API + Billing trên Google Cloud.'
          );
        } else {
          setMapError(
            'Lỗi bản đồ OpenStreetMap. Thử tải lại trang hoặc kiểm tra mạng / chặn tile bản đồ.'
          );
        }
      }
    };

    run();
    return () => {
      cancelled = true;
      osmCtrlRef.current?.destroy();
      osmCtrlRef.current = null;
    };
  }, [
    selectedOrderId,
    orders,
    isTracking,
    currentLocation?.lat,
    currentLocation?.lng,
  ]);

  const fetchOrders = async () => {
    setLoading(true);
    try {
      const res = await shipperApi.getOrders();
      if (res.data.success) {
        setOrders(res.data.orders);

        // Auto-select the first active order if none selected
        if (!selectedOrderId && res.data.orders.length > 0) {
          const activeOrder = res.data.orders.find(o => o.status === 'Shipping' || o.status === 'Pending');
          if (activeOrder) {
            setSelectedOrderId(activeOrder.id);
          }
        }
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  // Theo dõi vị trí của shipper
  const startTracking = () => {
    if (!navigator.geolocation) {
      setMessage('Trình duyệt không hỗ trợ GPS');
      return;
    }

    setIsTracking(true);

    // Check if we have permission
    navigator.permissions?.query({ name: 'geolocation' }).then(result => {
      if (result.state === 'denied') {
        setMessage('Vui lòng bật quyền truy cập vị trí trong trình duyệt');
        setIsTracking(false);
        return;
      }
    });

    watchId.current = navigator.geolocation.watchPosition(
      async (position) => {
        const { latitude, longitude, accuracy } = position.coords;
        const now = Date.now();

        // Only update if moved significantly (>5m) or 30 seconds passed
        if (now - lastUpdateTime.current < 30000) {
          setCurrentLocation({ lat: latitude, lng: longitude, accuracy });
          return;
        }

        lastUpdateTime.current = now;
        setCurrentLocation({ lat: latitude, lng: longitude, accuracy });

        // Gửi vị trí lên server cho đơn đang được chọn
        if (selectedOrderId) {
          try {
            const address = await getAddressFromCoords(latitude, longitude);

            await shipperApi.updateLocation(selectedOrderId, {
              latitude,
              longitude,
              address: address
            });
          } catch (e) {
            console.error('Lỗi cập nhật vị trí:', e);
          }
        }
      },
      (error) => {
        console.error('GPS Error:', error);
        switch (error.code) {
          case error.PERMISSION_DENIED:
            setMessage('Bạn đã từ chối quyền truy cập vị trí');
            break;
          case error.POSITION_UNAVAILABLE:
            setMessage('Không thể xác định vị trí');
            break;
          case error.TIMEOUT:
            setMessage('Yêu cầu vị trí đã hết thời gian');
            break;
          default:
            setMessage('Lỗi GPS không xác định');
        }
        setIsTracking(false);
      },
      {
        enableHighAccuracy: true,
        timeout: 15000,
        maximumAge: 10000
      }
    );
  };

  const stopTracking = () => {
    if (watchId.current !== null) {
      navigator.geolocation.clearWatch(watchId.current);
      watchId.current = null;
    }
    setIsTracking(false);
    setMessage('Đã tắt định vị GPS');
    setTimeout(() => setMessage(''), 3000);
  };

  // Simple reverse geocoding (in production, use Google Maps Geocoding API)
  const getAddressFromCoords = async (lat, lng) => {
    // For now, return coordinates as address
    // In production, call Google Maps Geocoding API
    return `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
  };

  const handleUpdateStatus = async (id, newStatus) => {
    try {
      // If starting to ship, auto-enable GPS
      if (newStatus === 'Shipping' && !isTracking) {
        setSelectedOrderId(id);
        setShowLocationModal(true);
        return;
      }

      // If marking as delivered
      if (newStatus === 'Delivered') {
        const confirmed = window.confirm('Xác nhận đã giao hàng thành công?\n\nHệ thống sẽ tự động tạo thanh toán cho bạn.');
        if (!confirmed) return;
      }

      await shipperApi.updateStatus(id, { status: newStatus });
      setMessage(`Đã cập nhật trạng thái: ${newStatus}`);
      fetchOrders();

      // If delivered, stop tracking
      if (newStatus === 'Delivered') {
        stopTracking();
      }
    } catch (err) {
      alert('Lỗi cập nhật trạng thái: ' + (err.response?.data?.message || err.message));
    }
  };

  const handleConfirmStartShipping = () => {
    setShowLocationModal(false);
    setIsTracking(true);
    startTracking();
  };

  const handleStartShippingWithoutGPS = () => {
    setShowLocationModal(false);
    handleUpdateStatus(selectedOrderId, 'Shipping');
  };

  const statusLabel = {
    Pending: 'Chờ lấy hàng',
    Shipping: 'Đang giao',
    Delivered: 'Đã giao',
    Cancelled: 'Đã hủy'
  };

  const statusColor = {
    Pending: '#f59e0b',
    Shipping: '#3b82f6',
    Delivered: '#10b981',
    Cancelled: '#ef4444'
  };

  // Get current order for tracking
  const currentOrder = orders.find(o => o.id === selectedOrderId);

  return (
    <div className="animate-fade">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '3rem' }}>
        <div>
          <h1 style={{ fontSize: '2rem', fontWeight: 900 }}>🚚 Chuyến hàng của tôi</h1>
          <p style={{ color: 'var(--text-muted)', fontWeight: 600 }}>
            {orders.filter(o => o.status === 'Shipping').length > 0
              ? `Có ${orders.filter(o => o.status === 'Shipping').length} đơn đang giao`
              : 'Cùng Giao Hàng Sonic mang niềm vui đến mọi nhà!'}
          </p>
        </div>
        <div style={{ display: 'flex', gap: '0.75rem' }}>
          {/* GPS Tracking Toggle */}
          <button
            className={`btn ${isTracking ? '' : 'btn-primary'}`}
            onClick={() => {
              if (isTracking) {
                stopTracking();
              } else {
                if (!selectedOrderId) {
                  setMessage('Vui lòng chọn đơn hàng để theo dõi');
                  return;
                }
                setShowLocationModal(true);
              }
            }}
            style={{
              background: isTracking ? '#ef4444' : undefined,
              color: isTracking ? 'white' : undefined,
              minWidth: '160px'
            }}
          >
            {isTracking ? '📍 TẮT GPS' : '📍 BẬT GPS'}
          </button>
          <button className="btn" onClick={fetchOrders} style={{ background: '#f1f5f9' }}>
            Làm mới 🔄
          </button>
        </div>
      </div>

      {/* GPS Status Banner */}
      {isTracking && currentLocation && (
        <div style={{
          background: 'linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%)',
          border: '2px solid #10b981',
          color: '#065f46',
          padding: '1rem 1.5rem',
          borderRadius: '16px',
          marginBottom: '2rem',
          display: 'flex',
          alignItems: 'center',
          gap: '1rem',
          fontSize: '0.9rem',
          fontWeight: 600
        }}>
          <span style={{
            width: '12px',
            height: '12px',
            borderRadius: '50%',
            background: '#10b981',
            animation: 'pulse 1s infinite',
            flexShrink: 0
          }}></span>
          <div style={{ flex: 1 }}>
            <div style={{ fontWeight: 800 }}>GPS đang hoạt động</div>
            {currentOrder && <div style={{ fontSize: '0.85rem', opacity: 0.8 }}>Theo dõi: #{currentOrder.trackingNumber}</div>}
          </div>
          <div style={{ textAlign: 'right', fontSize: '0.8rem', opacity: 0.8 }}>
            <div>{currentLocation.lat.toFixed(6)}, {currentLocation.lng.toFixed(6)}</div>
            {currentLocation.accuracy && <div>Độ chính xác: ±{Math.round(currentLocation.accuracy)}m</div>}
          </div>
        </div>
      )}

      {/* Messages */}
      {message && (
        <div style={{
          background: message.includes('Lỗi') || message.includes('từ chối') || message.includes('không') ? '#fef2f2' : '#ecfdf5',
          color: message.includes('Lỗi') || message.includes('từ chối') || message.includes('không') ? '#dc2626' : '#10b981',
          padding: '1rem 1.5rem',
          borderRadius: '12px',
          marginBottom: '2rem',
          fontWeight: 600
        }}>
          {message}
        </div>
      )}

      {/* Bản đồ chuyến đang chọn */}
      {!loading && orders.length > 0 && currentOrder && (
        <div
          className="card"
          style={{ marginBottom: '2rem', padding: 0, overflow: 'hidden', border: '2px solid #c7d2fe' }}
        >
          <div
            style={{
              padding: '1rem 1.25rem',
              borderBottom: '1px solid #e2e8f0',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              flexWrap: 'wrap',
              gap: '0.75rem',
            }}
          >
            <div>
              <h2 style={{ fontSize: '1.1rem', fontWeight: 800, margin: 0 }}>📍 Bản đồ chuyến hàng</h2>
              <p style={{ margin: '0.25rem 0 0', fontSize: '0.85rem', color: '#64748b' }}>
                Đơn #{currentOrder.trackingNumber} — bấm thẻ đơn bên dưới để đổi chuyến
              </p>
            </div>
            <button
              type="button"
              className="btn"
              style={{ background: '#eef2ff', color: '#6366f1' }}
              onClick={() => navigate(`/orders/${currentOrder.id}/live-map`)}
            >
              Theo dõi live →
            </button>
          </div>
          <div
            ref={mapElRef}
            className="sonic-osm-map-host"
            style={{ width: '100%', height: 400, minHeight: 280, background: '#e2e8f0', position: 'relative' }}
          />
          {mapInfo && (
            <div
              style={{
                padding: '0.75rem 1.25rem',
                background: '#eff6ff',
                color: '#1d4ed8',
                fontSize: '0.8rem',
                fontWeight: 600,
                borderTop: '1px solid #bfdbfe',
              }}
            >
              {mapInfo}
            </div>
          )}
          {mapError && (
            <div
              style={{
                padding: '1rem 1.25rem',
                background: '#fef2f2',
                color: '#b91c1c',
                fontSize: '0.875rem',
                fontWeight: 600,
                borderTop: '1px solid #fecaca',
              }}
            >
              {mapError}
            </div>
          )}
        </div>
      )}

      {/* Location Modal */}
      {showLocationModal && (
        <div style={{
          position: 'fixed',
          inset: 0,
          background: 'rgba(0,0,0,0.5)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1000,
          padding: '1rem'
        }}>
          <div className="card animate-fade" style={{ maxWidth: '500px', width: '100%', padding: '2rem' }}>
            <h3 style={{ fontSize: '1.25rem', fontWeight: 800, marginBottom: '1rem', color: 'var(--primary)' }}>
              🚚 Bắt đầu giao hàng
            </h3>
            <p style={{ color: '#64748b', marginBottom: '1.5rem' }}>
              Bạn có muốn bật định vị GPS để cập nhật vị trí realtime cho khách hàng không?
            </p>

            <div style={{ background: '#f8fafc', padding: '1rem', borderRadius: '12px', marginBottom: '1.5rem' }}>
              <strong>Ưu điểm khi bật GPS:</strong>
              <ul style={{ margin: '0.5rem 0 0 1.5rem', color: '#64748b', fontSize: '0.9rem' }}>
                <li>Khách hàng theo dõi được vị trí real-time</li>
                <li>Tính năng hiện đại, chuyên nghiệp</li>
                <li>Tăng độ tin cậy với khách hàng</li>
              </ul>
            </div>

            <div style={{ display: 'flex', gap: '1rem' }}>
              <button
                className="btn btn-primary"
                onClick={handleConfirmStartShipping}
                style={{ flex: 1 }}
              >
                ✅ BẬT GPS & BẮT ĐẦU GIAO
              </button>
              <button
                className="btn"
                onClick={handleStartShippingWithoutGPS}
                style={{ flex: 1, background: '#f1f5f9' }}
              >
                ⏭️ BẮT ĐẦU KHÔNG CẦN GPS
              </button>
            </div>
            <button
              className="btn"
              onClick={() => setShowLocationModal(false)}
              style={{ marginTop: '1rem', width: '100%', background: '#f1f5f9' }}
            >
              Hủy
            </button>
          </div>
        </div>
      )}

      {/* Orders Grid */}
      {loading ? (
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <div className="spinner" style={{ margin: '0 auto' }}></div>
          <p style={{ marginTop: '1rem', color: 'var(--text-muted)' }}>Đang tải danh sách hàng...</p>
        </div>
      ) : orders.length === 0 ? (
        <div className="card" style={{ textAlign: 'center', padding: '4rem', color: '#64748b' }}>
          <div style={{ fontSize: '4rem', marginBottom: '1rem' }}>📦</div>
          <h3 style={{ fontWeight: 800, marginBottom: '0.5rem' }}>Không có chuyến hàng nào</h3>
          <p>Bên kho chưa gán chuyến nào cho bạn hết. Nghỉ ngơi xíu đi! ☕</p>
        </div>
      ) : (
        <div style={{ display: 'grid', gap: '1.5rem' }}>
          {orders.map(o => (
            <div
              key={o.id}
              className="card"
              style={{
                padding: '1.5rem',
                border: selectedOrderId === o.id ? '3px solid #6366f1' : '2px solid #e2e8f0',
                cursor: 'pointer',
                transition: 'all 0.2s'
              }}
              onClick={() => setSelectedOrderId(o.id)}
            >
              {/* Header */}
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1.5rem' }}>
                <div>
                  <span style={{ color: 'var(--primary)', fontWeight: 900, fontSize: '1.1rem' }}>#{o.trackingNumber}</span>
                  {selectedOrderId === o.id && (
                    <span style={{
                      marginLeft: '0.75rem',
                      background: '#eef2ff',
                      color: '#6366f1',
                      padding: '0.2rem 0.6rem',
                      borderRadius: '999px',
                      fontSize: '0.7rem',
                      fontWeight: 700
                    }}>
                      ĐANG CHỌN
                    </span>
                  )}
                </div>
                <span style={{
                  background: `${statusColor[o.status]}20`,
                  color: statusColor[o.status],
                  padding: '0.4rem 1rem',
                  borderRadius: '999px',
                  fontSize: '0.8rem',
                  fontWeight: 800
                }}>
                  {statusLabel[o.status] || o.status}
                </span>
              </div>

              {/* Product Info */}
              <div style={{ marginBottom: '1.5rem' }}>
                <div style={{ fontSize: '0.75rem', color: '#94a3b8', fontWeight: 800, marginBottom: '0.25rem' }}>HÀNG HÓA</div>
                <div style={{ fontSize: '1.1rem', fontWeight: 900 }}>{o.product}</div>
              </div>

              {/* Addresses */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr auto 1fr', gap: '1rem', alignItems: 'center', marginBottom: '1.5rem' }}>
                <div>
                  <div style={{ fontSize: '0.7rem', color: '#22c55e', fontWeight: 800, marginBottom: '0.25rem' }}>📦 LẤY TẠI</div>
                  <div style={{ fontSize: '0.85rem', fontWeight: 600, lineHeight: 1.4 }}>
                    {o.senderAddress || o.pickupAddress || '—'}
                  </div>
                </div>
                <div style={{ color: '#94a3b8', fontSize: '1.5rem' }}>→</div>
                <div>
                  <div style={{ fontSize: '0.7rem', color: '#ef4444', fontWeight: 800, marginBottom: '0.25rem' }}>🏠 GIAO TẠI</div>
                  <div style={{ fontSize: '0.85rem', fontWeight: 600, lineHeight: 1.4 }}>
                    {o.receiverAddress || o.deliveryAddress || '—'}
                  </div>
                </div>
              </div>

              {/* Contact Info */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1.5rem', fontSize: '0.85rem' }}>
                <div>
                  <div style={{ color: '#64748b', fontSize: '0.7rem', fontWeight: 700 }}>NGƯỜI GỬI</div>
                  <div style={{ fontWeight: 600 }}>{o.senderName}</div>
                  <div style={{ color: '#6366f1', cursor: 'pointer' }} onClick={(e) => { e.stopPropagation(); window.location.href = `tel:${o.senderPhone}`; }}>
                    📞 {o.senderPhone}
                  </div>
                </div>
                <div>
                  <div style={{ color: '#64748b', fontSize: '0.7rem', fontWeight: 700 }}>NGƯỜI NHẬN</div>
                  <div style={{ fontWeight: 600 }}>{o.receiverName}</div>
                  <div style={{ color: '#6366f1', cursor: 'pointer' }} onClick={(e) => { e.stopPropagation(); window.location.href = `tel:${o.receiverPhone}`; }}>
                    📞 {o.receiverPhone}
                  </div>
                </div>
              </div>

              {/* Fee Info */}
              <div style={{
                background: '#f8fafc',
                padding: '0.75rem 1rem',
                borderRadius: '8px',
                display: 'flex',
                justifyContent: 'space-between',
                marginBottom: '1.5rem',
                fontSize: '0.85rem'
              }}>
                <div>
                  <span style={{ color: '#64748b' }}>Phí vận chuyển:</span>
                  <strong style={{ marginLeft: '0.5rem' }}>{o.shippingFee?.toLocaleString()} VND</strong>
                </div>
                <div>
                  <span style={{ color: '#64748b' }}>Tổng:</span>
                  <strong style={{ marginLeft: '0.5rem', color: '#10b981' }}>{o.totalAmount?.toLocaleString()} VND</strong>
                </div>
              </div>

              {/* Action Buttons */}
              <div style={{ display: 'flex', gap: '0.75rem' }} onClick={(e) => e.stopPropagation()}>
                {/* Nút LẤY HÀNG - khi đơn đang chờ */}
                {o.status === 'Pending' && (
                  <button
                    className="btn btn-primary"
                    style={{ flex: 1 }}
                    onClick={() => {
                      setSelectedOrderId(o.id);
                      handleUpdateStatus(o.id, 'Shipping');
                    }}
                  >
                    📦 LẤY HÀNG
                  </button>
                )}

                {/* Nút HOÀN TẤT - khi đơn đang giao */}
                {o.status === 'Shipping' && (
                  <>
                    <button
                      className="btn btn-primary"
                      style={{ flex: 2 }}
                      onClick={() => handleUpdateStatus(o.id, 'Delivered')}
                    >
                      ✅ HOÀN TẤT GIAO HÀNG
                    </button>
                    <button
                      className="btn"
                      style={{ flex: 1, background: '#f0fdf4', color: '#10b981' }}
                      onClick={() => navigate(`/orders/${o.id}/live-map`)}
                    >
                      📍 BẢN ĐỒ
                    </button>
                  </>
                )}

                {o.status === 'Pending' && (
                  <button
                    className="btn"
                    style={{ flex: 1, background: '#eef2ff', color: '#6366f1' }}
                    onClick={() => {
                      setSelectedOrderId(o.id);
                      navigate(`/orders/${o.id}/live-map`);
                    }}
                  >
                    📍 BẢN ĐỒ
                  </button>
                )}

                {/* Nút ĐÃ GIAO - disable */}
                {o.status === 'Delivered' && (
                  <button
                    className="btn"
                    style={{ flex: 1, background: '#ecfdf5', color: '#10b981', cursor: 'default' }}
                    disabled
                  >
                    ✅ ĐÃ GIAO
                  </button>
                )}

                {/* Nút CHI TIẾT */}
                <button
                  className="btn"
                  style={{ background: '#f8fafc', border: '1px solid #e2e8f0', color: '#64748b' }}
                  onClick={() => navigate(`/orders/${o.id}`)}
                >
                  🔍 Chi tiết
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* CSS Animation */}
      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; transform: scale(1); }
          50% { opacity: 0.5; transform: scale(0.95); }
        }
      `}</style>
    </div>
  );
}
