using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;

namespace PrintDeviceManagementService
{
    internal class Database
    {
        readonly SqlConnection connection;
        readonly Cache cache = new();

        readonly JsonSerializerSettings serializerSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        public Database(string connectionString)
        {
            connection = new(connectionString);
            connection.Open();
        }

        DataTable GetData(string command)
        {
            DataTable dataTable = new();
            dataTable.Load(new SqlCommand(command, connection).ExecuteReader());
            return dataTable;
        }
        bool? GetBool(string command) => (bool?)new SqlCommand(command, connection).ExecuteScalar();
        int GetNumber(string command)
        {
            object result = new SqlCommand(command, connection).ExecuteScalar();
            return result != null ? (int)result : -1;
        }
        string GetJsonData(string command) => JsonConvert.SerializeObject(GetData(command));

        public Response GetBranches() => Response.OK(GetJsonData("SELECT name FROM Branches"));
        public Response GetEmployees() => Response.OK(GetJsonData("SELECT Employees.name AS name, Branches.name AS branch FROM Employees " +
                                                                  "JOIN Branches ON Employees.branch_id = Branches.branch_id"));
        public Response GetPrinters(string? type = null)
        {
            if (type == null)
                return Response.OK(GetJsonData("SELECT name, CASE WHEN local = 1 THEN 'local' ELSE 'network' END AS type FROM Printers"));
            else if (type == "local" || type == "network")
                return Response.OK(GetJsonData("SELECT name, CASE WHEN local = 1 THEN 'local' ELSE 'network' END AS type FROM Printers " +
                                              $"WHERE local = {(type == "local" ? 1 : 0)}"));
            return Response.NotFound();
        }

        public Response GetInstallations(string? branch = null)
        {
            if (branch == null)
                return Response.OK(GetJsonData("SELECT Installations.installation_id AS id, Installations.name, Branches.name AS branch FROM Installations " +
                                               "JOIN Branches ON Branches.branch_id = Installations.branch_id"));
            else
                return Response.OK(GetJsonData("SELECT Installations.installation_id AS id, Installations.name, Branches.name AS branch FROM Installations " +
                                               "JOIN Branches ON Branches.branch_id = Installations.branch_id " +
                                              $"WHERE Branches.name = '{branch}'"));
        }
        public Response GetInstallation(int id)
        {
            if (cache.TryGet(id, out Installation? cached))
                return Response.OK(JsonConvert.SerializeObject(cached, serializerSettings));
            DataTable data = GetData("SELECT Installations.installation_id AS id, Installations.name, Branches.name AS branch, Installations.number, Installations.is_default AS 'default', Printers.name AS printer " +
                                     "FROM Installations " +
                                     "JOIN Branches ON Branches.branch_id = Installations.branch_id " +
                                     "JOIN Printers ON Printers.printer_id = Installations.printer_id " +
                                    $"WHERE installation_id = {id}");
            if (data.Rows.Count == 0)
                return Response.NotFound();
            Installation installation = new(data);
            cache.Add(installation);
            return Response.OK(JsonConvert.SerializeObject(installation, serializerSettings));
        }
        public Response AddInstallation(string data)
        {
            Installation installation = new(data);

            int branch = GetNumber($"SELECT branch_id FROM Branches WHERE name = '{installation.Branch}'");
            int printer = GetNumber($"SELECT printer_id FROM Printers WHERE name = '{installation.Printer}'");
            if (branch == -1)
                throw ResourceNotFoundException.Generate("branch");
            if (printer == -1)
                throw ResourceNotFoundException.Generate("printer");

            if (installation.Number == -1)
            {
                installation.Number = GetNumber($"SELECT CASE WHEN COUNT(*) = 0 THEN 1 ELSE MAX(number) + 1 END FROM Installations WHERE branch_id = {branch}");
                if (installation.Number > 255)
                    throw new DataConflictException("The maximum value for 'number' in this branch has been reached. Please provide a free 'number' or remove existing entries to make space for new ones.");
            }
            else if (GetNumber($"SELECT COUNT(*) FROM Installations WHERE branch_id = {branch} AND number = {installation.Number}") != 0)
                throw new DataConflictException("The provided 'number' already exists in the branch. Please provide a unique 'number' or omit it to let the system generate one automatically.");

            int currentDefault = GetNumber($"SELECT installation_id FROM Installations WHERE branch_id = {branch} AND is_default = 1");
            if (!installation.Default && currentDefault == -1)
                throw new DataConflictException("No default installation exists in this branch. Please set 'default' to true to designate this installation as the default.");
            int id;
            if (installation.Default && currentDefault != -1)
            {
                //throw new Exception("A default installation is already set for this branch. Please set 'default' to false or remove the existing default installation before setting a new one.");
                id = Convert.ToInt32(new SqlCommand($"UPDATE Installations SET is_default = 0 WHERE installation_id = {currentDefault} " + 
                                                     "INSERT INTO Installations (name, branch_id, number, is_default, printer_id) " +
                                                    $"VALUES ('{installation.Name}', {branch}, {installation.Number}, {(installation.Default ? 1 : 0)}, {printer}) " +
                                                     "SELECT SCOPE_IDENTITY()", connection).ExecuteScalar());
                if (cache.TryGet(currentDefault, out Installation? cached))
                    cached.Default = false;
            }
            else
                id = Convert.ToInt32(new SqlCommand("INSERT INTO Installations (name, branch_id, number, is_default, printer_id) " +
                                                   $"VALUES ('{installation.Name}', {branch}, {installation.Number} ,  {(installation.Default ? 1 : 0)}, {printer}) " +
                                                    "SELECT SCOPE_IDENTITY()", connection).ExecuteScalar());
            installation.ID = id;
            cache.Add(installation);
            return Response.Created($"{{\"id\":\"{id}\"}}");
        }
        public Response DeleteInstallation(int id)
        {
            bool isDefault = GetBool($"SELECT is_default FROM Installations WHERE installation_id = {id}") ?? throw new ResourceNotFoundException("The specified installation does not exist.");
            if (!isDefault)
            {
                new SqlCommand($"DELETE FROM Installations WHERE installation_id = {id}", connection).ExecuteNonQuery();
                return Response.OK();
            }
            int newDefault = GetNumber($"SELECT installation_id FROM Installations WHERE installation_id != {id} AND branch_id = (" +
                                       $"SELECT branch_id FROM Installations WHERE installation_id = {id})");
            if (newDefault == -1)
                throw new DataConflictException("The installation is currently set as default and is the only installation in the branch. " +
                    "Please set another installation as default or add a new installation before attempting to remove this one.");
            new SqlCommand($"UPDATE Installations SET is_default = 1 WHERE installation_id = {newDefault} " +
                           $"DELETE FROM Installations WHERE installation_id = {id}", connection).ExecuteScalar();
            if (cache.TryGet(newDefault, out Installation? cached))
                cached.Default = true;
            cache.Remove(id);
            return Response.OK();
        }

        public Response AddJob(string data)
        {
            Job job = new(data);

            int employee = GetNumber($"SELECT employee_id FROM Employees WHERE name = '{job.Employee}'");
            if (employee == -1)
                throw ResourceNotFoundException.Generate("employee");

            if (job.InstallationNumber != -1)
            {
                bool contains = GetNumber("SELECT COUNT(*) FROM Installations " +
                                          "JOIN Employees ON Installations.branch_id = Employees.branch_id " +
                                         $"WHERE Employees.employee_id = {employee} AND Installations.number = {job.InstallationNumber}") != 0;
                if (!contains)
                    throw ResourceNotFoundException.Generate("installation", "number");
            }
            else
            {
                job.InstallationNumber = GetNumber("SELECT Installations.number FROM Installations " +
                                                   "JOIN Employees ON Installations.branch_id = Employees.branch_id " +
                                                  $"WHERE Employees.employee_id = {employee} AND Installations.is_default = 1");
            }

            job.Execute();

            new SqlCommand("INSERT INTO Jobs (name, employee_id, printer_number, page_count, success) " +
                          $"VALUES ('{job.Name}', {employee}, {job.InstallationNumber}, {job.PageCount}, {(job.Success ? 1 : 0)}) ", connection).ExecuteNonQuery();

            return Response.Created($"{{\"success\":{job.Success.ToString().ToLower()}}}");
        }
    }
}
