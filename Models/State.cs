﻿using System;
using System.Collections.Generic;

#nullable disable

namespace FC_NDIS.Models
{
    public partial class State
    {
        public long Id { get; set; }
        public long JobId { get; set; }
        public string Name { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Data { get; set; }

        public virtual Job Job { get; set; }
    }
}
