import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function LoginPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { login, user } = useAuth();
  const navigate = useNavigate();

  if (user) {
    navigate('/', { replace: true });
    return null;
  }

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login(username, password);
      navigate('/');
    } catch (err) {
      setError(err.response?.data?.message || 'Tên đăng nhập hoặc mật khẩu không đúng. Vui lòng thử lại!');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      {/* Background effects */}
      <div className="auth-bg">
        <div className="auth-cloud c1"></div>
        <div className="auth-cloud c2"></div>
        <div className="auth-cloud c3"></div>
        <div className="auth-bolt b1">⚡</div>
        <div className="auth-bolt b2">⚡</div>
      </div>

      <div className="auth-card animate-fade">
        {/* Brand */}
        <div className="auth-brand">
          <div className="auth-logo">⚡</div>
          <div className="auth-logo-text">SONIC</div>
        </div>

        <div className="auth-header">
          <div className="auth-icon">🔐</div>
          <h1>Đăng Nhập</h1>
          <p>Chào mừng bạn quay trở lại với Sonic!</p>
        </div>

        {error && (
          <div className="alert-error">
            <span>⚠️</span> {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label">Tên đăng nhập</label>
            <input
              type="text"
              className="form-input"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              placeholder="Nhập tên đăng nhập..."
              autoComplete="username"
            />
          </div>

          <div className="form-group">
            <label className="form-label">Mật khẩu</label>
            <input
              type="password"
              className="form-input"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              placeholder="Nhập mật khẩu..."
              autoComplete="current-password"
            />
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '2rem' }}>
            <Link to="/forgot-password" style={{ color: '#3B82F6', fontSize: '1rem', fontWeight: 700 }}>
              Quên mật khẩu?
            </Link>
          </div>

          <button type="submit" className="btn btn-primary" style={{ width: '100%', padding: '1.1rem', fontSize: '1.1rem' }} disabled={loading}>
            {loading ? '⏳ Đang đăng nhập...' : '⚡ Đăng Nhập Ngay'}
          </button>
        </form>

        <div className="auth-footer">
          <p>Chưa có tài khoản? <Link to="/register">Đăng ký ngay</Link></p>
        </div>

        {/* Demo accounts */}
        <div className="demo-accounts">
          <p className="demo-title">Tài khoản demo:</p>
          <div className="demo-list">
            <div className="demo-item">
              <span className="badge badge-admin">Admin</span>
              <span>admin / admin123</span>
            </div>
            <div className="demo-item">
              <span className="badge badge-shipper">Shipper</span>
              <span>shipper3 / shipper123</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
