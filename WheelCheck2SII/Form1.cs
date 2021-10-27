using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Microsoft.VisualBasic.FileIO;
namespace WheelCheck2SII
{ 
    public partial class Form1 : Form
    {
        
        string[] CSV;
        string[] CSVData;
        double max;
        bool dzfound = false;
        int dzoffset = 0;
        int count = 0;
        int noforce = 0;
        decimal deadzone = 0;
        
        List<WheelCheckCSV> wheelCheck = new List<WheelCheckCSV>();
        OpenFileDialog OpenDialog = new OpenFileDialog();
        SaveFileDialog SaveDialog = new SaveFileDialog();
        public Form1()
        {
           
            InitializeComponent();
            numericUpDown1.Enabled = false;
      

            // Set up the delays for the ToolTip.
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 1000;
            toolTip1.ReshowDelay = 500;
            // Force the ToolTip text to be displayed whether or not the form is active.
            toolTip1.ShowAlways = true;
            toolTip1.SetToolTip(button1, "Press this to select CSV file");
            toolTip1.SetToolTip(button2, "Press this to save the generated LUT file");
            toolTip1.SetToolTip(checkBox1, "This makes the program add deadzone to output LUT file.\nIt's recommended to keep this off.");
            toolTip1.SetToolTip(numericUpDown1, "This sets the desired precision of given values.\nLower values make the LUT more coarse, higher will make it finer. ");

        }

        private void button1_Click(object sender, EventArgs e)
        {

            OpenDialog.ShowHelp = false;
            OpenDialog.AddExtension = true;
            noforce = 0;
            dzfound = false;
            dzoffset = 0;
            OpenDialog.DefaultExt = ".csv";
            OpenDialog.Title = "Select WheelCheck CSV";
            DialogResult Res = OpenDialog.ShowDialog();
            if (Res == DialogResult.OK)
            {
                string Path = OpenDialog.FileName;
                if (Path.Contains("csv"))
                {
                    CSV = File.ReadAllText(Path).Split(',');
                    if (!CSV[0].Contains("force"))
                    {

                        MessageBox.Show("The selected file isn't made by WheelCheck.");
                    }
                    else
                    {

                        using (TextFieldParser textFieldParser = new TextFieldParser(Path))
                        {
                            if (chart1.Series.FindByName("deltaXDeg") != null)
                            {
                                chart1.Series["deltaXDeg"].Points.Clear();
                            }
                            wheelCheck.Clear();
                            if (chart1.Series.FindByName("deltaXDeg") == null)
                            {
                                chart1.Series.Add("deltaXDeg");
                            }
                            chart1.DataSource = wheelCheck;


                            textFieldParser.TextFieldType = FieldType.Delimited;
                            textFieldParser.SetDelimiters(",");

                            while (!textFieldParser.EndOfData)
                            {
                                CSVData = textFieldParser.ReadFields();

                                if (double.TryParse(CSVData[0], out double n))
                                {
                                    wheelCheck.Add(new WheelCheckCSV
                                    {
                                        force = double.Parse(CSVData[0], CultureInfo.InvariantCulture),
                                        startX = double.Parse(CSVData[1], CultureInfo.InvariantCulture),
                                        endX = double.Parse(CSVData[2], CultureInfo.InvariantCulture),
                                        deltaX = double.Parse(CSVData[3], CultureInfo.InvariantCulture),
                                        deltaXDeg = double.Parse(CSVData[4], CultureInfo.InvariantCulture)
                                    });

                                }
                                else { continue; }



                            }

                            for (int i = 0; i < wheelCheck.Count() - 1; i++) {
                                if (wheelCheck[i].deltaXDeg < 0.01) { 
                                   noforce++;
                                }
                                else
                                {
                                    if (!dzfound)
                                    {
                                        dzfound = true;
                                        dzoffset = i;
                                    }
                                }
                                    chart1.Series["deltaXDeg"].Points.AddXY(i, wheelCheck[i].deltaXDeg);
                            } }

                        DataPoint maxDataPoint = chart1.Series["deltaXDeg"].Points.FindMaxByValue();
                        numericUpDown1.Enabled = true;
                        count = chart1.Series["deltaXDeg"].Points.Count;
                        max = maxDataPoint.YValues[0];

                        deadzone = (decimal)noforce / count;
                        label1.Text = $"Amount of points: {chart1.Series["deltaXDeg"].Points.Count}\nMaximum: {Math.Round(max, 2)}\nDeadzone: {deadzone*100} %";
                        if (chart1.Series.FindByName("outvals") != null)
                        {
                            chart1.Series["outvals"].Points.Clear();
                        }

                        if (chart1.Series.FindByName("outvals") == null)
                        {

                            chart1.Series.Add("outvals");
                            chart1.Series["outvals"].ChartType = SeriesChartType.Line;
                            chart1.Series["outvals"].BorderWidth = 2;
                        }


                        for (int i = 0; i < wheelCheck.Count(); i++)
                        {
                            
                            chart1.Series["outvals"].Points.AddXY(i, Math.Round(wheelCheck[i].deltaXDeg / max, (int)numericUpDown1.Value) * max);
                        }
                        chart1.Update();
                        MessageBox.Show($"Successfully loaded {count} values from selected CSV file.\n Detected deadzone:{deadzone * 100}%");
                    }

                }
            }
            else
            {
                if (Res != DialogResult.Cancel)
                {
                    MessageBox.Show("The selected file isn't made by WheelCheck.");
                }
            }
            }


        private void button2_Click(object sender, EventArgs e)
        {
            if (count > 0)
            {
                var header = "SiiNunit\r\n{\r\ninput_force_feedback_lut : ffb.lut {\n# This ffb_lut.sii file was generated by WheelCheck2SII converter.\n\n";
                var footer = "\n}\r\n}";
                var contents = "";
                noforce = 0;
       
                foreach (var item in wheelCheck)
                {
                    if(Math.Round(item.deltaXDeg / max, (int)numericUpDown1.Value) < 0.09)
                    {
                        if (checkBox1.Checked)
                        {
                            contents += $"output_values[]: {Math.Round(item.deltaXDeg / max, (int)numericUpDown1.Value).ToString("0.##################", CultureInfo.GetCultureInfo("en-GB"))}\n";
                        }
                        continue;
                    }
                    if (Math.Round(item.deltaXDeg / max, (int)numericUpDown1.Value) > 1)
                    {
                        contents += $"output_values[]: 1\n";
                    }
                    else
                    {
                        contents += $"output_values[]: {Math.Round(item.deltaXDeg / max, (int)numericUpDown1.Value).ToString("0.##################", CultureInfo.GetCultureInfo("en-GB"))}\n";
                    }
                }
                var fileContents = header + contents + footer;
                SaveDialog.FileName = "ffb_lut.sii";
                SaveDialog.Filter = "Force Feedback LUT file(ETS2/ATS 1.42+) | ffb_lut.sii";
                SaveDialog.Title = "Select the location where you want to save the file";
                if (SaveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(SaveDialog.FileName, fileContents);
                   
                        MessageBox.Show($"Successfully saved ffb_lut.sii file in {SaveDialog.FileName}");
                    }
                    catch (Exception E)
                    {
                        MessageBox.Show($"There was problem saving the file. Either you don't have permission, or the drive you want to write into is read only.\n{E.Message}\n{E.StackTrace}");
                    }

                }
            }
            else
            {
                MessageBox.Show($"Load the WheelCheck CSV first.");
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (chart1.Series.FindByName("outvals") != null)
            {
                chart1.Series["outvals"].Points.Clear();
            }
            
            if (chart1.Series.FindByName("outvals") == null)
            {
                
                chart1.Series.Add("outvals");
                chart1.Series["outvals"].ChartType = SeriesChartType.Line;
                chart1.Series["outvals"].BorderWidth = 2;
            }
           

            for (int i = 0; i < wheelCheck.Count(); i++)
                chart1.Series["outvals"].Points.AddXY(i, Math.Round(wheelCheck[i].deltaXDeg / max, (int)numericUpDown1.Value)*max);
                chart1.Update();

        }
    }




    public class WheelCheckCSV 
{

    public double force { get; set; }
    public double startX { get; set; }
    public double endX { get; set; }
    public double deltaX { get; set; }
    public double deltaXDeg { get; set; }

    public WheelCheckCSV()
    {
    }
}
}
