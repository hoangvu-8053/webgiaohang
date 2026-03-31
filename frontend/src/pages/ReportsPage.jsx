import { useState, useEffect } from 'react';
import { reportApi } from '../api/endpoints';

export default function ReportsPage() {
  const [stats, setStats] = useState({
    totalOrders: 0,
    totalRevenue: 0,
    totalShippingFee: 0,
    totalShipperCommission: 0,
    totalProductValue: 0,
    platformPercent: 0,
    pendingCount: 0,
    shippingCount: 0,
    deliveredCount: 0,
    cancelledCount: 0
  });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchStats();
  }, []);

  const fetchStats = async () => {
    try {
      const res = await reportApi.getSummary();
      if (res.data.success) setStats(res.data.stats || stats);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="animate-fade">
      <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '2rem' }}>📊 Báo cáo doanh thu nền tảng</h1>

      {/* Tổng quan 4 ô */}
      <div className="grid grid-3" style={{ marginBottom: '1.5rem', gap: '1rem' }}>
        <div className="card" style={{ background: 'linear-gradient(135deg, #6366f1 0%, #4f46e5 100%)', color: 'white' }}>
          <h3 style={{ fontSize: '0.8rem', textTransform: 'uppercase', opacity: 0.8 }}>Tổng đơn hàng</h3>
          <p style={{ fontSize: '2rem', fontWeight: 800, marginTop: '0.5rem' }}>{stats.totalOrders || 0}</p>
        </div>
        <div className="card" style={{ background: 'linear-gradient(135deg, #10b981 0%, #059669 100%)', color: 'white' }}>
          <h3 style={{ fontSize: '0.8rem', textTransform: 'uppercase', opacity: 0.8 }}>Doanh thu nền tảng (VND)</h3>
          <p style={{ fontSize: '2rem', fontWeight: 800, marginTop: '0.5rem' }}>{(stats.totalRevenue || 0).toLocaleString()}</p>
          <p style={{ fontSize: '0.75rem', opacity: 0.8, marginTop: '0.25rem' }}>
            Phần còn lại sau khi trả shipper ({(stats.platformPercent * 100 || 0).toFixed(0)}%)
          </p>
        </div>
        <div className="card" style={{ background: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)', color: 'white' }}>
          <h3 style={{ fontSize: '0.8rem', textTransform: 'uppercase', opacity: 0.8 }}>Tổng phí ship (VND)</h3>
          <p style={{ fontSize: '2rem', fontWeight: 800, marginTop: '0.5rem' }}>{(stats.totalShippingFee || 0).toLocaleString()}</p>
        </div>
      </div>

      {/* Chi tiết phân bổ 3 ô */}
      <div className="grid grid-3" style={{ marginBottom: '2rem', gap: '1rem' }}>
        <div className="card" style={{ borderLeft: '4px solid #3b82f6' }}>
          <h3 style={{ fontSize: '0.8rem', textTransform: 'uppercase', color: '#64748b', marginBottom: '0.5rem' }}>Phí trả cho shipper</h3>
          <p style={{ fontSize: '1.75rem', fontWeight: 800, color: '#3b82f6' }}>
            {(stats.totalShipperCommission || 0).toLocaleString()} VND
          </p>
          <p style={{ fontSize: '0.75rem', color: '#94a3b8', marginTop: '0.25rem' }}>
            {(100 - (stats.platformPercent * 100 || 0)).toFixed(0)}% của phí ship
          </p>
        </div>
        <div className="card" style={{ borderLeft: '4px solid #10b981' }}>
          <h3 style={{ fontSize: '0.8rem', textTransform: 'uppercase', color: '#64748b', marginBottom: '0.5rem' }}>Doanh thu thuần cho web</h3>
          <p style={{ fontSize: '1.75rem', fontWeight: 800, color: '#10b981' }}>
            {(stats.totalRevenue || 0).toLocaleString()} VND
          </p>
          <p style={{ fontSize: '0.75rem', color: '#94a3b8', marginTop: '0.25rem' }}>
            {(stats.platformPercent * 100 || 0).toFixed(0)}% của phí ship
          </p>
        </div>
        <div className="card" style={{ borderLeft: '4px solid #94a3b8' }}>
          <h3 style={{ fontSize: '0.8rem', textTransform: 'uppercase', color: '#64748b', marginBottom: '0.5rem' }}>Giá trị hàng hóa</h3>
          <p style={{ fontSize: '1.75rem', fontWeight: 800, color: '#64748b' }}>
            {(stats.totalProductValue || 0).toLocaleString()} VND
          </p>
          <p style={{ fontSize: '0.75rem', color: '#94a3b8', marginTop: '0.25rem' }}>
            Tiền hàng của người gửi (không tính doanh thu)
          </p>
        </div>
      </div>

      {/* Cơ cấu đơn hàng */}
      <div className="card">
        <h3 style={{ marginBottom: '1.5rem' }}>CƠ CẤU ĐƠN HÀNG</h3>
        {loading ? <p>Đang tính toán...</p> : (
          <div style={{ display: 'flex', gap: '2rem', flexWrap: 'wrap' }}>
            <div style={{ flex: 1, minWidth: '200px' }}>
              <ul style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                <li style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span>Hoàn tất:</span>
                  <span style={{ fontWeight: 700, color: '#10b981' }}>{stats.deliveredCount || 0}</span>
                </li>
                <li style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span>Đang giao:</span>
                  <span style={{ fontWeight: 700, color: '#3b82f6' }}>{stats.shippingCount || 0}</span>
                </li>
                <li style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span>Đang chờ:</span>
                  <span style={{ fontWeight: 700, color: '#f59e0b' }}>{stats.pendingCount || 0}</span>
                </li>
                <li style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span>Đã hủy:</span>
                  <span style={{ fontWeight: 700, color: '#ef4444' }}>{stats.cancelledCount || 0}</span>
                </li>
              </ul>
            </div>
            <div style={{ flex: 2, background: '#f8fafc', borderRadius: '8px', padding: '1rem', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <p style={{ color: '#64748b', fontStyle: 'italic' }}>Biểu đồ phân tích doanh thu theo thời gian đang được nâng cấp.</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
