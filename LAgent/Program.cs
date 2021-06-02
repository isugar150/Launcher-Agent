using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TMTAgent
{
    static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            IsExistProcess(true);

            Application.Run(new Form1(args));
        }

        static bool IsExistProcess(bool bKillProcess)
        {
            bool exist = false; 
            foreach (Process process in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName))
            {
                if (process.Id == Process.GetCurrentProcess().Id) { // 현재 실행되는 프로세스인 경우는 스킵
                    continue; 
                } 
                exist = true; 
                if (bKillProcess) { 
                    exist = KillProcess(process); // 다른 프로세스가 떠 있으면 강제 종료
                }
            }
            return exist;
        }

        static bool KillProcess(Process process)
        {
            try
            {
                process.Kill(); //Process Kill 강제종료
            }
            catch (Exception ex) { Debug.WriteLine(ex.Message); }
            return false;
        }

    }
}
