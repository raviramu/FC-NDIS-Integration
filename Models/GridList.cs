﻿using System;
using System.Collections.Generic;

#nullable disable

namespace FC_NDIS.Models
{
    public partial class GridList
    {
        public GridList()
        {
            GridSettings = new HashSet<GridSetting>();
            UserRoleNews = new HashSet<UserRoleNew>();
        }

        public int GridId { get; set; }
        public string GridName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public virtual ICollection<GridSetting> GridSettings { get; set; }
        public virtual ICollection<UserRoleNew> UserRoleNews { get; set; }
    }
}
