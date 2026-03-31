import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { authApi } from '../api/endpoints';
import { useAuth } from '../context/AuthContext';

export default function RegisterPage() {
  const [formData, setFormData] = useState({ username: '', password: '', role: 'Sender', fullName: '', email: '', phone: '', address: '' });
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const { user } = useAuth();

  if (user) {
    navigate('/', { replace: true });
    return null;
  }

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await authApi.register(formData);
      setSuccess(true);
      setTimeout(() => navigate('/login'), 3000);
    } catch (err) {
      setError(err.response?.data?.message || 'Đăng ký không thành công. Vui lòng kiểm tra lại thông tin!');
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e) => setFormData({ ...formData, [e.target.name]: e.target.value });

  if (success) {
    return (
      <div className="auth-page">
        <div className="auth-bg">
          <div className="auth-cloud c1"></div>
          <div className="auth-bolt b1">⚡</div>
        </div>
        <div className="auth-card animate-fade" style={{ textAlign: 'center', maxWidth: '520px' }}>
          <div className="auth-brand" style={{ justifyContent: 'center', marginBottom: '1.5rem' }}>
            <div className="auth-logo">⚡</div>
            <div className="auth-logo-text">SONIC</div>
          </div>
          <div style={{ fontSize: '5rem', marginBottom: '1.5rem' }} className="animate-float">⚡</div>
          <h1 style={{ fontSize: '2rem', fontWeight: 900, color: 'var(--text-primary)', marginBottom: '1rem' }}>CHÀO MỪNG!</h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '1.1rem', marginBottom: '0.5rem' }}>
            Tài khoản của bạn đang chờ được phê duyệt.
          </p>
          <p style={{ color: 'var(--text-secondary)', fontSize: '1rem', marginBottom: '2rem' }}>
            Sẽ tự chuyển về trang đăng nhập trong giây lát...
          </p>
          <Link to="/login" className="btn btn-secondary" style={{ padding: '1rem 2rem' }}>
            Đăng nhập ngay
          </Link>
        </div>
      </div>
    );
  }

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

      <div className="auth-card animate-fade" style={{ maxWidth: '560px' }}>
        {/* Brand */}
        <div className="auth-brand">
          <div className="auth-logo">⚡</div>
          <div className="auth-logo-text">SONIC</div>
        </div>

        <div className="auth-header">
          <div className="auth-icon">📝</div>
          <h1>Tạo Tài Khoản</h1>
          <p>Tham gia Sonic ngay hôm nay!</p>
        </div>

        {error && (
          <div className="alert-error">
            <span>⚠️</span> {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="grid grid-2">
            <div className="form-group">
              <label className="form-label">Tên đăng nhập</label>
              <input name="username" className="form-input" onChange={handleChange} required placeholder="Tên đăng nhập..." />
            </div>
            <div className="form-group">
              <label className="form-label">Mật khẩu</label>
              <input name="password" type="password" className="form-input" onChange={handleChange} required placeholder="Mật khẩu..." />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Bạn muốn làm gì?</label>
            <select name="role" className="form-input" value={formData.role} onChange={handleChange}>
              <option value="Sender">📦 Tôi muốn gửi đồ</option>
              <option value="Receiver">🏠 Tôi muốn nhận đồ</option>
            </select>
          </div>

          <div className="form-group">
            <label className="form-label">Họ và tên</label>
            <input name="fullName" className="form-input" onChange={handleChange} required placeholder="Nhập họ tên đầy đủ..." />
          </div>

          <div className="grid grid-2">
            <div className="form-group">
              <label className="form-label">Số điện thoại</label>
              <input name="phone" className="form-input" onChange={handleChange} required placeholder="0xxx.xxx.xxx" />
            </div>
            <div className="form-group">
              <label className="form-label">Email</label>
              <input name="email" type="email" className="form-input" onChange={handleChange} required placeholder="email@example.com" />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Địa chỉ mặc định</label>
            <textarea name="address" className="form-input" rows="2" onChange={handleChange} required placeholder="Địa chỉ để shipper qua nhận hàng..." />
          </div>

          <button type="submit" className="btn btn-primary" style={{ width: '100%', marginTop: '0.5rem', padding: '1.1rem', fontSize: '1.1rem' }} disabled={loading}>
            {loading ? '⏳ Đang đăng ký...' : '⚡ Đăng Ký Ngay'}
          </button>
        </form>

        <div className="auth-footer">
          <p>Đã có tài khoản? <Link to="/login">Đăng nhập ngay</Link></p>
        </div>
      </div>
    </div>
  );
}
