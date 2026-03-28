import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ordersApi, adminApi } from '../api/endpoints';
import { useAuth } from '../context/AuthContext';

export default function OrderDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [shippers, setShippers] = useState([]);
  const [selectedShipper, setSelectedShipper] = useState('');
  const [assigning, setAssigning] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    fetchOrder();
    if (user?.role === 'Admin' || user?.role === 'Staff') {
      fetchShippers();
    }
  }, [id, user]);

  const fetchOrder = async () => {
    try {
      const res = await ordersApi.getById(id);
      if (res.data.success) {
        setOrder(res.data.order);
        if (res.data.order.shipperName) {
          setSelectedShipper(res.data.order.shipperName);
        }
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const fetchShippers = async () => {
    try {
      const res = await adminApi.getShippers();
      if (res.data.success) setShippers(res.data.shippers);
    } catch (err) {
      console.error(err);
    }
  };

  const handleAssignShipper = async () => {
    if (!selectedShipper) {
      alert('Vui lòng chọn shipper');
      return;
    }
    setAssigning(true);
    try {
      await ordersApi.assignShipper(order.id, selectedShipper);
      alert('Đã gán shipper thành công!');
      fetchOrder();
    } catch (err) {
      alert('Lỗi khi gán shipper.');
    } finally {
      setAssigning(false);
    }
  };

  if (loading) return <p>Đang tải chi tiết đơn hàng...</p>;
  if (!order) return <p>Không tìm thấy đơn hàng.</p>;

  const statusColor = { Pending: '#f59e0b', Shipping: '#3b82f6', Delivered: '#10b981', Cancelled: '#ef4444' };
  const isAdmin = user?.role === 'Admin' || user?.role === 'Staff';

  return (
    <div className="animate-fade" style={{ maxWidth: '1000px', margin: '0 auto' }}>
      <button className="btn" onClick={() => navigate(-1)} style={{ marginBottom: '2rem', background: '#f8fafc' }}>← QUAY LẠI</button>
      
      <div className="card" style={{ padding: '3rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '3rem', borderBottom: '1px solid #f1f5f9', paddingBottom: '2rem' }}>
          <div>
            <h1 style={{ fontSize: '2rem', fontWeight: 900, color: 'var(--primary)' }}>ĐƠN HÀNG #{order.trackingNumber}</h1>
            <p style={{ color: '#94a3b8' }}>Ngày tạo: {new Date(order.orderDate).toLocaleString()}</p>
          </div>
          <div style={{ background: `${statusColor[order.status]}20`, color: statusColor[order.status], padding: '1rem 2rem', borderRadius: '999px', fontWeight: 900 }}>
            {order.status}
          </div>
        </div>

        <div className="grid grid-2">
          <div className="card" style={{ background: '#f8fbff', border: 'none', padding: '2rem' }}>
            <h3 style={{ marginBottom: '1.5rem', borderBottom: '2px solid #6366f1', display: 'inline-block', paddingBottom: '0.25rem' }}>BÊN GỬI</h3>
            <div style={{ marginBottom: '1rem' }}>
              <strong>Họ tên:</strong> {order.senderName}
            </div>
            <div style={{ marginBottom: '1rem' }}>
              <strong>SĐT:</strong> {order.senderPhone}
            </div>
            <div style={{ marginBottom: '1rem' }}>
              <strong>Địa chỉ lấy:</strong> {order.pickupAddress}
            </div>
          </div>

          <div className="card" style={{ background: '#fef2f2', border: 'none', padding: '2rem' }}>
            <h3 style={{ marginBottom: '1.5rem', borderBottom: '2px solid #ef4444', display: 'inline-block', paddingBottom: '0.25rem' }}>BÊN NHẬN</h3>
            <div style={{ marginBottom: '1rem' }}>
              <strong>Họ tên:</strong> {order.receiverName}
            </div>
            <div style={{ marginBottom: '1rem' }}>
              <strong>SĐT:</strong> {order.receiverPhone}
            </div>
            <div style={{ marginBottom: '1rem' }}>
              <strong>Địa chỉ giao:</strong> {order.deliveryAddress}
            </div>
          </div>
        </div>

        <div style={{ marginTop: '3rem' }}>
          <h3 style={{ marginBottom: '1.5rem', borderBottom: '2px solid #f59e0b', display: 'inline-block', paddingBottom: '0.25rem' }}>THÔNG TIN HÀNG HÓA</h3>
          <div className="card" style={{ padding: '2rem', display: 'flex', gap: '2rem', alignItems: 'center' }}>
            {order.productImagePath && (
              <img src={`${import.meta.env.VITE_API_URL || 'http://localhost:5170'}${order.productImagePath}`} alt="" style={{ width: '150px', height: '150px', objectFit: 'cover', borderRadius: '16px' }} />
            )}
            <div>
              <div style={{ fontSize: '1.5rem', fontWeight: 900 }}>{order.product}</div>
              <p style={{ color: 'var(--text-muted)', margin: '0.5rem 0' }}>{order.productDescription || 'Không có mô tả.'}</p>
              <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#10b981', marginTop: '1rem' }}>
                Tổng tiền: {order.totalAmount?.toLocaleString()} VND
              </div>
            </div>
          </div>
        </div>

        {isAdmin && (
          <div style={{ marginTop: '3rem', padding: '2rem', background: '#f0fdf4', borderRadius: '12px', border: '2px solid #86efac' }}>
            <h3 style={{ marginBottom: '1.5rem', color: '#166534' }}>📦 GIAO ĐƠN CHO SHIPPER</h3>
            <div style={{ display: 'flex', gap: '1rem', alignItems: 'flex-end' }}>
              <div style={{ flex: 1 }}>
                <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 600, fontSize: '0.875rem' }}>Chọn Shipper:</label>
                <select 
                  className="input" 
                  value={selectedShipper} 
                  onChange={(e) => setSelectedShipper(e.target.value)}
                  style={{ width: '100%' }}
                >
                  <option value="">-- Chọn shipper --</option>
                  {shippers.map(s => (
                    <option key={s.id} value={s.username}>
                      {s.fullName || s.username} {s.phone ? `- ${s.phone}` : ''}
                    </option>
                  ))}
                </select>
              </div>
              <button 
                className="btn btn-primary" 
                onClick={handleAssignShipper}
                disabled={assigning || !selectedShipper}
                style={{ padding: '0.75rem 2rem' }}
              >
                {assigning ? 'Đang gán...' : 'GÁN SHIPPER'}
              </button>
            </div>
            {order.shipperName && (
              <p style={{ marginTop: '1rem', color: '#166534', fontWeight: 600 }}>
                ✓ Đơn đang được giao bởi: <strong>{order.shipperName}</strong>
              </p>
            )}
          </div>
        )}

        {/* Action Buttons */}
        <div style={{ marginTop: '3rem', display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
          {/* Nút thanh toán - cho Sender/Receiver */}
          {(user?.role === 'Sender' || user?.role === 'Receiver') && (
            <button 
              className="btn btn-primary"
              onClick={() => navigate(`/orders/${order.id}/payment`)}
              style={{ flex: 1, minWidth: '200px' }}
            >
              💳 Thanh toán
            </button>
          )}
          
          {/* Nút theo dõi vị trí - khi có shipper */}
          {order.shipperName && (order.status === 'Shipping' || order.status === 'Pending') && (
            <button 
              className="btn"
              onClick={() => navigate(`/orders/${order.id}/live-map`)}
              style={{ flex: 1, minWidth: '200px', background: '#eef2ff', color: '#6366f1' }}
            >
              📍 Theo dõi vị trí
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
