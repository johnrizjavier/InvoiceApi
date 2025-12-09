using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Interfaces;

namespace InvoiceApi.Tests.Mocks;

public class MockSmsService : ISmsService
{
    public bool ShouldSucceed { get; set; } = true;
    public List<SmsRecord> SentMessages { get; } = new();

    public Task<SmsResult> SendPaymentReminderAsync(Invoice invoice)
    {
        if (string.IsNullOrEmpty(invoice.Client.Phone))
        {
            return Task.FromResult(new SmsResult
            {
                Success = false,
                ErrorMessage = "Client phone number not available"
            });
        }

        var record = new SmsRecord
        {
            Type = SmsType.PaymentReminder,
            ToPhone = invoice.Client.Phone,
            InvoiceId = invoice.Id,
            SentAt = DateTime.UtcNow
        };
        SentMessages.Add(record);

        return Task.FromResult(CreateResult());
    }

    public Task<SmsResult> SendPaymentConfirmationAsync(Invoice invoice)
    {
        if (string.IsNullOrEmpty(invoice.Client.Phone))
        {
            return Task.FromResult(new SmsResult
            {
                Success = false,
                ErrorMessage = "Client phone number not available"
            });
        }

        var record = new SmsRecord
        {
            Type = SmsType.PaymentConfirmation,
            ToPhone = invoice.Client.Phone,
            InvoiceId = invoice.Id,
            SentAt = DateTime.UtcNow
        };
        SentMessages.Add(record);

        return Task.FromResult(CreateResult());
    }

    private SmsResult CreateResult()
    {
        if (!ShouldSucceed)
        {
            return new SmsResult
            {
                Success = false,
                ErrorMessage = "SMS service error"
            };
        }

        return new SmsResult
        {
            Success = true,
            MessageSid = $"SM{Guid.NewGuid():N}"
        };
    }

    public void Reset()
    {
        ShouldSucceed = true;
        SentMessages.Clear();
    }
}

public class SmsRecord
{
    public SmsType Type { get; set; }
    public string ToPhone { get; set; } = string.Empty;
    public string InvoiceId { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public enum SmsType
{
    PaymentReminder,
    PaymentConfirmation
}
