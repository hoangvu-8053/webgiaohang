import { useState, useEffect } from 'react';
import { reportApi } from '../api/endpoints';

export default function ReportsPage() {
  const [stats, setStats] = useState({ totalOrders: 0, totalRevenue: 0, statusCounts: [] });
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
      <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '2rem' }}>📊 Báo cáo doanh thu</h1>
      
      <div className="grid grid-3" style={{ marginBottom: '2rem' }}>
        <div className="card" style={{ background: 'linear-gradient(135deg, #6366f1 0%, #4f46e5 100%)', color: 'white' }}>
          <h3 style={{ fontSize: '0.875rem', textTransform: 'uppercase', opacity: 0.8 }}>Tổng đơn hàng</h3>
          <p style={{ fontSize: '2.5rem', fontWeight: 800, marginTop: '0.5rem' }}>{stats.totalOrders || 0}</p>
        </div>
        <div className="card" style={{ background: 'linear-gradient(135deg, #10b981 0%, #059669 100%)', color: 'white' }}>
          <h3 style={{ fontSize: '0.875rem', textTransform: 'uppercase', opacity: 0.8 }}>Doanh thu (VND)</h3>
          <p style={{ fontSize: '2.5rem', fontWeight: 800, marginTop: '0.5rem' }}>{(stats.totalRevenue || 0).toLocaleString()}</p>
        </div>
        <div className="card">
          <h3 style={{ fontSize: '0.875rem', textTransform: 'uppercase', color: '#64748b' }}>Đang chờ xử lý</h3>
          <p style={{ fontSize: '2.5rem', fontWeight: 800, color: '#f59e0b', marginTop: '0.5rem' }}>{stats.pendingCount || 0}</p>
        </div>
      </div>

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
