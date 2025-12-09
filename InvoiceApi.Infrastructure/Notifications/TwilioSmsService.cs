using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace InvoiceApi.Infrastructure.Notifications;

public class TwilioSmsService : ISmsService
{
    private readonly string _fromNumber;

    public TwilioSmsService(string accountSid, string authToken, string fromNumber)
    {
        TwilioClient.Init(accountSid, authToken);
        _fromNumber = fromNumber;
    }

    public async Task<SmsResult> SendPaymentReminderAsync(Invoice invoice)
    {
        if (string.IsNullOrEmpty(invoice.Client.Phone))
        {
            return new SmsResult
            {
                Success = false,
                ErrorMessage = "Client phone number not available"
            };
        }

        var message = $"Reminder: Invoice {invoice.InvoiceNumber} for ${invoice.Total:N2} is due on {invoice.DueDate:MMM dd, yyyy}. ";

        if (!string.IsNullOrEmpty(invoice.StripePaymentLinkUrl))
        {
            message += $"Pay now: {invoice.StripePaymentLinkUrl}";
        }

        return await SendSmsAsync(invoice.Client.Phone, message);
    }

    public async Task<SmsResult> SendPaymentConfirmationAsync(Invoice invoice)
    {
        if (string.IsNullOrEmpty(invoice.Client.Phone))
        {
            return new SmsResult
            {
                Success = false,
                ErrorMessage = "Client phone number not available"
            };
        }

        var message = $"Payment received! Thank you for paying ${invoice.Total:N2} for invoice {invoice.InvoiceNumber}.";

        return await SendSmsAsync(invoice.Client.Phone, message);
    }

    private async Task<SmsResult> SendSmsAsync(string toNumber, string message)
    {
        try
        {
            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new Twilio.Types.PhoneNumber(_fromNumber),
                to: new Twilio.Types.PhoneNumber(toNumber)
            );

            return new SmsResult
            {
                Success = messageResource.Status != MessageResource.StatusEnum.Failed,
                MessageSid = messageResource.Sid,
                ErrorMessage = messageResource.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            return new SmsResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
