namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 邮件配置
/// </summary>
public class MailConfigDto
{
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string Encryption { get; set; } = "tls";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool NotifyOnRegister { get; set; }
    public string AdminEmail { get; set; } = "";
    public string EmailSignature { get; set; } = "";
}
