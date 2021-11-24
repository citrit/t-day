using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BingMapsRESTToolkit;
using CsvHelper;
using ExcelDataReader;
using System.Collections.Concurrent;
using Auios.QuadTree;
using System.Net;
using System.Threading;
using System.Xml.Linq;

namespace TDayLib
{
    public delegate void MsgOut(string msg, bool status = false);

    public class TDayAddress
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string AptNum { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string HomePhone { get; set; }
        public string CellPHone { get; set; }
        public string DelDay { get; set; }
        public int NumMeals { get; set; }
        public string Notes { get; set; }
        public float Lon { get; set; }
        public float Lat { get; set; }
        public string Restaurant { get; set; }

        public override string ToString()
        {
            return $"{FirstName},{LastName},{Address1},{AptNum},{Address2},{City},{State},{Zip},{HomePhone},{CellPHone},{DelDay},{NumMeals},{Notes},{Lon},{Lat},{Restaurant}";
        }

        public string ToAddress()
        {
            return $"{Address1},{City},{State},{Zip}";
        }

        static DataTable zipToRest = null;

        class Worker
        {
            public ConcurrentBag<TDayAddress> goodAddr { get; set; }
            public ConcurrentBag<TDayAddress> badAddr { get; set; }
            public MsgOut msgOut { get; set; }
            public DataRow Row { get; set; }
        }
        public static void ReadExcelFile(string excelFile, string outPath, ConcurrentBag<TDayAddress> goodAddr, ConcurrentBag<TDayAddress> badAddr, MsgOut msgOut)
        {
            using (var stream = File.Open(excelFile, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var dataSet = reader.AsDataSet();
                    // Now you can get data from each sheet by its index or its "name"
                    var dataTable = dataSet.Tables[0];
                    int numCPU = 10; // Environment.ProcessorCount * 2;

                    msgOut($"Processing on {numCPU} threads\n");

                    Parallel.ForEach(dataTable.Rows.OfType<DataRow>(), new ParallelOptions { MaxDegreeOfParallelism = numCPU }, (Row) =>
                    {
                        DoGeocode(goodAddr, badAddr, msgOut, Row, dataTable.Rows.Count);
                    });

                    // Write out the addresses
                    using (var writer = new StreamWriter(outPath + "_GoodAddresses.csv"))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(goodAddr);
                    }
                    using (var writer = new StreamWriter(outPath + "_BadAddresses.csv"))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(badAddr);
                    }
                    msgOut($"\nData written to {outPath + @"_GoodAddresses.csv"} and {outPath + @"_Badddresses.csv"}");
                }
            }
        }

        private static void DoGeocode(ConcurrentBag<TDayAddress> goodAddr, ConcurrentBag<TDayAddress> badAddr, MsgOut msgOut, DataRow Row, int rowCnt)
        {
            // tblRecipient_Address, tblRecipient_Address1, tblRecipient_City, tblRecipient_State, tblRecipient_PostalCode
            TDayAddress addr = new TDayAddress()
            {
                FirstName = EatTheDamReturns(Row[0].ToString().Replace(",", " ")),
                LastName = EatTheDamReturns(Row[1].ToString().Replace(",", " ")),
                Address1 = EatTheDamReturns(Row[2].ToString().Replace(",", " ")),
                AptNum = EatTheDamReturns(Row[3].ToString().Replace(",", " ")),
                Address2 = EatTheDamReturns(Row[4].ToString().Replace(",", " ")),
                City = EatTheDamReturns(Row[5].ToString().Replace(",", " ")),
                State = EatTheDamReturns(Row[6].ToString().Replace(",", " ")),
                Zip = EatTheDamReturns(Row[7].ToString()),
                HomePhone = EatTheDamReturns(Row[8].ToString()),
                CellPHone = EatTheDamReturns(Row[9].ToString()),
                DelDay = EatTheDamReturns(Row[10].ToString().Replace(",", " ")),
                NumMeals = 0,
                Notes = EatTheDamReturns(Row[12].ToString().Replace(",", " "))
            };
            int numMeals = 0;
            addr.NumMeals = 0;
            if (int.TryParse(Row[11].ToString(), out numMeals))
            {
                addr.NumMeals = numMeals;
                //msgOut.WriteLine(addr);
            }
            GeocodeAddress(addr, goodAddr, badAddr, msgOut).Wait();
            //GeocodeAddressGoogle(addr, goodAddr, badAddr, msgOut);
            msgOut($"Procesed address: {goodAddr.Count + badAddr.Count} of {rowCnt} on thread: {Thread.CurrentThread.ManagedThreadId}\r", true);
        }

        public static void LoadRestaurantZips(string zipfilename)
        {
            if (zipToRest == null)
            {
                using (Stream resFilestream = File.Open(zipfilename, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(resFilestream))
                    {
                        var dataSet = reader.AsDataSet();
                        // Now you can get data from each sheet by its index or its "name"
                        zipToRest = dataSet.Tables[0];
                    }
                }
            }
        }

        private static string EatTheDamReturns(string str)
        {
            string ret = str;
            int pos = str.IndexOf("\n");
            if (pos > 0)
            {
                ret = str.Substring(0, pos);
            }
            return ret;
        }

        private static async Task GeocodeAddress(TDayAddress addr, ConcurrentBag<TDayAddress> goodAddr, ConcurrentBag<TDayAddress> badAddr, MsgOut msgOut)
        {
            var bingKey = "Aq4xnynDuADw0OMoyrHEvCrM8VqhI8HlGIusrLdL30LGJBz71X9JtUCMcVU6mj7Y";

            // ServiceManager.Proxy = new WebProxy("http://proxy.research.ge.com:80/");

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
                        //if (((Location)res).Address.PostalCode != addr.Zip)
                        //{
                        //    msgOut($"Changed zip: {addr.ToAddress()} {foundAddr.Address.FormattedAddress}");
                        //}
                        break;
                    }
                }
                //var result = response.ResourceSets[0].Resources[0] as Location;

                addr.Restaurant = GetRestaurant(addr.Zip);

                if (foundAddr != null && foundAddr.MatchCodes[0].ToString() == "Good")
                {
                    //addr.Address1 = foundAddr.Address.AddressLine;
                    //addr.City = foundAddr.Address.Locality;
                    //addr.State = foundAddr.Address.AdminDistrict;
                    //addr.Zip = foundAddr.Address.PostalCode;
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
                //msgOut($"Geocode: {addr} => Results - Code: {result.MatchCodes[0]}\r");
            }
        }

        private static void GeocodeAddressGoogle(TDayAddress addr, ConcurrentBag<TDayAddress> goodAddr, ConcurrentBag<TDayAddress> badAddr, MsgOut msgOut)
        {
            string YOUR_API_KEY = "AIzaSyDSyz9RnSKz7AMs_EQhDVhpXvuhHmyDTGc";

            string address = addr.ToAddress();
            string requestUri = string.Format("https://maps.googleapis.com/maps/api/geocode/xml?key={1}&address={0}&sensor=false", Uri.EscapeDataString(address), YOUR_API_KEY);

            WebRequest request = WebRequest.Create(requestUri);
            WebResponse response = request.GetResponse();
            XDocument xdoc = XDocument.Load(response.GetResponseStream());

            addr.Restaurant = GetRestaurant(addr.Zip);

            if (xdoc.Element("GeocodeResponse").Element("status").Value == "OK")
            {
                XElement result = xdoc.Element("GeocodeResponse").Element("result");
                string [] vals = result.Element("formatted_address").ToString().Split(',');
                string[] tmp = vals[2].TrimStart(' ').Split(' ');
                vals[2] = tmp[0];
                vals[3] = tmp[1];
                //msgOut($"Addr: {addr.ToAddress()} Return: {addrFormat.ToString()}");

                if (vals[3] != addr.Zip)
                {
                    msgOut($"Changed zip: {addr.ToAddress()} => {result.Element("formatted_address").ToString()}");
                }
                XElement locationElement = result.Element("geometry").Element("location");
                addr.Address1 = vals[0];
                addr.City = vals[1];
                addr.State = vals[2];
                addr.Zip = vals[3];
                addr.Lat = float.Parse(locationElement.Element("lat").Value);
                addr.Lon = float.Parse(locationElement.Element("lng").Value);
                goodAddr.Add(addr);
            }
            else
            {
                badAddr.Add(addr);
            }
        }

        private static string GetRestaurant(string zip)
        {
            string ret = null;
            DataRow[] filtered = new DataRow[0];
            try
            {
                filtered = zipToRest.Select($"Column1 = '{zip}'");
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error: " + ex.Message);
            }
            //.AsEnumerable()
            //    .Where(r => r.Field<int>("Column1").ToString().Contains(zip));
            foreach (var row in filtered)
            {
                ret += row.Field<string>("Column2") + ", ";
            }

            return (ret == null ? $"Oops, did not find {zip}" : ret);
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

        public static void GenerateRoutes(IEnumerable<TDayAddress> records, string outPath, MsgOut msgOut)
        {

            int delivertCnt = 0;

            // create a QuadTree and fill it with objects
            var quadTree = new QuadTree<TDayAddress>(-80F, 35, 10, 10, new MyPolygonBounds(), 8, 13);
            //quadTree.InsertRange(records); // "myPolygons" are an array of all your objects
            using (StreamWriter ew = new StreamWriter(outPath + "_DeliveriesErrors.txt"))
            {
                foreach (var obj in records)
                {
                    if (!quadTree.Insert(obj))
                        ew.WriteLine($"Where the heck: {obj.FirstName} {obj.LastName}, {obj.City} {obj.State} {obj.Zip} "); // throw new ApplicationException("Failed insert");
                    else
                        delivertCnt++;
                }
            }
            msgOut($"Inserted {delivertCnt} into delivery scheduler\n");

            delivertCnt = 0;
            Dictionary<int, TDayAddress[]> grids = new Dictionary<int, TDayAddress[]>();
            StreamWriter sw = new StreamWriter(outPath + "_Deliveries.txt");
            quadTree.GetDeliveries(grids);
            //string jsonString = JsonSerializer.Serialize(grids);
            foreach (KeyValuePair<int, TDayAddress[]> delivery in grids)
            {
                //msgOut($"DeliverID: {delivery.Key}: with {delivery.Value.Length} stops: ");
                sw.WriteLine($"DeliverID: {delivery.Key} with {delivery.Value.Length} stops: ");
                foreach (var res in delivery.Value)
                {
                    //msgOut($"\t Delivery: {res.ToString()}");
                    sw.WriteLine($"\t Delivery^ {res.ToString().Replace(",","|")}");
                    delivertCnt++;
                }    // do something with entry.Value or entry.Key
            }
            msgOut($"Scheduled {delivertCnt} deliveries\n");
            sw.WriteLine($"Scheduled {delivertCnt} deliveries");
            sw.Close();
            msgOut($"Results written to {outPath + "_Deliveries.txt"}");


            // find the intersecting objects among the nearest 
            // bool IsIntersect(Point[] obj1, Point[] obj2) is your function for checking intersections
            //var intersectingObjects = nearestObjects.Where(nearest => IsIntersect(nearest, myPolygon);
        }

    }
}
