using System;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
namespace SensorsServer
{
    public class LocationUpdate
    {
        /* obj.put("latitude", stepTo.latitude);
                obj.put("longitude", stepTo.longitude);
                obj.put("timestamp", timestamp);
                obj.put("channelID", mainAct.channelID);
                obj.put("roomName", mainAct.roomName);
        */
        
        public double latitude { get; set; }
        public double longitude { get; set; }
        public long timestamp { get; set; }
        public string channelID { get; set; }
        public string roomName { get; set; }
        public int color { get; set; }
        public string name { get; set; }

    }
    class HttpServer
    {
        
        public static HttpListener listener;
        public static string url = "http://192.168.43.157:6667/";
        public static Dictionary<string, List<LocationUpdate>> 
          rooms = new Dictionary<string, List<LocationUpdate>>();
        public static string GetRequestPostData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                return null;
            }
            using (System.IO.Stream body = request.InputStream) // here we have data
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(body, request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static List<LocationUpdate> UpdateLocatiotions(LocationUpdate location)
        {
            try
            {
                List<LocationUpdate> locationsToUpdate = new List<LocationUpdate>();
                List<LocationUpdate> locations = rooms[location.roomName];
                for(int i = 0; i < locations.Count; i++)
                {
                    if (locations[i].name == location.name)
                    {
                        locations[i] = location;
                    }
                    else
                    {
                        locationsToUpdate.Add(locations[i]);
                    }
                }

                return locationsToUpdate;
            }
            catch(KeyNotFoundException e)
            {
                List<LocationUpdate> locations = new List<LocationUpdate>();
                locations.Add(location);
                rooms.Add(location.roomName, locations);
                
            }catch(Exception e)
            {
                Console.Write(e.StackTrace);
            }
            return null;
        }
        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            int responseCount = 0;
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                String response = GetRequestPostData(req);
                LocationUpdate locUpdate = JsonSerializer.Deserialize<LocationUpdate>(response);
                Console.WriteLine("Room name is:"+ locUpdate.roomName);
                Console.WriteLine(response);
                List<LocationUpdate> locations = UpdateLocatiotions(locUpdate);
                string locationUpdate = "No update";
                if (locations != null) {
                    locationUpdate = JsonSerializer.Serialize(locations);
                }

                byte[] data = Encoding.UTF8.GetBytes(locationUpdate);
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }


        public static void Main(string[] args)
        {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }
    }
}
