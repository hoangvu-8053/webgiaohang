import { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { notificationApi } from '../api/endpoints';
import { useEffect } from 'react';

const roleLabel = { Admin: 'Quản trị viên', Staff: 'Nhân viên', Shipper: 'Shipper', Sender: 'Người gửi', Receiver: 'Người nhận' };
const roleBadge = { Admin: 'badge-admin', Staff: 'badge-staff', Shipper: 'badge-shipper', Sender: 'badge-sender', Receiver: 'badge-receiver' };

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [menuOpen, setMenuOpen] = useState(false);
  const [unread, setUnread] = useState(0);

  useEffect(() => {
    if (!user) return;
    notificationApi.getUnreadCount().then(r => setUnread(r.data.count || 0)).catch(() => {});
    const interval = setInterval(() => {
      notificationApi.getUnreadCount().then(r => setUnread(r.data.count || 0)).catch(() => {});
    }, 30000);
    return () => clearInterval(interval);
  }, [user]);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const navLinks = () => {
    if (!user) return [];
    const role = user.role;
    const links = [];
    if (['Admin', 'Staff', 'Sender', 'Receiver'].includes(role)) {
      links.push({ to: '/orders', label: '📦 Đơn hàng' });
    }
    if (role === 'Shipper') {
      links.push({ to: '/shipper/orders', label: '🚚 Giao hàng' });
    }
    if (['Admin', 'Staff'].includes(role)) {
      links.push({ to: '/admin/users', label: '👥 Quản lý' });
      links.push({ to: '/reports', label: '📊 Báo cáo' });
    }
    links.push({ to: '/chat', label: '💬 Nhắn tin' });
    links.push({ to: '/notifications', label: unread > 0 ? `🔔 ${unread}` : '🔔' });
    return links;
  };

  return (
    <nav className="navbar" style={{ position: 'sticky', top: 0 }}>
      <div className="navbar-brand">
        <Link to="/" className="brand-logo" style={{ textDecoration: 'none' }}>
          <span className="brand-icon">💨</span>
          <span className="brand-name" style={{ fontWeight: 900, fontSize: '2rem' }}>SONIC</span>
        </Link>
      </div>

      <div className={`navbar-links ${menuOpen ? 'open' : ''}`}>
        {navLinks().map(l => (
          <Link key={l.to} to={l.to} className={`nav-link ${location.pathname.startsWith(l.to) ? 'active' : ''}`}
            onClick={() => setMenuOpen(false)}>
            {l.label}
          </Link>
        ))}
      </div>

      <div className="navbar-user">
        {user ? (
          <div className="user-menu" style={{ position: 'relative' }}>
            <button className="user-btn" onClick={() => setMenuOpen(!menuOpen)} style={{ background: 'white', border: '1px solid #eef2ff', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '1rem', padding: '0.65rem 1.5rem', borderRadius: '16px', transition: 'all 0.3s', boxShadow: '0 4px 15px rgba(99, 102, 241, 0.1)' }}>
              <div style={{ position: 'relative' }}>
                {user.avatar ? 
                  <img src={`${import.meta.env.VITE_API_URL || 'http://localhost:5170'}${user.avatar}`} alt="" style={{ width: '36px', height: '36px', borderRadius: '12px', objectFit: 'cover', border: '2px solid var(--primary)' }} /> : 
                  <div style={{ width: '36px', height: '36px', borderRadius: '12px', background: 'linear-gradient(135deg, #6366f1, #a855f7)', color: 'white', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 800 }}>{user.username?.charAt(0).toUpperCase()}</div>}
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start' }}>
                <span style={{ color: 'var(--text)', fontWeight: 800, fontSize: '0.9rem' }}>{user.fullName || user.username}</span>
                <span className={`badge ${roleBadge[user.role] || 'badge-admin'}`} style={{ fontSize: '0.65rem', marginTop: '2px', padding: '0.1rem 0.6rem' }}>{user.role}</span>
              </div>
            </button>
            {menuOpen && (
              <div className="card animate-fade" style={{ position: 'absolute', top: '100%', right: 0, marginTop: '1.25rem', width: '240px', padding: '1.25rem', zIndex: 1001, background: 'white', border: '1px solid #e2e8f0', boxShadow: '0 20px 40px rgba(0,0,0,0.1)' }}>
                <Link to="/profile" className="nav-link" style={{ display: 'block', marginBottom: '0.75rem', color: 'var(--text)', textDecoration: 'none', fontWeight: 700 }} onClick={() => setMenuOpen(false)}>👤 Hồ sơ cá nhân</Link>
                <div style={{ borderTop: '1px solid #f1f5f9', margin: '0.75rem 0', paddingTop: '0.75rem' }}>
                  <button onClick={handleLogout} className="btn" style={{ width: '100%', background: '#fff1f2', color: '#e11d48', fontWeight: 800, padding: '0.75rem', borderRadius: '12px' }}>ĐĂNG XUẤT 🚪</button>
                </div>
              </div>
            )}
          </div>
        ) : (
          <div style={{ display: 'flex', gap: '0.75rem' }}>
            <Link to="/login" className="btn btn-secondary" style={{ textDecoration: 'none', padding: '0.65rem 1.5rem' }}>ĐĂNG NHẬP</Link>
            <Link to="/register" className="btn btn-primary" style={{ textDecoration: 'none', padding: '0.65rem 1.5rem' }}>ĐĂNG KÝ ⚡</Link>
          </div>
        )}
      </div>
    </nav>
  );
}
