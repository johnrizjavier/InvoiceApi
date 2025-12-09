using FluentAssertions;
using InvoiceApi.Core.Enums;
using InvoiceApi.Core.Interfaces;
using InvoiceApi.Tests.Fixtures;
using InvoiceApi.Tests.Mocks;

namespace InvoiceApi.Tests.Unit;

public class MockRepositoryTests
{
    private readonly MockInvoiceRepository _repository;

    public MockRepositoryTests()
    {
        _repository = new MockInvoiceRepository();
    }

    [Fact]
    public async Task CreateAsync_ShouldAddInvoiceToStore()
    {
        // Arrange
        var invoice = TestFixture.CreateTestInvoice();

        // Act
        var result = await _repository.CreateAsync(invoice);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(invoice.Id);
        _repository.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnInvoice()
    {
        // Arrange
        var invoice = TestFixture.CreateTestInvoice();
        await _repository.CreateAsync(invoice);

        // Act
        var result = await _repository.GetByIdAsync(invoice.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(invoice.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existing");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByInvoiceNumberAsync_ShouldFindByNumber()
    {
        // Arrange
        var invoice = TestFixture.CreateTestInvoice();
        await _repository.CreateAsync(invoice);

        // Act
        var result = await _repository.GetByInvoiceNumberAsync(invoice.InvoiceNumber);

        // Assert
        result.Should().NotBeNull();
        result!.InvoiceNumber.Should().Be(invoice.InvoiceNumber);
    }

    [Fact]
    public async Task GetAllAsync_WithNoFilter_ShouldReturnAll()
    {
        // Arrange
        await _repository.CreateAsync(TestFixture.CreateTestInvoice());
        await _repository.CreateAsync(TestFixture.CreateTestInvoice());
        await _repository.CreateAsync(TestFixture.CreateTestInvoice());

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ShouldFilter()
    {
        // Arrange
        await _repository.CreateAsync(TestFixture.CreateTestInvoice(status: InvoiceStatus.Draft));
        await _repository.CreateAsync(TestFixture.CreateTestInvoice(status: InvoiceStatus.Sent));
        await _repository.CreateAsync(TestFixture.CreateTestInvoice(status: InvoiceStatus.Paid));

        // Act
        var filter = new InvoiceFilter { Status = InvoiceStatus.Sent };
        var results = await _repository.GetAllAsync(filter);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InvoiceStatus.Sent);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldSkipAndTake()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await _repository.CreateAsync(TestFixture.CreateTestInvoice());
        }

        // Act
        var filter = new InvoiceFilter { Skip = 2, Take = 3 };
        var results = await _repository.GetAllAsync(filter);

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingInvoice()
    {
        // Arrange
        var invoice = TestFixture.CreateTestInvoice();
        await _repository.CreateAsync(invoice);

        invoice.Status = InvoiceStatus.Paid;
        invoice.Notes = "Updated";

        // Act
        var result = await _repository.UpdateAsync(invoice);

        // Assert
        result.Status.Should().Be(InvoiceStatus.Paid);
        result.Notes.Should().Be("Updated");

        var retrieved = await _repository.GetByIdAsync(invoice.Id);
        retrieved!.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldReturnTrueAndRemove()
    {
        // Arrange
        var invoice = TestFixture.CreateTestInvoice();
        await _repository.CreateAsync(invoice);

        // Act
        var result = await _repository.DeleteAsync(invoice.Id);

        // Assert
        result.Should().BeTrue();
        _repository.Count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync("non-existing");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Clear_ShouldRemoveAllInvoices()
    {
        // Arrange
        _repository.Add(TestFixture.CreateTestInvoice());
        _repository.Add(TestFixture.CreateTestInvoice());

        // Act
        _repository.Clear();

        // Assert
        _repository.Count.Should().Be(0);
    }
}
