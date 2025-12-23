using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class GenerationResult
    {
        public bool Success { get; set; } = true;
        public List<string> Errors { get; set; } = new();

        public List<GeneratedSlotPreview>? Slots { get; set; }

        public static GenerationResult Failed(string error)
        {
            return new GenerationResult
            {
                Success = false,
                Errors = new List<string> { error }
            };
        }

        public static GenerationResult Failed(IEnumerable<string> errors)
        {
            return new GenerationResult
            {
                Success = false,
                Errors = new List<string>(errors)
            };
        }

        public static GenerationResult Ok(List<GeneratedSlotPreview>? slots = null)
        {
            return new GenerationResult { Success = true, Slots = slots };
        }

        public string GetErrorMessage()
        {
            return Errors != null && Errors.Count > 0
                ? string.Join("\n", Errors)
                : "Unknown error";
        }
    }



}
