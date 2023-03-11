using System.Collections.Concurrent;

namespace SpeckleSync
{
    public interface IJobDetails
    {
        IJobTicket GetJobTicket();
    }

    public interface IJobTicket
    {
        string Id { get; }
    }

    public interface IResult
    {
        ResultType ResultType { get; }
        object? ResultValue { get; }
        string? Message { get; }
    }

    public enum ResultType
    {
        Success, Fail, Skipped
    } 

    public abstract class JobberSingleton<T> where T : IJobDetails
    {
        private readonly ConcurrentQueue<T> jobs = new ConcurrentQueue<T>();

        private ConcurrentQueue<IResult> computeJobs = new ConcurrentQueue<IResult>();

        private bool jobQueueIsCurrentlyIterating = false;


        public JobberSingleton()
        {

        }

        public IJobTicket RegisterJob(T jobDetails)
        {
            jobs.Enqueue(jobDetails);

            if (jobQueueIsCurrentlyIterating) return jobDetails.GetJobTicket();

            jobQueueIsCurrentlyIterating = true;

            Task.Run(() =>
            {
                while (jobQueueIsCurrentlyIterating && !jobs.IsEmpty)
                {
                    if (jobs.TryDequeue(out var job) && job is not null)
                    {
                        computeJobs.Enqueue(RunJob(job));
                    }
                }
                jobQueueIsCurrentlyIterating = false;
            });

            return jobDetails.GetJobTicket();
        }

        protected abstract IResult RunJob(T job);

    }


}