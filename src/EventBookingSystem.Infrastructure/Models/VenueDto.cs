using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Infrastructure.Models
{
    public class VenueDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Capacity { get; set; }
    }
}
