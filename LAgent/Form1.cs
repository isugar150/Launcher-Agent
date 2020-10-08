using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using vtortola.WebSockets;
using vtortola.WebSockets.Deflate;

namespace LAgent
{
    public partial class Form1 : Form
    {
        private static Logger logger = LogManager.GetLogger("Launcher_Agent");
        private static WebSocketListener webSocketServer = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Visible = false; 
            ShowInTaskbar = false;
            webSocketInit();
        }

        #region Web socket
        public static void webSocketInit()
        {
            #region init WebSocket Server
            try
            {
                logger.Info("Init WebSocket Server");
                CancellationTokenSource cancellation = new CancellationTokenSource();
                //var endpoint = new IPEndPoint(IPAddress.Any, 1818);
                IPEndPoint endpoint;

                endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), int.Parse("49862"));

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
        private static async Task AcceptWebSocketClientsAsync(WebSocketListener server, CancellationToken token)
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
                catch (Exception)
                {
                    /*DevLog.Write("[WebSocket] Error Accepting clients: " + aex.GetBaseException().Message, LOG_LEVEL.ERROR);*/
                }
            }
        }

        /// <summary>
        /// 클라이언트가 메시지 던졌을때 로직
        /// </summary>
        /// <param name="ws">웹 소켓</param>
        /// <param name="cancellation">토큰</param>
        /// <returns></returns>
        private static async Task HandleConnectionAsync(WebSocket ws, CancellationToken cancellation)
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

                            // 앱 버전 체크
                            if (!requestMsg["version"].ToString().Equals("1.0.1"))
                            {
                                Debug.WriteLine("Old app version");
                                responseMsg["msg"] = "Old app version";
                            }

                            else
                            {

                                // 파일 실행
                                if (requestMsg["method"].ToString().Equals("RunFile"))
                                {
                                    Debug.WriteLine("RunFile");
                                    Process.Start(requestMsg["path"].ToString(), requestMsg["args"].ToString());
                                }

                                // 원격 데스크톱 연결
                                else if (requestMsg["method"].ToString().Equals("RunRDP"))
                                {
                                    Process rdcProcess = new Process();


                                    rdcProcess.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmdkey.exe");
                                    Debug.WriteLine("/delete:\"" + requestMsg["url"].ToString() + "\"");
                                    rdcProcess.StartInfo.Arguments = "/delete:\"" + requestMsg["url"].ToString() + "\"";
                                    rdcProcess.Start();

                                    rdcProcess.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmdkey.exe");
                                    Debug.WriteLine("/generic:\"" + requestMsg["url"].ToString() + ":" + requestMsg["port"].ToString() + "\" /user:\"" + requestMsg["user"].ToString() + "\" /pass:\"" + requestMsg["pwd"].ToString() + "\"");
                                    rdcProcess.StartInfo.Arguments = "/generic:\"" + requestMsg["url"].ToString() + "\" /user:\"" + requestMsg["user"].ToString() + "\" /pass:\"" + requestMsg["pwd"].ToString() + "\"";
                                    rdcProcess.Start();

                                    rdcProcess.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe");
                                    Debug.WriteLine("/v \"" + requestMsg["url"].ToString() + ":" + requestMsg["port"].ToString() + "\" /admin /f");
                                    rdcProcess.StartInfo.Arguments = "/v \"" + requestMsg["url"].ToString() + ":" + requestMsg["port"].ToString() + "\" /admin /f"; // ip or name of computer to connect
                                    rdcProcess.Start();

                                    Thread.Sleep(500);

                                    rdcProcess.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmdkey.exe");
                                    Debug.WriteLine("/delete:\"" + requestMsg["url"].ToString() + "\"");
                                    rdcProcess.StartInfo.Arguments = "/delete:\"" + requestMsg["url"].ToString() + "\"";
                                    rdcProcess.Start();

                                    rdcProcess.WaitForExit();
                                    rdcProcess.Close();
                                    rdcProcess.Dispose();
                                }

                                responseMsg["msg"] = "Success";
                            }

                            ws.WriteString(responseMsg.ToString());

                            ws.Close();
                        }
                        catch (Exception e1)
                        {
                            Debug.WriteLine(e1.Message);
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
    }
}
