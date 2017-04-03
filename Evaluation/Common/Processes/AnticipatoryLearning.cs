﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Processes
{
    using Enums;
    using Entities;
    using Helpers;

    public class AnticipatoryLearning : VolatileProcess
    {

        GoalState currentGoalState;


        IEnumerable<Goal> SortByProportion(Dictionary<Goal, GoalState> goals)
        {
            int numberOfGoal = goals.Count;

            var noConfidenceGoals = goals.Where(kvp => kvp.Value.Confidence == false).ToArray();


            if (noConfidenceGoals.Length > 0)
            {

                var noConfidenceProportions = noConfidenceGoals.Select(kvp =>
                    new { Proportion = kvp.Value.Proportion * (1 + Math.Abs(kvp.Value.DiffPriorAndCurrent) / kvp.Key.MaxGoalValue), Goal = kvp.Key }).ToArray();

                double totalNoConfidenctUnadjustedProportions = noConfidenceGoals.Sum(kvp => kvp.Value.Proportion);

                double totalNoConfidenceAdjustedProportions = noConfidenceProportions.Sum(p => p.Proportion);

                var confidenceGoals = goals.Where(kvp => kvp.Value.Confidence == true).ToArray();

                var confidenceProportions = confidenceGoals.Select(kvp =>
                    new { Proportion = kvp.Value.Proportion * (1 - totalNoConfidenceAdjustedProportions) / totalNoConfidenctUnadjustedProportions, Goal = kvp.Key }).ToArray();

                

                var temp = Enumerable.Concat(noConfidenceProportions, confidenceProportions).Sum(o => o.Proportion);


                Enumerable.Concat(noConfidenceProportions, confidenceProportions).ForEach(p =>
                {
                    goals[p.Goal].Proportion = p.Proportion;

                });

            }


            List<Goal> vector = new List<Goal>(100);

            goals.ForEach(kvp =>
            {
                int numberOfInsertions = Convert.ToInt32(Math.Round(kvp.Value.Proportion * 100));

                for (int i = 0; i < numberOfInsertions; i++) { vector.Add(kvp.Key); }
            });


            for (int i = 0; i < numberOfGoal && vector.Count > 0; i++)
            {
                Goal nextGoal = vector.RandomizeOne();

                vector.RemoveAll(o => o == nextGoal);


                yield return nextGoal;
            }
        }


        protected override void AboveMin()
        {
            if (currentGoalState.DiffCurrentAndMin <= 0)
            {
                currentGoalState.AnticipatedDirection = AnticipatedDirection.Up;

                if (currentGoalState.DiffCurrentAndMin > currentGoalState.DiffPriorAndMin)
                {
                    currentGoalState.Confidence = true;
                }
                else
                {
                    currentGoalState.Confidence = false;
                }
            }
            else
            {
                currentGoalState.AnticipatedDirection = AnticipatedDirection.Stay;
                currentGoalState.Confidence = true;
            }
        }

        protected override void BelowMax()
        {
            if (currentGoalState.DiffCurrentAndMin > 0)
            {
                currentGoalState.AnticipatedDirection = AnticipatedDirection.Down;

                if (currentGoalState.DiffCurrentAndMin > currentGoalState.DiffPriorAndMin)
                {
                    currentGoalState.Confidence = true;
                }
                else
                {
                    currentGoalState.Confidence = false;
                }
            }
            else
            {
                currentGoalState.AnticipatedDirection = AnticipatedDirection.Stay;
                currentGoalState.Confidence = true;
            }
        }

        protected override void Maximize()
        {
            AboveMin();
        }

        public Goal[] Execute(IConfigurableAgent agent, LinkedListNode<Dictionary<IConfigurableAgent, AgentState>> lastIteration)
        {
            AgentState currentIterationAgentState = lastIteration.Value[agent];
            AgentState previousIterationAgentState = lastIteration.Previous.Value[agent];

            foreach (var kvp in previousIterationAgentState.GoalsState)
            {
                Goal goal = kvp.Key;
                GoalState prevGoalState = kvp.Value;

                currentGoalState = currentIterationAgentState.GoalsState[goal];

                currentGoalState.Value = agent[goal.ReferenceVariable];

                if (goal.ChangeFocalValueOnPrevious)
                    currentGoalState.FocalValue = previousIterationAgentState.GoalsState[goal].Value;

                currentGoalState.DiffCurrentAndMin = currentGoalState.Value - currentGoalState.FocalValue;

                //todo: check
                currentGoalState.DiffPriorAndMin = prevGoalState.Value - currentGoalState.FocalValue;

                currentGoalState.DiffPriorAndCurrent = prevGoalState.Value - currentGoalState.Value;


                //goalState.Value contains prior Iteration value
                currentGoalState.AnticipatedInfluenceValue = currentGoalState.Value - prevGoalState.Value;


                //todo
                //currentGS.FocalValue = value;

                //2.Update the anticipated influence of heuristics activated in prior Iteration
                IEnumerable<Rule> activatedInPriorIteration = previousIterationAgentState.Activated;

                //todo
                activatedInPriorIteration.ForEach(r =>
                {
                    currentIterationAgentState.AnticipationInfluence[r][goal] = currentGoalState.AnticipatedInfluenceValue;
                });

                SpecificLogic(goal.Tendency);
            }

            return SortByProportion(currentIterationAgentState.GoalsState).ToArray();
        }
    }
}