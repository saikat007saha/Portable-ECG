using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.IO.Ports;

namespace Portable_ECG
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        int[] filter = new int[40];
        int[] graph = new int[100];
        int index = -1, n = -98, xaxis = 0, count = 0, number, counter = 0, beats = 0, t = 0, m, s;
        string line, path1, path2, now, time;

        private void timer1_Tick(object sender, EventArgs e)
        {
            t++;
            m = t / 60;
            s = t - m * 60;
            time = "";
            if (m < 10)
                time += "0" + m + ":";
            else
                time += m + ":";
            if (s < 10)
                time += "0" + s;
            else
                time += s;
        }

        Thread ecgRead;
        Thread update;

        private void start_Click(object sender, EventArgs e)
        {
            index = -1; n = -98; xaxis = 0; count = 0; counter = 0; beats = 0;
            filter = new int[40];
            graph = new int[100];
            now = DateTime.Now.ToString();
            now = now.Replace('/', '.');
            now = now.Replace(':', '.');
            now = pid.Text + now;
            path1 = now + ".txt";
            path2 = now + "_filtered.txt";

            using (File.Create(path1)) { }
            //using (File.Create(path2)) { }
            CheckForIllegalCrossThreadCalls = false;

            m = DateTime.Now.Minute;
            s = DateTime.Now.Second;
            //timer1.Start();

            ecgRead = new Thread(getValues);
            //ecgRead.IsBackground = true;
            ecgRead.Start();
            start.Enabled = false;

            update = new Thread(getECG);
            update.IsBackground = true;
            update.Start();


        }

        private void stop_Click(object sender, EventArgs e)
        {
            if (!start.Enabled)
            {
                ecgRead.Abort();
                update.Abort();
                ecgReading();
            }
            start.Enabled = true;

            //MessageBox.Show(beats.ToString());
            //timer1.Stop();
            //t = 0;
        }

        private void fill(int value)
        {
            index++;
            filter[index] = value;
            if(index>0)
            {
                for(int k=1;k<=index;k++)
                {
                    if (Math.Abs(filter[k - 1] - filter[k]) < 100 && filter[k] < 700)
                        filter[k - 1] = (filter[k - 1] + filter[k]) / 2;
                }
            }
        }

        private void shift(int value)
        {
            //write 1st value of filter[] array;
            using (StreamWriter sw2 = File.AppendText(path2))
            {
                sw2.WriteLine(filter[0]);
            }
            updateChart(value);
            Array.Copy(filter, 1, filter, 0, 39);
            filter[39] = value;
            for (int k = 1; k < index; k++)
            {
                if (Math.Abs(filter[k - 1] - filter[k]) < 100 && filter[k] < 700)
                    filter[k - 1] = (filter[k - 1] + filter[k]) / 2;
            }
            if (filter[38] < 600 && filter[39] > 600)
            {
                //ecgReading(counter);
                counter = 0;
                beats++;
            }
        }

        private void getECG()
        {
            while (true)
            {
                if (chart1.IsHandleCreated)
                {
                    this.Invoke((MethodInvoker)delegate { updatechart(); });
                }
                else
                {
                    //...
                }
                //Thread.Sleep(1);
            }
        }
        private void updateChart(int value)
        {
            Array.Copy(graph, 1, graph, 0, 99);
            graph[99] = value;
            xaxis++;
            //chart1.Series["ECG"].Points.Clear();
            //for (int k = 0; k < 100; k++)
            //    chart1.Series["ECG"].Points.AddXY(n + k, graph[k]);
            if (xaxis == 100)
            {
                n = n + 100;
                xaxis = 0;
            }
        }

        private void updatechart()
        {
            //Array.Copy(graph, 1, graph, 0, 99);
            //graph[99] = value;
            //xaxis++;
            chart1.Series["ECG"].Points.Clear();
            for (int k = 0; k < 100; k++)
                chart1.Series["ECG"].Points.AddXY(n + k, graph[k]);
            //if (xaxis == 100)
            //{
            //    n = n + 100;
            //    xaxis = 0;
            //}
            label2.Text = ((DateTime.Now.Minute * 60 + DateTime.Now.Second)-(m*60+ s)) / 60 + ":" + ((DateTime.Now.Minute * 60 + DateTime.Now.Second) - (m * 60 + s)) % 60;
            //if(time!="")
            //    label2.Text = time;
        }

        private void getValues()
        {
            //SerialPort myport = new SerialPort();
            //myport.BaudRate = 9600;
            //myport.PortName = "COM3";
            //myport.DataBits = 8;
            //myport.Parity = Parity.None;
            //myport.StopBits = StopBits.One;
            //myport.Handshake = Handshake.None;
            //myport.DtrEnable = true;
            //myport.ReceivedBytesThreshold = 4096;
            //myport.Open();

            string[] port = SerialPort.GetPortNames();

            using(SerialPort myport = new SerialPort(port[0],9600))
            {
                myport.Open();
                while (true)
                {
                    line = myport.ReadLine();
                    if (Int32.TryParse(line, out number) && number > 100)
                    {
                        //if (count < 40)
                        //{
                        //    count++;
                        //    fill(number);
                        //}
                        //else
                        //    shift(number);
                        using (StreamWriter sw1 = File.AppendText(path1))
                        {
                            sw1.WriteLine(number);
                        }
                        //counter++;
                        updateChart(number);
                    }
                }
            }

            
        }

        private void ecgReading()
        {
            int[] a = new int[1];
            int c = 0, value, i, b=0, beats = 0;
            System.IO.StreamReader file = new System.IO.StreamReader(path1);
            while ((line = file.ReadLine()) != null)
            {
                //MessageBox.Show(line);
                Int32.TryParse(line, out value);
                if (value > 10 && value < 1010)
                {
                    a[c] = value;
                    c++;
                    Array.Resize(ref a, a.Length + 1);
                }
            }
            TextWriter tw = new StreamWriter(path1.Replace(".txt", ".dat"));
            for (i = 0; i < c; i++)
            {
                tw.WriteLine(i + "," + a[i]);
                if (a[i + 1] - a[i] > 120 && i - b > 5)
                {
                    beats++;
                    b = i;
                    //MessageBox.Show(b.ToString());
                }
            }
            tw.Close();
            MessageBox.Show("Total number of beats recorded: " + beats.ToString());
            float x = 6000.0f / c;
            x = beats * x;
            int y = (int)Math.Round(x);
            MessageBox.Show("Average heart beat: " + y + " bpm");
        }
    }
}
