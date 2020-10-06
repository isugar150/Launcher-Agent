using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;
using vtortola.WebSockets.Deflate;

namespace Launcher_Agent
{
    partial class Service : ServiceBase
    {
        private Thread service = null;

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: 여기에 서비스를 시작하는 코드를 추가합니다.
            Console.WriteLine("서비스 시작됨.");
            service = new Thread(new ThreadStart(Program.webSocketInit));
            service.IsBackground = true;
            service.Start();
        }

        protected override void OnStop()
        {
            // TODO: 서비스를 중지하는 데 필요한 작업을 수행하는 코드를 여기에 추가합니다.
            Console.WriteLine("서비스 종료됨.");
            Environment.Exit(0);
        }
    }
}
