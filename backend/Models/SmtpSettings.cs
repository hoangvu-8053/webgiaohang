namespace webgiaohang.Models
{
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string User { get; set; } = string.Empty;
        public string Pass { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
    }
}






