using System.Collections.Specialized;

namespace PrintDeviceManagementService
{
    internal class Response
    {
        public int Status { get; }
        public string? Message { get; }
        public NameValueCollection? Headers { get; }

        public Response(int status, string? message = null, NameValueCollection? headers = null)
        {
            Status = status;
            Headers = headers;
            Message = message;
        }

        public static Response OK(string? data = null) => new(200, data);
        public static Response Created(string data) => new(201, data);

        public static Response BadRequest(string message) => new(400, message);
        public static Response NotFound(string message = "Not Found") => new(404, message);
        public static Response MethodNotAllowed(string allowed) => new(405, "Method Not Allowed", new() { { "Allow", allowed } });
        public static Response Conflict(string message) => new(409, message);

        public static Response InternalServerError(string message) => new(500, message);
    }
}
