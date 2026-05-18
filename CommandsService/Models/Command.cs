using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommandsService.Models
{
    public class Command
    {
        public int Id { get; set; }

        public string HowTo { get; set; } = string.Empty;

        public string CommandLine { get; set; } = string.Empty;

        public int PlatformId { get; set; }

        public Platform? Platform { get; set; }
        // this is what is called a navigation property which allows us to btw Command and Platform
    }
}