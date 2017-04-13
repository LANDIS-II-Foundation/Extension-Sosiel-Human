﻿using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;

namespace Common.Helpers
{
    using Configuration;
    using Randoms;
    using Entities;

    public static class IterationHelper
    {
        public static Dictionary<IAgent, AgentState> InitilizeBeginningState(InitialStateConfiguration configuration, IEnumerable<IAgent> agents)
        {
            Dictionary<IAgent, AgentState> temp = new Dictionary<IAgent, AgentState>();


            agents.ForEach(a =>
            {
                AgentState agentState = AgentState.Create();

                Dictionary<Rule, Dictionary<Goal, double>> ai = new Dictionary<Rule, Dictionary<Goal, double>>();

                a.AssignedRules.ForEach(r =>
                {
                    Dictionary<string, double> source;

                    a.InitialStateConfiguration.AnticipatedInfluenceState.TryGetValue(r.Id, out source);

                    Dictionary<Goal, double> inner = new Dictionary<Goal, double>();

                    a.Goals.ForEach(g =>
                    {
                        inner.Add(g, source != null && source.ContainsKey(g.Name) ? source[g.Name] : 0 /*(r.IsAction ? 0 : -1)*/);
                    });

                    ai.Add(r, inner);
                });

                agentState.AnticipationInfluence = ai;

                if (configuration.GenerateGoalImportance)
                {
                    double unadjustedProportion = 1;

                    var goals = a.Goals.Join(a.InitialStateConfiguration.AssignedGoals, g => g.Name, gs => gs, (g, gs) => new { g, gs }).ToArray();

                    int numberOfRankingGoals = goals.Count(o => o.g.RankingEnabled);

                    goals.OrderByDescending(o => o.g.RankingEnabled).ForEach((o, i) =>
                          {
                              double proportion = unadjustedProportion;

                              if (o.g.RankingEnabled)
                              {
                                  if (numberOfRankingGoals > 1 && i < numberOfRankingGoals - 1)
                                  {
                                      double d =  NormalDistributionRandom.GetInstance.Next();

                                      if (d < 0)
                                          d = 0;

                                      if (d > 1)
                                          d = 1;

                                      proportion = Math.Round(d, 1, MidpointRounding.AwayFromZero);
                                  }

                                  unadjustedProportion = Math.Round(unadjustedProportion - proportion, 1, MidpointRounding.AwayFromZero);
                              }
                              else
                              {
                                  proportion = 0;
                              }

                              GoalState goalState = new GoalState(0, o.g.FocalValue, proportion);

                              agentState.GoalsState.Add(o.g, goalState);
                          });
                }
                else
                {
                    a.InitialStateConfiguration.GoalState.ForEach(gs =>
                    {
                        Goal goal = a.Goals.First(g => g.Name == gs.Key);

                        GoalState goalState = new GoalState(0, goal.FocalValue, gs.Value.Importance);

                        agentState.GoalsState.Add(goal, goalState);
                    });
                }

                if (configuration.RandomlySelectRule)
                {
                    a.AssignedRules.Where(r => r.IsAction && r.IsCollectiveAction == false).GroupBy(r => new { r.RuleSet, r.RuleLayer }).ForEach(g =>
                       {
                           Rule selectedRule = g.RandomizeOne();

                           agentState.Matched.Add(selectedRule);
                           agentState.Activated.Add(selectedRule);
                       });
                }
                else
                {
                    Rule[] firstIterationsRule = a.InitialStateConfiguration.ActivatedRulesOnFirstIteration.Select(rId => a.AssignedRules.First(ar => ar.Id == rId)).ToArray();

                    agentState.Matched.AddRange(firstIterationsRule);
                    agentState.Activated.AddRange(firstIterationsRule);
                }

                temp.Add(a, agentState);
            });

            return temp;
        }

    }
}
