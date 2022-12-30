using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace WindowsAudioMonitoring
{
    public partial class ServiceEntry : ServiceBase
    {
        private Engine e = null;

        public ServiceEntry()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Task.Factory.StartNew(() =>
            {
                this.e = new();
            });
        }

        protected override void OnStop()
        {
            this.e?.Dispose();
        }

        public void DebugStart()
        {
            this.OnStart(null);
        }
    }
}
