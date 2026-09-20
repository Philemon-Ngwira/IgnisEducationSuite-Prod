using System;
using System.Collections.Generic;

namespace EDUSphereSharedProject.UniversalModels.ParentLinking
{
    /// <summary>
    /// Result of running SyncStudentsToClasses, which matches students to the classes for their
    /// level, section and group.
    ///
    /// Without this step a newly created student has no StudentClasses rows at all, so they are
    /// absent from class lists, from report card mark sheets, and from anything else driven by
    /// enrolment — while still looking correctly created.
    /// </summary>
    public class SyncStudentClassesResult
    {
        public bool Succeeded { get; set; }

        /// <summary>Enrolment rows the student now has. Zero after creating a student means nothing
        /// matched, which almost always means the level, section or group does not line up with any
        /// class — worth surfacing rather than leaving to be noticed at reporting time.</summary>
        public int StudentClassCount { get; set; }

        public List<string> MatchedClassNames { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// A group token a student can belong to, e.g. "Group A".
    ///
    /// SyncStudentsToClasses matches a group class by name — <c>ClassName LIKE '%(' + GroupName +
    /// ')%'</c> — so the value stored on the student has to be exactly the token that appears inside
    /// the parentheses of the class name. Offering the real values from the school's classes removes
    /// the guesswork that free text invited.
    /// </summary>
    public class StudentGroupOptionDto
    {
        public string GroupName { get; set; } = "";

        /// <summary>Classes a student in this group would be matched to, so an admin can see what
        /// choosing it actually does.</summary>
        public List<string> ClassNames { get; set; } = new();

        public int ClassCount => ClassNames.Count;
    }
}
