public interface IRhinoComputeListener
{
    JobTicket StartJob(string stream, string algo);

    IEnumerable<string> GetLatestJobsAndClearQueue();
}