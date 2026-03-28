import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ordersApi } from '../api/endpoints';
import { useAuth } from '../context/AuthContext';

export default function CreateOrderPage() {
  const { user } = useAuth();
  const [formData, setFormData] = useState({ 
    product: '', 
    price: 0, 
    senderName: user?.fullName || '', 
    senderPhone: user?.phone || '', 
    senderEmail: user?.email || '',
    pickupAddress: user?.address || '', 
    receiverName: '', 
    receiverPhone: '', 
    receiverEmail: '',
    deliveryAddress: '', 
    notes: '',
    deliveryType: 'Standard'
  });
  const [loading, setLoading] = useState(false);
  const [image, setImage] = useState(null);
  const navigate = useNavigate();

  const handleChange = (e) => setFormData({ ...formData, [e.target.name]: e.target.value });

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      const data = new FormData();
      Object.keys(formData).forEach(key => data.append(key, formData[key]));
      // Đảm bảo các field model mong đợi đúng hoa/thường (tùy config backend)
      // Ở đây ta dùng trùng tên property trong model C#
      if (image) data.append('productImage', image);
      
      const res = await ordersApi.create(data);
      if (res.data.success) {
        navigate('/orders');
      }
    } catch (err) {
      console.error(err);
      const msg = err.response?.data?.errors 
        ? Object.values(err.response.data.errors).flat().join(', ')
        : (err.response?.data?.message || 'Có lỗi xảy ra khi tạo đơn hàng.');
      alert(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="animate-fade" style={{ maxWidth: '900px', margin: '0 auto' }}>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '2rem' }}>🚚 Tạo đơn hàng mới</h1>
      <form onSubmit={handleSubmit} className="card">
        <div style={{ padding: '1rem', borderBottom: '1px solid #f1f5f9', fontWeight: 700, marginBottom: '1.5rem', color: '#6366f1' }}>1. THÔNG TIN SẢN PHẨM</div>
        <div className="grid" style={{ gridTemplateColumns: '1fr 200px 1fr' }}>
          <div className="form-group">
            <label className="form-label">Tên sản phẩm</label>
            <input name="product" className="input" placeholder="Ví dụ: Giày Nike" onChange={handleChange} required />
          </div>
          <div className="form-group">
            <label className="form-label">Giá (VND)</label>
            <input name="price" type="number" className="input" placeholder="0" onChange={handleChange} required />
          </div>
          <div className="form-group">
            <label className="form-label">Loại giao hàng</label>
            <select name="deliveryType" className="input" onChange={handleChange}>
              <option value="Standard">Giao thường</option>
              <option value="Express">Giao nhanh</option>
              <option value="SameDay">Giao trong ngày</option>
            </select>
          </div>
        </div>

        <div className="form-group">
          <label className="form-label">Hình ảnh sản phẩm</label>
          <input type="file" className="input" onChange={(e) => setImage(e.target.files[0])} accept="image/*" />
        </div>

        <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: '2rem', marginTop: '2rem' }}>
          <div>
            <div style={{ padding: '0.5rem 0', fontWeight: 700, borderBottom: '2px solid #6366f1', marginBottom: '1rem' }}>NGƯỜI GỬI</div>
            <div className="form-group">
              <label className="form-label">Họ tên</label>
              <input name="senderName" className="input" value={formData.senderName} onChange={handleChange} required />
            </div>
            <div className="grid" style={{ gridTemplateColumns: '1fr 1fr' }}>
              <div className="form-group">
                <label className="form-label">SĐT</label>
                <input name="senderPhone" className="input" value={formData.senderPhone} onChange={handleChange} required />
              </div>
              <div className="form-group">
                <label className="form-label">Email</label>
                <input name="senderEmail" type="email" className="input" value={formData.senderEmail} onChange={handleChange} required />
              </div>
            </div>
            <div className="form-group">
              <label className="form-label">Địa chỉ lấy hàng</label>
              <textarea name="pickupAddress" className="input" rows="3" value={formData.pickupAddress} onChange={handleChange} required></textarea>
            </div>
          </div>

          <div>
            <div style={{ padding: '0.5rem 0', fontWeight: 700, borderBottom: '2px solid #f59e0b', marginBottom: '1rem' }}>NGƯỜI NHẬN</div>
            <div className="form-group">
              <label className="form-label">Họ tên</label>
              <input name="receiverName" className="input" onChange={handleChange} required />
            </div>
            <div className="grid" style={{ gridTemplateColumns: '1fr 1fr' }}>
              <div className="form-group">
                <label className="form-label">SĐT</label>
                <input name="receiverPhone" className="input" onChange={handleChange} required />
              </div>
              <div className="form-group">
                <label className="form-label">Email</label>
                <input name="receiverEmail" type="email" className="input" onChange={handleChange} required />
              </div>
            </div>
            <div className="form-group">
              <label className="form-label">Địa chỉ giao hàng</label>
              <textarea name="deliveryAddress" className="input" rows="3" onChange={handleChange} required></textarea>
            </div>
          </div>
        </div>

        <div className="form-group">
          <label className="form-label">Ghi chú thêm</label>
          <textarea name="notes" className="input" rows="2" onChange={handleChange}></textarea>
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '1rem', marginTop: '2rem' }}>
          <button type="button" className="btn" style={{ background: '#f1f5f9' }} onClick={() => navigate(-1)}>Hủy</button>
          <button type="submit" className="btn btn-primary" style={{ padding: '0.75rem 3rem' }} disabled={loading}>
            {loading ? 'Đang tạo...' : 'XÁC NHẬN TẠO ĐƠN'}
          </button>
        </div>
      </form>
    </div>
  );
}
