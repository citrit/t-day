using Auios.QuadTree;
using CommandLine;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using TDayLib;

namespace gen_routes
{
    class Program
    {
        public class Options
        {
            [Option('i', "inputFile", Required = true, HelpText = "CSV File with validated addresses.")]
            public string inputFile { get; set; }

            [Option('o', "outputDir", Required = true, HelpText = "Folder to write the results.")]
            public string outputDir { get; set; }
        }

        public static void Main(string[] args)
        {

            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
        }

        private static void RunOptions(Options opts)
        {
            using (var reader = new StreamReader(opts.inputFile))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<TDayAddress>();

                GenerateRoutes(records, opts.outputDir);
            }
        }

        // implement IQuadTreeObjectBounds<T> interface for your object type 
        public class MyPolygonBounds : IQuadTreeObjectBounds<TDayAddress>
        {
            public float GetLeft(TDayAddress obj) => obj.Lon;

            public float GetRight(TDayAddress obj) => obj.Lon;

            public float GetTop(TDayAddress obj)
            {
                return obj.Lat;
            }

            public float GetBottom(TDayAddress obj)
            {
                return obj.Lat;
            }
        }

        private static void GenerateRoutes(IEnumerable<TDayAddress> records, string outDir)
        {

            int delivertCnt = 0;

            // create a QuadTree and fill it with objects
            var quadTree = new QuadTree<TDayAddress>(-80F, 35, 10, 10, new MyPolygonBounds(), 6, 14);
            //quadTree.InsertRange(records); // "myPolygons" are an array of all your objects
            using (StreamWriter ew = new StreamWriter(Path.Combine(outDir, "DeliveriesErrors.txt")))
            {
                foreach (var obj in records)
                {
                    if (!quadTree.Insert(obj))
                        ew.WriteLine($"Where the heck: {obj.FirstName} {obj.LastName}, {obj.City} {obj.State} {obj.Zip} "); // throw new ApplicationException("Failed insert");
                    else
                        delivertCnt++;
                }
            }
            Console.Out.WriteLine($"Inserted {delivertCnt} into sorting");

            delivertCnt = 0;
            Dictionary<int, TDayAddress[]>  grids = new Dictionary<int, TDayAddress[]> ();
            StreamWriter sw = new StreamWriter(Path.Combine(outDir,"Deliveries.txt"));
            quadTree.GetDeliveries(grids);
            foreach (KeyValuePair<int, TDayAddress[]> delivery in grids)
            {
                //Console.WriteLine($"DeliverID: {delivery.Key}: with {delivery.Value.Length} stops: ");
                sw.WriteLine($"DeliverID: {delivery.Key}: with {delivery.Value.Length} stops: ");
                foreach (var res in delivery.Value)
                {
                    //Console.WriteLine($"\t Delivery: {res.ToString()}");
                    sw.WriteLine($"\t Delivery: {res.ToString()}");
                    delivertCnt++;
                }    // do something with entry.Value or entry.Key
            }
            Console.WriteLine($"Scheduled {delivertCnt} deliveries");
            sw.WriteLine($"Scheduled {delivertCnt} deliveries");
            sw.Close();

            // find the intersecting objects among the nearest 
            // bool IsIntersect(Point[] obj1, Point[] obj2) is your function for checking intersections
            //var intersectingObjects = nearestObjects.Where(nearest => IsIntersect(nearest, myPolygon);
        }
    }
}
