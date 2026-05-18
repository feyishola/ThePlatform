using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlatformService.Dtos
{
    public class PlatformReadDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Publisher { get; set; } = string.Empty;

        public string Cost { get; set; } = string.Empty;
    }
}