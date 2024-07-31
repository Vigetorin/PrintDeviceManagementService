using Newtonsoft.Json;

namespace PrintDeviceManagementService
{
    internal class Job
    {
        public string Name { get; }
        public string Employee { get; }
        public int InstallationNumber { get; set; }
        public int PageCount { get; }
        public bool Success { get; private set; }

        public Job(string json)
        {
            Dictionary<string, string>? data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (data == null || !data.ContainsKey("name") || !data.ContainsKey("employee") || !data.ContainsKey("count"))
                throw new InvalidInputException("The request is missing one or more required parameters. Please include 'name', 'employee' and 'count' in your request and try again.");
            if (data["name"] == "")
                throw new InvalidInputException("The name field cannot be empty. Ensure that the 'name' value is not an empty string and try again.");
            Name = data["name"];
            Employee = data["employee"];
            if (!int.TryParse(data["count"], out int count) || count <= 0)
                throw new InvalidInputException("The provided 'count' is invalid. Please ensure that the 'count' follows the correct format and is within the acceptable range.");
            PageCount = count;
            if (data.ContainsKey("number"))
            {
                if (!int.TryParse(data["number"], out int number) || number <= 0 || number > 255)
                    throw new InvalidInputException("The provided 'number' is invalid. Please ensure that the 'number' follows the correct format and is within the acceptable range.");
                InstallationNumber = number;
            }
            else
                InstallationNumber = -1;
        }

        public void Execute()
        {
            Thread.Sleep(RandomGenerator.GetTime());
            Success = RandomGenerator.GetResult();
        }
    }
}
