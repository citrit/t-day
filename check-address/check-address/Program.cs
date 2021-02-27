using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BingMapsRESTToolkit;
using CommandLine;
using CsvHelper;
using ExcelDataReader;
using TDayLib;

namespace checkaddress
{
    class MainClass
    {
        public class Options
        {
            [Option('x', "excelfile", Required = true, HelpText = "Excel file with raw addresses.")]
            public string excelFile { get; set; }

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
            Console.WriteLine("Hello World!");

            var goodAddr = new BlockingCollection<TDayAddress>();
            var badAddr = new BlockingCollection<TDayAddress>();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            ReadExcelFile(opts.excelFile, opts.outputDir, goodAddr, badAddr);
            sw.Stop();
            Console.WriteLine($"\nValidate addresses time: {sw.Elapsed}");
        }

        private static void ReadExcelFile(string excelFile, string outDir, BlockingCollection<TDayAddress> goodAddr, BlockingCollection<TDayAddress> badAddr)
        {
            using (var stream = File.Open(excelFile, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var dataSet = reader.AsDataSet();
                    // Now you can get data from each sheet by its index or its "name"
                    var dataTable = dataSet.Tables[0];
                    int numCPU =  Environment.ProcessorCount * 4;


                    Parallel.ForEach(dataTable.Rows.OfType<DataRow>(), new ParallelOptions { MaxDegreeOfParallelism = numCPU }, (Row) =>
                    {
                        // tblRecipient_Address, tblRecipient_Address1, tblRecipient_City, tblRecipient_State, tblRecipient_PostalCode
                        TDayAddress addr = new TDayAddress()
                        {
                            FirstName = Row[0].ToString(),
                            LastName = Row[1].ToString(),
                            Address1 = Row[2].ToString(),
                            Address2 = Row[3].ToString(),
                            City = Row[4].ToString(),
                            State = Row[5].ToString(),
                            Zip = "1" + Row[6].ToString().Remove(0, 1),
                            HomePhone = Row[7].ToString(),
                            CellPHone = Row[8].ToString()//,
                            //Note = Row[9].ToString()
                        };
                        //Console.Out.WriteLine(addr);
                        GeocodeAddress(addr, goodAddr, badAddr).Wait();
                        Console.Write($"Procesed count: {goodAddr.Count + badAddr.Count}\r");
                    });

                    // Write out the addresses
                    using (var writer = new StreamWriter(Path.Combine(outDir, "GoodAddresses.csv")))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(goodAddr);
                    }
                    using (var writer = new StreamWriter(Path.Combine(outDir, "BadAddresses.csv")))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(badAddr);
                    }
                }
            }
        }

        private static async Task GeocodeAddress(TDayAddress addr, BlockingCollection<TDayAddress> goodAddr, BlockingCollection<TDayAddress> badAddr)
        {
            var bingKey = "Aq4xnynDuADw0OMoyrHEvCrM8VqhI8HlGIusrLdL30LGJBz71X9JtUCMcVU6mj7Y";

            //ServiceManager.Proxy = new WebProxy("http://newproxy.research.ge.com:80/");

            var request = new GeocodeRequest()
            {
                Query = addr.ToAddress(),
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
                Location foundAddr = null;

                foreach (var res in response.ResourceSets[0].Resources)
                {
                    if (((Location)res).Address.AdminDistrict == "NY" && ((Location)res).Address.PostalCode != null && ((Location)res).Address.PostalCode.StartsWith("12"))
                    {
                        foundAddr = (Location)res;
                        break;
                    }
                }
                //var result = response.ResourceSets[0].Resources[0] as Location;

                if (foundAddr != null && foundAddr.MatchCodes[0].ToString() == "Good")
                {
                    addr.Address1 = foundAddr.Address.AddressLine;
                    addr.City = foundAddr.Address.Locality;
                    addr.State = foundAddr.Address.AdminDistrict;
                    addr.Zip = foundAddr.Address.PostalCode;
                    var coords = foundAddr.Point.Coordinates;
                    if (coords != null && coords.Length == 2)
                    {
                        addr.Lat = (float)coords[0];
                        addr.Lon = (float)coords[1];
                    }
                    goodAddr.Add(addr);
                }
                else
                {
                    badAddr.Add(addr);
                }
                //Console.Write($"Geocode: {addr} => Results - Code: {result.MatchCodes[0]}\r");
            }
        }
    }
}
