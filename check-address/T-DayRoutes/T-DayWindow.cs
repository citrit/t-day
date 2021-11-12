using CsvHelper;
using RTFExporter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TDayLib;

namespace TDayRoutes
{
    public partial class T_DayWindow : Form
    {
        public T_DayWindow()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void generateRoutsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Excel files(*.xlsx)| *.xlsx";
            ;

            string excelFile, filePath;
    
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                excelFile = dlg.FileName;
                filePath = System.IO.Path.GetDirectoryName(excelFile);
                DebugOut("Processing: " + excelFile);
                Task.Run(() => {
                    CheckAddresses(excelFile);
                    GenRoutes(filePath + "/GoodAddresses.csv");
                    GenReport(filePath + "/Deliveries.txt");
                });

                //msgText.Buffer.Text += msgTextStr;
            }
        }

        private void CheckAddresses(string excelFile)
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
                        t.style.color = new RTFExporter.Color(0, 0, 0);
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
            if (textBox1.InvokeRequired)
            {
                // Call this same method but append THREAD2 to the text
                Action safeWrite = delegate { DebugOut(msg); };
                textBox1.Invoke(safeWrite);
            }
            else
                textBox1.Text = msg;

            msgTextStr += msg;
        }
    }
}
