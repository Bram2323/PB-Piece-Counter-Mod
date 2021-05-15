using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using Timer = System.Windows.Forms.Timer;

namespace Piece_Counter_Mod
{
    public partial class MainForm : Form
    {
        public static MainForm instance;

        public MainForm()
        {
            InitializeComponent();

            instance = this;
            Application.ApplicationExit += OnProcessExit;

            Counters = new List<PictureBox>[8];
            for (int i = 0; i < 8; i++) Counters[i] = new List<PictureBox>();

            for (int i = 0; i < 8; i++) DisplayInt(i, 0);

            timer = new Timer();
            timer.Interval = 1;
            timer.Tick += Update;
            timer.Start();

            StartServer();
        }

        void OnProcessExit(object sender, EventArgs e)
        {
            if (client != null) client.Close();
            listener.Stop();
        }

        Timer timer;
        List<PictureBox>[] Counters;
        int[] nums = new int[8];
        int counterAmount = 3;
        int currentCounterAmount = 0;
        bool isOnTop = false;
        Stopwatch sw = new Stopwatch();

        void Update(object sender, EventArgs e)
        {
            if (currentCounterAmount != counterAmount) UpdateCounters(counterAmount);
            currentCounterAmount = counterAmount;
            for (int i = 0; i < 8; i++) DisplayInt(i, nums[i]);

            if (isOnTop != TopMost) TopMost = isOnTop;

            if (LookingForClient == false)
            {
                LookingForClient = true;
                sw.Reset();
                serverThread = new Thread(new ThreadStart(serverTasks));
                serverThread.IsBackground = true;
                serverThread.Start();
                Console.WriteLine("Server thread created");
            }
        }


        public void DisplayInt(int index, int num)
        {
            if (index < 0 || index >= 8) return;
            if (num < 0) num *= -1;

            List<PictureBox> list = Counters[index];

            Char[] digits = num.ToString().ToCharArray();

            for (int i = 0; i < list.Count; i++)
            {
                int ind = digits.Length - i - 1;
                if (ind < 0)
                {
                    list[i].Image = Properties.Resources._7seg_AllOff;
                    continue;
                }
                switch (digits[ind])
                {
                    case '0':
                        list[i].Image = Properties.Resources._7seg_0;
                        break;
                    case '1':
                        list[i].Image = Properties.Resources._7seg_1;
                        break;
                    case '2':
                        list[i].Image = Properties.Resources._7seg_2;
                        break;
                    case '3':
                        list[i].Image = Properties.Resources._7seg_3;
                        break;
                    case '4':
                        list[i].Image = Properties.Resources._7seg_4;
                        break;
                    case '5':
                        list[i].Image = Properties.Resources._7seg_5;
                        break;
                    case '6':
                        list[i].Image = Properties.Resources._7seg_6;
                        break;
                    case '7':
                        list[i].Image = Properties.Resources._7seg_7;
                        break;
                    case '8':
                        list[i].Image = Properties.Resources._7seg_8;
                        break;
                    case '9':
                        list[i].Image = Properties.Resources._7seg_9;
                        break;
                    default:
                        break;
                }
            }
        }

        public void UpdateCounters(int Amount)
        {
            if (Amount < 0) return;

            for (int i = 0; i < 8; i++) 
            {
                List<PictureBox> list = Counters[i];
                for (int j = 0; j < list.Count; j++)
                {
                    Controls.Remove(list[j]);
                }
                Counters[i] = new List<PictureBox>();
            }

            int n = 0;
            RoadLogo.Location = new Point(n * 30 * Amount + n * 56 + 10, 10);
            n++;
            RRoadLogo.Location = new Point(n * 30 * Amount + n * 56 + 10, 10);
            n++;
            WoodLogo.Location = new Point(n * 30 * Amount + n * 56 + 10, 10);
            n++;
            SteelLogo.Location = new Point(n * 30 * Amount + n * 56 + 10, 10);
            n++;
            HydroLogo.Location = new Point(n * 30 * Amount + n * 56 + 10, 10);
            n++;
            RopeLogo.Location = new Point(n * 30 * Amount + n * 56 + 10, 10);
            n++;
            CableLogo.Location = new Point(n * 30 * Amount + n * 56 + 10, 10);
            n++;
            SpringLogo.Location = new Point(n * 30 * Amount + n * 56 + 10, 10);

            for (int i = 0; i < 8; i++)
                for (int j = Amount - 1; j >= 0; j--)
                {
                    PictureBox digit = new PictureBox();
                    digit.Name = "Digit " + (i * Amount + j);
                    digit.Size = new Size(30, 50);
                    digit.Location = new Point(i * 30 * Amount + i * 56 + 60 + j * 30, 10);
                    digit.Image = Properties.Resources._7seg_AllOff;
                    digit.SizeMode = PictureBoxSizeMode.Zoom;
                    Counters[i].Add(digit);
                    Controls.Add(digit);
                }

            counterAmount = Amount;
        }


        TcpListener listener;
        TcpClient client;
        Thread serverThread;
        bool LookingForClient = false;

        public void StartServer()
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 23232);

            try
            {
                listener = new TcpListener(localEndPoint);
                listener.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        void serverTasks()
        {
            LookingForClient = true;
            try
            {
                while (true)
                {

                    client = listener.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    NetworkStream stream = client.GetStream();
                    sw.Restart();

                    var buffer = new byte[1024];
                    int bytesRead;
                    while (true)
                    {
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead != 0) HandleData(buffer);
                        if (buffer[0] == 16) break;
                        if (sw.ElapsedMilliseconds > 10000) break;
                    }
                    sw.Stop();

                    Console.WriteLine("Disconnected!");

                    client.Close();
                    LookingForClient = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong:\n" + ex.Message);
                if (client != null)
                    client.Close();
            }
        }

        public void HandleData(byte[] data)
        {
            if (data[0] == 1 && data[1] < 8) nums[data[1]] = BitConverter.ToInt32(data, 2);
            else if (data[0] == 2 && data[1] <= 10) counterAmount = data[1];
            else if (data[0] == 3 && data[1] <= 1) isOnTop = data[1] == 1;
            else if (data[0] == 15) sw.Restart();
        }
    }
}
