using System.ServiceProcess;

namespace MqttRemoteControl
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var ServicesToRun = new ServiceBase[]
            {
                new MqttRemoteControl()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
