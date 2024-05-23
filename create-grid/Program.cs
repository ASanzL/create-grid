using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using CreateGrid;
using System.IO;
using MongoDB.Bson;
using System.Diagnostics;

namespace CreateGrid
{
    class Program
    {
        private const double EarthRadius = 6371000;
        static async Task Main(string[] args)
        {
            string connectionString = "mongodb://127.0.0.1:27017";
            string databaseName = "greenmapper";
            Console.WriteLine("Using database: greenmapper");
            Console.Write("Collectionname(should be empty): ");
            string collectionName = Console.ReadLine();
            for (int i = 0; i < 1; i++)
            {
                MongoClient client = new MongoClient(connectionString);
            IMongoDatabase db = client.GetDatabase(databaseName);
            IMongoCollection<CellModel> collection = db.GetCollection<CellModel>(collectionName);

            Stopwatch stopWatch = new Stopwatch();

            const double minLat = -90;
            const double maxLat = 90;
            const double minLon = -180;
            const double maxLon = 180;
            Console.Write("Cell size (meters): ");
            int cellSize = Convert.ToInt32(Console.ReadLine());

            long approximateNumberOfCells = 510000000000000 / (long)(Math.Pow(cellSize, 2));
                Console.WriteLine("Creating approximately " + approximateNumberOfCells + " cells...");

                var tasks = new List<Task>();
            var vertices = new List<CellModel>();
            var verticesPerThread = 500000;

            int cellsInserted = 0;
            
                double latStepAmount = Math.Abs(minLat - CalculateDegreesLatitude(minLat, cellSize));
                Console.WriteLine(minLat + 15);
                Console.WriteLine(latStepAmount - 100);
                double lonStepAmount = Math.Abs(CalculateDegreesLongitude(minLon, cellSize, minLat, minLat + latStepAmount));
                double lastLon = minLon;
                stopWatch.Start();
                for (double lat = minLat; lat + latStepAmount < maxLat + 0.0001; lat += latStepAmount)
                {
                    latStepAmount = Math.Abs(lat - CalculateDegreesLatitude(lat, cellSize, 0));
                    lonStepAmount = Math.Abs(CalculateDegreesLongitude(lastLon, cellSize, lat, lat + latStepAmount));
                    for (double lon = minLon; maxLon > lon + lonStepAmount; lon += lonStepAmount)
                    {
                        lastLon = lon;

                        vertices.Add(new CellModel
                        {
                            _id = new LocationId { lat = lat, lon = lon, cellSize = cellSize },
                            geometry = new Geometry { type = "Point", coordinates = new double[] { ((lon + 180) % 360 + 360) % 360 - 180, lat } },
                            offset = new Offset { lon = lonStepAmount, lat = latStepAmount }
                        });

                        if (vertices.Count >= verticesPerThread)
                        {
                            var verticesCopy = vertices.ToList();
                            tasks.Add(Task.Run(async () =>
                            {
                                await collection.InsertManyAsync(verticesCopy, new InsertManyOptions { IsOrdered = false });
                                cellsInserted += verticesPerThread;
                                Console.Write("\r{0}%   ", Decimal.Round((decimal)cellsInserted * 100 / (decimal)approximateNumberOfCells));
                            }));
                            vertices.Clear();
                            if (tasks.Count() > 5)
                            {
                                await Task.WhenAll(tasks);
                            }
                        }
                    }
                }

                if (vertices.Any())
                {
                    await collection.InsertManyAsync(vertices, new InsertManyOptions { IsOrdered = false });
                    Console.Write("\r{0}%   ", 100);
                }

                await Task.WhenAll(tasks);
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;

                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds);
                Console.WriteLine("RunTime " + elapsedTime);
                Console.WriteLine("grid creation done");
            }
        }

        public static double CalculateDegreesLatitude(double lat1, double distance, double bearing = 90)
        {
            double lat1Rad = lat1 * (Math.PI / 180); // Convert latitude to radians
            double bearingRad = bearing * (Math.PI / 180); // Convert bearing to radians

            return Math.Asin(Math.Sin(lat1Rad) * Math.Cos(distance / EarthRadius) + Math.Cos(lat1Rad) * Math.Sin(distance / EarthRadius) * Math.Cos(bearingRad)) * (180 / Math.PI);
        }

        public static double CalculateDegreesLongitude(double lon1, double distance, double lat1, double lat2, double bearing = 90)
        {
            double lat1Rad = lat1 * (Math.PI / 180); // Convert latitudes to radians
            double lat2Rad = lat2 * (Math.PI / 180);
            double bearingRad = bearing * (Math.PI / 180); // Convert bearing to radians

            return Math.Atan2(Math.Sin(bearingRad) * Math.Sin(distance / EarthRadius) * Math.Cos(lat1Rad),
                Math.Cos(distance / EarthRadius) - Math.Sin(lat1Rad) * Math.Sin(lat2Rad)) * (180 / Math.PI);
        }
    }
}