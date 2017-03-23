﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Entities
{
    using Helpers;   

    public class RuleLayer
    {
        int RuleIndexer = 0;
        public int PositionNumber { get; set; }

        public RuleSet Set { get; set; }

        public RuleLayerParameters LayerParameters { get; private set; }

        public List<Rule> Rules { get; private set; }

        public RuleLayer(RuleLayerParameters parameters)
        {
            Rules = new List<Rule>(parameters.MaxRuleCount);
            LayerParameters = parameters;
        }

        public RuleLayer(RuleLayerParameters parameters, IEnumerable<Rule> rules) : this(parameters)
        {
            rules.ForEach(r => Add(r));
        }

        void CheckAndRemove()
        {
            if (Rules.Count == LayerParameters.MaxRuleCount)
            {
                Rule oldestRules = Rules.OrderByDescending(h => h.FreshnessStatus).First(h => h.IsAction == true);

                //Set.UnassignRule(oldestRules);

                Rules.Remove(oldestRules);
            }
        }

        public void Add(Rule Rule)
        {
            CheckAndRemove();

            RuleIndexer++;
            Rule.RulePositionNumber = RuleIndexer;
            Rule.Layer = this;

            Rules.Add(Rule);
        }
    }
}