

using EDUSphereSharedProject.UniversalModels;
using IgnisEducationSuite.Components.Account.Pages.Manage;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace IgnisEducationSuite.ServerServices;
public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public async Task SendEmailAsync(EmailRequest email)
    {
        try
        {
            // Get email settings from environment variables
            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
            var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT"));
            var smtpUsername = Environment.GetEnvironmentVariable("SMTP_EMAIL_IGNIS");
            var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD_IGNIS");

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                throw new InvalidOperationException("SMTP settings are not configured properly.");
            }

            // Create the email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Ignis Education Suite", smtpUsername));
            message.To.Add(MailboxAddress.Parse($"{email.To}"));
            message.Subject = email.Subject;
            message.Body = new TextPart("html")
            {
                Text = email.Body
            };

            // Connect to the SMTP server and send the email
            using var client = new SmtpClient();

            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            var _ = ex.Message;
            throw;
        }

    }
    public async Task SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetPassword, string Username, string StudentID)
    {
        var debugMode = true; // Or _env.IsDevelopment()

        var recipientEmail = debugMode
            ? "gelebik929@jparksky.com"  // temp inbox for all OTPs
            : toEmail;
        var senderName = _configuration["EmailSettings:SenderName"];
        var year = DateTime.Now.Year.ToString();
        var emailBody = string.Empty;
        if (string.IsNullOrEmpty(Username))
        {
            emailBody = GeneratePasswordResetEmailBody(recipientName, resetPassword, senderName, year);
        }
        else
        {
            emailBody = GeneratePasswordFirstResetEmailBody(recipientName, resetPassword, senderName, year, Username, StudentID);
        }

        var emailRequest = new EmailRequest
        {
            To = recipientEmail,
            Subject = "Your Password Has Been Reset",
            Body = emailBody,
            IsHtml = true
        };

        await SendEmailAsync(emailRequest);
    }

    public async Task SendReportCardEmail(byte[] pdfBytes, string recipientEmail)
    {
        // Get email settings from environment variables
        var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
        var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT"));
        var smtpUsername = Environment.GetEnvironmentVariable("SMTP_EMAIL_IGNIS");
        var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD_IGNIS");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Ignis Education Suite", smtpUsername));
        message.To.Add(new MailboxAddress("", recipientEmail));
        message.Subject = "Student Report Card";

        // Email body
        var builder = new BodyBuilder
        {
            TextBody = "Dear Parent/Guardian,\n\nPlease find attached the report card for the student. If you have any questions, feel free to contact us.\n\nBest regards,\nSchool Administration"
        };

        // Attach PDF
        builder.Attachments.Add("ReportCard.pdf", pdfBytes, new ContentType("application", "pdf"));

        message.Body = builder.ToMessageBody();

        // Send email
        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
            throw;
        }
    }

    #region HTML Generation
    public string GeneratePasswordResetEmailBody(string recipientName, string resetPassword, string senderName, string year)
    {
        // Define the email body template with placeholders
        string emailBody = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Reset</title>
    <style>
        body {
            margin: 0;
            padding: 0;
            font-family: Arial, sans-serif;
            background-color: #f4f4f4;
        }
        .email-container {
            width: 100%;
            max-width: 600px;
            margin: 0 auto;
            background-color: #fff;
        }
        .email-header {
            padding: 20px;
            background-color: #29104A; /* Customize the header background */
            color: white;
            text-align: center;
        }
        .email-header img {
            width: 150px;
            margin-bottom: 20px;
        }
        .email-header h1 {
            font-size: 24px;
            font-weight: bold;
        }
        .email-body {
            padding: 20px;
            background-color: #ffffff;
        }
        .email-body p {
            font-size: 16px;
            line-height: 1.5;
            color: #333;
        }
        .email-body a {
            color: #4CAF50;
            font-weight: bold;
            text-decoration: none;
        }
        .email-body ul {
            list-style-type: disc;
            margin-left: 20px;
        }
        .email-footer {
            padding: 20px;
            background-color: #29104A;
            text-align: center;
            font-size: 12px;
            color: #FFFFFF;
        }
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""email-header"">
            <img src=""images/MainLogo.png"" alt=""Logo"">
            <h1>Password Reset Confirmation</h1>
        </div>
        <div class=""email-body"">
            <p>Hi {recipientName},</p>
            <p>Your Ignis Education Suite password has been reset successfully. Your One-Time Password (OTP) is: <strong>{resetPassword}</strong></p>
            <p>You can use this password to login and create a new password. Please note the following guidelines when creating your new password:</p>
            <ul>
                <li>Must be at least 8 characters long</li>
                <li>Contain at least one uppercase letter</li>
                <li>Contain at least one number</li>
                <li>Contain at least one special character (e.g., !@#$%^&*)</li>
            </ul>
            <p>Once you have logged in with your OTP, you will be prompted to set your new password.</p>
            <p>If you did not request this reset, please contact support immediately.</p>
            <p>Best regards,</p>
            <p>{senderName} Team</p>
        </div>
        <div class=""email-footer"">
            <p>&copy; {year} {senderName}. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        // Replace the placeholders with actual values
        emailBody = emailBody.Replace("{recipientName}", recipientName)
                             .Replace("{resetPassword}", resetPassword)
                             .Replace("{senderName}", senderName)
                             .Replace("{year}", year);

        return emailBody;
    }

    public string GeneratePasswordFirstResetEmailBody(string recipientName, string resetPassword, string senderName, string year, string Username, string StudentID)
    {
        // Define the email body template with placeholders
        string emailBody = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Reset</title>
    <style>
        body {
            margin: 0;
            padding: 0;
            font-family: Arial, sans-serif;
            background-color: #f4f4f4;
        }
        .email-container {
            width: 100%;
            max-width: 600px;
            margin: 0 auto;
            background-color: #fff;
        }
        .email-header {
            padding: 20px;
            background-color: #29104A; /* Customize the header background */
            color: white;
            text-align: center;
        }
        .email-header img {
            width: 150px;
            margin-bottom: 20px;
        }
        .email-header h1 {
            font-size: 24px;
            font-weight: bold;
        }
        .email-body {
            padding: 20px;
            background-color: #ffffff;
        }
        .email-body p {
            font-size: 16px;
            line-height: 1.5;
            color: #333;
        }
        .email-body a {
            color: #4CAF50;
            font-weight: bold;
            text-decoration: none;
        }
        .email-body ul {
            list-style-type: disc;
            margin-left: 20px;
        }
        .email-footer {
            padding: 20px;
            background-color: #29104A;
            text-align: center;
            font-size: 12px;
            color: #FFFFFF;
        }
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""email-header"">
           <img src=""https://drive.google.com/uc?export=view&id=1gCnSyOyB5VkqRInm54aiXnsJHlIHgEJd"" alt=""Logo"">
            <h1>Password Reset Confirmation</h1>
        </div>
        <div class=""email-body"">
            <p>Hi {recipientName},</p>
            <p>Welcome to Ignis Education Suite</p>
            <p>Your UserName is: <strong>{Username}</strong>,</p>
            {StudentIDPlaceholder}
            <p>Your Ignis Education Suite password has been reset successfully. Your One-Time Password (OTP) is: <strong>{resetPassword}</strong></p>
            <p>You can use this password to login and create a new password. Please note the following guidelines when creating your new password:</p>
            <ul>
                <li>Must be at least 8 characters long</li>
                <li>Contain at least one uppercase letter</li>
                <li>Contain at least one number</li>
                <li>Contain at least one special character (e.g., !@#$%^&*)</li>
            </ul>
            <p>Once you have logged in with your OTP, you will be prompted to set your new password.</p>
            <p>If you did not request this reset, please contact support immediately.</p>

            <p>Welcome to the Ignis Education Suite family! We're excited to have you on board. Our system is designed to make teaching, learning, and managing education simpler and more enjoyable. Dive in, explore the features, and let us help you ignite the spark of learning in your institution. If you need assistance, our team is always here to support you!</p>

            <p>Best regards,</p>
            <p>{senderName} Team</p>
        </div>
        <div class=""email-footer"">
            <p>&copy; {year} {senderName}. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        // Conditionally add Student ID information
        string studentIDMessage = !string.IsNullOrEmpty(StudentID) && StudentID != "N/A"
    ? $"<p>Your Student ID is: <strong>{StudentID}</strong></p>"
    : "";


        // Replace placeholders with actual values
        emailBody = emailBody.Replace("{recipientName}", recipientName)
                             .Replace("{resetPassword}", resetPassword)
                             .Replace("{senderName}", senderName)
                             .Replace("{year}", year)
                             .Replace("{Username}", Username)
                             .Replace("{StudentIDPlaceholder}", studentIDMessage);

        return emailBody;
    }

    #endregion
}


