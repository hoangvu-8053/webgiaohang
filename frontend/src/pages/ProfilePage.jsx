import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { authApi } from '../api/endpoints';

export default function ProfilePage() {
  const { user, setUser } = useAuth();
  const [formData, setFormData] = useState({ fullName: '', email: '', phone: '', address: '' });
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState('');
  const [avatar, setAvatar] = useState(null);

  useEffect(() => {
    if (user) setFormData({ fullName: user.fullName || '', email: user.email || '', phone: user.phone || '', address: user.address || '' });
  }, [user]);

  const handleChange = (e) => setFormData({ ...formData, [e.target.name]: e.target.value });

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setSuccess('');
    try {
      const data = new FormData();
      Object.keys(formData).forEach(key => data.append(key, formData[key]));
      if (avatar) data.append('avatar', avatar);
      const res = await authApi.updateProfile(data);
      if (res.data.success) {
        setSuccess('Cập nhật hồ sơ thành công!');
        const fresh = await authApi.me();
        setUser(fresh.data.user);
        localStorage.setItem('user', JSON.stringify(fresh.data.user));
      }
    } catch (err) {
      alert('Lỗi cập nhật hồ sơ.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="animate-fade" style={{ maxWidth: '600px', margin: '0 auto' }}>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '2rem' }}>👤 Hồ sơ cá nhân</h1>
      
      {success && <div style={{ background: '#ecfdf5', color: '#10b981', padding: '1rem', borderRadius: '8px', marginBottom: '1.5rem', fontWeight: 600 }}>{success}</div>}

      <div className="card">
        <div style={{ textAlign: 'center', marginBottom: '2.5rem' }}>
          <div style={{ position: 'relative', display: 'inline-block' }}>
            {user.avatar ? (
              <img src={`${import.meta.env.VITE_API_URL || 'http://localhost:5170'}${user.avatar}`} alt="" style={{ width: '120px', height: '120px', borderRadius: '50%', objectFit: 'cover', border: '4px solid #f1f5f9' }} />
            ) : (
              <div style={{ width: '120px', height: '120px', borderRadius: '50%', background: '#6366f1', color: 'white', display: 'flex', alignItems: 'center', justifyCenter: 'center', fontSize: '3rem', fontWeight: 800 }}>{user.username?.charAt(0).toUpperCase()}</div>
            )}
            <input type="file" id="avatarInput" style={{ display: 'none' }} onChange={(e) => setAvatar(e.target.files[0])} />
            <button type="button" onClick={() => document.getElementById('avatarInput').click()} style={{ position: 'absolute', bottom: 0, right: 0, background: 'white', border: '1px solid #e2e8f0', borderRadius: '50%', width: '36px', height: '36px', boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)', cursor: 'pointer' }}>📸</button>
          </div>
          <h2 style={{ marginTop: '1rem', fontSize: '1.25rem' }}>{user.username}</h2>
          <p style={{ color: '#64748b' }}>Vai trò: <strong>{user.role}</strong></p>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label">Họ và tên</label>
            <input name="fullName" className="input" value={formData.fullName} onChange={handleChange} required />
          </div>

          <div className="form-group">
            <label className="form-label">Email</label>
            <input name="email" type="email" className="input" value={formData.email} onChange={handleChange} required />
          </div>

          <div className="form-group">
            <label className="form-label">Số điện thoại</label>
            <input name="phone" className="input" value={formData.phone} onChange={handleChange} required />
          </div>

          <div className="form-group">
            <label className="form-label">Địa chỉ mặc định</label>
            <textarea name="address" className="input" rows="3" value={formData.address} onChange={handleChange}></textarea>
          </div>

          <button type="submit" className="btn btn-primary" style={{ width: '100%', marginTop: '2rem' }} disabled={loading}>
            {loading ? 'Đang lưu...' : 'Lưu thay đổi'}
          </button>
        </form>
      </div>
    </div>
  );
}
