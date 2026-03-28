import api from './client';

export const authApi = {
  login: (data) => api.post('/api/AccountApi/login', data),
  register: (data) => api.post('/api/AccountApi/register', data),
  logout: () => api.post('/api/AccountApi/logout'),
  me: () => api.get('/api/AccountApi/me'),
  updateProfile: (data) => api.put('/api/AccountApi/profile', data, { headers: { 'Content-Type': 'multipart/form-data' } }),
  changePassword: (data) => api.post('/api/AccountApi/change-password', data),
  forgotPassword: (data) => api.post('/api/AccountApi/forgot-password', data),
  resetPassword: (data) => api.post('/api/AccountApi/reset-password', data),
};

export const ordersApi = {
  getAll: (params) => api.get('/api/OrderApi', { params }),
  getById: (id) => api.get(`/api/OrderApi/${id}`),
  create: (data) => api.post('/api/OrderApi', data, { headers: { 'Content-Type': 'multipart/form-data' } }),
  update: (id, data) => api.put(`/api/OrderApi/${id}`, data),
  delete: (id) => api.delete(`/api/OrderApi/${id}`),
  assignShipper: (orderId, shipperName) => api.post(`/api/OrderApi/${orderId}/assign`, { shipperName }),
  updateStatus: (orderId, status) => api.post(`/api/OrderApi/${orderId}/status`, { status }),
  myOrders: () => api.get('/api/OrderApi/my-orders'),
  shipperOrders: () => api.get('/api/ShipperApi/orders'),
};

export const adminApi = {
  getUsers: () => api.get('/api/AdminApi/users'),
  getShippers: () => api.get('/api/AdminApi/shippers'),
  approveUser: (id) => api.post(`/api/AdminApi/users/${id}/approve`),
  deleteUser: (id) => api.delete(`/api/AdminApi/users/${id}`),
  setRole: (id, role) => api.put(`/api/AdminApi/users/${id}/role`, { role }),
  createUser: (data) => api.post('/api/AdminApi/users', data),
  getDashboard: () => api.get('/api/AdminApi/dashboard'),
};

export const notificationApi = {
  getAll: () => api.get('/api/NotificationApi'),
  markRead: (id) => api.post(`/api/NotificationApi/${id}/read`),
  markAllRead: () => api.post('/api/NotificationApi/read-all'),
  getUnreadCount: () => api.get('/api/NotificationApi/unread-count'),
};

export const chatApi = {
  getConversations: () => api.get('/api/ChatApi/conversations'),
  getMessages: (otherUser, orderId) => api.get('/api/ChatApi/messages', { params: { otherUsername: otherUser, orderId } }),
  sendMessage: (data) => api.post('/api/ChatApi/send', data),
  getUsers: () => api.get('/api/ChatApi/users'),
};

export const trackingApi = {
  getByTracking: (trackingNumber) => api.get(`/api/TrackingApi/${trackingNumber}`),
  getOrderMap: (orderId) => api.get(`/api/TrackingApi/map/${orderId}`),
};

export const reportApi = {
  getSummary: (params) => api.get('/api/ReportApi/summary', { params }),
  getRevenue: (params) => api.get('/api/ReportApi/revenue', { params }),
};

export const shipperApi = {
  login: (data) => api.post('/api/ShipperApi/login', data),
  getOrders: (status) => api.get('/api/ShipperApi/orders', { params: { status } }),
  getOrder: (id) => api.get(`/api/ShipperApi/orders/${id}`),
  updateStatus: (id, data) => api.post(`/api/ShipperApi/orders/${id}/status`, data),
  updateLocation: (id, data) => api.post(`/api/ShipperApi/orders/${id}/location`, data),
  getProfile: () => api.get('/api/ShipperApi/profile'),
};

export const shipperPaymentApi = {
  getAll: () => api.get('/api/ShipperPaymentApi'),
  getById: (id) => api.get(`/api/ShipperPaymentApi/${id}`),
  markPaid: (id, data) => api.post(`/api/ShipperPaymentApi/${id}/pay`, data),
};

export const paymentApi = {
  create: (data) => api.post('/api/PaymentApi', data),
  getByOrder: (orderId) => api.get(`/api/PaymentApi/order/${orderId}`),
  process: (paymentId, data) => api.post(`/api/PaymentApi/${paymentId}/process`, data),
  confirmCash: (paymentId) => api.post(`/api/PaymentApi/${paymentId}/confirm`),
  refund: (paymentId, data) => api.post(`/api/PaymentApi/${paymentId}/refund`, data),
  getHistory: (params) => api.get('/api/PaymentApi/history', { params }),
  getQRCode: (paymentId) => api.get(`/api/PaymentApi/${paymentId}/qrcode`),
  getBankInfo: () => api.get('/api/PaymentApi/bank-info'),
  getAll: (params) => api.get('/api/PaymentApi/all', { params }),
  uploadProof: (paymentId, file) => {
    const formData = new FormData();
    formData.append('file', file);
    return api.post(`/api/PaymentApi/${paymentId}/upload-proof`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },
  getStats: () => api.get('/api/PaymentApi/stats'),
};

export const liveMapApi = {
  getOrderMapInfo: (orderId) => api.get(`/api/LiveMapApi/order/${orderId}`),
  updateLocation: (data) => api.post('/api/LiveMapApi/shipper/location', data),
  getActiveShippers: () => api.get('/api/LiveMapApi/shippers/active'),
  track: (orderId) => api.get(`/api/LiveMapApi/track/${orderId}`),
  getRoute: (orderId) => api.get(`/api/LiveMapApi/route/${orderId}`),
  subscribe: (orderId) => api.post(`/api/LiveMapApi/subscribe/${orderId}`),
  getAllOrders: () => api.get('/api/LiveMapApi/all-orders'),
};
