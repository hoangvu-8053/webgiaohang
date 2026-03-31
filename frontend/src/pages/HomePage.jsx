import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './HomePage.css';

export default function HomePage() {
  const { user } = useAuth();
  if (!user) return <LandingPage />;
  return <Dashboard user={user} />;
}

/* =============================================
   LANDING PAGE - Thunder / Lightning Theme
   ============================================= */
function LandingPage() {
  const features = [
    {
      icon: (
        <svg viewBox="0 0 64 64" fill="none">
          <circle cx="32" cy="32" r="28" fill="rgba(245,158,11,0.1)"/>
          <path d="M36 8L18 36H30L28 56L46 28H34L36 8Z" fill="#F59E0B" stroke="#D97706" strokeWidth="2" strokeLinejoin="round"/>
        </svg>
      ),
      title: 'Siêu Tốc',
      desc: 'Nhận hàng trong 2h nội thành, giao toàn quốc 24-48h',
      color: '#F59E0B',
    },
    {
      icon: (
        <svg viewBox="0 0 64 64" fill="none">
          <circle cx="32" cy="32" r="28" fill="rgba(59,130,246,0.1)"/>
          <circle cx="32" cy="32" r="12" fill="#3B82F6"/>
          <path d="M32 8V16M32 48V56M8 32H16M48 32H56" stroke="#3B82F6" strokeWidth="3" strokeLinecap="round"/>
          <circle cx="32" cy="32" r="20" stroke="#3B82F6" strokeWidth="2" strokeDasharray="4 4"/>
        </svg>
      ),
      title: 'Theo Dõi Live',
      desc: 'Cập nhật vị trí đơn hàng real-time 24/7',
      color: '#3B82F6',
    },
    {
      icon: (
        <svg viewBox="0 0 64 64" fill="none">
          <circle cx="32" cy="32" r="28" fill="rgba(16,185,129,0.1)"/>
          <rect x="18" y="24" width="28" height="20" rx="4" fill="#10B981"/>
          <path d="M18 32H46" stroke="#059669" strokeWidth="2"/>
          <circle cx="32" cy="38" r="4" fill="white"/>
          <path d="M30 38L31 39L34 37" stroke="#059669" strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      ),
      title: 'Thanh Toán An Toàn',
      desc: 'COD, chuyển khoản, ví điện tử - Bảo mật tuyệt đối',
      color: '#10B981',
    },
    {
      icon: (
        <svg viewBox="0 0 64 64" fill="none">
          <circle cx="32" cy="32" r="28" fill="rgba(139,92,246,0.1)"/>
          <path d="M20 44L32 20L44 44H20Z" fill="#8B5CF6"/>
          <circle cx="32" cy="38" r="4" fill="white"/>
          <rect x="24" y="26" width="4" height="6" rx="1" fill="white" opacity="0.7"/>
          <rect x="30" y="22" width="4" height="10" rx="1" fill="white" opacity="0.7"/>
          <rect x="36" y="28" width="4" height="4" rx="1" fill="white" opacity="0.7"/>
        </svg>
      ),
      title: 'Phí Ship Tối Ưu',
      desc: 'Giá cước cạnh tranh nhất thị trường, không phí ẩn',
      color: '#8B5CF6',
    },
  ];

  const steps = [
    { num: '01', icon: '📝', title: 'Đăng Ký', desc: 'Tạo tài khoản miễn phí' },
    { num: '02', icon: '📦', title: 'Tạo Đơn', desc: 'Nhập thông tin gửi & nhận' },
    { num: '03', icon: '⚡', title: 'Giao Hàng', desc: 'Shipper nhận đơn ngay' },
    { num: '04', icon: '✅', title: 'Xác Nhận', desc: 'Nhận & đánh giá dịch vụ' },
  ];

  return (
    <div className="landing">
      {/* ======= HEADER ======= */}
      <header className="landing-header">
        <div className="landing-header-inner">
          <div className="landing-brand">
            <span className="landing-brand-icon">⚡</span>
            <span className="landing-brand-name">SONIC</span>
          </div>
          <nav className="landing-nav">
            <Link to="/login" className="landing-nav-link">Đăng nhập</Link>
            <Link to="/register" className="landing-nav-cta">
              ⚡ Bắt đầu ngay
            </Link>
          </nav>
        </div>
      </header>

      {/* ======= HERO ======= */}
      <section className="hero-section">
        <div className="hero-bg">
          <div className="hero-cloud cloud-1"></div>
          <div className="hero-cloud cloud-2"></div>
          <div className="hero-cloud cloud-3"></div>
          <div className="hero-lightning lightning-1">⚡</div>
          <div className="hero-lightning lightning-2">⚡</div>
        </div>

        <div className="hero-content">
          <div className="hero-text">
            <div className="hero-badge">
              <span className="badge-pulse"></span>
              Dịch Vụ Giao Hàng Siêu Tốc
            </div>

            <h1 className="hero-title">
              Giao Hàng<br />
              <span className="hero-title-lightning">Siêu Tốc</span><br />
              Như Chớp ⚡
            </h1>

            <p className="hero-desc">
              Giải pháp giao vận thông minh cho cá nhân & doanh nghiệp.
              Đặt đơn dễ dàng, theo dõi real-time, giao hàng tận nơi.
            </p>

            <div className="hero-cta">
              <Link to="/register" className="btn btn-primary btn-lg">
                ⚡ Bắt đầu ngay
              </Link>
              <Link to="/tracking" className="btn btn-secondary btn-lg">
                🔍 Tra cứu đơn hàng
              </Link>
            </div>

            <div className="hero-highlights">
              <span>✅ Miễn phí đăng ký</span>
              <span>✅ Phí ship từ 15K</span>
              <span>✅ Giao trong 2 giờ</span>
            </div>
          </div>

          <div className="hero-visual">
            <div className="hero-illustration">
              {/* Truck */}
              <svg viewBox="0 0 320 200" fill="none" className="truck-svg">
                <rect x="20" y="50" width="180" height="90" rx="10" fill="#1E293B"/>
                <rect x="25" y="57" width="80" height="76" rx="6" fill="#F59E0B"/>
                <rect x="115" y="57" width="80" height="76" rx="6" fill="#F59E0B"/>
                <rect x="200" y="65" width="70" height="55" rx="6" fill="#38BDF8"/>
                <rect x="208" y="73" width="50" height="30" rx="3" fill="#BAE6FD"/>
                <circle cx="60" cy="148" r="18" fill="#0F172A"/>
                <circle cx="60" cy="148" r="9" fill="#475569"/>
                <circle cx="60" cy="148" r="4" fill="#94A3B8"/>
                <circle cx="160" cy="148" r="18" fill="#0F172A"/>
                <circle cx="160" cy="148" r="9" fill="#475569"/>
                <circle cx="160" cy="148" r="4" fill="#94A3B8"/>
                <text x="45" y="100" fontSize="28" fill="#1E293B" fontWeight="900">⚡</text>
                <text x="125" y="100" fontSize="22" fill="#1E293B" fontWeight="900">SONIC</text>
                <path d="M280 75 L310 75" stroke="#F59E0B" strokeWidth="4" strokeLinecap="round" opacity="0.6"/>
                <path d="M280 90 L310 90" stroke="#F59E0B" strokeWidth="4" strokeLinecap="round" opacity="0.6"/>
                <path d="M280 105 L310 105" stroke="#F59E0B" strokeWidth="4" strokeLinecap="round" opacity="0.6"/>
              </svg>

              {/* Floating lightning bolts */}
              <div className="float-bolt bolt-1">⚡</div>
              <div className="float-bolt bolt-2">⚡</div>
              <div className="float-bolt bolt-3">⚡</div>
            </div>
          </div>
        </div>
      </section>

      {/* ======= FEATURES ======= */}
      <section className="features-section">
        <div className="section-container">
          <div className="section-header">
            <h2>Tại Sao Chọn <span className="text-yellow">SONIC</span>?</h2>
            <p>Dịch vụ giao hàng chuyên nghiệp hàng đầu Việt Nam</p>
          </div>
          <div className="features-grid">
            {features.map((f, i) => (
              <div className="feature-card" key={i} style={{ '--feat-color': f.color }}>
                <div className="feature-icon">{f.icon}</div>
                <h3>{f.title}</h3>
                <p>{f.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ======= STEPS ======= */}
      <section className="steps-section">
        <div className="section-container">
          <div className="section-header">
            <h2>Chỉ <span className="text-yellow">4 Bước</span> Đơn Giản</h2>
            <p>Giao hàng thành công trong tích tắc</p>
          </div>
          <div className="steps-grid">
            {steps.map((s, i) => (
              <div className="step-card" key={i}>
                <div className="step-num">{s.num}</div>
                <div className="step-icon">{s.icon}</div>
                <h3>{s.title}</h3>
                <p>{s.desc}</p>
                {i < steps.length - 1 && <div className="step-connector">→</div>}
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ======= CTA ======= */}
      <section className="cta-section">
        <div className="cta-content">
          <div className="cta-bolt">⚡</div>
          <h2>Sẵn Sàng Trải Nghiệm?</h2>
          <p>Đăng ký ngay hôm nay và nhận ưu đãi giao hàng miễn phí lần đầu!</p>
          <Link to="/register" className="btn btn-primary btn-lg">
            ⚡ Đăng Ký Miễn Phí
          </Link>
        </div>
      </section>

      {/* ======= FOOTER ======= */}
      <footer className="landing-footer">
        <div className="footer-inner">
          <div className="footer-brand">
            <span className="footer-brand-icon">⚡</span>
            <span className="footer-brand-name">SONIC</span>
            <p>Dịch vụ giao hàng siêu tốc</p>
          </div>
          <div className="footer-links">
            <Link to="/login">Đăng nhập</Link>
            <Link to="/register">Đăng ký</Link>
            <Link to="/tracking">Tra cứu</Link>
          </div>
        </div>
        <div className="footer-bottom">
          © 2026 SonicExpress. Tất cả quyền được bảo lưu.
        </div>
      </footer>
    </div>
  );
}

/* =============================================
   DASHBOARD - Logged In User
   ============================================= */
function Dashboard({ user }) {
  const getMenuItems = () => {
    const items = [];

    if (['Admin', 'Staff', 'Sender'].includes(user.role)) {
      items.push({
        to: '/orders/create',
        icon: '📦',
        title: 'Lên Đơn',
        desc: 'Tạo vận đơn siêu tốc',
        color: '#F59E0B',
        gradient: 'linear-gradient(135deg, #F59E0B, #D97706)',
      });
    }

    items.push({
      to: user.role === 'Shipper' ? '/shipper/orders' : '/orders',
      icon: user.role === 'Shipper' ? '🚚' : '📋',
      title: user.role === 'Shipper' ? 'Đơn Cần Giao' : 'Đơn Hàng',
      desc: user.role === 'Shipper' ? 'Danh sách đơn được gán cho bạn' : 'Quản lý & theo dõi đơn hàng',
      color: '#3B82F6',
      gradient: 'linear-gradient(135deg, #3B82F6, #1D4ED8)',
    });

    items.push({
      to: '/tracking',
      icon: '🔍',
      title: 'Tra Cứu',
      desc: 'Theo dõi hàng hóa realtime',
      color: '#8B5CF6',
      gradient: 'linear-gradient(135deg, #8B5CF6, #7C3AED)',
    });

    if (['Admin', 'Staff'].includes(user.role)) {
      items.push({
        to: '/admin/users',
        icon: '👥',
        title: 'Quản Lý',
        desc: 'Phê duyệt & quản lý thành viên',
        color: '#10B981',
        gradient: 'linear-gradient(135deg, #10B981, #059669)',
      });
      items.push({
        to: '/reports',
        icon: '📊',
        title: 'Thống Kê',
        desc: 'Biểu đồ doanh thu & hiệu suất',
        color: '#EC4899',
        gradient: 'linear-gradient(135deg, #EC4899, #DB2777)',
      });
    }

    items.push({
      to: '/chat',
      icon: '💬',
      title: 'Nhắn Tin',
      desc: 'Liên hệ & hỗ trợ',
      color: '#06B6D4',
      gradient: 'linear-gradient(135deg, #06B6D4, #0891B2)',
    });

    return items;
  };

  const menuItems = getMenuItems();

  const roleLabel = {
    Admin: 'Quản trị viên',
    Staff: 'Nhân viên',
    Shipper: 'Shipper',
    Sender: 'Người gửi',
    Receiver: 'Người nhận',
  };

  return (
    <div className="dashboard">
      <div className="dashboard-container">
        {/* Welcome Banner */}
        <div className="welcome-banner">
          <div className="welcome-left">
            <div className="welcome-avatar">
              {user.avatar ? (
                <img
                  src={`${import.meta.env.VITE_API_URL || 'http://localhost:5170'}${user.avatar}`}
                  alt=""
                  className="user-avatar"
                />
              ) : (
                <div className="user-avatar-placeholder" style={{ width: 64, height: 64, fontSize: '1.5rem' }}>
                  {user.username?.charAt(0).toUpperCase()}
                </div>
              )}
            </div>
            <div className="welcome-info">
              <h1>Chào Mừng, {user.fullName || user.username}! 👋</h1>
              <p>Chúc bạn một ngày giao hàng hiệu quả!</p>
            </div>
          </div>
          <div className="welcome-right">
            <div className="welcome-badge">
              <span className={`badge badge-${user.role?.toLowerCase()}`} style={{ fontSize: '1rem', padding: '0.5rem 1.25rem' }}>
                ⚡ {roleLabel[user.role] || user.role}
              </span>
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="dashboard-section">
          <h2 className="section-title">Thao Tác Nhanh</h2>
          <div className="menu-grid">
            {menuItems.map((item, i) => (
              <Link key={i} to={item.to} className="menu-card" style={{ '--card-color': item.color, '--card-gradient': item.gradient }}>
                <div className="menu-card-icon">{item.icon}</div>
                <div className="menu-card-info">
                  <h3>{item.title}</h3>
                  <p>{item.desc}</p>
                </div>
                <div className="menu-card-arrow">→</div>
              </Link>
            ))}
          </div>
        </div>

        {/* Info Cards */}
        <div className="dashboard-section">
          <h2 className="section-title">Thông Tin Hệ Thống</h2>
          <div className="info-grid">
            <div className="info-card">
              <div className="info-icon">⚡</div>
              <div className="info-value">24/7</div>
              <div className="info-label">Hỗ trợ liên tục</div>
            </div>
            <div className="info-card">
              <div className="info-icon">🚚</div>
              <div className="info-value">2h</div>
              <div className="info-label">Giao nhanh nội thành</div>
            </div>
            <div className="info-card">
              <div className="info-icon">🔒</div>
              <div className="info-value">100%</div>
              <div className="info-label">Bảo mật thanh toán</div>
            </div>
            <div className="info-card">
              <div className="info-icon">📱</div>
              <div className="info-value">Dễ</div>
              <div className="info-label">Sử dụng trên mọi thiết bị</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
