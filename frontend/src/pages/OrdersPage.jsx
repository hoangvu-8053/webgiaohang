import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { ordersApi } from '../api/endpoints';
import { useAuth } from '../context/AuthContext';

export default function OrdersPage() {
  const { user } = useAuth();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [status, setStatus] = useState('');
  const isShipper = user?.role === 'Shipper';

  useEffect(() => {
    fetchOrders();
  }, [searchTerm, status]);

  const fetchOrders = async () => {
    setLoading(true);
    try {
      const res = await ordersApi.getAll({ searchTerm, status });
      setOrders(res.data.orders);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const statusMap = {
    Pending: { label: 'Chờ xử lý', color: '#64748b', bg: '#f1f5f9' },
    Shipping: { label: 'Đang giao', color: '#3b82f6', bg: '#eff6ff' },
    Delivered: { label: 'Hoàn tất', color: '#10b981', bg: '#ecfdf5' },
    Cancelled: { label: 'Đã hủy', color: '#ef4444', bg: '#fef2f2' },
  };

  return (
    <div className="animate-fade">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 800 }}>{isShipper ? '📦 Đơn được gán cho bạn' : '📦 Quản lý đơn hàng'}</h1>
        {!isShipper && (
          <Link to="/orders/create" className="btn btn-primary">➕ Tạo đơn mới</Link>
        )}
      </div>

      <div className="card" style={{ marginBottom: '2rem', padding: '1rem' }}>
        <div className="grid" style={{ gridTemplateColumns: '1fr 240px auto', gap: '1rem', alignItems: 'flex-end' }}>
          <div className="form-group" style={{ marginBottom: 0 }}>
            <label className="form-label">Tìm kiếm</label>
            <input className="input" placeholder="Tên khách hàng, mã đơn, sản phẩm..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} />
          </div>
          <div className="form-group" style={{ marginBottom: 0 }}>
            <label className="form-label">Trạng thái</label>
            <select className="input" value={status} onChange={(e) => setStatus(e.target.value)}>
              <option value="">Tất cả trạng thái</option>
              <option value="Pending">Chờ xử lý</option>
              <option value="Shipping">Đang giao</option>
              <option value="Delivered">Đã giao</option>
              <option value="Cancelled">Đã hủy</option>
            </select>
          </div>
          <button className="btn" onClick={fetchOrders} style={{ background: '#f1f5f9', height: '42px' }}>Làm mới</button>
        </div>
      </div>

      {loading ? (
        <div style={{ textAlign: 'center', padding: '3rem' }}>Đang tải danh sách...</div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
            <thead style={{ background: '#f8fafc', borderBottom: '1px solid #e2e8f0' }}>
              <tr>
                <th style={{ padding: '1rem' }}>Mã đơn</th>
                <th style={{ padding: '1rem' }}>Sản phẩm</th>
                <th style={{ padding: '1rem' }}>Người nhận</th>
                <th style={{ padding: '1rem' }}>Tổng tiền</th>
                <th style={{ padding: '1rem' }}>Trạng thái</th>
                <th style={{ padding: '1rem' }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {orders.length === 0 ? (
                <tr><td colSpan="6" style={{ padding: '2rem', textAlign: 'center', color: '#64748b' }}>Không tìm thấy đơn hàng nào</td></tr>
              ) : (
                orders.map((o) => (
                  <tr key={o.id} style={{ borderBottom: '1px solid #f1f5f9' }}>
                    <td style={{ padding: '1rem', fontWeight: 600 }}>#{o.trackingNumber}</td>
                    <td style={{ padding: '1rem' }}>{o.product}</td>
                    <td style={{ padding: '1rem' }}>
                      <div style={{ fontWeight: 500 }}>{o.receiverName}</div>
                      <div style={{ fontSize: '0.75rem', color: '#64748b' }}>{o.receiverPhone}</div>
                    </td>
                    <td style={{ padding: '1rem', color: '#6366f1', fontWeight: 700 }}>{o.totalAmount?.toLocaleString()}đ</td>
                    <td style={{ padding: '1rem' }}>
                      <span className="badge" style={{ background: statusMap[o.status]?.bg, color: statusMap[o.status]?.color }}>{statusMap[o.status]?.label || o.status}</span>
                    </td>
                    <td style={{ padding: '1rem' }}>
                      <Link to={`/orders/${o.id}`} style={{ color: '#6366f1', fontSize: '0.875rem', fontWeight: 600 }}>Chi tiết →</Link>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
