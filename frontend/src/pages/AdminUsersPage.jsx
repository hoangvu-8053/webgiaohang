import { useState, useEffect } from 'react';
import { adminApi } from '../api/endpoints';

export default function AdminUsersPage() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState('');

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const res = await adminApi.getUsers();
      if (res.data.success) setUsers(res.data.users);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleApprove = async (id) => {
    try {
      await adminApi.approveUser(id);
      setMessage('Đã duyệt người dùng thành công!');
      fetchUsers();
    } catch (err) {
      alert('Lỗi khi phê duyệt.');
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa người dùng này?')) return;
    try {
      await adminApi.deleteUser(id);
      setMessage('Đã xóa người dùng.');
      fetchUsers();
    } catch (err) {
      alert('Lỗi khi xóa.');
    }
  };

  const handleSetRole = async (id, role) => {
    try {
      await adminApi.setRole(id, role);
      setMessage('Đã cập nhật quyền hạn.');
      fetchUsers();
    } catch (err) {
      alert('Lỗi cập nhật quyền.');
    }
  };

  const roleLabel = { Admin: 'Admin', Staff: 'Nhân viên', Shipper: 'Shipper', Sender: 'Người gửi', Receiver: 'Người nhận' };

  return (
    <div className="animate-fade">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 800 }}>👥 Quản lý người dùng</h1>
        <button className="btn" onClick={fetchUsers} style={{ background: '#f1f5f9' }}>🔄 Làm mới</button>
      </div>

      {message && <div style={{ background: '#ecfdf5', color: '#10b981', padding: '0.75rem', borderRadius: '8px', marginBottom: '1.5rem', fontWeight: 600 }}>{message}</div>}

      {loading ? <p>Đang tải dữ liệu...</p> : (
        <div className="card" style={{ padding: 0 }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead style={{ background: '#f8fafc', borderBottom: '1px solid #e2e8f0' }}>
              <tr>
                <th style={{ padding: '1rem', textAlign: 'left' }}>Người dùng</th>
                <th style={{ padding: '1rem', textAlign: 'left' }}>Email/SĐT</th>
                <th style={{ padding: '1rem', textAlign: 'left' }}>Vai trò</th>
                <th style={{ padding: '1rem', textAlign: 'left' }}>Trạng thái</th>
                <th style={{ padding: '1rem', textAlign: 'center' }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {users.map(u => (
                <tr key={u.id} style={{ borderBottom: '1px solid #f1f5f9' }}>
                  <td style={{ padding: '1rem' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                      <div style={{ width: '32px', height: '32px', borderRadius: '50%', background: '#6366f1', color: 'white', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 600, fontSize: '0.75rem' }}>{u.username.charAt(0).toUpperCase()}</div>
                      <div>
                        <div style={{ fontWeight: 600 }}>{u.username}</div>
                        <div style={{ fontSize: '0.75rem', color: '#64748b' }}>{u.fullName || 'Chưa cập nhật tên'}</div>
                      </div>
                    </div>
                  </td>
                  <td style={{ padding: '1rem' }}>
                    <div style={{ fontSize: '0.875rem' }}>{u.email || 'N/A'}</div>
                    <div style={{ fontSize: '0.75rem', color: '#64748b' }}>{u.phone || 'N/A'}</div>
                  </td>
                  <td style={{ padding: '1rem' }}>
                    <select value={u.role} onChange={(e) => handleSetRole(u.id, e.target.value)} style={{ padding: '0.25rem 0.5rem', borderRadius: '4px', border: '1px solid #e2e8f0', fontSize: '0.875rem' }}>
                      <option value="Admin">Admin</option>
                      <option value="Staff">Nhân viên</option>
                      <option value="Shipper">Shipper</option>
                      <option value="Sender">Người gửi</option>
                      <option value="Receiver">Người nhận</option>
                    </select>
                  </td>
                  <td style={{ padding: '1rem' }}>
                    {u.isApproved ? (
                      <span style={{ color: '#10b981', fontWeight: 600, fontSize: '0.875rem' }}>● Đã duyệt</span>
                    ) : (
                      <span style={{ color: '#f59e0b', fontWeight: 600, fontSize: '0.875rem' }}>○ Chờ duyệt</span>
                    )}
                  </td>
                  <td style={{ padding: '1rem', textAlign: 'center' }}>
                    <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'center' }}>
                      {!u.isApproved && (
                        <button className="btn btn-primary" style={{ padding: '0.25rem 0.75rem', fontSize: '0.75rem' }} onClick={() => handleApprove(u.id)}>Duyệt</button>
                      )}
                      <button className="btn" style={{ padding: '0.25rem 0.75rem', fontSize: '0.75rem', background: '#fee2e2', color: '#ef4444' }} onClick={() => handleDelete(u.id)}>Xóa</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
