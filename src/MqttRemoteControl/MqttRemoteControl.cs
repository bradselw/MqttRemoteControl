using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MqttRemoteControl
{
    public partial class MqttRemoteControl : ServiceBase
    {
        private MqttClient client;

        private static string[] Topics
        {
            get
            {
                return topics.Value;
            }
        }
        private static Lazy<string[]> topics = new Lazy<string[]>(() => ConfigurationManager.AppSettings["topics"].Split(';'));

        private static byte[] QosLevels
        {
            get
            {
                return qosLevels.Value;
            }
        }
        private static Lazy<byte[]> qosLevels = new Lazy<byte[]>(() => Enumerable.Repeat(MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, Topics.Length).ToArray());

        public MqttRemoteControl()
        {
            InitializeComponent();
            eventLog = new EventLog()
            {
                Source = nameof(MqttRemoteControl),
                Log = "Application"
            };
            if (!EventLog.SourceExists(eventLog.Source))
            {
                EventLog.CreateEventSource(eventLog.Source, eventLog.Log);
            }

            client = new MqttClient(ConfigurationManager.AppSettings["brokerUrl"]);
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.Connect(Guid.NewGuid().ToString());
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            var serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(ServiceHandle, ref serviceStatus);

            eventLog.WriteEntry("In OnStart");

            // MQTT registration
            client.Subscribe(Topics, QosLevels);

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(ServiceHandle, ref serviceStatus);
        }

        public void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            var result = Encoding.Default.GetString(e.Message);
            Console.WriteLine(result);
        }

        protected override void OnStop()
        {
            // Update the service state to Start Pending.
            var serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(ServiceHandle, ref serviceStatus);

            eventLog.WriteEntry("In onStop.");

            // MQTT unsubscribe
            client.Unsubscribe(Topics);

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(ServiceHandle, ref serviceStatus);
        }

        #region Status

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public long dwServiceType;
            public ServiceState dwCurrentState;
            public long dwControlsAccepted;
            public long dwWin32ExitCode;
            public long dwServiceSpecificExitCode;
            public long dwCheckPoint;
            public long dwWaitHint;
        };

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        #endregion
    }
}
