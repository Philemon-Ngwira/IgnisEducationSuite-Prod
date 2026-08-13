using EDUSphereSharedProject.PaymentDTos.Lipila;
using IgnisEducationSuite.ServerServices.PaymentsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers;

[Route("api/payments/lipila")]
[ApiController]
public class LipilaPaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentGatewayAccountService _gatewayAccountService;
    private readonly ILogger<LipilaPaymentController> _logger;

    public LipilaPaymentController(
        IPaymentService paymentService,
        IPaymentGatewayAccountService gatewayAccountService,
        ILogger<LipilaPaymentController> logger)
    {
        _paymentService = paymentService;
        _gatewayAccountService = gatewayAccountService;
        _logger = logger;
    }

    [Authorize(Roles = "Admin, Finance, Parent, Student")]
    [HttpPost("mobile-money")]
    public async Task<ActionResult<LipilaPaymentInitiationResult>> InitiateMobileMoney(
        [FromBody] InitiateLipilaMobileMoneyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.InitiateLipilaMobileMoneyAsync(
                request.InvoiceId,
                request.Amount,
                request.PhoneNumber,
                request.Email,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lipila mobile money collection failed for invoice {InvoiceId}.", request.InvoiceId);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Lipila is currently unavailable. Please try again shortly." });
        }
    }

    [Authorize(Roles = "Admin, Finance, Parent, Student")]
    [HttpPost("card")]
    public async Task<ActionResult<LipilaPaymentInitiationResult>> InitiateCard(
        [FromBody] InitiateLipilaCardPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.InitiateLipilaCardAsync(
                request.InvoiceId,
                request.Amount,
                request.Customer,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lipila card collection failed for invoice {InvoiceId}.", request.InvoiceId);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Lipila is currently unavailable. Please try again shortly." });
        }
    }

    [Authorize(Roles = "Admin, Finance, Parent, Student")]
    [HttpGet("status/{referenceId}")]
    public async Task<ActionResult<LipilaCollectionResponse>> GetStatus(
        string referenceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.CheckLipilaPaymentStatusAsync(referenceId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lipila status check failed for reference {ReferenceId}.", referenceId);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Lipila is currently unavailable. Please try again shortly." });
        }
    }

    [Authorize(Roles = "Admin, Finance")]
    [HttpGet("wallet-balance/{bucketId:guid}")]
    public async Task<ActionResult<LipilaWalletBalanceResponse>> GetWalletBalance(
        Guid bucketId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.GetWalletBalanceAsync(bucketId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lipila wallet balance lookup failed for bucket {BucketId}.", bucketId);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Lipila is currently unavailable. Please try again shortly." });
        }
    }

    [Authorize(Roles = "Admin, Finance, Parent, Student")]
    [HttpGet("eligibility/{invoiceId:guid}")]
    public async Task<ActionResult<object>> GetGatewayEligibility(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var eligible = await _paymentService.IsInvoiceGatewayEligibleAsync(invoiceId, cancellationToken);
            return Ok(new { eligible });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin, Finance, Parent, Student")]
    [HttpGet("bucket/outstanding/{studentFinanceId:guid}")]
    public async Task<ActionResult<List<StudentBucketSummaryDto>>> GetOutstandingBuckets(
        Guid studentFinanceId,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetOutstandingBucketsAsync(studentFinanceId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin, Finance, Parent, Student")]
    [HttpGet("bucket/outstanding/{studentFinanceId:guid}/{bucketId:guid}")]
    public async Task<ActionResult<BucketOutstandingDto>> GetBucketOutstanding(
        Guid studentFinanceId,
        Guid bucketId,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetBucketOutstandingAsync(studentFinanceId, bucketId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin, Finance, Parent, Student")]
    [HttpGet("bucket/eligibility/{bucketId:guid}")]
    public async Task<ActionResult<object>> GetBucketGatewayEligibility(
        Guid bucketId,
        CancellationToken cancellationToken)
    {
        var eligible = await _paymentService.IsBucketGatewayEligibleAsync(bucketId, cancellationToken);
        return Ok(new { eligible });
    }

    [Authorize(Roles = "Admin, Finance, Parent, Student")]
    [HttpPost("bucket/mobile-money")]
    public async Task<ActionResult<LipilaPaymentInitiationResult>> InitiateBucketMobileMoney(
        [FromBody] InitiateLipilaBucketMobileMoneyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.InitiateLipilaBucketMobileMoneyAsync(
                request.StudentFinanceId,
                request.BucketId,
                request.Amount,
                request.PhoneNumber,
                request.Email,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lipila bucket collection failed for bucket {BucketId}.", request.BucketId);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Lipila is currently unavailable. Please try again shortly." });
        }
    }

    [Authorize(Roles = "Admin, Finance")]
    [HttpGet("accounts/school/{schoolId:guid}")]
    public async Task<ActionResult<List<FeeBucketGatewayDto>>> GetGatewayAccounts(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        var result = await _gatewayAccountService.GetForSchoolAsync(schoolId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin, Finance")]
    [HttpPost("accounts")]
    public async Task<ActionResult<PaymentGatewayAccountDto>> CreateGatewayAccount(
        [FromBody] CreatePaymentGatewayAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gatewayAccountService.CreateAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin, Finance")]
    [HttpPut("accounts/{id:guid}")]
    public async Task<ActionResult<PaymentGatewayAccountDto>> UpdateGatewayAccount(
        Guid id,
        [FromBody] UpdatePaymentGatewayAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gatewayAccountService.UpdateAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Sets/rotates the wallet's API key. The plaintext key is accepted here once,
    /// encrypted immediately, and never echoed back - the response carries no
    /// key material.
    /// </summary>
    [Authorize(Roles = "Admin, Finance")]
    [HttpPost("accounts/{id:guid}/credential")]
    public async Task<IActionResult> SetGatewayAccountCredential(
        Guid id,
        [FromBody] SetPaymentGatewayCredentialRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _gatewayAccountService.SetCredentialAsync(id, request.ApiKey, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin, Finance")]
    [HttpPost("accounts/{id:guid}/test")]
    public async Task<ActionResult<LipilaWalletBalanceResponse>> TestGatewayAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gatewayAccountService.TestConnectionAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // No credential configured yet, etc.
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            // Lipila itself rejected the request (bad wallet ID, invalid/expired API key, etc).
            // The exception message may echo Lipila's raw response body, so log it rather than
            // returning it verbatim to the client.
            _logger.LogWarning(ex, "Lipila connection test failed for gateway account {AccountId}.", id);
            return BadRequest(new { message = "Lipila rejected the request. Check the wallet ID and API key." });
        }
    }

    /// <summary>
    /// Server-to-server webhook Lipila posts collection results to. No user is
    /// signed in for this call, so it cannot carry [Authorize]; correctness instead
    /// comes from validating the callback against the gateway transaction Ignis
    /// itself created at initiation time (see PaymentService.ProcessLipilaCallbackAsync).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("callback")]
    public async Task<IActionResult> Callback(
        [FromBody] LipilaCollectionResponse callback,
        CancellationToken cancellationToken)
    {
        try
        {
            await _paymentService.ProcessLipilaCallbackAsync(callback, cancellationToken);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing Lipila callback for reference {ReferenceId}.", callback?.ReferenceId);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
