﻿using System;
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
        int count = 0;
        List<WheelCheckCSV> wheelCheck = new List<WheelCheckCSV>();
        OpenFileDialog OpenDialog = new OpenFileDialog();
        SaveFileDialog SaveDialog = new SaveFileDialog();
        public Form1()
        {
           
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            OpenDialog.ShowHelp = false;
            OpenDialog.AddExtension = true;
            OpenDialog.DefaultExt = ".csv";
            OpenDialog.Title = "Select WheelCheck CSV";
            if (OpenDialog.ShowDialog() == DialogResult.OK)
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

                            for (int i = 0; i < wheelCheck.Count(); i++)
                                chart1.Series["deltaXDeg"].Points.AddXY(i, wheelCheck[i].deltaXDeg);
                        }

                        chart1.Update();
                        DataPoint maxDataPoint = chart1.Series["deltaXDeg"].Points.FindMaxByValue();

                        count = chart1.Series["deltaXDeg"].Points.Count;
                        max = maxDataPoint.YValues[0];
                        MessageBox.Show($"Successfully loaded {count} values from selected CSV file.");
                        label1.Text = $"Amount of points: {chart1.Series["deltaXDeg"].Points.Count}\nMaximum: {max}";
                    }

                }
            }
            else
            {
                MessageBox.Show("The selected file isn't made by WheelCheck.");
            }
            }


        private void button2_Click(object sender, EventArgs e)
        {
            if (count > 0)
            {
                var header = "SiiNunit\r\n{\r\ninput_force_feedback_lut : ffb.lut {\n# This ffb_lut.sii file was generated by WheelCheck2SII converter.\n\n";
                var footer = "\n}\r\n}";
                var contents = "";
                foreach (var item in wheelCheck)
                {
                    contents += $"output_values[]: {Math.Round(item.deltaXDeg / max, 1).ToString(CultureInfo.CreateSpecificCulture("en-GB"))}\n";
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
                        MessageBox.Show($"Successfully saved ffb_lut.sii file in {SaveDialog.FileName}.\n");
                    }
                    catch (Exception E)
                    {
                        MessageBox.Show($"There was problem saving the file. Either you don't have permission, or the drive you want to write into is read only.\n{E.Message}");
                    }

                }
            }
            else
            {
                MessageBox.Show($"Load the WheelCheck CSV first.");
            }
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
