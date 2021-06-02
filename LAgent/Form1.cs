using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using vtortola.WebSockets;
using vtortola.WebSockets.Deflate;
using static TMTAgent.Ini;

namespace TMTAgent
{
    public partial class Form1 : Form
    {
        private static WebSocketListener webSocketServer = null;
        private bool debugMode = false;

        public Form1(string[] args)
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            for (int i = 0; i < args.Length; i++) { 
                if(args[i].IndexOf("show", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    debugMode = true;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!debugMode)
            {
                this.Visible = false;
            }
            ShowInTaskbar = false;

            webSocketInit();
        }

        #region Web socket
        public void webSocketInit()
        {
            #region init WebSocket Server
            try
            {
                int webSocketPort = 49862;
                appendText("Init WebSocket Server Port is " + webSocketPort);
                CancellationTokenSource cancellation = new CancellationTokenSource();
                //var endpoint = new IPEndPoint(IPAddress.Any, 1818);
                IPEndPoint endpoint;

                endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), webSocketPort);

                int[] time = new int[2];
                time[0] = int.Parse("0"); // 분
                time[1] = int.Parse("10"); // 초
                WebSocketListenerOptions options = new WebSocketListenerOptions()
                {
                    WebSocketReceiveTimeout = new TimeSpan(0, time[0], time[1]), // 클라이언트가 서버로 요청했을때 서버가 바쁘면 Timeout
                    WebSocketSendTimeout = new TimeSpan(0, 0, 5), // 클라이언트가 연결을 끊었을때 Timeout
                    NegotiationTimeout = new TimeSpan(0, time[0], time[1]),
                    PingTimeout = new TimeSpan(0, time[0], time[1]),
                    PingMode = PingModes.LatencyControl
                };
                webSocketServer = new WebSocketListener(endpoint, options);
                vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455 rfc6455 = new vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455(webSocketServer);
                rfc6455.MessageExtensions.RegisterExtension(new WebSocketDeflateExtension());
                webSocketServer.Standards.RegisterStandard(rfc6455);
                webSocketServer.Start();

                Task task = Task.Run(() => AcceptWebSocketClientsAsync(webSocketServer, cancellation.Token));
            }
            catch (Exception e1)
            {
                throw e1;
            }
            #endregion
        }
        #endregion

        #region Web socket related

        /// <summary>
        /// 클라이언트가 웹 소켓으로 접속했을때 작동하는 로직
        /// </summary>
        /// <param name="server">웹 소켓 리스너</param>
        /// <param name="token">웹 소켓 토큰</param>
        /// <returns></returns>
        private async Task AcceptWebSocketClientsAsync(WebSocketListener server, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    WebSocket ws = await server.AcceptWebSocketAsync(token).ConfigureAwait(false);

                    // 웹 소켓이 null 이 아니면, 핸들러를 스타트 합니다.(또 다른 친구가 들어올 수도 있으니 비동기로...)
                    if (ws != null)
                    {
                        await Task.Run(() => HandleConnectionAsync(ws, token)); //<== await 시 요청을 받으면 이전 요청이 종료할때까지 대기했다가 실행
                    }
                }
                catch (Exception aex)
                {
                    appendText("[WebSocket] Error Accepting clients: " + aex.GetBaseException().Message);
                }
            }
        }

        /// <summary>
        /// 클라이언트가 메시지 던졌을때 로직
        /// </summary>
        /// <param name="ws">웹 소켓</param>
        /// <param name="cancellation">토큰</param>
        /// <returns></returns>
        private async Task HandleConnectionAsync(WebSocket ws, CancellationToken cancellation)
        {
            try
            {
                //연결이 끊기지 않았고, 캔슬이 들어오지 않는 한 루프를 돔.
                while (ws.IsConnected && !cancellation.IsCancellationRequested)
                {
                    //클라이언트로부터 메시지가 왔는지 비동기읽음
                    string requestInfo = await ws.ReadStringAsync(cancellation).ConfigureAwait(false);

                    if (requestInfo != null)
                    {
                        try
                        {
                            JObject requestMsg = JObject.Parse(requestInfo.Replace("[", "").Replace("]", ""));

                            JObject responseMsg = new JObject();

                            Process process = new Process();

                            appendText("recive Message: " + requestMsg.ToString());

                            // 앱 버전 체크
                            if (!requestMsg["version"].ToString().Equals("1.0.0"))
                            {
                                appendText("Old app version");
                                responseMsg["msg"] = "Old app version";
                            }

                            else
                            {
                                string tmtPath = "";
                                if (new FileInfo(Application.StartupPath + @"\TMT.exe").Exists)
                                {
                                    tmtPath = Application.StartupPath + @"\TMT.exe";
                                }
                                else
                                {
                                    tmtPath = @"C:\Program Files (x86)\WEMEETS\MeetTalk\TMT.exe";
                                }

                                appendText("tmtPath is " + tmtPath);

                                if (requestMsg["method"].ToString().Equals("RunTMT"))
                                {
                                    Process.Start(tmtPath, String.Format("-u {0} START_GSC_MeetTalk_SSO", requestMsg["sawonNo"].ToString()));
                                    appendText("Arguments: " + String.Format("-u {0} START_GSC_MeetTalk_SSO", requestMsg["sawonNo"].ToString()));
                                } else if (requestMsg["method"].ToString().Equals("CloseTMT"))
                                {
                                    Process.Start(tmtPath, String.Format("-x {0} CLOSE_GSC_MeetTalk_SSO", requestMsg["sawonNo"].ToString()));
                                    appendText("Arguments: " + String.Format("-x {0} CLOSE_GSC_MeetTalk_SSO", requestMsg["sawonNo"].ToString()));
                                } else if (requestMsg["method"].ToString().Equals("openChatRoom"))
                                {
                                    Process.Start(tmtPath, String.Format("-p2pchatn:{0}", requestMsg["userIdnfr"].ToString()));
                                    appendText("Arguments: " + String.Format("-p2pchatn:{0}", requestMsg["userIdnfr"].ToString()));
                                }
                                else
                                {
                                    responseMsg["msg"] = "Invalid Method";
                                    ws.WriteString(responseMsg.ToString());
                                    ws.Close();
                                }

                                responseMsg["msg"] = "Success";
                            }

                            appendText("response Message: " + responseMsg.ToString());

                            ws.WriteString(responseMsg.ToString());

                            ws.Close();
                        }
                        catch (Exception e1)
                        {
                            appendText(e1.StackTrace);
                            appendText(e1.Message);
                        }
                    }
                }
            }
            catch (Exception e1)
            {
                throw e1;
            }
            finally
            {
                try { ws.Close(); }
                catch { }
                // 웹 소켓은 Dispose 해 줍니다.
                ws.Dispose();
            }
        }
        #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            webSocketServer.Dispose();
        }

        private void appendText(string text)
        {
            if (debugMode)
                textBox1.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "   " + text + "\r\n");
        }
    }
}
