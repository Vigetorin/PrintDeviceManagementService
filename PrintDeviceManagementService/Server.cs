using System.Net;
using System.Text;

namespace PrintDeviceManagementService
{
    internal class Server
    {
        readonly HttpListener httpListener = new();
        readonly Router router;

        bool running;

        public Server(string address, Database database)
        {
            httpListener.Prefixes.Add(address);
            router = new Router(database);
        }

        static void SendResponse(HttpListenerResponse response, Response responseData)
        {
            response.StatusCode = responseData.Status;
            response.ContentType = "application/json; charset=utf-8";
            if (responseData.Headers != null)
                response.Headers.Add(responseData.Headers);
            if (responseData.Message != null)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(responseData.Message);
                response.OutputStream.Write(bytes, 0, bytes.Length);
            }
            response.Close();
        }

        void ResponseThread()
        {
            while (running)
            {
                HttpListenerContext context = httpListener.GetContext();
                SendResponse(context.Response, router.HandleRequest(context.Request));
            }
        }

        public void Start()
        {
            httpListener.Start();
            running = true;
            Thread responseThread = new(ResponseThread);
            responseThread.Start();
        }

        public void Stop()
        {
            running = false;
            httpListener.Stop();
        }
    }
}
