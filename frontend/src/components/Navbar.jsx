import { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { notificationApi } from '../api/endpoints';
import { useEffect } from 'react';

const roleLabel = {
  Admin: 'Quản trị viên',
  Staff: 'Nhân viên',
  Shipper: 'Shipper',
  Sender: 'Người gửi',
  Receiver: 'Người nhận',
};

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [menuOpen, setMenuOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
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
    setUserMenuOpen(false);
    await logout();
    navigate('/login');
  };

  const getNavLinks = () => {
    if (!user) return [];
    const role = user.role;
    const links = [];
    if (['Admin', 'Staff', 'Sender', 'Receiver'].includes(role)) {
      links.push({ to: '/orders', label: 'Đơn hàng', icon: '📦' });
    }
    if (role === 'Shipper') {
      links.push({ to: '/shipper/orders', label: 'Giao hàng', icon: '🚚' });
    }
    if (['Admin', 'Staff'].includes(role)) {
      links.push({ to: '/admin/users', label: 'Quản lý', icon: '👥' });
      links.push({ to: '/reports', label: 'Báo cáo', icon: '📊' });
    }
    links.push({ to: '/chat', label: 'Nhắn tin', icon: '💬' });
    links.push({
      to: '/notifications',
      label: unread > 0 ? `Thông báo (${unread})` : 'Thông báo',
      icon: unread > 0 ? '🔔' : '🔕',
    });
    return links;
  };

  return (
    <nav className="navbar">
      {/* Brand */}
      <Link to="/" className="brand-logo">
        <span className="brand-icon">⚡</span>
        <span className="brand-name">SONIC</span>
      </Link>

      {/* Nav Links */}
      <div className="navbar-links">
        {getNavLinks().map(l => (
          <Link
            key={l.to}
            to={l.to}
            className={`nav-link ${location.pathname.startsWith(l.to) ? 'active' : ''}`}
          >
            <span>{l.icon}</span> {l.label}
          </Link>
        ))}
      </div>

      {/* User Area */}
      <div className="navbar-user" style={{ position: 'relative' }}>
        {user ? (
          <>
            <button
              className="user-btn"
              onClick={() => setUserMenuOpen(!userMenuOpen)}
            >
              {user.avatar ? (
                <img
                  src={`${import.meta.env.VITE_API_URL || 'http://localhost:5170'}${user.avatar}`}
                  alt=""
                  className="user-avatar"
                />
              ) : (
                <div className="user-avatar-placeholder">
                  {user.username?.charAt(0).toUpperCase()}
                </div>
              )}
              <div>
                <div className="user-info-name">{user.fullName || user.username}</div>
              </div>
              <span style={{ color: 'rgba(255,255,255,0.6)', fontSize: '0.8rem' }}>▼</span>
            </button>

            {userMenuOpen && (
              <div className="user-menu-dropdown animate-fade">
                <div style={{ padding: '0.5rem 1rem 1rem', borderBottom: '2px solid var(--border)', marginBottom: '0.75rem' }}>
                  <div style={{ fontWeight: 900, fontSize: '1.1rem', color: 'var(--text-primary)' }}>
                    {user.fullName || user.username}
                  </div>
                  <div style={{ fontSize: '0.9rem', color: 'var(--text-secondary)', marginTop: '0.2rem' }}>
                    {user.username}
                  </div>
                  <div style={{ marginTop: '0.5rem' }}>
                    <span className={`badge badge-${user.role?.toLowerCase()}`}>
                      ⚡ {roleLabel[user.role] || user.role}
                    </span>
                  </div>
                </div>

                <Link to="/profile" className="user-menu-item" onClick={() => setUserMenuOpen(false)}>
                  👤 Hồ sơ cá nhân
                </Link>
                <Link to="/tracking" className="user-menu-item" onClick={() => setUserMenuOpen(false)}>
                  🔍 Tra cứu đơn hàng
                </Link>

                <hr className="user-menu-divider" />

                <button
                  onClick={handleLogout}
                  className="user-menu-item"
                  style={{
                    background: '#fff1f2',
                    color: '#dc2626',
                    border: 'none',
                    width: '100%',
                    cursor: 'pointer',
                    fontFamily: 'var(--font-family)',
                    borderRadius: '10px',
                  }}
                >
                  🚪 Đăng xuất
                </button>
              </div>
            )}
          </>
        ) : (
          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
            <Link to="/login" className="btn btn-outline" style={{ padding: '0.6rem 1.5rem', fontSize: '1rem' }}>
              Đăng nhập
            </Link>
            <Link to="/register" className="btn btn-primary" style={{ padding: '0.6rem 1.5rem', fontSize: '1rem' }}>
              ⚡ Đăng ký
            </Link>
          </div>
        )}
      </div>
    </nav>
  );
}
