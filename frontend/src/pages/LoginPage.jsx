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

  // Already logged in -> go to dashboard
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
      setError(err.response?.data?.message || 'Đăng nhập không đúng rồi bạn ơi! Xem lại nhen.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-card animate-fade">
        <div className="auth-header">
          <div style={{ fontSize: '3.5rem', marginBottom: '1.5rem' }} className="animate-float">☀️</div>
          <h1 className="brand-name" style={{ fontSize: '3rem', marginBottom: '0.75rem' }}>HELLO SONIC!</h1>
          <p className="auth-subtitle" style={{ color: '#64748b', fontWeight: 600 }}>Cùng bắt đầu ngày mới đầy năng lượng nhé!</p>
        </div>

        {error && <div className="alert-error">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label">Tài khoản</label>
            <input type="text" className="input" value={username} onChange={(e) => setUsername(e.target.value)} required placeholder="Nhập tên đăng nhập..." />
          </div>

          <div className="form-group">
            <label className="form-label">Mật khẩu</label>
            <input type="password" className="input" value={password} onChange={(e) => setPassword(e.target.value)} required placeholder="••••••••" />
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '2.5rem' }}>
            <Link to="/forgot-password" style={{ color: 'var(--primary)', fontSize: '0.9rem', fontWeight: 700, textDecoration: 'none' }}>Quên mật khẩu nè?</Link>
          </div>

          <button type="submit" className="btn btn-primary" style={{ width: '100%', padding: '1.25rem' }} disabled={loading}>
            {loading ? 'Đang vào...' : 'ĐĂNG NHẬP NGAY ✨'}
          </button>
        </form>

        <div style={{ textAlign: 'center', marginTop: '3rem', color: '#64748b', fontSize: '0.95rem' }}>
          Mới chơi Sonic hả?{' '}
          <Link to="/register" style={{ color: 'var(--secondary)', fontWeight: 800, textDecoration: 'none' }}>ĐĂNG KÝ LUÔN</Link>
        </div>
      </div>
    </div>
  );
}
