using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Gtk;
using TDayLib;
using System.IO;
using CsvHelper;
using System.Globalization;
using RTFExporter;
using System.Threading.Tasks;

public partial class MainWindow : Gtk.Window
{
    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }

    protected void ExitApp(object sender, EventArgs e)
    {
        Environment.Exit(0);
    }

    private  void CheckAddresses(string excelFile)
    {
        DebugOut("Lets check some addresses!");

        var goodAddr = new ConcurrentBag<TDayAddress>();
        var badAddr = new ConcurrentBag<TDayAddress>();

        Stopwatch sw = new Stopwatch();
        sw.Start();
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        TDayAddress.ReadExcelFile(excelFile, System.IO.Path.GetDirectoryName(excelFile), goodAddr, badAddr, new MsgOut(DebugOut));
        sw.Stop();
        string drcty = System.IO.Path.GetDirectoryName(excelFile);
        DebugOut($"\nValidate addresses time: {sw.Elapsed}  with {goodAddr.Count} good and {badAddr.Count} failed addresses");
        DebugOut($"\nData written to {drcty + "/GoodAddresses.csv"} and {drcty + "/Badddresses.csv"}");
    }

    private void GenRoutes(string inputFile)
    {
        using (var reader = new StreamReader(inputFile))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<TDayAddress>();

            TDayAddress.GenerateRoutes(records, System.IO.Path.GetDirectoryName(inputFile), new MsgOut(DebugOut));
        }
        DebugOut($"Results written to {System.IO.Path.GetDirectoryName(inputFile) + "/Deliveries.txt"}");
    }

    private void GenReport(string deliveriesFile)
    {
        System.IO.StreamReader file = new System.IO.StreamReader(deliveriesFile);
        string fname = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(deliveriesFile), "DeliveriesReport.rtf");
        string line;
        int delCnt = 0;
        int stpCnt = 0;
        using (RTFDocument doc = new RTFDocument(fname))
        {
            doc.SetMargin(0.5F, 0.5F, 1.0F, 1.0F);
            while ((line = file.ReadLine()) != null)
            {
                string[] vals = line.Split(' ');
                if (vals[0] == "DeliverID:" && vals[3] != "0")
                {
                    delCnt++;
                    var p = doc.AppendParagraph();

                    p.style.alignment = RTFExporter.Alignment.Left;
                    p.style.spaceAfter = 400;

                    var t = p.AppendText($"DeliveryID: {vals[1]} with {vals[3]} stops.\n");
                    t.style.bold = true;
                    t.style.color = new Color(0, 0, 0);
                    t.style.fontFamily = "Courier New";
                    int cnt = int.Parse(vals[3]);
                    int rsp = 45;
                    int numMeals = 0;
                    int totMeals = 0;
                    var rand = new Random();
                    for (int ii = 0; ii < cnt; ii++)
                    {
                        t = p.AppendText("_".PadLeft(65, '_') + "\n");
                        t.style.fontFamily = "Courier New";
                        stpCnt++;
                        vals = file.ReadLine().Split(':')[1].Split('|');
                        totMeals += numMeals = int.Parse(vals[11]);
                        t.content += $"{vals[0]} {vals[1]}" + sp(vals[0] + vals[1], rsp);
                        t.content += $"<b># of Meals: {numMeals}</b>\n";
                        t.content += $"  {vals[2]} {vals[3]}" + sp(vals[2] + vals[3], rsp - 1) + "Phone Numbers:\n";
                        string pn = vals[9] != "" ? String.Format("{0:(###) ###-####}", Int64.Parse(vals[9])) : "";
                        t.content += $"  {vals[4]}" + sp(vals[4], rsp) + $"Home: {pn}\n";
                        pn = vals[8] != "" ? String.Format("{0:(###)###-####}", Int64.Parse(vals[8])) : "";
                        t.content += $"  {vals[5]}, {vals[6]} {vals[7]}" + sp(vals[5] + vals[6] + vals[7], rsp - 3) + $"Cell: {pn}\n";
                        t.content += $"  Delivery Notes: {vals[12]}\n";
                    }
                    t = p.AppendText("\n".PadRight(15, '=') + $" Meal Total: {totMeals} " + "\n\n".PadLeft(15, '='));
                    t.style.fontFamily = "Courier New";
                    t.style.fontSize += 4;
                    p.PageBreak = true;
                }
            }
        }
        DebugOut($"Processed {delCnt} deliveries with a total of {stpCnt} stops written to {fname}");
    }

    private static string sp(string inStr, int cnt)
    {
        string ret = "";
        int pads = cnt - inStr.Length;
        for (int ii = 0; ii < (pads); ii++)
            ret += "\t";

        return "".PadLeft(pads > 0 ? pads : 0);
    }

    string msgTextStr;

    private void DebugOut(string msg, bool status = false)
    {
        msgTextStr += msg;
        Gtk.Application.Invoke(delegate {
            if (status)
            {
                statusMsg.Text = msg;
            }
            else
                msgText.Buffer.Text += msg + "\n";
        });
    }

    protected void OpenAddress(object sender, EventArgs e)
    {
        Gtk.FileChooserDialog fcd = new Gtk.FileChooserDialog("Open File", null, Gtk.FileChooserAction.Open);
        fcd.AddButton(Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
        fcd.AddButton(Gtk.Stock.Open, Gtk.ResponseType.Ok);
        fcd.DefaultResponse = Gtk.ResponseType.Ok;
        fcd.SelectMultiple = false;

        string excelFile, filePath;
        Gtk.ResponseType response = (Gtk.ResponseType)fcd.Run();
        if (response == Gtk.ResponseType.Ok)
        {
            excelFile = fcd.Filename;
            filePath = System.IO.Path.GetDirectoryName(excelFile);
            fcd.Destroy();
            DebugOut("Processing: " + excelFile);
            Task.Run(() => {
                CheckAddresses(excelFile);
                GenRoutes(filePath + "/GoodAddresses.csv");
                GenReport(filePath + "/Deliveries.txt");
            });
            
            //msgText.Buffer.Text += msgTextStr;
        }

    }
}
