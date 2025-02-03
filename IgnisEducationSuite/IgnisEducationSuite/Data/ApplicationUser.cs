using Microsoft.AspNetCore.Identity;

namespace IgnisEducationSuite.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public bool isFirstLogin { get; set; }
        public string? UserID { get; set; }
        public bool requiresPasswordReset { get; set; }
        public bool AccountActive { get; set; }

        public Guid? SchoolID { get; set; }
        public byte[]? ProfilePic { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

}
