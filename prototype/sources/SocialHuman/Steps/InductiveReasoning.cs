﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SocialHuman.Steps
{
    using Actors;
    using Entities;
    using Enums;
    using Models;
    using Randoms;

    class InductiveReasoning
    {
        public void Execute(Actor actor, LinkedListNode<Period> periodModel, Site site, HeuristicLayer layer)
        {
            Period currentPeriod = periodModel.Value;
            Period priorPeriod = periodModel.Previous.Value;

            Heuristic activatedPriorPeriodHeuristic = priorPeriod.GetStateForSite(actor, site).GetActivated(layer);

            LinkedListNode<Period> tempNode = periodModel.Previous;

            

            while (activatedPriorPeriodHeuristic.IsAction == false && tempNode.Previous != null)
            {
                tempNode = tempNode.Previous;
                activatedPriorPeriodHeuristic = tempNode.Value.GetStateForSite(actor, site).GetActivated(layer);
            }

            double antecedentConst = currentPeriod.TotalBiomass;

            if(layer.PositionNumber != 1)
            {
                HeuristicLayer previousLayer = layer.Set.Layers.Single(l => l.PositionNumber == layer.PositionNumber - 1);

                Heuristic previousCreatedHeuristic = previousLayer.Heuristics.SingleOrDefault(h => h.FreshnessStatus == 0 && h.CreatedBy == site);

                if (previousCreatedHeuristic != null)
                {
                    antecedentConst = previousCreatedHeuristic.ConsequentValue;
                }
                else
                {
                    antecedentConst = activatedPriorPeriodHeuristic.AncetedentConst;
                }
            }

            HeuristicConsequentRule rule = layer.ConsequentRule;

            double min = rule.MinValue;
            double max = rule.MaxValue;

            double newConsequentValue = activatedPriorPeriodHeuristic.ConsequentValue;

            switch (currentPeriod.GetCriticalGoal(actor).AnticipatedDirection)
            {
                case AnticipatedDirection.Up:
                    {
                        if (rule.ConsequentRelationship == ConsequentRelationship.Positive)
                        {
                            max = Math.Abs(activatedPriorPeriodHeuristic.ConsequentValue - max);

                            newConsequentValue += (Math.Abs(PowerLawRandom.GetInstance.Next(min, max) - max));
                        }
                        if (rule.ConsequentRelationship == ConsequentRelationship.Negative)
                        {
                            max = Math.Abs(activatedPriorPeriodHeuristic.ConsequentValue - min);

                            newConsequentValue -= (Math.Abs(PowerLawRandom.GetInstance.Next(min, max) - max));
                        }

                        break;
                    }
                case AnticipatedDirection.Down:
                    {
                        if (rule.ConsequentRelationship == ConsequentRelationship.Positive)
                        {
                            max = Math.Abs(activatedPriorPeriodHeuristic.ConsequentValue - min);

                            newConsequentValue -= (Math.Abs(PowerLawRandom.GetInstance.Next(min, max) - max));
                        }
                        if (rule.ConsequentRelationship == ConsequentRelationship.Negative)
                        {
                            max = Math.Abs(activatedPriorPeriodHeuristic.ConsequentValue - max);

                            newConsequentValue += (Math.Abs(PowerLawRandom.GetInstance.Next(min, max) - max));
                        }

                        break;
                    }
                default:
                    {
                        throw new Exception("Not implemented for AnticipatedDirection == 'stay'");
                    }
            }

            //Heuristic[] activatedPriorPeriodSiteHeuristics = priorPeriod.GetStateForSite(actor, site).Activated
            //    .Where(h=>h.Layer.Set == layer.Set)
            //    .OrderBy(h => h.Layer.PositionNumber)
            //    .ToArray();

            ////calculating new antecedent constant
            //double result = priorPeriod.TotalBiomass;

            //foreach (Heuristic heuristic in activatedPriorPeriodSiteHeuristics)
            //{
            //    if (heuristic == activatedPriorPeriodHeuristic)
            //    {
            //        break;
            //    }

            //    result = heuristic.ConsequentValue;
            //}

            HeuristicParameters newHeuristicParams = new HeuristicParameters
            {
                AntecedentConst = antecedentConst,
                AntecedentInequalitySign = activatedPriorPeriodHeuristic.AntecedentInequalitySign,
                ConsequentValue = newConsequentValue,
                IsAction = true,
                FreshnessStatus = 0
            };

            layer.Add(Heuristic.Create(newHeuristicParams, currentPeriod.GoalStates[actor], site));
        }
    }
}