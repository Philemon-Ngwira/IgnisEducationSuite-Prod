using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EDUSphereSharedProject.UniversalModels.ParentLinking
{
    /// <summary>
    /// Dashboard headline: how many enrolled students have nobody responsible for them.
    ///
    /// An unparented student is invisible to the parent portal, gets no report card notification and
    /// cannot be contacted through chat, so this is a data gap worth surfacing rather than leaving
    /// to be discovered at the end of term.
    /// </summary>
    public class UnparentedSummaryDto
    {
        public int TotalStudents { get; set; }
        public int UnparentedCount { get; set; }

        /// <summary>Worst-affected levels first, so an admin knows where to start.</summary>
        public List<UnparentedByLevelDto> ByLevel { get; set; } = new();

        public bool NeedsWork => UnparentedCount > 0;

        public int PercentUnparented =>
            TotalStudents == 0 ? 0 : (int)Math.Round(100.0 * UnparentedCount / TotalStudents);
    }

    public class UnparentedByLevelDto
    {
        public int? AcademicLevel { get; set; }
        public string? LevelName { get; set; }
        public string? GradeSection { get; set; }
        public int UnparentedCount { get; set; }
    }

    public class UnparentedStudentDto
    {
        public Guid StudentId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? StudentNumber { get; set; }
        public int? AcademicLevel { get; set; }
        public string? LevelName { get; set; }
        public string? GradeSection { get; set; }
        public string? Gender { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    /// <summary>An existing parent an unparented student can be attached to.</summary>
    public class ParentOptionDto
    {
        public Guid ParentId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        /// <summary>Children already linked. Shown so an admin can recognise the right family and
        /// spot a likely sibling match.</summary>
        public int LinkedChildrenCount { get; set; }

        public List<string> LinkedChildNames { get; set; } = new();

        /// <summary>False when the parent record has no login, so they cannot actually use the
        /// portal even once children are attached.</summary>
        public bool HasUserAccount { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public class LinkStudentsToParentRequest
    {
        [Required]
        public Guid ParentId { get; set; }

        public List<Guid> StudentIds { get; set; } = new();
    }

    public class LinkStudentsToParentResult
    {
        public bool Succeeded { get; set; }
        public int LinkedCount { get; set; }

        /// <summary>Students that already had a parent and were left alone, so an accidental
        /// reassignment cannot happen silently.</summary>
        public List<string> SkippedAlreadyLinked { get; set; } = new();

        public List<string> Errors { get; set; } = new();
    }

    public class UnlinkStudentRequest
    {
        [Required]
        public Guid StudentId { get; set; }
    }
}
