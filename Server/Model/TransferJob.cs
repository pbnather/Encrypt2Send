using System.Net.Sockets;
using System.Threading;

namespace Server.Model
{
    class TransferJob
    {
        private Thread _thread { get; set; }
        private TcpClient _client { get; set; }
        public JobType _jobType { get; private set; }

        public enum JobType
        {
            DOWNLOAD,
            UPLOAD,
            OTHER
        }

        public TransferJob(Thread job, TcpClient client, JobType jobType)
        {
            _thread = job;
            _client = client;
            _jobType = jobType;
        }

        public void Start(bool parametrized = true)
        {
            if(parametrized) _thread.Start(_client);
            else _thread.Start();
        }
    }
}
