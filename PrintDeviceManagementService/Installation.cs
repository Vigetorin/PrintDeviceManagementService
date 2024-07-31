using Newtonsoft.Json;
using System.Data;

namespace PrintDeviceManagementService
{
    internal class Installation
    {
        public int ID { get; set; }
        public string Name { get; }
        public string Branch { get; }
        public int Number { get; set; }
        public bool Default { get; set; }
        public string Printer { get; }

        public Installation(string json)
        {
            Dictionary<string, string>? data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (data == null || !data.ContainsKey("name") || !data.ContainsKey("branch") || !data.ContainsKey("printer") || !data.ContainsKey("default"))
                throw new InvalidInputException("The request is missing one or more required parameters. Please include 'name', 'branch', 'printer', and 'default' in your request and try again.");
            if (data["name"] == "")
                throw new InvalidInputException("The name field cannot be empty. Ensure that the 'name' value is not an empty string and try again.");
            Name = data["name"];
            Branch = data["branch"];
            Printer = data["printer"];
            if (data.ContainsKey("number"))
            {
                if (!int.TryParse(data["number"], out int number) || number <= 0 || number > 255)
                    throw new InvalidInputException("The provided 'number' is invalid. Please ensure that the 'number' follows the correct format and is within the acceptable range.");
                Number = number;
            }
            else
                Number = -1;
            if (!bool.TryParse(data["default"], out bool isDefault))
                throw new InvalidInputException("The 'default' parameter must be a boolean value (true or false). Please ensure that 'default' is correctly set to either true or false and try again.");
            Default = isDefault;
        }
        public Installation(DataTable dataTable)
        {
            Dictionary<string, object> data = dataTable.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => dataTable.Rows[0][column]);
            ID = (int)data["id"];
            Name = (string)data["name"];
            Branch = (string)data["branch"];
            Number = (int)data["number"];
            Default = (bool)data["default"];
            Printer = (string)data["printer"];
        }
    }
}
