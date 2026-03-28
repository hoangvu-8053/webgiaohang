import { Link } from 'react-router-dom';

export default function UnauthorizedPage() {
  return (
    <div style={{ textAlign: 'center', padding: '5rem 2rem' }}>
      <div style={{ fontSize: '5rem', marginBottom: '1rem' }}>🚫</div>
      <h1 style={{ fontSize: '2rem', fontWeight: 800 }}>Truy cập bị từ chối</h1>
      <p style={{ color: '#64748b', fontSize: '1.25rem', maxWidth: '500px', margin: '1rem auto' }}>
        Bạn không có quyền truy cập vào trang này. Vui lòng liên hệ quản trị viên nếu bạn tin rằng đây là một lỗi.
      </p>
      <Link to="/" className="btn btn-primary" style={{ marginTop: '2rem' }}>Quay lại Trang chủ</Link>
    </div>
  );
}
