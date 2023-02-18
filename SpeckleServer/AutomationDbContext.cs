using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeckleServer.Database
{
    public class AutomationDbContext : DbContext
    {
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Stream> Streams { get; set; }
        public DbSet<Command> Commands { get; set; }
        public DbSet<Automation> Automations { get; set; }

        public AutomationDbContext(DbContextOptions options) : base(options)
        {
        }
    }

    public class Job
    {
        [Key]
        public int JobId { get; set; }

        public string StreamId { get; set; }
        public Stream Stream { get; set; }
        public string CommandId { get; set; }
        public Command Command { get; set; }

    }

    public class Stream
    {
        [Key]
        public string StreamId { get; set; } = "";

        [InverseProperty(nameof(Stream))]
        public ICollection<Job> Jobs { get; set; } = new List<Job>();
    }

    public class Command
    {
        [Key]
        public string Name { get; set; } = "";

        [InverseProperty(nameof(Command))]
        public ICollection<Job> Jobs { get; set; } = new List<Job>();

        [InverseProperty(nameof(Command))]
        public ICollection<Automation> AutomationHistory { get; set; } = new List<Automation>();
    }

    public class Automation
    {
        public Command Command { get; set; }

        [Key]
        public int AutomationId { get; set; }

        public string GhString { get; set; }

        public DateTime DateTime { get; set; } = DateTime.Now.ToUniversalTime();
    }
}