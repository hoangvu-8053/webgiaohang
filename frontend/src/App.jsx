import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import Navbar from './components/Navbar';

import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import HomePage from './pages/HomePage';
import OrdersPage from './pages/OrdersPage';
import CreateOrderPage from './pages/CreateOrderPage';
import ProfilePage from './pages/ProfilePage';
import UnauthorizedPage from './pages/UnauthorizedPage';
import ReportsPage from './pages/ReportsPage';
import AdminUsersPage from './pages/AdminUsersPage';
import OrderDetailPage from './pages/OrderDetailPage';
import ShipperOrdersPage from './pages/ShipperOrdersPage';
import NotificationsPage from './pages/NotificationsPage';
import ChatPage from './pages/ChatPage';
import TrackingPage from './pages/TrackingPage';
import PaymentPage from './pages/PaymentPage';
import LiveMapPage from './pages/LiveMapPage';

function Layout({ children }) {
  return (
    <div className="app-layout">
      <Navbar />
      <main className="main-content">{children}</main>
      <footer className="app-footer">
        <p>© 2026 Giao Hàng Sonic - Giải pháp giao vận hàng đầu</p>
      </footer>
    </div>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Public - Home page is public, shows landing for guests, dashboard for logged-in users */}
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/tracking" element={<TrackingPage />} />
          <Route path="/unauthorized" element={<UnauthorizedPage />} />
          <Route path="/profile" element={<ProtectedRoute><Layout><ProfilePage /></Layout></ProtectedRoute>} />
          <Route path="/notifications" element={<ProtectedRoute><Layout><NotificationsPage /></Layout></ProtectedRoute>} />
          <Route path="/chat" element={<ProtectedRoute><Layout><ChatPage /></Layout></ProtectedRoute>} />

          {/* Orders - Admin, Staff, Sender, Receiver */}
          <Route path="/orders" element={<ProtectedRoute roles={['Admin','Staff','Sender','Receiver','Shipper']}><Layout><OrdersPage /></Layout></ProtectedRoute>} />
          <Route path="/orders/create" element={<ProtectedRoute roles={['Admin','Staff','Sender']}><Layout><CreateOrderPage /></Layout></ProtectedRoute>} />
          <Route path="/orders/:id" element={<ProtectedRoute roles={['Admin','Staff','Sender','Receiver','Shipper']}><Layout><OrderDetailPage /></Layout></ProtectedRoute>} />
          <Route path="/orders/:orderId/payment" element={<ProtectedRoute roles={['Sender','Receiver']}><Layout><PaymentPage /></Layout></ProtectedRoute>} />
          <Route path="/orders/:orderId/live-map" element={<ProtectedRoute roles={['Admin','Staff','Sender','Receiver','Shipper']}><Layout><LiveMapPage /></Layout></ProtectedRoute>} />

          {/* Shipper */}
          <Route path="/shipper/orders" element={<ProtectedRoute roles={['Shipper']}><Layout><ShipperOrdersPage /></Layout></ProtectedRoute>} />

          {/* Admin */}
          <Route path="/admin/users" element={<ProtectedRoute roles={['Admin','Staff']}><Layout><AdminUsersPage /></Layout></ProtectedRoute>} />
          <Route path="/reports" element={<ProtectedRoute roles={['Admin','Staff']}><Layout><ReportsPage /></Layout></ProtectedRoute>} />

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
