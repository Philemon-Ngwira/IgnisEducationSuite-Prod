using DocumentFormat.OpenXml.Office2010.Excel;
using EduSphereDomain.FinanceData;
using EDUSphereSharedProject.FinanceModels;
using EDUSphereSharedProject.FinanceModels.DTOs;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduSphereDomain.Repositories
{
    public class FinananceRepository
    {
        private readonly PhoenixEdusphereFinanceContext _context;
        private readonly PhoenixEdusphereFinanceContextProcedures _procedures;
        public FinananceRepository(PhoenixEdusphereFinanceContext context, PhoenixEdusphereFinanceContextProcedures procedures)
        {
            _context = context;
            _procedures = procedures;
        }

        public async Task<IEnumerable<InvoiceType>> GetInvoiceTypes()
        {
            var result = await _context.InvoiceTypes.ToListAsync();
            return result;
        }
        public async Task<string> GenerateInvoiceNumberAsync(string SchoolName, string invoiceType, Guid SchoolId)
        {
            // Get school short code (first 3 letters)


            if (string.IsNullOrEmpty(SchoolName)) throw new Exception("School not found");

            string schoolCode = SchoolName.Substring(0, Math.Min(3, SchoolName.Length)).ToUpper();
            string typeCode = invoiceType.Substring(0, Math.Min(2, invoiceType.Length)).ToUpper();

            string yyyyMM = DateTime.UtcNow.ToString("yyyyMM");

            // Count invoices for this school, type, and month
            int sequence = await _context.Invoices
                .Where(i => i.SchoolID == SchoolId
                            && i.InvoiceType == invoiceType
                            && i.IssuedDate.Year == DateTime.UtcNow.Year
                            && i.IssuedDate.Month == DateTime.UtcNow.Month)
                .CountAsync() + 1;

            string seqStr = sequence.ToString("D3"); // pad 3 digits

            string invoiceNumber = $"{schoolCode}-{typeCode}-{yyyyMM}-{seqStr}";

            return invoiceNumber;
        }
        public async Task<IEnumerable<Invoice>> GetStudentInvoices(string StudentID)
        {
            var result = await _procedures.GetStudentInvoicesAsync(StudentID);
            return result.Select(x => new Invoice
            {
                Id = x.Id,
                StudentFinanceId = x.StudentFinanceId,
                Amount = x.Amount,
                PaidAmount = x.PaidAmount,
                PaymentStatus = x.PaymentStatus,
                TermStartDate = x.TermStartDate,
                TermEndDate = x.TermEndDate,
                DueDate = x.DueDate,
                IssuedDate = x.IssuedDate,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                InvoiceTypeID = x.InvoiceTypeID,
                InvoiceType = x.InvoiceType,
                SchoolID = x.SchoolID
            }).ToList();
        }
        public async Task<IEnumerable<StudentFinance>> GetStudentFinances(Guid SchoolID)
        {
            var result = await _procedures.GetStudentFinanceBySchoolAsync(SchoolID);
            return result.Select(x => new StudentFinance
            {
                Id = x.Id,
                StudentId = x.StudentId,
                TotalFees = x.TotalFees,
                Scholarships = x.Scholarships,
                OutstandingAmount = x.OutstandingAmount,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList();
        }
        public async Task<(bool Success, string Message, int Count)> GenerateBulkInvoicesAsync(
       BulkInvoiceRequest request,
       Guid schoolId)
        {
            if (request.Amount <= 0)
                return (false, "Amount must be greater than zero.", 0);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🔒 Duplicate Protection
                bool alreadyGenerated = await _context.Invoices.AnyAsync(i =>
                    i.SchoolID == schoolId &&
                    i.InvoiceTypeID == request.InvoiceTypeId &&
                    i.TermStartDate == request.TermStartDate &&
                    i.TermEndDate == request.TermEndDate);

                if (alreadyGenerated)
                    return (false, "Invoices for this term and type already exist.", 0);

                var studentFinances = await _context.StudentFinances
                    .Where(sf => sf.SchoolID == schoolId && sf.Status == "Active")
                    .ToListAsync();

                if (!studentFinances.Any())
                    return (false, "No active student finance accounts found.", 0);

                var now = DateTime.UtcNow;

                var invoices = new List<Invoice>();
                var ledgerEntries = new List<FinanceLedger>();

                foreach (var finance in studentFinances)
                {
                    var invoiceId = Guid.NewGuid();
                    var invoiceNumber = await GenerateInvoiceNumberAsync(request.SchoolName, request.InvoiceType, schoolId);
                    var invoice = new Invoice
                    {
                        Id = invoiceId,
                        SchoolID = schoolId,
                        StudentFinanceId = finance.Id,
                        InvoiceTypeID = request.InvoiceTypeId,
                        Amount = request.Amount,
                        PaidAmount = 0,
                        PaymentStatus = "Pending",
                        TermStartDate = request.TermStartDate,
                        TermEndDate = request.TermEndDate,
                        DueDate = request.DueDate,
                        IssuedDate = now,
                        CreatedAt = now,
                        UpdatedAt = now,
                        InvoiceNumber = invoiceNumber,
                        InvoiceType = request.InvoiceType
                    };

                    invoices.Add(invoice);

                    ledgerEntries.Add(new FinanceLedger
                    {
                        Id = Guid.NewGuid(),
                        SchoolId = schoolId,
                        StudentFinanceId = finance.Id,
                        EntryType = "Debit",
                        Amount = request.Amount,
                        ReferenceId = invoiceId,
                        ReferenceType = "Invoice",
                        CreatedAt = now
                    });

                    // Maintain running totals (optional but efficient)
                    finance.TotalFees += request.Amount;
                }

                await _context.Invoices.AddRangeAsync(invoices);
                await _context.FinanceLedgers.AddRangeAsync(ledgerEntries);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Successfully generated {invoices.Count} invoices.", invoices.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error generating invoices: {ex.Message}", 0);
            }
        }

        public async Task<Payment> RecordPayment(Payment paymentData)
        {
            try
            {
                var result = await _context.AddAsync(paymentData);
                await _context.SaveChangesAsync();
                return result.Entity;
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                throw;
            }

        }
        public async Task<IEnumerable<StudentPayments>> GetStudentPayments(string StudentID)
        {
            var result = await _procedures.sp_GetStudentPaymentsAsync(StudentID);
            return result.Select(x => new StudentPayments
            {
                InvoiceId = x.InvoiceId,
                PaymentId = x.PaymentId,
                AmountPaid = x.AmountPaid,
                InvoiceAmount = x.InvoiceAmount,
                InvoicePaidAmount = x.InvoicePaidAmount,
                OutstandingAmount = x.OutstandingAmount,
                PaymentCreatedAt = x.PaymentCreatedAt,
                InvoiceType = x.InvoiceType,
                StudentFinanceStatus = x.StudentFinanceStatus,
                PaymentDate = x.PaymentDate,
                TermEndDate = x.TermEndDate,
                TermStartDate = x.TermStartDate,
                PaymentMethod = x.PaymentMethod,
                TotalFees = x.TotalFees,
                InvoiceNumber = x.InvoiceNumber

            }).ToList();
        }

        public async Task<DashboardSummaryDTO> GetDashboardSummary(Guid SchoolID)
        {
            var result = await _procedures.sp_GetFinanceDashboardSummaryAsync(SchoolID);
            var summary = result.FirstOrDefault();
            return new DashboardSummaryDTO
            {
                OutstandingBalance = summary?.OutstandingBalance,
                OverdueInvoices = summary?.OverdueInvoices,
                PaymentsToday = summary?.PaymentsToday,
                TotalCollected = summary?.TotalCollected,
                TotalInvoiced = summary?.TotalInvoiced
            };

        }

        public async Task<IEnumerable<MonthlyPaymentTrend>> GetMonthlyPaymentTrends(Guid SchoolID)
        {
            var result = await _procedures.sp_GetMonthlyPaymentTrendAsync(SchoolID);
            return result.Select(x => new MonthlyPaymentTrend
            {
                Amount = x.Amount,
                Month = x.Month,
            }).ToList();
        }

        public async Task<IEnumerable<RecentPayments>> GetRecentPaymentsAsync(Guid SchoolID)
        {
            var result = await _procedures.sp_GetRecentPaymentsAsync(SchoolID, 10);
            return result.Select(x => new RecentPayments
            {
                AmountPaid = x.AmountPaid,
                InvoiceNumber = x.InvoiceNumber,
                PaymentDate = x.PaymentDate,
                StudentName = x.StudentName,
                PaymentMethod = x.PaymentMethod
            }).ToList();
        }
    }
}
