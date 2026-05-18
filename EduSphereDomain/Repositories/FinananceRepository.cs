using EduSphereDomain.FinanceData;
using EDUSphereSharedProject.FinanceModels;
using EDUSphereSharedProject.FinanceModels.DTOs;
using EDUSphereSharedProject.UniversalModels;
using Microsoft.EntityFrameworkCore;

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
            try
            {
                var result = await _context.InvoiceTypes.ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                throw;
            }

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
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🔥 1. Load Fee Structure
                var structure = await _context.FeeStructures
                    .Include(x => x.FeeStructureItems)
                    .FirstOrDefaultAsync(x => x.Id == request.FeeStructureId);

                if (structure == null)
                    return (false, "Fee structure not found.", 0);

                // 🔒 2. Duplicate Protection (Invoice level)
                bool alreadyGenerated = await _context.Invoices.AnyAsync(i =>
                    i.SchoolID == schoolId &&
                    i.TermStartDate == request.TermStartDate &&
                    i.TermEndDate == request.TermEndDate &&
                    i.FeeStructureId == request.FeeStructureId);

                if (alreadyGenerated)
                    return (false, "Invoices for this structure and term already exist.", 0);

                // 🔥 3. Students
                var studentFinances = await _context.StudentFinances
                    .Where(sf => sf.SchoolID == schoolId && sf.Status == "Active")
                    .Include(sf => sf.Student)
                    .ToListAsync();

                if (!studentFinances.Any())
                    return (false, "No active student finance accounts found.", 0);

                var now = DateTime.UtcNow;

                // 🔥 4. Preload lookup
                var invoiceTypeLookup = await _context.InvoiceTypes
                    .ToDictionaryAsync(x => x.Id, x => x.Name);

                // 🔥 5. PRELOAD existing ledger keys (IMPORTANT FIX)
                var existingLedgerKeys = await _context.FinanceLedgers
                    .Where(x => x.SchoolId == schoolId)
                    .Select(x => x.UniqueKey)
                    .ToListAsync();

                var invoices = new List<Invoice>();
                var ledgerEntries = new List<FinanceLedger>();

                foreach (var finance in studentFinances)
                {
                    var student = finance.Student;

                    var studentType =
                        student.isDaySchool == true
                            ? StudentType.DayScholars
                            : StudentType.Boarders;

                    foreach (var item in structure.FeeStructureItems)
                    {
                        // 🔥 Target type filter
                        var target = (StudentType)item.TargetType;

                        if (target != StudentType.All && target != studentType)
                            continue;

                        var invoiceId = Guid.NewGuid();

                        var invoiceNumber = await GenerateInvoiceNumberAsync(
                            request.SchoolName,
                            "STRUCT",
                            schoolId);

                        invoices.Add(new Invoice
                        {
                            Id = invoiceId,
                            SchoolID = schoolId,
                            StudentFinanceId = finance.Id,

                            InvoiceTypeID = item.InvoiceTypeId,
                            InvoiceType = invoiceTypeLookup.TryGetValue(item.InvoiceTypeId, out var name)
                                ? name
                                : "Unknown",

                            Amount = item.Amount,
                            PaidAmount = 0,
                            PaymentStatus = "Pending",

                            TermStartDate = request.TermStartDate,
                            TermEndDate = request.TermEndDate,
                            DueDate = request.DueDate,

                            IssuedDate = now,
                            CreatedAt = now,
                            UpdatedAt = now,

                            InvoiceNumber = invoiceNumber,

                            IsOptional = item.IsOptional,
                            FeeStructureId = structure.Id
                        });

                        // 🔥 UNIQUE LEDGER KEY (CRITICAL FIX)
                        var uniqueKey = $"{finance.Id}-{item.Id}-{request.TermStartDate:yyyyMMdd}-{request.TermEndDate:yyyyMMdd}-DEBIT";
                        var lastSeq = await _context.FinanceLedgers.MaxAsync(x => (long?)x.SequenceNumber) ?? 0;
                        if (!existingLedgerKeys.Contains(uniqueKey))
                        {
                            ledgerEntries.Add(new FinanceLedger
                            {

                                Id = Guid.NewGuid(),
                                SchoolId = schoolId,
                                StudentFinanceId = finance.Id,
                                EntryType = "Debit",
                                Amount = item.Amount,
                                ReferenceId = invoiceId,
                                ReferenceType = "Invoice",
                                CreatedAt = now,

                                UniqueKey = uniqueKey
                            });

                            existingLedgerKeys.Add(uniqueKey);

                            finance.TotalFees += item.Amount;
                        }
                    }
                }

                if (!invoices.Any())
                    return (false, "No invoices generated. Check fee structure rules.", 0);

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
        public async Task<FeeBucket> SaveFeeBucket(FeeBucket feeBucket)
        {
            try
            {
                await _context.AddAsync(feeBucket);
                await _context.SaveChangesAsync();
                return feeBucket;
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                throw;
            }

        }
        public async Task<FeeStructure> SaveFeeStructureAsync(FeeStructure feeStructure)
        {
            if (feeStructure == null)
                throw new ArgumentNullException(nameof(feeStructure));

            // 🔷 BASIC VALIDATION
            if (string.IsNullOrWhiteSpace(feeStructure.Name))
                throw new Exception("Fee structure name is required.");

            if (feeStructure.FeeStructureItems == null || !feeStructure.FeeStructureItems.Any())
                throw new Exception("At least one fee item is required.");

            // 🔷 PREVENT DUPLICATES (same invoice type)
            var duplicateTypes = feeStructure.FeeStructureItems
                .GroupBy(x => x.InvoiceTypeId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateTypes.Any())
                throw new Exception("Duplicate invoice types are not allowed in a fee structure.");

            // 🔷 VALIDATE AMOUNTS
            if (feeStructure.FeeStructureItems.Any(x => x.Amount <= 0))
                throw new Exception("All fee items must have an amount greater than zero.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;

                // 🔷 SET METADATA (parent)
                feeStructure.Id = Guid.NewGuid();
                feeStructure.CreatedAt = now;

                // 🔷 PREP CHILDREN
                foreach (var item in feeStructure.FeeStructureItems)
                {
                    item.Id = Guid.NewGuid();
                    item.FeeStructureId = feeStructure.Id;
                    item.CreatedAt = now;
                }

                // 🔷 SINGLE INSERT GRAPH
                await _context.FeeStructures.AddAsync(feeStructure);

                // 🔷 SAVE ONCE
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return feeStructure;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
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
        public async Task<IEnumerable<StudentFinanceLedgerDTO>> GetStudentLedgersAsync(FinanceLedgerRequestDTO requestDTO)
        {
            var result = await _procedures.sp_GetStudentLedgerStatementAsync(requestDTO.SchoolID, requestDTO.StudentFinanceID, requestDTO.TermStart, requestDTO.TermEnd);
            return result.Select(x => new StudentFinanceLedgerDTO
            {
                Amount = x.Amount,
                CreatedAt = x.CreatedAt,
                Description = x.Description,
                EntryType = x.EntryType,
                Id = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                ReferenceId = x.ReferenceId,
                ReferenceType = x.ReferenceType,
                RunningBalance = x.RunningBalance,
                LedgerSequence = x.LedgerSequence,

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
        public async Task<bool> UpdateFeeStructureAsync(UpdateFeeStructureDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🔷 1. Update main structure
                var existingStructure = await _context.FeeStructures
                    .FirstOrDefaultAsync(x => x.Id == dto.FeeStructure.Id);

                if (existingStructure == null)
                    throw new Exception("Fee Structure not found");

                existingStructure.Name = dto.FeeStructure.Name;
                existingStructure.ClassId = dto.FeeStructure.ClassId;
                existingStructure.TermStartDate = dto.FeeStructure.TermStartDate;
                existingStructure.TermEndDate = dto.FeeStructure.TermEndDate;
                existingStructure.AcademicYear = dto.FeeStructure.AcademicYear;
                existingStructure.UpdatedAt = DateTime.UtcNow;

                // 🔷 2. DELETE removed items
                if (dto.Deleted.Any())
                {
                    var itemsToDelete = await _context.FeeStructureItems
                        .Where(x => dto.Deleted.Contains(x.Id))
                        .ToListAsync();

                    _context.FeeStructureItems.RemoveRange(itemsToDelete);
                }

                // 🔷 3. ADD new items
                if (dto.Added.Any())
                {
                    foreach (var item in dto.Added)
                    {
                        item.Id = Guid.NewGuid();
                        item.FeeStructureId = existingStructure.Id;
                        item.CreatedAt = DateTime.UtcNow;
                    }

                    await _context.FeeStructureItems.AddRangeAsync(dto.Added);
                }

                // 🔷 4. UPDATE existing items
                if (dto.Updated.Any())
                {
                    var existingItems = await _context.FeeStructureItems
                        .Where(x => dto.Updated.Select(u => u.Id).Contains(x.Id))
                        .ToListAsync();

                    foreach (var item in existingItems)
                    {
                        var updated = dto.Updated.First(x => x.Id == item.Id);

                        item.Amount = updated.Amount;
                        item.InvoiceTypeId = updated.InvoiceTypeId;
                        item.IsOptional = updated.IsOptional;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Error updating fee structure: {ex.Message}");
            }
        }

        public async Task<IEnumerable<FeeStructure>> GetFeeStructures(Guid SchoolID)
        {
            var result = await _procedures.sp_GetFeeStructuresAsync(SchoolID);
            return result.Select(x => new FeeStructure
            {
                AcademicYear = x.AcademicYear,
                CreatedAt = x.CreatedAt,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt,
                ClassId = x.ClassId,
                Id = x.Id,
                SchoolId = x.SchoolId,
                TermStartDate = x.TermStartDate,
                TermEndDate = x.TermEndDate,
                Name = x.Name,
            }).ToList();
        }
        public async Task<IEnumerable<FeeStructureItem>> GetFeeStructureItems(Guid FeeStructureID)
        {
            var result = await _procedures.sp_GetFeeStructureItemsAsync(FeeStructureID);
            return result.Select(x => new FeeStructureItem
            {
                Amount = x.Amount,
                CreatedAt = x.CreatedAt,
                FeeStructureId = x.FeeStructureId,
                Id = x.Id,
                InvoiceTypeId = x.InvoiceTypeId,
                IsOptional = x.IsOptional,
            }).ToList();
        }

        public async Task<IEnumerable<Student>> GetStudentsByParent(Guid ParentID)
        {
            var result = await _context.Students.Where(x => x.ParentID == ParentID).ToListAsync();
            return result;
        }
    }
}