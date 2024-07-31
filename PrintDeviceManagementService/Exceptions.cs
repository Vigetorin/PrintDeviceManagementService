namespace PrintDeviceManagementService
{
    internal class InvalidInputException : Exception
    {
        public InvalidInputException(string message) : base(message) { }
    }

    internal class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException(string message) : base(message) { }

        public static ResourceNotFoundException Generate(string name, string? value = null)
        {
            return new($"The specified {name} could not be found. Please ensure that the '{value ?? name}' value is correct and corresponds to an existing {name}. Verify and try again.");
        }
    }

    internal class DataConflictException : Exception
    {
        public DataConflictException(string message) : base(message) { }
    }
}
