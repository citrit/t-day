using System;
using System.IO;
using System.Threading.Tasks;
using BingMapsRESTToolkit;
using ExcelDataReader;

namespace checkaddress
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            using (var stream = File.Open(args[0], FileMode.Open, FileAccess.Read))
            {
                // Auto-detect format, supports:
                //  - Binary Excel files (2.0-2003 format; *.xls)
                //  - OpenXml Excel files (2007 format; *.xlsx, *.xlsb)
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // Choose one of either 1 or 2:

                    // 1. Use the reader methods
                    //do
                    //{
                    //    while (reader.Read())
                    //    {
                    //        // reader.GetDouble(0);
                    //    }
                    //} while (reader.NextResult());

                    // 2. Use the AsDataSet extension method
                    var result = reader.AsDataSet();

                    // The result of each spreadsheet is in result.Tables
                }
            }
            GeocodeAddress().Wait();
        }

        private static async Task GeocodeAddress()
        {
            var bingKey = "Aq4xnynDuADw0OMoyrHEvCrM8VqhI8HlGIusrLdL30LGJBz71X9JtUCMcVU6mj7Y";

            var request = new GeocodeRequest()
            {
                Query = "River dale Rd, 12309",
                IncludeIso2 = true,
                IncludeNeighborhood = true,
                MaxResults = 25,
                BingMapsKey = bingKey
            };

            //Process the request by using the ServiceManager.
            var response = await request.Execute();

            if (response != null &&
                response.ResourceSets != null &&
                response.ResourceSets.Length > 0 &&
                response.ResourceSets[0].Resources != null &&
                response.ResourceSets[0].Resources.Length > 0)
            {
                var result = response.ResourceSets[0].Resources[0] as Location;

                var coords = result.Point.Coordinates;
                if (coords != null && coords.Length == 2)
                {
                    var lat = coords[0];
                    var lng = coords[1];
                    Console.WriteLine($"Geocode Results - Code: {result.MatchCodes[0]} Lat: {lat} / Long: {lng}");
                }
            }
        }
    }
}
