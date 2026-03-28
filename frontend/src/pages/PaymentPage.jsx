import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ordersApi, paymentApi } from '../api/endpoints';
import { useAuth } from '../context/AuthContext';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5170';

export default function PaymentPage() {
  const { orderId } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();

  const [order, setOrder] = useState(null);
  const [payment, setPayment] = useState(null);
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState(false);
  const [selectedMethod, setSelectedMethod] = useState('Cash');
  const [qrCodeUrl, setQrCodeUrl] = useState('');
  const [showQR, setShowQR] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [bankInfo, setBankInfo] = useState(null);
  const [proofFile, setProofFile] = useState(null);

  useEffect(() => {
    fetchData();
    fetchBankInfo();
  }, [orderId]);

  const fetchData = async () => {
    setLoading(true);
    try {
      const orderRes = await ordersApi.getById(orderId);
      if (orderRes.data.success) {
        setOrder(orderRes.data.order);

        // Nếu đơn đang ở trạng thái Pending, chưa giao thì chỉ có thể thanh toán trước
        // Nếu đơn đang ở trạng thái Shipping hoặc Delivered, có thể là COD đã thu tiền
        if (orderRes.data.order.status === 'Pending') {
          // Đơn chưa giao - chỉ có thể thanh toán trước (Bank/MoMo)
          setSelectedMethod(orderRes.data.order.paymentMethod || 'Bank Transfer');
        }
      }

      try {
        const paymentRes = await paymentApi.getByOrder(orderId);
        if (paymentRes.data.success) {
          setPayment(paymentRes.data.payment);
          setSelectedMethod(paymentRes.data.payment.paymentMethod || 'Cash');
        }
      } catch (e) {
        // Chưa có thanh toán
      }
    } catch (err) {
      setError('Không thể tải thông tin đơn hàng');
    } finally {
      setLoading(false);
    }
  };

  const fetchBankInfo = async () => {
    try {
      const res = await paymentApi.getBankInfo();
      if (res.data.success) {
        setBankInfo(res.data.bankInfo);
      }
    } catch (e) {
      console.error(e);
    }
  };

  const handleCreatePayment = async () => {
    if (!order) return;
    setProcessing(true);
    setError('');
    setSuccess('');

    try {
      const res = await paymentApi.create({
        orderId: parseInt(orderId),
        paymentMethod: selectedMethod,
        amount: order.totalAmount
      });

      if (res.data.success) {
        setPayment(res.data.payment);
        setSuccess('Đã tạo yêu cầu thanh toán!');

        if (selectedMethod === 'Bank Transfer' || selectedMethod === 'MoMo') {
          fetchQRCode(res.data.payment.id);
        }
      }
    } catch (err) {
      setError(err.response?.data?.message || 'Lỗi khi tạo thanh toán');
    } finally {
      setProcessing(false);
    }
  };

  // Xác nhận đã chuyển khoản (sau khi quét QR)
  const handleConfirmTransfer = async () => {
    if (!payment || !proofFile) {
      setError('Vui lòng tải lên biên lai/chứng từ chuyển khoản');
      return;
    }
    setProcessing(true);
    setError('');

    try {
      // Upload proof first
      await paymentApi.uploadProof(payment.id, proofFile);

      // Notify admin (in real app, this would create a notification)
      setSuccess('Đã gửi biên lai. Vui lòng chờ Admin xác nhận!');
      setProofFile(null);

      // Refresh payment status
      fetchData();
    } catch (err) {
      setError(err.response?.data?.message || 'Lỗi khi gửi biên lai');
    } finally {
      setProcessing(false);
    }
  };

  // Xác nhận thanh toán COD đã thu tiền (chỉ Admin/Shipper mới làm được)
  const handleConfirmCash = async () => {
    if (!payment) return;
    setProcessing(true);
    setError('');

    try {
      const res = await paymentApi.confirmCash(payment.id);
      if (res.data.success) {
        setPayment({ ...payment, status: 'Completed' });
        setSuccess('Xác nhận thu tiền COD thành công!');
      }
    } catch (err) {
      setError(err.response?.data?.message || 'Lỗi khi xác nhận');
    } finally {
      setProcessing(false);
    }
  };

  const fetchQRCode = async (paymentId) => {
    try {
      const res = await paymentApi.getQRCode(paymentId);
      if (res.data.success) {
        setQrCodeUrl(`${API_URL}${res.data.qrCodeUrl}`);
        setShowQR(true);
      }
    } catch (err) {
      setError('Không thể tạo mã QR');
    }
  };

  const handleShowQR = async () => {
    if (!payment) {
      await handleCreatePayment();
    } else {
      await fetchQRCode(payment.id);
    }
  };

  const handleProofUpload = (e) => {
    const file = e.target.files[0];
    if (file && file.size <= 5 * 1024 * 1024) {
      setProofFile(file);
    } else {
      setError('File quá lớn (tối đa 5MB)');
    }
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '3rem' }}>
        <div className="spinner" style={{ margin: '0 auto' }}></div>
        <p style={{ marginTop: '1rem', color: 'var(--text-muted)' }}>Đang tải...</p>
      </div>
    );
  }

  if (!order) {
    return (
      <div style={{ textAlign: 'center', padding: '3rem' }}>
        <p style={{ color: 'var(--error)' }}>Không tìm thấy đơn hàng</p>
        <button className="btn btn-primary" onClick={() => navigate('/orders')} style={{ marginTop: '1rem' }}>
          Quay lại danh sách
        </button>
      </div>
    );
  }

  const paymentStatus = payment?.status;
  const statusConfig = {
    Pending: { color: '#f59e0b', bg: '#fffbeb', label: 'Chờ thanh toán' },
    Completed: { color: '#10b981', bg: '#ecfdf5', label: 'Đã thanh toán' },
    Failed: { color: '#ef4444', bg: '#fef2f2', label: 'Thanh toán thất bại' },
    Refunded: { color: '#6b7280', bg: '#f3f4f6', label: 'Đã hoàn tiền' }
  };

  // Chỉ cho phép thanh toán khi đơn chưa giao
  const canPay = order.status === 'Pending' || order.status === 'Shipping';
  const isAlreadyPaid = paymentStatus === 'Completed';
  const isAdmin = user?.role === 'Admin' || user?.role === 'Staff';

  return (
    <div className="animate-fade" style={{ maxWidth: '800px', margin: '0 auto' }}>
      <button className="btn" onClick={() => navigate(-1)} style={{ marginBottom: '2rem', background: '#f8fafc' }}>
        ← Quay lại
      </button>

      <div className="card" style={{ padding: '2.5rem' }}>
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 900, color: 'var(--primary)' }}>
            THANH TOÁN ĐƠN HÀNG
          </h1>
          <p style={{ color: '#94a3b8', marginTop: '0.5rem' }}>
            Mã vận đơn: <strong style={{ color: 'var(--primary)' }}>#{order.trackingNumber}</strong>
          </p>
          <div style={{ marginTop: '0.5rem' }}>
            <span style={{
              background: order.status === 'Pending' ? '#fffbeb' : order.status === 'Shipping' ? '#dbeafe' : '#ecfdf5',
              color: order.status === 'Pending' ? '#f59e0b' : order.status === 'Shipping' ? '#3b82f6' : '#10b981',
              padding: '0.25rem 1rem',
              borderRadius: '999px',
              fontSize: '0.85rem',
              fontWeight: 700
            }}>
              Trạng thái đơn: {order.status === 'Pending' ? 'Chờ xử lý' : order.status === 'Shipping' ? 'Đang giao' : 'Đã giao'}
            </span>
          </div>
        </div>

        {/* Thông tin đơn hàng */}
        <div style={{ background: '#f8fafc', borderRadius: '16px', padding: '1.5rem', marginBottom: '2rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem' }}>
            <span style={{ color: '#64748b' }}>Tên hàng:</span>
            <strong>{order.product}</strong>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem' }}>
            <span style={{ color: '#64748b' }}>Giá sản phẩm:</span>
            <span>{order.price?.toLocaleString()} VND</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem' }}>
            <span style={{ color: '#64748b' }}>Phí vận chuyển:</span>
            <span>{order.shippingFee?.toLocaleString()} VND</span>
          </div>
          <hr style={{ margin: '1rem 0', borderColor: '#e2e8f0' }} />
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '1.25rem', fontWeight: 800 }}>
            <span>Tổng cộng:</span>
            <span style={{ color: '#10b981' }}>{order.totalAmount?.toLocaleString()} VND</span>
          </div>
        </div>

        {/* Trạng thái thanh toán hiện tại */}
        {payment && (
          <div style={{
            background: statusConfig[paymentStatus]?.bg || '#f3f4f6',
            color: statusConfig[paymentStatus]?.color || '#6b7280',
            padding: '1rem 1.5rem',
            borderRadius: '12px',
            marginBottom: '2rem',
            textAlign: 'center',
            fontWeight: 700
          }}>
            {statusConfig[paymentStatus]?.label || paymentStatus}
            {payment.ReceiptNumber && <span style={{ marginLeft: '1rem' }}>• Mã biên lai: {payment.ReceiptNumber}</span>}
          </div>
        )}

        {/* Thông báo */}
        {error && <div className="alert-error" style={{ marginBottom: '1.5rem' }}>{error}</div>}
        {success && (
          <div style={{ background: '#ecfdf5', color: '#10b981', padding: '1rem', borderRadius: '12px', marginBottom: '1.5rem', fontWeight: 600 }}>
            {success}
          </div>
        )}

        {/* Đơn đã thanh toán */}
        {isAlreadyPaid && (
          <div style={{ textAlign: 'center', padding: '2rem' }}>
            <div style={{ fontSize: '4rem', marginBottom: '1rem' }}>✅</div>
            <h2 style={{ color: '#10b981', marginBottom: '1rem' }}>Đơn hàng đã được thanh toán!</h2>
            <p style={{ color: '#64748b' }}>Cảm ơn bạn đã sử dụng dịch vụ Giao Hàng Sonic</p>
            <button className="btn btn-primary" onClick={() => navigate('/orders')} style={{ marginTop: '2rem' }}>
              Quay lại danh sách đơn hàng
            </button>
          </div>
        )}

        {/* Không thể thanh toán */}
        {!canPay && !isAlreadyPaid && (
          <div style={{ textAlign: 'center', padding: '2rem', background: '#fffbeb', borderRadius: '12px' }}>
            <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>⚠️</div>
            <h3 style={{ color: '#f59e0b', marginBottom: '1rem' }}>Đơn hàng đang được giao hoặc đã giao</h3>
            <p style={{ color: '#64748b' }}>
              {order.status === 'Shipping'
                ? 'Shipper đang giao hàng đến bạn. Vui lòng thanh toán trực tiếp cho shipper (COD).'
                : 'Đơn hàng đã được giao. Nếu chưa thanh toán, vui lòng liên hệ Admin.'}
            </p>
          </div>
        )}

        {/* Chọn phương thức thanh toán - chỉ khi đơn chưa thanh toán và có thể thanh toán */}
        {canPay && !payment && (
          <div style={{ marginBottom: '2rem' }}>
            <h3 style={{ fontSize: '1.1rem', fontWeight: 800, marginBottom: '1.5rem', color: 'var(--primary)' }}>
              CHỌN PHƯƠNG THỨC THANH TOÁN
            </h3>

            {/* Thông báo quan trọng */}
            <div style={{ background: '#fffbeb', padding: '1rem', borderRadius: '12px', marginBottom: '1.5rem', border: '1px solid #fcd34d' }}>
              <strong style={{ color: '#f59e0b' }}>💡 Lưu ý:</strong>
              <p style={{ color: '#92400e', marginTop: '0.5rem', fontSize: '0.9rem' }}>
                - <strong>COD (Tiền mặt)</strong>: Thanh toán trực tiếp cho shipper khi nhận hàng
                <br />
                - <strong>Chuyển khoản/MoMo</strong>: Thanh toán trước qua mã QR
              </p>
            </div>

            <div style={{ display: 'grid', gap: '1rem' }}>
              {/* Tiền mặt COD */}
              <div
                onClick={() => setSelectedMethod('Cash')}
                style={{
                  padding: '1.25rem',
                  borderRadius: '12px',
                  border: `2px solid ${selectedMethod === 'Cash' ? '#6366f1' : '#e2e8f0'}`,
                  background: selectedMethod === 'Cash' ? '#eef2ff' : 'white',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '1rem',
                  transition: 'all 0.2s'
                }}
              >
                <div style={{
                  width: '48px', height: '48px', borderRadius: '12px',
                  background: '#fef3c7', display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontSize: '1.5rem'
                }}>
                  💵
                </div>
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 800 }}>Tiền mặt (COD)</div>
                  <div style={{ fontSize: '0.85rem', color: '#64748b' }}>Thanh toán trực tiếp cho shipper khi nhận hàng</div>
                </div>
                <div style={{
                  width: '24px', height: '24px', borderRadius: '50%',
                  border: `2px solid ${selectedMethod === 'Cash' ? '#6366f1' : '#e2e8f0'}`,
                  display: 'flex', alignItems: 'center', justifyContent: 'center'
                }}>
                  {selectedMethod === 'Cash' && <div style={{ width: '12px', height: '12px', borderRadius: '50%', background: '#6366f1' }} />}
                </div>
              </div>

              {/* Chuyển khoản ngân hàng */}
              <div
                onClick={() => setSelectedMethod('Bank Transfer')}
                style={{
                  padding: '1.25rem',
                  borderRadius: '12px',
                  border: `2px solid ${selectedMethod === 'Bank Transfer' ? '#6366f1' : '#e2e8f0'}`,
                  background: selectedMethod === 'Bank Transfer' ? '#eef2ff' : 'white',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '1rem',
                  transition: 'all 0.2s'
                }}
              >
                <div style={{
                  width: '48px', height: '48px', borderRadius: '12px',
                  background: '#dbeafe', display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontSize: '1.5rem'
                }}>
                  🏦
                </div>
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 800 }}>Chuyển khoản ngân hàng</div>
                  <div style={{ fontSize: '0.85rem', color: '#64748b' }}>Quét mã QR VietQR - Thanh toán ngay</div>
                </div>
                <div style={{
                  width: '24px', height: '24px', borderRadius: '50%',
                  border: `2px solid ${selectedMethod === 'Bank Transfer' ? '#6366f1' : '#e2e8f0'}`,
                  display: 'flex', alignItems: 'center', justifyContent: 'center'
                }}>
                  {selectedMethod === 'Bank Transfer' && <div style={{ width: '12px', height: '12px', borderRadius: '50%', background: '#6366f1' }} />}
                </div>
              </div>

              {/* MoMo */}
              <div
                onClick={() => setSelectedMethod('MoMo')}
                style={{
                  padding: '1.25rem',
                  borderRadius: '12px',
                  border: `2px solid ${selectedMethod === 'MoMo' ? '#6366f1' : '#e2e8f0'}`,
                  background: selectedMethod === 'MoMo' ? '#eef2ff' : 'white',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '1rem',
                  transition: 'all 0.2s'
                }}
              >
                <div style={{
                  width: '48px', height: '48px', borderRadius: '12px',
                  background: '#fce7f3', display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontSize: '1.5rem'
                }}>
                  📱
                </div>
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 800 }}>Ví MoMo</div>
                  <div style={{ fontSize: '0.85rem', color: '#64748b' }}>Thanh toán qua ví điện tử MoMo</div>
                </div>
                <div style={{
                  width: '24px', height: '24px', borderRadius: '50%',
                  border: `2px solid ${selectedMethod === 'MoMo' ? '#6366f1' : '#e2e8f0'}`,
                  display: 'flex', alignItems: 'center', justifyContent: 'center'
                }}>
                  {selectedMethod === 'MoMo' && <div style={{ width: '12px', height: '12px', borderRadius: '50%', background: '#6366f1' }} />}
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Nút hành động */}
        {canPay && !payment && (
          <div style={{ display: 'flex', gap: '1rem' }}>
            {selectedMethod === 'Cash' ? (
              <button
                className="btn"
                style={{ flex: 1, padding: '1.25rem', background: '#fef3c7', color: '#92400e', border: '2px solid #fcd34d' }}
                onClick={() => {
                  alert('Đơn hàng COD sẽ được thanh toán khi shipper giao hàng đến bạn.\n\nKhi nhận hàng, vui lòng:\n1. Kiểm tra hàng hóa\n2. Thanh toán đủ số tiền cho shipper\n3. Yêu cầu shipper xác nhận đã thu tiền');
                }}
              >
                💵 TÔI ĐÃ HIỂU - THANH TOÁN KHI NHẬN HÀNG
              </button>
            ) : (
              <button
                className="btn btn-primary"
                onClick={handleShowQR}
                disabled={processing}
                style={{ flex: 1, padding: '1.25rem' }}
              >
                {processing ? 'Đang tạo mã...' : '📱 TIẾP TỤC ĐỂ THANH TOÁN'}
              </button>
            )}
          </div>
        )}

        {/* QR Code Modal */}
        {showQR && !isAlreadyPaid && (
          <div style={{ marginTop: '2rem', textAlign: 'center' }}>
            <div style={{ background: 'white', border: '2px solid #e2e8f0', borderRadius: '16px', padding: '2rem', display: 'inline-block' }}>
              <h3 style={{ marginBottom: '1.5rem', color: 'var(--primary)' }}>QUÉT MÃ QR ĐỂ THANH TOÁN</h3>
              {qrCodeUrl ? (
                <img src={qrCodeUrl} alt="QR Code" style={{ width: '280px', height: '280px', marginBottom: '1rem' }} />
              ) : (
                <div style={{ width: '280px', height: '280px', background: '#f3f4f6', margin: '0 auto 1rem', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <div className="spinner"></div>
                </div>
              )}
              <div style={{ fontSize: '1.5rem', fontWeight: 900, color: '#10b981', marginBottom: '1rem' }}>
                {order.totalAmount?.toLocaleString()} VND
              </div>
              {bankInfo && (
                <div style={{ textAlign: 'left', background: '#f8fafc', padding: '1rem', borderRadius: '8px', fontSize: '0.9rem', marginBottom: '1rem' }}>
                  <p><strong>Ngân hàng:</strong> {bankInfo.bankName}</p>
                  <p><strong>STK:</strong> {bankInfo.accountNumber}</p>
                  <p><strong>Tên:</strong> {bankInfo.accountName}</p>
                  <p style={{ color: '#6366f1', marginTop: '0.5rem' }}>
                    <strong>Nội dung:</strong> THANHTOAN#{orderId}
                  </p>
                </div>
              )}

              <hr style={{ margin: '1.5rem 0', borderColor: '#e2e8f0' }} />

              {/* Upload proof */}
              <h4 style={{ marginBottom: '1rem', color: '#64748b' }}>Sau khi chuyển khoản, tải lên biên lai:</h4>
              <input
                type="file"
                accept="image/*,.pdf"
                onChange={handleProofUpload}
                style={{ marginBottom: '1rem', fontSize: '0.9rem' }}
              />
              {proofFile && (
                <p style={{ color: '#10b981', fontSize: '0.85rem', marginBottom: '1rem' }}>
                  ✓ Đã chọn: {proofFile.name}
                </p>
              )}

              <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'center' }}>
                <button
                  className="btn btn-primary"
                  onClick={handleConfirmTransfer}
                  disabled={processing || !proofFile}
                  style={{ flex: 1 }}
                >
                  {processing ? 'Đang gửi...' : '✓ XÁC NHẬN ĐÃ CHUYỂN KHOẢN'}
                </button>
                <button className="btn" onClick={() => setShowQR(false)} style={{ background: '#f8fafc' }}>
                  Đóng
                </button>
              </div>

              <p style={{ marginTop: '1rem', color: '#94a3b8', fontSize: '0.8rem' }}>
                Admin sẽ xác nhận sau khi nhận được tiền
              </p>
            </div>
          </div>
        )}

        {/* Nút quay lại khi đã thanh toán */}
        {isAlreadyPaid && (
          <button className="btn btn-primary" onClick={() => navigate('/orders')} style={{ marginTop: '1rem', width: '100%' }}>
            Quay lại danh sách đơn hàng
          </button>
        )}
      </div>
    </div>
  );
}
