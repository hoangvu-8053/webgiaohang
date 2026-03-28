import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function HomePage() {
  const { user } = useAuth();

  if (!user) {
    return <LandingPage />;
  }
  return <Dashboard />;
}

function LandingPage() {
  return (
    <div className="animate-fade">
      {/* Hero Section */}
      <div style={{
        padding: '6rem 2rem 4rem',
        textAlign: 'center',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        color: 'white',
        margin: '-2rem -2rem 3rem',
        borderRadius: '0 0 40px 40px'
      }}>
        <div style={{ fontSize: '5rem', marginBottom: '1.5rem' }} className="animate-float">🚀</div>
        <h1 style={{ fontSize: '4rem', fontWeight: 900, letterSpacing: '-2px', marginBottom: '1rem', color: 'white' }}>
          GIAO HÀNG SONIC
        </h1>
        <p style={{ fontSize: '1.5rem', opacity: 0.9, maxWidth: '600px', margin: '0 auto 2.5rem', fontWeight: 500 }}>
          Giải pháp giao vận siêu tốc - Đặt đơn trong 30 giây, giao hàng trong tích tắc!
        </p>
        <div style={{ display: 'flex', gap: '1.5rem', justifyContent: 'center', flexWrap: 'wrap' }}>
          <Link to="/register" style={{
            padding: '1.25rem 3rem',
            background: 'white',
            color: '#667eea',
            borderRadius: '50px',
            fontWeight: 900,
            fontSize: '1.25rem',
            textDecoration: 'none',
            boxShadow: '0 10px 30px rgba(0,0,0,0.2)'
          }}>
            ĐĂNG KÝ NGAY
          </Link>
          <Link to="/login" style={{
            padding: '1.25rem 3rem',
            background: 'rgba(255,255,255,0.2)',
            color: 'white',
            borderRadius: '50px',
            fontWeight: 900,
            fontSize: '1.25rem',
            textDecoration: 'none',
            border: '3px solid white'
          }}>
            ĐĂNG NHẬP
          </Link>
        </div>
      </div>

      {/* Features */}
      <div className="grid grid-3" style={{ marginBottom: '3rem' }}>
        <div className="card" style={{ textAlign: 'center', borderTop: '6px solid #667eea' }}>
          <div style={{ fontSize: '4rem', marginBottom: '1.5rem' }}>⚡</div>
          <h3 style={{ fontSize: '1.5rem', fontWeight: 900, marginBottom: '0.75rem' }}>SIÊU TỐC</h3>
          <p style={{ color: 'var(--text-muted)' }}>Đặt đơn chỉ trong 30 giây, giao hàng nhanh như chớp!</p>
        </div>
        <div className="card" style={{ textAlign: 'center', borderTop: '6px solid #ec4899' }}>
          <div style={{ fontSize: '4rem', marginBottom: '1.5rem' }}>📍</div>
          <h3 style={{ fontSize: '1.5rem', fontWeight: 900, marginBottom: '0.75rem' }}>THEO DÕI</h3>
          <p style={{ color: 'var(--text-muted)' }}>Tra cứu đơn hàng realtime, biết chính xác hàng đâu!</p>
        </div>
        <div className="card" style={{ textAlign: 'center', borderTop: '6px solid #10b981' }}>
          <div style={{ fontSize: '4rem', marginBottom: '1.5rem' }}>💰</div>
          <h3 style={{ fontSize: '1.5rem', fontWeight: 900, marginBottom: '0.75rem' }}>TIẾT KIỆM</h3>
          <p style={{ color: 'var(--text-muted)' }}>Phí giao hàng cực kỳ cạnh tranh, không phí ẩn!</p>
        </div>
      </div>

      {/* How it works */}
      <div className="card" style={{ textAlign: 'center', marginBottom: '3rem', background: 'linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%)' }}>
        <h2 style={{ fontSize: '2.5rem', fontWeight: 900, marginBottom: '2rem', color: 'var(--primary)' }}>
          🏃‍♂️ CÁCH THỨC HOẠT ĐỘNG
        </h2>
        <div className="grid grid-4">
          <div>
            <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>📝</div>
            <h4 style={{ fontWeight: 900 }}>1. ĐĂNG KÝ</h4>
            <p style={{ color: 'var(--text-muted)' }}>Tạo tài khoản miễn phí</p>
          </div>
          <div>
            <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>📦</div>
            <h4 style={{ fontWeight: 900 }}>2. TẠO ĐƠN</h4>
            <p style={{ color: 'var(--text-muted)' }}>Nhập thông tin gửi/nhận</p>
          </div>
          <div>
            <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>🚚</div>
            <h4 style={{ fontWeight: 900 }}>3. GIAO HÀNG</h4>
            <p style={{ color: 'var(--text-muted)' }}>Shipper nhận và giao ngay</p>
          </div>
          <div>
            <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>✅</div>
            <h4 style={{ fontWeight: 900 }}>4. NHẬN HÀNG</h4>
            <p style={{ color: 'var(--text-muted)' }}>Xác nhận và đánh giá</p>
          </div>
        </div>
      </div>

      {/* CTA */}
      <div style={{ textAlign: 'center', padding: '4rem 2rem', background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)', borderRadius: '20px', color: 'white' }}>
        <h2 style={{ fontSize: '2.5rem', fontWeight: 900, marginBottom: '1rem', color: 'white' }}>
          SẴN SÀNG TRẢI NGHIỆM?
        </h2>
        <p style={{ fontSize: '1.25rem', opacity: 0.9, marginBottom: '2rem' }}>
          Gia nhập cộng đồng Sonic ngay hôm nay!
        </p>
        <Link to="/register" style={{
          padding: '1.25rem 3rem',
          background: 'white',
          color: '#667eea',
          borderRadius: '50px',
          fontWeight: 900,
          fontSize: '1.25rem',
          textDecoration: 'none',
          boxShadow: '0 10px 30px rgba(0,0,0,0.2)'
        }}>
          BẮT ĐẦU NGAY
        </Link>
      </div>
    </div>
  );
}

function ActionCard({ to, icon, title, desc, color }) {
  return (
    <Link to={to} className="card animate-fade" style={{ textDecoration: 'none', borderBottom: `6px solid ${color}` }}>
      <div style={{ fontSize: '3.5rem', marginBottom: '1.5rem', filter: `drop-shadow(0 4px 10px ${color}40)` }} className="animate-float">
        {icon}
      </div>
      <h3 style={{ fontSize: '1.75rem', fontWeight: 900, marginBottom: '0.75rem', color: 'var(--text)' }}>{title}</h3>
      <p style={{ color: 'var(--text-muted)', fontSize: '1rem', fontWeight: 500 }}>{desc}</p>
    </Link>
  );
}

function Dashboard() {
  const { user } = useAuth();

  return (
    <div className="animate-fade">
      <div style={{ padding: '4rem 0', textAlign: 'center', marginBottom: '3rem' }}>
        <h1 style={{ fontSize: '3rem', fontWeight: 900, letterSpacing: '-2px', marginBottom: '1rem' }}>
          GIAO HÀNG SONIC ✨
        </h1>
        <p style={{ fontSize: '1.5rem', color: 'var(--text-muted)', fontWeight: 600 }}>
          Chào <span style={{ color: 'var(--primary)', fontWeight: 900 }}>{user.fullName || user.username}</span>! Bạn muốn "bay" đơn nào hôm nay?
        </p>
      </div>

      <div className="grid grid-3">
        {['Admin', 'Staff', 'Sender'].includes(user.role) && (
          <ActionCard
            to="/orders/create"
            icon="🚀"
            title="LÊN ĐƠN"
            desc="Tạo vận đơn siêu tốc chỉ trong 30 giây."
            color="#6366f1"
          />
        )}

        <ActionCard
          to={user.role === 'Shipper' ? '/shipper/orders' : '/orders'}
          icon="📦"
          title="KIỂM ĐƠN"
          desc={user.role === 'Shipper' ? 'Đơn được gán cho bạn, bản đồ & cập nhật trạng thái.' : 'Xem trạng thái và quản lý danh sách đơn hàng.'}
          color="#ec4899"
        />

        <ActionCard
          to="/tracking"
          icon="📍"
          title="TRA CỨU"
          desc="Hàng hóa của bạn đang di chuyển tới đâu rồi?"
          color="#3b82f6"
        />

        {['Admin', 'Staff'].includes(user.role) && (
          <>
            <ActionCard
              to="/admin/users"
              icon="👥"
              title="QUẢN LÝ"
              desc="Phê duyệt thành viên mới gia nhập Sonic."
              color="#10b981"
            />
            <ActionCard
              to="/reports"
              icon="📊"
              title="THỐNG KÊ"
              desc="Biểu đồ doanh thu và hiệu suất giao vận."
              color="#f59e0b"
            />
          </>
        )}

        <ActionCard
          to="/chat"
          icon="💬"
          title="NHẮN TIN"
          desc="Kết nối với người giao và người nhận hàng."
          color="#ef4444"
        />
      </div>

      <div className="card" style={{ marginTop: '5rem', background: 'white', border: '5px solid #e2e8f0', borderStyle: 'dashed', textAlign: 'center' }}>
        <h2 style={{ fontSize: '1.75rem', fontWeight: 900, marginBottom: '1.25rem', color: 'var(--primary)' }}>CỘNG ĐỒNG SONIC GIAO NHANH 🔥</h2>
        <p style={{ color: 'var(--text-muted)', fontSize: '1.15rem', fontWeight: 600 }}>Tích lũy điểm Sonic để đổi ưu đãi giao hàng miễn phí vào mỗi cuối tuần!</p>
      </div>
    </div>
  );
}
