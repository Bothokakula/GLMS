// CurrencyCalculationTests.cs
// Unit tests for the CurrencyService, covering currency conversion logic and API fallback behavior.
// Verifies rounding to 2 decimal places, edge cases (zero, negative, large amounts),
// and graceful degradation when the external exchange rate API is unavailable.
// xUnit test framework (xUnit.net, 2024)
// Moq mocking framework for unit test isolation (Moq, 2024)

using System.Net;
using GLMS.Core.Services;
using Moq;
using Moq.Protected;

namespace GLMS.Tests.Unit;

/// <summary>
/// Tests the <see cref="CurrencyService"/> for correct currency conversion calculations
/// and resilient behavior when the external exchange rate API fails.
/// </summary>
public class CurrencyCalculationTests
{
    private readonly CurrencyService _currencyService;
    private readonly Mock<HttpMessageHandler> _mockHandler;

    public CurrencyCalculationTests()
    {
        // Moq mocking framework for unit test isolation (Moq, 2024)
        _mockHandler = new Mock<HttpMessageHandler>();
        // HttpClient pattern for service communication (Microsoft, 2024, Make HTTP requests using IHttpClientFactory)
        var httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.er-api.com/")
        };
        _currencyService = new CurrencyService(httpClient);
    }

    /// <summary>
    /// Validates the basic multiplication: 100 USD * 18.5 rate = 1850 ZAR.
    /// This is the standard conversion used throughout the GLMS billing workflow.
    /// </summary>
    [Fact]
    public void ConvertCurrency_WithValidAmountAndRate_ReturnsCorrectZarAmount()
    {
        // Follows Arrange-Act-Assert testing pattern
        var result = _currencyService.ConvertCurrency(100m, 18.5m);

        Assert.Equal(1850.00m, result);
    }

    /// <summary>
    /// Verifies that converting zero USD returns zero ZAR (identity edge case).
    /// </summary>
    [Fact]
    public void ConvertCurrency_WithZeroAmount_ReturnsZero()
    {
        var result = _currencyService.ConvertCurrency(0m, 18.5m);

        Assert.Equal(0m, result);
    }

    /// <summary>
    /// Validates precision with large monetary amounts near the million-dollar boundary.
    /// Ensures no overflow or rounding drift at scale.
    /// </summary>
    [Fact]
    public void ConvertCurrency_WithLargeAmount_ReturnsCorrectResult()
    {
        var result = _currencyService.ConvertCurrency(999999.99m, 18.5m);

        Assert.Equal(18499999.82m, result);
    }

    /// <summary>
    /// Parameterized test verifying that conversion results are always rounded to exactly 2 decimal places.
    /// Uses varied input combinations to catch rounding-mode bugs (banker's rounding vs. standard rounding).
    /// </summary>
    [Theory]
    [InlineData(33.33, 18.123, 604.04)]
    [InlineData(10.005, 2.0, 20.01)]
    [InlineData(0.01, 100.0, 1.00)]
    public void ConvertCurrency_RoundsToTwoDecimalPlaces(
        double amount, double rate, double expected)
    {
        var result = _currencyService.ConvertCurrency((decimal)amount, (decimal)rate);

        Assert.Equal((decimal)expected, result);
    }

    /// <summary>
    /// Validates that negative amounts (e.g., refunds or credit adjustments) produce negative ZAR results.
    /// The service should not reject negative inputs.
    /// </summary>
    [Fact]
    public void ConvertCurrency_WithNegativeAmount_ReturnsNegativeResult()
    {
        var result = _currencyService.ConvertCurrency(-50m, 18.5m);

        Assert.Equal(-925.00m, result);
    }

    /// <summary>
    /// Validates the fallback behavior: when the external exchange rate API throws an HttpRequestException,
    /// the service returns a hardcoded fallback rate of 18.5 ZAR/USD rather than propagating the error.
    /// This ensures billing can continue even during external service outages.
    /// </summary>
    [Fact]
    public async Task GetExchangeRateAsync_WhenApiDown_ReturnsFallbackRate()
    {
        // Moq mocking framework for unit test isolation (Moq, 2024)
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("API is unavailable"));

        var rate = await _currencyService.GetExchangeRateAsync("USD", "ZAR");

        Assert.Equal(18.5m, rate);
    }
}
