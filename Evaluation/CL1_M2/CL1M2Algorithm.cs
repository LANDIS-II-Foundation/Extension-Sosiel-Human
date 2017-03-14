﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


using Common.Configuration;
using Common.Algorithm;
using Common.Entities;
using Common.Helpers;
using Common.Randoms;
using Common.Models;

namespace CL1_M2
{
    public sealed class CL1M2Algorithm : AlgorithmBase, IAlgorithm
    {
        readonly Configuration<CL1M2Agent> _configuration;

        List<CommonPoolOutput> _commonPoolStatistic;

        bool isAgentMovement;

        public string Name { get { return "Cognitive level 1 Model 2"; } }

        public CL1M2Algorithm(Configuration<CL1M2Agent> configuration)
        {
            _configuration = configuration;

            _outputFolder = @"Output\CL1_M2";

            _subtypeProportionStatistic = new List<SubtypeProportionOutput>(_configuration.AlgorithmConfiguration.IterationCount);
            _commonPoolStatistic = new List<CommonPoolOutput>(_configuration.AlgorithmConfiguration.IterationCount);

            if (Directory.Exists(_outputFolder) == false)
                Directory.CreateDirectory(_outputFolder);
        }

       
        

        protected override void Initialize()
        {
            _siteList = SiteList.Generate(_configuration.AlgorithmConfiguration.AgentCount,
                _configuration.AlgorithmConfiguration.VacantProportion);

            _agentList = AgentList.Generate(_configuration.AlgorithmConfiguration.AgentCount,
                _configuration.AgentConfiguration, _siteList);

            //todo: temporary solution
            _agentList.Agents.ForEach(agent => SetRandomVariables(agent));
        }

        protected override void ExecuteAlgorithm()
        {
            _subtypeProportionStatistic.Add(CalculateSubtypeProportion(0));

            for (int i = 1; i <= _configuration.AlgorithmConfiguration.IterationCount; i++)
            {
                Console.WriteLine($"Starting {i} iteration");

                isAgentMovement = false;

                List<IAgent> orderingAgents = RandomizeHelper.Randomize(_agentList.Agents.Where(a => a[Agent.VariablesUsedInCode.AgentStatus] == "active"));

                List<Site> vacantSites = _siteList.AsSiteEnumerable().Where(s => s.IsOccupied == false).ToList();

                foreach (var agent in orderingAgents)
                {
                    SetRandomVariables(agent);
                    CalculateParamsDependOnSite(agent);

                    Site[] betterSites = vacantSites
                        //in this case we exclude the center site because it isn't occupied by agent
                        .Select(site => new { site, Occupied = _siteList.AdjacentSites(site).Where(s => s.IsOccupied) })
                        .Select(obj => new
                        {
                            obj.site,
                            Wellbeing = CalculateAgentWellbeing(agent, obj.Occupied)
                        })
                        .Where(obj => obj.Wellbeing > agent[Agent.VariablesUsedInCode.AgentSiteWellbeing])
                        .GroupBy(obj => obj.Wellbeing).OrderByDescending(obj => obj.Key)
                        .Take(1).SelectMany(g => g.Select(o => o.site)).ToArray();

                    agent[Agent.VariablesUsedInCode.AgentBetterSiteAvailable] = betterSites.Length > 0;

                    if (agent[Agent.VariablesUsedInCode.AgentBetterSiteAvailable])
                    {
                        Rule rule = agent.Rules.First();

                        if (rule.IsMatch(agent))
                        {
                            Site oldSite = agent[Agent.VariablesUsedInCode.AgentSite];

                            Site bestSite = betterSites.RandomizeOne();

                            agent[Agent.VariablesUsedInCode.AgentBetterSite] = bestSite;

                            rule.Apply(agent);

                            isAgentMovement = true;

                            vacantSites.Add(oldSite);
                            vacantSites.Remove(bestSite);
                        }
                    }
                }

                _subtypeProportionStatistic.Add(CalculateSubtypeProportion(i));
                _commonPoolStatistic.Add(CalculateCommonPoolStatistics(i));

                if (isAgentMovement == false)
                {
                    break;
                }
            }
        }

        protected override void SaveCustomStatistic()
        {
            SaveCommonPoolStatistic();
        }


        private void SetRandomVariables(IAgent agent)
        {
            int agentSubtype = LinearUniformRandom.GetInstance.Next(1, 3);

            agent[Agent.VariablesUsedInCode.E] = LinearUniformRandom.GetInstance.Next(1, (int)agent[Agent.VariablesUsedInCode.MaxE] + 1);
            agent[Agent.VariablesUsedInCode.AgentSubtype] = (AgentSubtype)agentSubtype;
            agent[Agent.VariablesUsedInCode.AgentC] = agent[Agent.VariablesUsedInCode.AgentSubtype] == AgentSubtype.Co ? agent[Agent.VariablesUsedInCode.E] : 0;
        }

        private void CalculateParamsDependOnSite(IAgent agent)
        {
            Site currentSite = (Site)agent[Agent.VariablesUsedInCode.AgentSite];

            Site[] adjacentSites = _siteList.AdjacentSites(currentSite).ToArray();

            agent[Agent.VariablesUsedInCode.NeighborhoodSize] = (double)currentSite.GroupSize;
            agent[Agent.VariablesUsedInCode.NeighborhoodVacantSites] = adjacentSites.Count(s => s.IsOccupied == false);
            agent[Agent.VariablesUsedInCode.NeighborhoodUnalike] = adjacentSites.Where(s => s.IsOccupied)
                .Count(s => s.OccupiedBy[Agent.VariablesUsedInCode.AgentSubtype] != agent[Agent.VariablesUsedInCode.AgentSubtype]);

            agent[Agent.VariablesUsedInCode.NeighborhoodVacantSites] = adjacentSites.Count(s => s.IsOccupied == false);
            agent[Agent.VariablesUsedInCode.CommonPoolSize] = (double)(agent[Agent.VariablesUsedInCode.NeighborhoodSize] + 1 - agent[Agent.VariablesUsedInCode.NeighborhoodVacantSites]);

            agent[Agent.VariablesUsedInCode.CommonPoolSubtupeProportion] = (agent[Agent.VariablesUsedInCode.CommonPoolSize] - agent[Agent.VariablesUsedInCode.NeighborhoodUnalike])
                / agent[Agent.VariablesUsedInCode.CommonPoolSize];

            agent[Agent.VariablesUsedInCode.CommonPoolC] = _siteList.AdjacentSites(currentSite, true).Where(s => s.IsOccupied).Sum(s => s.OccupiedBy[Agent.VariablesUsedInCode.AgentC]);
            agent[Agent.VariablesUsedInCode.AgentSiteWellbeing] = agent[Agent.VariablesUsedInCode.E] - agent[Agent.VariablesUsedInCode.AgentC]
                + agent[Agent.VariablesUsedInCode.M] * agent[Agent.VariablesUsedInCode.CommonPoolC] / agent[Agent.VariablesUsedInCode.CommonPoolSize];


        }

        private double CalculateAgentWellbeing(IAgent agent, IEnumerable<Site> occupied)
        {
            return agent[Agent.VariablesUsedInCode.E] - agent[Agent.VariablesUsedInCode.AgentC]
                + agent[Agent.VariablesUsedInCode.M] * (occupied.Sum(s => s.OccupiedBy[Agent.VariablesUsedInCode.AgentC]) + agent[Agent.VariablesUsedInCode.AgentC]) / ((double)occupied.Count() + 1);
        }
        

        

        private CommonPoolOutput CalculateCommonPoolStatistics(int iteration)
        {
            CommonPoolOutput cp = new CommonPoolOutput { Iteration = iteration };

            cp.CommonPools = _siteList.AsSiteEnumerable().Where(s => s.IsOccupied)
                .Select(site => CalculateCommonPoolStat(site)).ToArray();

            return cp;
        }

        private CommonPoolProportion CalculateCommonPoolStat(Site centerSite)
        {
            IAgent agent = centerSite.OccupiedBy;
            var pool = _siteList.AdjacentSites(centerSite, true).Where(s => s.IsOccupied);
            int commonPoolC = pool.Sum(s => s.OccupiedBy[Agent.VariablesUsedInCode.AgentC]);
            int poolSize = pool.Count();


            return new CommonPoolProportion
            {
                Center = new CommonPoolCenter { X = centerSite.HorizontalPosition, Y = centerSite.VerticalPosition },
                Wellbeing = agent[Agent.VariablesUsedInCode.E] * poolSize + commonPoolC
                    * (agent[Agent.VariablesUsedInCode.M] / (double)poolSize - 1),
                CoProportion = pool.Count(s => s.OccupiedBy[Agent.VariablesUsedInCode.AgentSubtype] == AgentSubtype.Co) / (double)poolSize
            };
        }

        private SubtypeProportionOutput CalculateSubtypeProportion(int iteration)
        {
            SubtypeProportionOutput sp = new SubtypeProportionOutput { Iteration = iteration };

            sp.Proportion = _siteList.AsSiteEnumerable()
                .Where(s=>s.IsOccupied)
                .Select(site => new { site, CommonPool = _siteList.AdjacentSites(site, true).Where(s => s.IsOccupied) })
                .Average(o => o.CommonPool.Count(s => s.OccupiedBy[Agent.VariablesUsedInCode.AgentSubtype] == AgentSubtype.Co) / (double)o.CommonPool.Count());

            return sp;
        }

        private void SaveCommonPoolStatistic()
        {
            ResultSavingHelper.Save(_commonPoolStatistic.Select(cps => new SimpleLineOutput(cps)), $@"{_outputFolder}\common_pool_statistics.csv");
        }
    }
}
