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
      await authApi.register(formData);
      setSuccess(true);
      setTimeout(() => navigate('/login'), 3000);
    } catch (err) {
      setError(err.response?.data?.message || 'Đăng ký fail rồi. Xem lại nha!');
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e) => setFormData({ ...formData, [e.target.name]: e.target.value });

  if (success) {
    return (
      <div className="auth-container">
        <div className="auth-card" style={{ textAlign: 'center' }}>
          <div style={{ fontSize: '5rem', marginBottom: '1.5rem' }} className="animate-float">✨</div>
          <h2 className="brand-name" style={{ fontSize: '2rem' }}>WELCOME TO SONIC!</h2>
          <p className="auth-subtitle">Tài khoản của bạn đang chờ "sếp" duyệt. Sẽ quay lại trang đăng nhập trong tích tắc...</p>
          <Link to="/login" className="btn btn-primary" style={{ marginTop: '2rem' }}>QUAY LẠI ĐĂNG NHẬP ⚡</Link>
        </div>
      </div>
    );
  }

  return (
    <div className="auth-container">
      <div className="auth-card animate-fade" style={{ maxWidth: '600px' }}>
        <div className="auth-header">
          <div style={{ fontSize: '2.5rem', marginBottom: '1rem' }} className="animate-float">💥</div>
          <h1 className="brand-name" style={{ fontSize: '2.5rem' }}>THAM GIA SONIC</h1>
          <p className="auth-subtitle">Giao hàng cực chill, chốt đơn cực real</p>
        </div>

        {error && <div style={{ background: 'rgba(239, 68, 68, 0.15)', color: '#fca5a5', padding: '1rem', borderRadius: '12px', marginBottom: '1.5rem', border: '1px solid rgba(239, 68, 68, 0.3)', textAlign: 'center' }}>{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="grid grid-2">
            <div className="form-group">
              <label className="form-label">USERNAME</label>
              <input name="username" className="input" onChange={handleChange} required placeholder="Tên gì nè..." />
            </div>
            <div className="form-group">
              <label className="form-label">PASSWORD</label>
              <input name="password" type="password" className="input" onChange={handleChange} required placeholder="Mật mật khẩu khẩu..." />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">BẠN MUỐN LÀM GÌ?</label>
            <select name="role" className="input" value={formData.role} onChange={handleChange} style={{ background: 'var(--bg-card)' }}>
              <option value="Sender">Tôi muốn gửi đồ 📦</option>
              <option value="Receiver">Tôi muốn nhận đồ 🏠</option>
            </select>
          </div>

          <div className="form-group">
            <label className="form-label">HỌ TÊN ĐẦY ĐỦ</label>
            <input name="fullName" className="input" onChange={handleChange} required placeholder="Tên thật nhen..." />
          </div>

          <div className="grid grid-2">
            <div className="form-group">
              <label className="form-label">MOBILE</label>
              <input name="phone" className="input" onChange={handleChange} required placeholder="Số đt nè..." />
            </div>
            <div className="form-group">
              <label className="form-label">EMAIL</label>
              <input name="email" type="email" className="input" onChange={handleChange} required placeholder="Địa chỉ mail..." />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">ĐỊA CHỈ DEFAULT</label>
            <textarea name="address" className="input" rows="2" onChange={handleChange} required placeholder="Ở đâu để Sonic qua đón?"></textarea>
          </div>

          <button type="submit" className="btn btn-primary" style={{ width: '100%', marginTop: '1rem', padding: '1.2rem' }} disabled={loading}>
            {loading ? 'Đang tạo...' : 'ĐĂNG KÝ NGAY 🚀'}
          </button>
        </form>

        <div style={{ textAlign: 'center', marginTop: '2rem', color: '#94a3b8', fontSize: '0.9rem' }}>
          Đã có acc?{' '}
          <Link to="/login" style={{ color: 'var(--primary)', fontWeight: 800, textDecoration: 'none' }}>ĐĂNG NHẬP LUÔN</Link>
        </div>
      </div>
    </div>
  );
}
