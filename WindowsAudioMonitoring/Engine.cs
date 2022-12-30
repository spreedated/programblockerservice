using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace WindowsAudioMonitoring.Logic
{
    internal class Engine : IDisposable
    {
        internal Timer loopTimer = new();
        internal bool isRunning = false;


        public Engine()
        {
            loopTimer.Elapsed += LoopTimer_Elapsed;
            loopTimer.Interval = new TimeSpan(0,5,0).TotalMilliseconds;
            loopTimer.Enabled = true;
            loopTimer.Start();

            this.LoopTimer_Elapsed(this, null);
        }

        public void LoopTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.isRunning)
            {
                return;
            }

            this.isRunning = true;

            

            this.isRunning = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                loopTimer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
