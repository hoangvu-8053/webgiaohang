import { useState, useEffect } from 'react';
import { notificationApi } from '../api/endpoints';

export default function NotificationsPage() {
  const [notifications, setNotifications] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchNotifications();
  }, []);

  const fetchNotifications = async () => {
    setLoading(true);
    try {
      const res = await notificationApi.getAll();
      if (res.data.success) setNotifications(res.data.notifications);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleMarkRead = async (id) => {
    try {
      await notificationApi.markRead(id);
      setNotifications(notifications.map(n => n.id === id ? { ...n, isRead: true } : n));
    } catch (err) {
      console.error(err);
    }
  };

  const handleReadAll = async () => {
    try {
      await notificationApi.markAllRead();
      setNotifications(notifications.map(n => ({ ...n, isRead: true })));
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <div className="animate-fade">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '2rem', fontWeight: 900 }}>🔔 Thông báo của bạn</h1>
        <button className="btn" onClick={handleReadAll} style={{ background: '#f1f5f9', fontSize: '0.85rem' }}>Đã đọc tất cả</button>
      </div>

      {loading ? <p>Đang tải thông báo...</p> : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {notifications.length === 0 ? (
            <div className="card" style={{ textAlign: 'center', color: '#64748b' }}>Bạn chưa có thông báo nào.</div>
          ) : (
            notifications.map(n => (
              <div key={n.id} className="card" 
                   style={{ 
                     padding: '1.5rem', 
                     opacity: n.isRead ? 0.7 : 1, 
                     borderLeft: n.isRead ? '4px solid #e2e8f0' : '4px solid var(--primary)',
                     cursor: 'pointer'
                   }}
                   onClick={() => !n.isRead && handleMarkRead(n.id)}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                  <div>
                    <h3 style={{ fontSize: '1.1rem', fontWeight: 800, marginBottom: '0.5rem' }}>{n.title}</h3>
                    <p style={{ color: 'var(--text-muted)' }}>{n.message}</p>
                  </div>
                  <span style={{ fontSize: '0.75rem', color: '#94a3b8' }}>{new Date(n.createdAt).toLocaleString()}</span>
                </div>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
}
