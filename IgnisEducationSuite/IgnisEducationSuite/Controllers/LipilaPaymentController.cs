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
    private readonly ILogger<LipilaPaymentController> _logger;

    public LipilaPaymentController(
        IPaymentService paymentService,
        ILogger<LipilaPaymentController> logger)
    {
        _paymentService = paymentService;
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
