using System.Diagnostics;
using System.Security.Claims;
using EduSphereDomain.FinanceData;
using EduSphereDomain.Repositories;
using EDUSphereSharedProject.UniversalModels.ReportCards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IgnisEducationSuite.Controllers
{
    /// <summary>
    /// Recalculating report card positions.
    ///
    /// Positions are derived from marks, so they go stale whenever marks are corrected after the
    /// fact — a late entry, an amended score, a student enrolled mid-term. This gives an
    /// administrator a way to bring them back in line without waiting for whatever scheduled job
    /// would otherwise do it.
    ///
    /// The school comes from the signed-in user, never from the route. The previous client code
    /// posted a school id in the URL, which would have let any authenticated user renumber another
    /// school's students.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportCardPositionsController : ControllerBase
    {
        /// <summary>Roles allowed to renumber a whole school. Deliberately narrow: this rewrites
        /// every student's position, and it is not something a subject teacher should trigger.</summary>
        private static readonly string[] AllowedRoles = { "Admin", "SuperAdmin", "Principal", "Dean" };

        /// <summary>
        /// Renumbering a whole school walks every report card in it, which on a large roll runs past
        /// EF's default 30-second command timeout. Timing out halfway is the worst outcome here —
        /// the caller sees a failure while the procedure may already have rewritten part of the
        /// school — so give it room.
        /// </summary>
        private const int CommandTimeoutSeconds = 300;

        private readonly PhoenixEdusphereFinanceContextProcedures _procedures;
        private readonly PhoenixEdusphereFinanceContext _financeContext;
        private readonly EduSphereRepository _repository;
        private readonly ILogger<ReportCardPositionsController> _logger;

        public ReportCardPositionsController(
            PhoenixEdusphereFinanceContextProcedures procedures,
            PhoenixEdusphereFinanceContext financeContext,
            EduSphereRepository repository,
            ILogger<ReportCardPositionsController> logger)
        {
            _procedures = procedures;
            _financeContext = financeContext;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Recalculates every student's position across the school.
        ///
        /// Always returns 200 with a result object. The caller needs to tell "the procedure refused"
        /// from "the request never arrived", and a bare status code cannot say which.
        /// </summary>
        [HttpPost("school")]
        public async Task<IActionResult> RecalculateSchool(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var scope = await ResolveScopeAsync(userId);

            if (scope.SchoolId is not { } schoolId || schoolId == Guid.Empty)
                return Ok(PositionRecalculationResult.Fail("No school is associated with this account."));

            if (!scope.Roles.Overlaps(AllowedRoles))
                return Ok(PositionRecalculationResult.Fail("Recalculating school positions requires an administrator."));

            var stopwatch = Stopwatch.StartNew();

            // The procedures class and this context are both scoped, so they are the same instance —
            // raising the timeout here raises it for the procedure call below.
            var previousTimeout = _financeContext.Database.GetCommandTimeout();
            _financeContext.Database.SetCommandTimeout(CommandTimeoutSeconds);

            try
            {
                await _procedures.RecalculateSchoolPositionsAsync(schoolId, cancellationToken: ct);
                stopwatch.Stop();

                _logger.LogInformation(
                    "School positions recalculated for {SchoolId} by {UserId} in {Elapsed}ms",
                    schoolId, userId, stopwatch.ElapsedMilliseconds);

                return Ok(PositionRecalculationResult.Ok(
                    "School positions have been recalculated.",
                    stopwatch.Elapsed));
            }
            catch (OperationCanceledException)
            {
                // The browser gave up, not the database. The procedure may well still be running,
                // so do not report this as a failure that needs retrying immediately.
                _logger.LogWarning("School position recalculation for {SchoolId} was cancelled by the caller", schoolId);

                return Ok(PositionRecalculationResult.Fail(
                    "The request was cancelled before it finished. The recalculation may still be running — " +
                    "check the positions before starting another."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "School position recalculation failed for {SchoolId}", schoolId);
                return Ok(PositionRecalculationResult.Fail("The recalculation could not be completed. Nothing was changed."));
            }
            finally
            {
                // Restore it: the context is scoped to this request, but leaving a five-minute
                // timeout on it would apply to anything else this request goes on to do.
                _financeContext.Database.SetCommandTimeout(previousTimeout);
            }
        }

        /// <summary>School and roles for the signed-in user, from the same initialisation data the
        /// rest of the app resolves scope with.</summary>
        private async Task<(Guid? SchoolId, HashSet<string> Roles)> ResolveScopeAsync(string userId)
        {
            var data = (await _repository.GetInitializationDataResults(userId))?.FirstOrDefault();

            var roles = new HashSet<string>(
                data?.RoleName?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim())
                    ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            return (data?.SchoolID, roles);
        }
    }
}
