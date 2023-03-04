using System;
using System.Text.RegularExpressions;

namespace SpeckleServer
{
    public record SpeckleUrl
    {
        public bool IsValid { get; }
        public string Preamble { get; }
        public string ServerUrl { get; }
        public string Stream { get; }
        public string Path { get; }
        public string Key { get; }
        public string BranchOrCommit { get; }

        public SpeckleUrl(string url)
        {
            //look, sorry, I was trying to be clever
            var pattern = $"(?<{nameof(Preamble)}>(?<{nameof(ServerUrl)}>.+\\.+\\w+\\/)?(?:.*streams\\/)?)(?<{nameof(Stream)}>[\\w\\*]+)\\/(?<{nameof(Path)}>(?<{nameof(Key)}>\\w+)\\/(?<{nameof(BranchOrCommit)}>[^\\?]+))";
            var regex = new Regex(pattern);
            var match = regex.Match(url);

            if (!match.Success)
            {
                IsValid = false;
                Preamble = "";
                ServerUrl = "";
                Stream = "";
                Path = "";
                Key = "";
                BranchOrCommit = "";
                return;
            }

            //still sorry, I was still trying to be clever
            Preamble = match.Groups.GetValueOrDefault(nameof(Preamble))?.Value ?? "";
            ServerUrl = match.Groups.GetValueOrDefault(nameof(ServerUrl))?.Value ?? "";
            Stream = match.Groups.GetValueOrDefault(nameof(Stream))?.Value ?? "";
            Path = match.Groups.GetValueOrDefault(nameof(Path))?.Value ?? "";
            Key = match.Groups.GetValueOrDefault(nameof(Key))?.Value ?? "";
            BranchOrCommit = match.Groups.GetValueOrDefault(nameof(BranchOrCommit))?.Value ?? "";
        }

        public bool MatchesBranchPattern(string testBranch)
        {
            var url = this.BranchOrCommit.ToLower().Replace("*", "\\w+");

            var regex = new Regex(url);
            var match = regex.Match(testBranch.ToLower());

            return match.Success;
        }
    }    
}
