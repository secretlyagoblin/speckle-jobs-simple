namespace SpeckleServer.RhinoJobber
{
    internal interface IRhinoJobService
    {
        JobTicket RunCommandByName(string command, CommandRunSettings runSettings);
        IEnumerable<JobTicket> RunCommandFromStream(string server, string streamId, string branch);


    }
}