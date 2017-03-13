﻿using System;
using System.Collections.Generic;
using System.Linq;

using FileHelpers;

namespace CL1_M2.Models
{
    [DelimitedRecord(";")]
    class EdgeOutput 
    {
        public int AgentId { get; set; }

        public int AdjacentAgentId { get; set; }

      
    }
}
