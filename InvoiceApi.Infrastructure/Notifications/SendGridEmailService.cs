using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace InvoiceApi.Infrastructure.Notifications;

public class SendGridEmailService : IEmailService
{
    private readonly SendGridClient _client;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(string apiKey, string fromEmail, string fromName = "Invoice System")
    {
        _client = new SendGridClient(apiKey);
        _fromEmail = fromEmail;
        _fromName = fromName;
    }

    public async Task<EmailResult> SendInvoiceAsync(Invoice invoice)
    {
        var subject = $"Invoice {invoice.InvoiceNumber} - Payment Due {invoice.DueDate:MMM dd, yyyy}";
        var htmlContent = BuildInvoiceEmailHtml(invoice);
        var plainTextContent = BuildInvoiceEmailText(invoice);

        return await SendEmailAsync(invoice.Client.Email, invoice.Client.Name, subject, plainTextContent, htmlContent);
    }

    public async Task<EmailResult> SendPaymentConfirmationAsync(Invoice invoice)
    {
        var subject = $"Payment Received - Invoice {invoice.InvoiceNumber}";
        var htmlContent = BuildPaymentConfirmationHtml(invoice);
        var plainTextContent = BuildPaymentConfirmationText(invoice);

        return await SendEmailAsync(invoice.Client.Email, invoice.Client.Name, subject, plainTextContent, htmlContent);
    }

    public async Task<EmailResult> SendPaymentReminderAsync(Invoice invoice)
    {
        var subject = $"Payment Reminder - Invoice {invoice.InvoiceNumber}";
        var htmlContent = BuildPaymentReminderHtml(invoice);
        var plainTextContent = BuildPaymentReminderText(invoice);

        return await SendEmailAsync(invoice.Client.Email, invoice.Client.Name, subject, plainTextContent, htmlContent);
    }

    private async Task<EmailResult> SendEmailAsync(string toEmail, string toName, string subject, string plainTextContent, string htmlContent)
    {
        try
        {
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail, toName);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await _client.SendEmailAsync(msg);

            return new EmailResult
            {
                Success = response.IsSuccessStatusCode,
                MessageId = response.Headers?.TryGetValues("X-Message-Id", out var values) == true
                    ? values.FirstOrDefault()
                    : null,
                ErrorMessage = response.IsSuccessStatusCode ? null : await response.Body.ReadAsStringAsync()
            };
        }
        catch (Exception ex)
        {
            return new EmailResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static string BuildInvoiceEmailHtml(Invoice invoice)
    {
        var lineItemsHtml = string.Join("", invoice.LineItems.Select(li =>
            $"<tr><td style='padding: 10px; border-bottom: 1px solid #eee;'>{li.Description}</td>" +
            $"<td style='padding: 10px; border-bottom: 1px solid #eee; text-align: center;'>{li.Quantity}</td>" +
            $"<td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>${li.UnitPrice:N2}</td>" +
            $"<td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>${li.Total:N2}</td></tr>"));

        var paymentLink = !string.IsNullOrEmpty(invoice.StripePaymentLinkUrl)
            ? $"<a href='{invoice.StripePaymentLinkUrl}' style='display: inline-block; padding: 15px 30px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>Pay Now</a>"
            : "";

        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 10px;'>
        <h1 style='color: #333; margin-bottom: 5px;'>Invoice {invoice.InvoiceNumber}</h1>
        <p style='color: #666; margin-top: 0;'>Issue Date: {invoice.IssueDate:MMMM dd, yyyy}</p>

        <div style='background-color: white; padding: 20px; border-radius: 5px; margin: 20px 0;'>
            <h3 style='margin-top: 0;'>Bill To:</h3>
            <p style='margin: 5px 0;'><strong>{invoice.Client.Name}</strong></p>
            <p style='margin: 5px 0;'>{invoice.Client.Email}</p>
            {(invoice.Client.Company is not null ? $"<p style='margin: 5px 0;'>{invoice.Client.Company}</p>" : "")}
        </div>

        <table style='width: 100%; border-collapse: collapse; background-color: white; border-radius: 5px;'>
            <thead>
                <tr style='background-color: #f1f1f1;'>
                    <th style='padding: 10px; text-align: left;'>Description</th>
                    <th style='padding: 10px; text-align: center;'>Qty</th>
                    <th style='padding: 10px; text-align: right;'>Price</th>
                    <th style='padding: 10px; text-align: right;'>Total</th>
                </tr>
            </thead>
            <tbody>
                {lineItemsHtml}
            </tbody>
        </table>

        <div style='background-color: white; padding: 20px; border-radius: 5px; margin-top: 20px;'>
            <div style='display: flex; justify-content: space-between; margin: 5px 0;'>
                <span>Subtotal:</span><span style='float: right;'>${invoice.Subtotal:N2}</span>
            </div>
            <div style='display: flex; justify-content: space-between; margin: 5px 0;'>
                <span>Tax ({invoice.TaxRate}%):</span><span style='float: right;'>${invoice.TaxAmount:N2}</span>
            </div>
            <div style='display: flex; justify-content: space-between; margin: 10px 0; font-size: 1.2em; font-weight: bold; border-top: 2px solid #333; padding-top: 10px;'>
                <span>Total Due:</span><span style='float: right;'>${invoice.Total:N2}</span>
            </div>
        </div>

        <div style='text-align: center; margin: 30px 0;'>
            <p style='color: #e74c3c; font-weight: bold;'>Due Date: {invoice.DueDate:MMMM dd, yyyy}</p>
            {paymentLink}
        </div>

        {(invoice.Notes is not null ? $"<div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin-top: 20px;'><strong>Notes:</strong><br>{invoice.Notes}</div>" : "")}
    </div>
</body>
</html>";
    }

    private static string BuildInvoiceEmailText(Invoice invoice)
    {
        var lineItems = string.Join("\n", invoice.LineItems.Select(li =>
            $"  - {li.Description}: {li.Quantity} x ${li.UnitPrice:N2} = ${li.Total:N2}"));

        return $@"
INVOICE {invoice.InvoiceNumber}
Issue Date: {invoice.IssueDate:MMMM dd, yyyy}

Bill To:
{invoice.Client.Name}
{invoice.Client.Email}
{(invoice.Client.Company is not null ? invoice.Client.Company : "")}

Items:
{lineItems}

Subtotal: ${invoice.Subtotal:N2}
Tax ({invoice.TaxRate}%): ${invoice.TaxAmount:N2}
Total Due: ${invoice.Total:N2}

Due Date: {invoice.DueDate:MMMM dd, yyyy}

{(invoice.StripePaymentLinkUrl is not null ? $"Pay online: {invoice.StripePaymentLinkUrl}" : "")}

{(invoice.Notes is not null ? $"Notes: {invoice.Notes}" : "")}
";
    }

    private static string BuildPaymentConfirmationHtml(Invoice invoice)
    {
        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background-color: #d4edda; padding: 30px; border-radius: 10px; text-align: center;'>
        <h1 style='color: #155724;'>Payment Received!</h1>
        <p style='font-size: 1.2em;'>Thank you for your payment of <strong>${invoice.Total:N2}</strong></p>
        <p>Invoice: {invoice.InvoiceNumber}</p>
        <p>Paid on: {invoice.PaidAt:MMMM dd, yyyy 'at' h:mm tt}</p>
    </div>
</body>
</html>";
    }

    private static string BuildPaymentConfirmationText(Invoice invoice)
    {
        return $@"
PAYMENT RECEIVED!

Thank you for your payment of ${invoice.Total:N2}

Invoice: {invoice.InvoiceNumber}
Paid on: {invoice.PaidAt:MMMM dd, yyyy 'at' h:mm tt}
";
    }

    private static string BuildPaymentReminderHtml(Invoice invoice)
    {
        var paymentLink = !string.IsNullOrEmpty(invoice.StripePaymentLinkUrl)
            ? $"<a href='{invoice.StripePaymentLinkUrl}' style='display: inline-block; padding: 15px 30px; background-color: #e74c3c; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>Pay Now</a>"
            : "";

        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background-color: #fff3cd; padding: 30px; border-radius: 10px; text-align: center;'>
        <h1 style='color: #856404;'>Payment Reminder</h1>
        <p style='font-size: 1.2em;'>Invoice <strong>{invoice.InvoiceNumber}</strong> is due</p>
        <p style='font-size: 1.5em; font-weight: bold;'>Amount: ${invoice.Total:N2}</p>
        <p style='color: #e74c3c;'>Due Date: {invoice.DueDate:MMMM dd, yyyy}</p>
        <div style='margin: 20px 0;'>
            {paymentLink}
        </div>
    </div>
</body>
</html>";
    }

    private static string BuildPaymentReminderText(Invoice invoice)
    {
        return $@"
PAYMENT REMINDER

Invoice {invoice.InvoiceNumber} is due

Amount: ${invoice.Total:N2}
Due Date: {invoice.DueDate:MMMM dd, yyyy}

{(invoice.StripePaymentLinkUrl is not null ? $"Pay online: {invoice.StripePaymentLinkUrl}" : "")}
";
    }
}
