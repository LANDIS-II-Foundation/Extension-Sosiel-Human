﻿using System;
using System.Collections.Generic;
using System.Linq;

using Common.Entities;
using Common.Exceptions;
using Common.Randoms;

namespace CL1_M2
{
    public class CL1M2Agent : Agent, ICloneableAgent<CL1M2Agent>
    {
        private Dictionary<string, dynamic> PrivateVariables { get; set; } = new Dictionary<string, dynamic>();

        public override dynamic this[string key]
        {
            get
            {
                if (PrivateVariables.ContainsKey(key))
                    return PrivateVariables[key];
                else
                {
                    return base[key];
                }
            }

            set
            {
                if (Variables.ContainsKey(key))
                {
                    base[key] = value;
                }
                else
                {
                    PrivateVariables[key] = value;
                }

            }
        }


        public new CL1M2Agent Clone()
        {
            return (CL1M2Agent)base.Clone();
        }

        protected override Agent CreateInstance()
        {
            return new CL1M2Agent();
        }

        public new void GenerateCustomParams()
        {
            AgentSubtype agentSubtype = (AgentSubtype)LinearUniformRandom.GetInstance.Next(1, 3);

            this[VariablesUsedInCode.AgentSubtype] = agentSubtype;

            if (agentSubtype == AgentSubtype.Co)
            {
                this[VariablesUsedInCode.AgentC] = this[VariablesUsedInCode.Endowment];
            }
            else
            {
                this[VariablesUsedInCode.AgentC] = 0;
            }
        }
    }
}
