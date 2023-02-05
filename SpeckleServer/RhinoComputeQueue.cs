using System.Collections.Concurrent;

namespace SpeckleServer
{
    public class RhinoComputeQueue
    {
        public RhinoComputeQueue() { 

        }

        private readonly Stack<Automation> _automations = new();
        private string _rhinoComputeUrl = "";

        public void AddAutomation(Automation automation)
        {
            _automations.Push(automation);

            RecursiveDequeue();
        }

        private void RecursiveDequeue()
        {
            if (_automations.Count == 0) return;

            TryRunGrasshopperScript(_automations.Pop());

            RecursiveDequeue();
        }

        public void ConfigureComputeUrl(string url)
        {
            _rhinoComputeUrl = url;
        }

        private void TryRunGrasshopperScript(Automation automation)
        {
            //Rhino.Compute.ComputeServer.WebAddress = _rhinoComputeUrl;
            //
            //var trees = new List<Rhino.Compute.GrasshopperDataTree>();
            //
            //var results = Rhino.Compute.GrasshopperCompute.EvaluateDefinition(scriptName + ".gh", trees);
            //
            //Console.WriteLine("Trying to run script" + scriptName);
            //
            //foreach (var tree in results)
            //{
            //    foreach (var branch in tree)
            //    {
            //        foreach (var item in branch.Value)
            //        {
            //            Console.WriteLine(branch.Key + ": " + item.Data.ToString());
            //        }
            //    }
            //}
        }
    }
}
