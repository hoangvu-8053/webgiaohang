// Stub pages for non-implemented routes
import { useParams, Link } from 'react-router-dom';

export function OrderDetailPage() {
  const { id } = useParams();
  return <div className="card animate-fade"><h1 style={{ marginBottom: '1.5rem', fontWeight: 800 }}>📦 Chi tiết đơn hàng #{id}</h1><p>Tính năng xem chi tiết đang được phát triển. Vui lòng quay lại sau!</p><Link to="/orders" className="btn btn-primary" style={{ marginTop: '2rem' }}>Quay lại</Link></div>;
}

export function ShipperOrdersPage() {
  return <div className="card animate-fade"><h1 style={{ marginBottom: '1.5rem', fontWeight: 800 }}>🚚 Danh sách đơn giao</h1><p>Tính năng dành cho Shipper đang được chuyển đổi sang phiên bản Web API mới.</p></div>;
}

export function AdminUsersPage() {
  return <div className="card animate-fade"><h1 style={{ marginBottom: '1.5rem', fontWeight: 800 }}>👥 Quản lý người dùng</h1><p>Tính năng Quản trị viên đang được đồng bộ hóa với Backend mới.</p></div>;
}

export function NotificationsPage() {
  return <div className="card animate-fade"><h1 style={{ marginBottom: '1.5rem', fontWeight: 800 }}>🔔 Thông báo</h1><p>Không có thông báo mới nào cho bạn.</p></div>;
}

export function ChatPage() {
  return <div className="card animate-fade"><h1 style={{ marginBottom: '1.5rem', fontWeight: 800 }}>💬 Tin nhắn</h1><p>Tính năng chat thời gian thực đang được bảo trì.</p></div>;
}

export function ReportsPage() {
  return <div className="card animate-fade"><h1 style={{ marginBottom: '1.5rem', fontWeight: 800 }}>📊 Báo cáo doanh thu</h1><p>Thống kê và báo cáo đang được tính toán theo dữ liệu mới.</p></div>;
}

export function TrackingPage() {
  return <div className="card animate-fade"><h1 style={{ marginBottom: '1.5rem', fontWeight: 800 }}>🗺️ Theo dõi đơn hàng</h1><p>Bản đồ theo dõi đang được cấu hình Google Maps API.</p></div>;
}
