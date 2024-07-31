using System.Collections.Specialized;
using System.Net;

namespace PrintDeviceManagementService
{
    internal class Router
    {
        readonly Database database;

        public Router(Database database)
        {
            this.database = database;
        }

        public Response HandleRequest(HttpListenerRequest request)
        {
            try
            {
                string[]? segments = request.Url?.Segments;
                string method = request.HttpMethod;
                NameValueCollection query = request.QueryString;
                if (segments == null || segments.Length < 2)
                    return Response.NotFound();
                switch (segments[1].Replace("/", ""))
                {
                    case "branches":
                        if (method == "GET")
                            return database.GetBranches();
                        return Response.MethodNotAllowed("GET");
                    case "employees":
                        if (method == "GET")
                            return database.GetEmployees();
                        return Response.MethodNotAllowed("GET");
                    case "printers":
                        if (method == "GET")
                            return database.GetPrinters(query["type"]);
                        return Response.MethodNotAllowed("GET");
                    case "installations":
                        if (segments.Length < 3)
                            return method switch
                            {
                                "GET" => database.GetInstallations(query["branch"]),
                                "POST" => database.AddInstallation(new StreamReader(request.InputStream, request.ContentEncoding).ReadToEnd()),
                                _ => Response.MethodNotAllowed("GET, POST"),
                            };
                        else
                        {
                            int id = Convert.ToInt32(segments[2]);
                            return method switch
                            {
                                "GET" => database.GetInstallation(id),
                                "DELETE" => database.DeleteInstallation(id),
                                _ => Response.MethodNotAllowed("GET, DELETE"),
                            };
                        }
                    case "jobs":
                        if (method == "POST")
                            return database.AddJob(new StreamReader(request.InputStream, request.ContentEncoding).ReadToEnd());
                        return Response.MethodNotAllowed("POST");
                }
                return Response.NotFound();
            }
            catch (Exception e)
            {
                return e.GetType().Name switch
                {
                    "JsonSerializationException" or "InvalidInputException" => Response.BadRequest(e.Message),
                    "ResourceNotFoundException" => Response.NotFound(e.Message),
                    "DataConflictException" => Response.Conflict(e.Message),
                    _ => Response.InternalServerError(e.Message),
                };
            }
        }
    }
}
