using System;
using System.Collections.Generic;
using System.Linq;

namespace EDUSphereSharedProject.UniversalModels.Updates
{
    public enum ReleaseChangeKind
    {
        New,
        Improved,
        Fixed,
        Security,
    }

    /// <summary>One line in a release's notes, written for the people who use Ignis rather than for
    /// the people who build it.</summary>
    public class ReleaseChange
    {
        public ReleaseChangeKind Kind { get; init; }

        /// <summary>Short enough to scan in a list.</summary>
        public string Title { get; init; } = "";

        /// <summary>What it means in practice. Say what someone can now do, or what stopped going
        /// wrong — not which class was edited.</summary>
        public string Detail { get; init; } = "";

        /// <summary>
        /// Who this matters to, when it is not everyone. Shown as a small label so a school
        /// administrator can tell at a glance which lines are about their school and which are about
        /// the platform as a whole.
        /// </summary>
        public string? Audience { get; init; }
    }

    public class ReleaseNote
    {
        public string Version { get; init; } = "";
        public DateOnly ReleasedOn { get; init; }

        /// <summary>One sentence describing the release, shown under the version number.</summary>
        public string Headline { get; init; } = "";

        public List<ReleaseChange> Changes { get; init; } = new();

        public IEnumerable<ReleaseChange> Of(ReleaseChangeKind kind) => Changes.Where(c => c.Kind == kind);

        public bool Has(ReleaseChangeKind kind) => Changes.Any(c => c.Kind == kind);
    }

    /// <summary>
    /// The update history shown on the What's New page.
    ///
    /// Held in code rather than in the database, deliberately. Notes describe the build that is
    /// running, so shipping them with that build means the two can never disagree — no migration to
    /// run, and no chance of a school reading about a feature its deployment does not have. It also
    /// makes the habit cheap: adding a release is adding one entry at the top of this list.
    ///
    /// <para>
    /// Adding a release:
    /// insert the new <see cref="ReleaseNote"/> as the FIRST element of <see cref="All"/>. Everything
    /// else — the footer's version number, the "new" badge in the menu, and the page itself — reads
    /// from here, so nothing else needs changing.
    /// </para>
    /// </summary>
    public static class ReleaseNotes
    {
        /// <summary>Newest first. The first entry is the running version.</summary>
        public static IReadOnlyList<ReleaseNote> All { get; } = new List<ReleaseNote>
        {
            new ReleaseNote
            {
                Version = "1.0.6",
                ReleasedOn = new DateOnly(2026, 9, 11),
                Headline = "A large release. Report cards are now entered a class at a time, timetable " +
                           "generation has been rebuilt, chat delivers in real time, and licensing, " +
                           "enrolment and offline caching have all had significant fixes.",
                Changes = new List<ReleaseChange>
                {
                    // ---- New ----
                    new()
                    {
                        Kind = ReleaseChangeKind.New,
                        Title = "Enter report card marks a whole class at a time",
                        Detail = "Teachers now open the subject-class they teach and mark every enrolled student " +
                                 "on one sheet, instead of opening students one by one. The sheet shows how many " +
                                 "of the class have been entered, highlights anyone still blank, and warns before " +
                                 "saving a partly-filled sheet. Entering per student is what allowed students to " +
                                 "be missed entirely.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.New,
                        Title = "Edit a generated timetable by hand",
                        Detail = "A generated timetable can now be adjusted directly, with clashes checked as you " +
                                 "go, rather than having to regenerate the whole thing to move one lesson.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.New,
                        Title = "Students with no parent on record",
                        Detail = "Your dashboard now tells you how many students have no parent linked, and which " +
                                 "classes they are in. From there you can attach them to an existing parent or " +
                                 "create the parent's account without leaving the screen. Those students were " +
                                 "invisible to the parent portal, report card notifications and chat.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.New,
                        Title = "Edit subject policy in bulk",
                        Detail = "Set periods per week, doubles, core status and preferred time of day for a whole " +
                                 "level and section at once, then save them together rather than one subject at a " +
                                 "time.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.New,
                        Title = "Your licence, on your dashboard",
                        Detail = "See your plan, when it runs out, how many student places you have used and how " +
                                 "many are left, and exactly what you can and cannot do right now. Teachers, " +
                                 "parents and staff do not count towards the student limit.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.New,
                        Title = "Recalculate school positions",
                        Detail = "A button on Report Card Management works out every student's position across " +
                                 "the whole school again from their current marks. Run it once marks are " +
                                 "finalised, or after correcting a late entry.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.New,
                        Title = "This page",
                        Detail = "Every update from now on is listed here, so you can see what changed without " +
                                 "having to ask.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.New,
                        Title = "Platform console",
                        Detail = "Every school on Ignis in one place, with its size, licence state and " +
                                 "administrators, and anything that needs attention listed first.",
                        Audience = "Philtiara staff",
                    },

                    // ---- Improved ----
                    new()
                    {
                        Kind = ReleaseChangeKind.Improved,
                        Title = "Timetable generation rebuilt",
                        Detail = "Subjects set to early-morning, morning or afternoon are now placed accordingly " +
                                 "throughout, break and lunch slots are left alone, several sections can be " +
                                 "generated in one run, and the settings you enter are remembered rather than " +
                                 "being asked for again each time.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Improved,
                        Title = "Teachers only see the classes they teach",
                        Detail = "The report card list used to show every class in the school. A teacher now sees " +
                                 "only their own subject-classes, and can narrow further by level and section.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Improved,
                        Title = "Switching role now takes effect",
                        Detail = "Someone who is both an administrator and a teacher used to see the " +
                                 "administrator's view regardless. Choosing Teacher in the role selector now gives " +
                                 "the teacher's view, scoped to their own classes.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Improved,
                        Title = "Chat arrives wherever you are",
                        Detail = "Messages now appear immediately anywhere in the app, not only on the chat page " +
                                 "and not only after a refresh. Unread counts show in the menu and against each " +
                                 "conversation, and a notification opens the conversation when clicked.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Improved,
                        Title = "Subject policy is filtered",
                        Detail = "Rather than listing every subject in the school, subject policy and the " +
                                 "\"must not follow\" rules now show only the level and section you are working on.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Improved,
                        Title = "A licence problem no longer hides the app",
                        Detail = "An expired or unverified licence used to blank the dashboard and disable the " +
                                 "whole administration menu — including the link to your own profile. Now you are " +
                                 "told clearly, everything you have already recorded stays available, and only " +
                                 "adding new users is paused.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Improved,
                        Title = "Registering a school",
                        Detail = "Details are now checked before saving, duplicate names are refused, and once a " +
                                 "school is created you are taken straight through activating its licence and " +
                                 "adding its first administrator.",
                        Audience = "Philtiara staff",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Improved,
                        Title = "Managing administrators",
                        Detail = "Choosing a school now loads its administrators immediately, and a school with no " +
                                 "administrator at all is called out — nobody can sign in to run it.",
                        Audience = "Philtiara staff",
                    },

                    // ---- Fixed ----
                    new()
                    {
                        Kind = ReleaseChangeKind.Fixed,
                        Title = "A student's overall grade could be overwritten",
                        Detail = "The overall grade was recalculated from only the subject being saved, so " +
                                 "whichever teacher saved last replaced it with their own average. It is now " +
                                 "worked out from every subject on the report card, each time any of them is " +
                                 "saved.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Fixed,
                        Title = "A blank mark counted as zero",
                        Detail = "Leaving a mark empty was treated as a score of nought, which dragged averages " +
                                 "and positions down. Blank now means \"not yet entered\" and is shown as " +
                                 "outstanding rather than scored.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Fixed,
                        Title = "Students added one at a time were in no classes",
                        Detail = "Adding a student individually skipped the class-matching step that the bulk " +
                                 "upload runs, so they were enrolled in nothing and never appeared on a mark " +
                                 "sheet. Individual students are now matched to their classes on creation, and " +
                                 "you are told which classes they joined. A parent can be linked at the same " +
                                 "time, or later.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Fixed,
                        Title = "Timetable clashes and misplaced lessons",
                        Detail = "Teachers could be double-booked when the timetable was being improved after " +
                                 "generation, lessons could land in break and lunch slots, option-set classes " +
                                 "were scheduled as though they were ordinary subjects, and the early-morning " +
                                 "cut-off was ignored outside the first pass.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Fixed,
                        Title = "Chat problems",
                        Detail = "Messages did not appear until the page was refreshed, the New Message button did " +
                                 "nothing, and sending could report \"message not sent\" even though it had been " +
                                 "delivered. All three are resolved, and the connection now recovers on its own " +
                                 "after a dropout.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Fixed,
                        Title = "Schools shown as unlicensed when they were not",
                        Detail = "If the licensing service could not be reached, that was treated as proof the " +
                                 "school had no licence, and the wrong answer was kept for half an hour. A check " +
                                 "that cannot be completed is now reported as unverified, retried shortly after, " +
                                 "and never restricts anything.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Fixed,
                        Title = "\"Failed to fetch\" errors and pages that would not update",
                        Detail = "The offline cache was interfering with saving, messaging and signing in, and " +
                                 "could keep serving an old version of a page after an update. It now stays out " +
                                 "of the way, and new versions appear as soon as they are released.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Fixed,
                        Title = "Suspend, restore and reset password did nothing",
                        Detail = "These buttons on the administrator list were not connected to anything. They now " +
                                 "work, and a password reset is emailed with its own confirmation.",
                        Audience = "Philtiara staff",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Fixed,
                        Title = "Schools without a logo could not be opened",
                        Detail = "Managing a school that had no logo uploaded failed to open at all.",
                        Audience = "Philtiara staff",
                    },

                    // ---- Security ----
                    new()
                    {
                        Kind = ReleaseChangeKind.Security,
                        Title = "Licence changes require a signed-in platform administrator",
                        Detail = "Activating or terminating a licence is now restricted to Philtiara staff, and a " +
                                 "school is always identified from the signed-in account rather than from the " +
                                 "request.",
                    },
                    new()
                    {
                        Kind = ReleaseChangeKind.Security,
                        Title = "Less account data sent to the browser",
                        Detail = "Administrator lists now send only what is displayed on screen, rather than the " +
                                 "full stored account record.",
                    },
                },
            },
        };

        /// <summary>The running version. Single source of truth for the footer and the menu badge —
        /// the footer previously carried its own hardcoded number, which had to be remembered
        /// separately and had already fallen behind.</summary>
        public static ReleaseNote Current => All[0];

        public static string CurrentVersion => Current.Version;
    }
}
