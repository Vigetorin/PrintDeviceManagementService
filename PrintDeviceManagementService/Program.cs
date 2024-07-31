using PrintDeviceManagementService;

class Program
{
    static void Main()
    {
        Database database = new("Data Source=TERMINATOR;Initial Catalog=PrintDeviceManagementServiceDatabase;Integrated Security=True;Encrypt=False");
        Server server = new("http://localhost:23456/", database);
        server.Start();
    }
}