﻿using System;
using System.Collections.Generic;
using System.Linq;


namespace Common.Processes
{
    using Enums;
    using Entities;
    using Models;
    using Helpers;


    public class SocialLearning
    {
        Dictionary<IConfigurableAgent, List<Rule>> confidentAgents = new Dictionary<IConfigurableAgent, List<Rule>>();

        public void ExecuteSelection(IConfigurableAgent agent, AgentState agentState,
            Goal[] rankedGoals, RuleLayer layer)
        {
            GoalState associatedGoalState = agentState.GoalsState[rankedGoals.First(g => layer.Set.AssociatedWith.Contains(g))];


            if (associatedGoalState.Confidence)
            {
                Rule activatedRule = agentState.Activated.Single(h => h.Layer == layer);

                if (confidentAgents.ContainsKey(agent))
                    confidentAgents[agent].Add(activatedRule);
                else
                    confidentAgents.Add(agent, new List<Rule>() { activatedRule });
            }
        }

        public void ExecuteLearning(IConfigurableAgent[] allAgents, Dictionary<IConfigurableAgent, AgentState> iterationState)
        {
            foreach (var agent in allAgents)
            {
                foreach (IConfigurableAgent connectedAgent in agent.ConnectedAgents)
                {
                    foreach (Rule rule in confidentAgents[connectedAgent]
                        .Where(r => r.Layer.Set.AssociatedWith.Any(g => iterationState[agent].GoalsState.Any(kvp => kvp.Key == g))))
                    {
                        if (agent.AssignedRules.Any(r => r != rule))
                        {
                            agent.AssignedRules.Add(rule);
                        }
                    }
                }
            }

            //clean state
            confidentAgents.Clear();
        }
    }
}