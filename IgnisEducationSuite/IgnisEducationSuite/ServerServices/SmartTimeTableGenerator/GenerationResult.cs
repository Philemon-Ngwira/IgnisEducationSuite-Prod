namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class GenerationResult
    {
        public bool Success { get; set; } = true;
        public List<string> Errors { get; set; } = new();

        // Factory method for a single error
        public static GenerationResult Failed(string error)
        {
            return new GenerationResult
            {
                Success = false,
                Errors = new List<string> { error }
            };
        }

        // Factory method for multiple errors
        public static GenerationResult Failed(IEnumerable<string> errors)
        {
            return new GenerationResult
            {
                Success = false,
                Errors = new List<string>(errors)
            };
        }

        // Success result
        public static GenerationResult Ok()
        {
            return new GenerationResult { Success = true };
        }

        // Helper to get all errors as a single string
        public string GetErrorMessage()
        {
            return Errors != null && Errors.Count > 0
                ? string.Join("\n", Errors)
                : "Unknown error";
        }
    }
}
