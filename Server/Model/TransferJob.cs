using System.Net.Sockets;
using System.Threading;

namespace Server.Model
{
    class TransferJob
    {
        public Thread _thread { get; set; }
        public TcpClient _client { get; set; }
        public JobType Type { get; private set; }
        public Progress JobProgress { get; set; } 
        public class Progress
        {
            public double Value { get; set; }
            public double Maximum { get; set; }
            public double Minimum { get; set; }
        }
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
            Type = jobType;
            JobProgress = new Progress();
        }

        public void Start(bool parametrized = true)
        {
            if(parametrized) _thread.Start(JobProgress);
            else _thread.Start();
        }
    }
}
