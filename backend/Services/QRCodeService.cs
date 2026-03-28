using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text;
using System.Collections.Generic;

namespace webgiaohang.Services
{
    public interface IQRCodeService
    {
        string GenerateQRCodeImage(string data, int size = 300);
        string GenerateVietQRCode(string accountNumber, string accountName, decimal amount, string content = "");
        string GenerateMoMoQRCode(string phoneNumber, string accountName, decimal amount, string content = "");
    }

    [SupportedOSPlatform("windows")]
    public class QRCodeService : IQRCodeService
    {
        private readonly IWebHostEnvironment _environment;

        public QRCodeService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [SupportedOSPlatform("windows")]
        public string GenerateQRCodeImage(string data, int size = 300)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    using (Bitmap qrBitmap = qrCode.GetGraphic(20))
                    {
                        // Resize to desired size
                        using (Bitmap resized = new Bitmap(qrBitmap, new Size(size, size)))
                        {
                            var fileName = $"qr_{DateTime.Now.Ticks}.png";
                            var filePath = Path.Combine(_environment.WebRootPath, "qr-codes", fileName);
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                            resized.Save(filePath, ImageFormat.Png);
                            return $"/qr-codes/{fileName}";
                        }
                    }
                }
            }
        }

        public string GenerateVietQRCode(string accountNumber, string accountName, decimal amount, string content = "")
        {
            // Tạo VietQR code theo chuẩn EMV QR Code
            // Format: 00020101021238[MerchantInfo]52[Category]53[Currency]54[Amount]58[Country]62[Additional]63[CRC]
            
            var builder = new StringBuilder();
            
            // 00: Payload Format Indicator (01 = EMV QR Code)
            builder.Append("000201");
            
            // 01: Point of Initiation Method (12 = Dynamic - có thể thay đổi số tiền)
            builder.Append("010212");
            
            // 38: Merchant Account Information
            var merchantInfo = new StringBuilder();
            merchantInfo.Append("0010A000000727"); // GUID cho VietQR
            
            // 01: Account Number (số tài khoản) - độ dài tính theo byte
            var accNumBytes = Encoding.UTF8.GetBytes(accountNumber);
            merchantInfo.Append($"01{accNumBytes.Length:D2}{accountNumber}");
            
            // 02: Account Name (tên chủ tài khoản) - độ dài tính theo byte
            var accNameBytes = Encoding.UTF8.GetBytes(accountName);
            merchantInfo.Append($"02{accNameBytes.Length:D2}{accountName}");
            
            var merchantInfoStr = merchantInfo.ToString();
            var merchantInfoBytes = Encoding.UTF8.GetBytes(merchantInfoStr);
            builder.Append($"38{merchantInfoBytes.Length:D2}{merchantInfoStr}");
            
            // 52: Merchant Category Code (0000 = Default)
            builder.Append("52040000");
            
            // 53: Transaction Currency (704 = VND)
            builder.Append("5303704");
            
            // 54: Transaction Amount
            var amountStr = ((long)amount).ToString();
            builder.Append($"54{amountStr.Length:D2}{amountStr}");
            
            // 58: Country Code (VN)
            builder.Append("5802VN");
            
            // 62: Additional Data Field Template
            if (!string.IsNullOrEmpty(content))
            {
                var contentBytes = Encoding.UTF8.GetBytes(content);
                var additionalData = $"08{contentBytes.Length:D2}{content}";
                var additionalDataByteLen = Encoding.UTF8.GetByteCount(additionalData);
                builder.Append($"62{additionalDataByteLen:D2}{additionalData}");
            }
            
            // 63: CRC16 - tính toán CRC cho toàn bộ payload (trước khi thêm CRC)
            var payload = builder.ToString();
            var crc = CalculateCRC16(payload);
            builder.Append($"6304{crc}");
            
            return GenerateQRCodeImage(builder.ToString(), 400);
        }
        
        private string CalculateCRC16(string data)
        {
            // Convert string to bytes (UTF-8 encoding)
            var bytes = Encoding.UTF8.GetBytes(data);
            
            // CRC-16/CCITT-FALSE algorithm
            ushort crc = 0xFFFF;
            foreach (var b in bytes)
            {
                crc ^= (ushort)(b << 8);
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }
            return crc.ToString("X4");
        }

        [SupportedOSPlatform("windows")]
        public string GenerateMoMoQRCode(string phoneNumber, string accountName, decimal amount, string content = "")
        {
            // MoMo sử dụng VietQR code format (EMV QR Code)
            // Tạo VietQR code với số điện thoại MoMo làm account number
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return GenerateVietQRCode(accountName, accountName, amount, content);
            }
            else
            {
                // Tạo VietQR code với số điện thoại MoMo làm account number
                return GenerateVietQRCode(phoneNumber, accountName, amount, content);
            }
        }
    }
}
