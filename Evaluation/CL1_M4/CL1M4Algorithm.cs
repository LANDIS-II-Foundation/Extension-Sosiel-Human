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

namespace CL1_M4
{
    public sealed class CL1M4Algorithm : AlgorithmBase, IAlgorithm
    {
        public string Name { get { return "Cognitive level 1 Model 4"; } }

        readonly Configuration<CL1M4Agent> _configuration;

        //List<AvgWellbeingOutput> _averageWellbeingStatistic;
        //List<AvgVariablesOutput> _averageVariablesStatistic;

        List<CommonPoolSubtypeFrequencyOutput> _commonPoolFrequency;

        bool _isAgentMovement;

        double _disturbance;

        double _disturbanceIncrement;

        public CL1M4Algorithm(Configuration<CL1M4Agent> configuration)
        {
            _configuration = configuration;

            _outputFolder = @"Output\CL1_M4";

            _disturbance = _configuration.AgentConfiguration[Agent.VariablesUsedInCode.InitialDisturbance];
            _disturbanceIncrement = _configuration.AgentConfiguration[Agent.VariablesUsedInCode.DisturbanceIncrement];

            _subtypeProportionStatistic = new List<SubtypeProportionOutput>(_configuration.AlgorithmConfiguration.IterationCount);

            //_averageWellbeingStatistic = new List<AvgWellbeingOutput>(_configuration.AlgorithmConfiguration.IterationCount);
            //_averageVariablesStatistic = new List<AvgVariablesOutput>(_configuration.AlgorithmConfiguration.IterationCount);

            _commonPoolFrequency = new List<CommonPoolSubtypeFrequencyOutput>(_configuration.AlgorithmConfiguration.IterationCount);

            if (Directory.Exists(_outputFolder) == false)
                Directory.CreateDirectory(_outputFolder);
        }

        protected override void Initialize()
        {
            _siteList = SiteList.Generate(_configuration.AlgorithmConfiguration.AgentCount,
                _configuration.AlgorithmConfiguration.VacantProportion);

            _agentList = AgentList.Generate(_configuration.AlgorithmConfiguration.AgentCount,
                _configuration.AgentConfiguration, _siteList);
        }

        protected override void ExecuteAlgorithm()
        {
            int agentType = (int)AgentSubtype.Co;

            _subtypeProportionStatistic.Add(CreateCommonPoolSubtypeProportionRecord(0, agentType));
            _commonPoolFrequency.Add(CreateCommonPoolFrequencyRecord(0, _disturbance, agentType));

            for (int i = 1; i <= _configuration.AlgorithmConfiguration.IterationCount; i++)
            {
                Console.WriteLine($"Starting {i} iteration");

                _isAgentMovement = false;

                _disturbance += _disturbanceIncrement;

                List<IAgent> orderingAgents = RandomizeHelper.Randomize(_agentList.Agents.Where(a => a[Agent.VariablesUsedInCode.AgentStatus] == "active"));

                List<Site> vacantSites = _siteList.AsSiteEnumerable().Where(s => s.IsOccupied == false).ToList();

                if (orderingAgents.Count == 0)
                    break;

                foreach (var agent in orderingAgents)
                {
                    CalculateParamsDependOnSite(agent);

                    Site[] betterSites = vacantSites.AsParallel()
                        .Select(site => new
                        {
                            site,
                            Wellbeing = CalculateAgentWellbeing(agent, site)
                        })
                        .Where(obj => obj.Wellbeing > agent[Agent.VariablesUsedInCode.AgentSiteWellbeing]).AsSequential()
                        .GroupBy(obj => obj.Wellbeing).OrderByDescending(obj => obj.Key)
                        .Take(1).SelectMany(g => g.Select(o => o.site)).ToArray();

                    agent[Agent.VariablesUsedInCode.AgentBetterSiteAvailable] = betterSites.Length > 0;

                    if (agent[Agent.VariablesUsedInCode.AgentBetterSiteAvailable])
                    {
                        Rule rule = agent.Rules.First();

                        if (rule.IsMatch(agent))
                        {
                            Site oldSite = agent[Agent.VariablesUsedInCode.AgentCurrentSite];

                            Site bestSite = betterSites.RandomizeOne();

                            agent[Agent.VariablesUsedInCode.AgentBetterSite] = bestSite;

                            rule.Apply(agent);

                            _isAgentMovement = true;

                            vacantSites.Add(oldSite);
                            vacantSites.Remove(bestSite);
                        }
                    }
                }

                _subtypeProportionStatistic.Add(CreateCommonPoolSubtypeProportionRecord(i, agentType));
                _commonPoolFrequency.Add(CreateCommonPoolFrequencyRecord(i, _disturbance, agentType));

                //_averageWellbeingStatistic.Add(CreateAvgWellbeingStatisticRecord(i));
                //_averageVariablesStatistic.Add(CreateAvgVariablesStatisticRecord(i));

                FindInactiveAgents();

                if (_isAgentMovement == false)
                {
                    break;
                }
            }
        }

        protected override void SaveCustomStatistic()
        {
            //SaveCommonPoolStatistic();
            //SaveAvgVariablesStatistic();
            SaveCommonPoolFrequncyStatistic();
        }

        

        private void CalculateParamsDependOnSite(IAgent agent)
        {
            Site currentSite = (Site)agent[Agent.VariablesUsedInCode.AgentCurrentSite];

            Site[] adjacentSites = _siteList.CommonPool(currentSite).ToArray();

            agent[Agent.VariablesUsedInCode.AgentSiteWellbeing] = CalculateAgentWellbeing(agent, currentSite);



            //optional calculations, they may be use in rules

            agent[Agent.VariablesUsedInCode.NeighborhoodSize] = (double)currentSite.GroupSize;
            agent[Agent.VariablesUsedInCode.NeighborhoodVacantSites] = adjacentSites.Count(s => s.IsOccupied == false);
            agent[Agent.VariablesUsedInCode.NeighborhoodUnalike] = adjacentSites.Where(s => s.IsOccupied)
                .Count(s => s.OccupiedBy[Agent.VariablesUsedInCode.AgentSubtype] != agent[Agent.VariablesUsedInCode.AgentSubtype]);

            agent[Agent.VariablesUsedInCode.CommonPoolSize] = (double)(agent[Agent.VariablesUsedInCode.NeighborhoodSize] + 1 - agent[Agent.VariablesUsedInCode.NeighborhoodVacantSites]);

            //agent[Agent.VariablesUsedInCode.CommonPoolSubtupeProportion] = (agent[Agent.VariablesUsedInCode.CommonPoolSize] - agent[Agent.VariablesUsedInCode.NeighborhoodUnalike])
            //    / agent[Agent.VariablesUsedInCode.CommonPoolSize];

            agent[Agent.VariablesUsedInCode.CommonPoolC] = _siteList.CommonPool(currentSite).Where(s => s.IsOccupied).Sum(s => s.OccupiedBy[Agent.VariablesUsedInCode.AgentC]);
            agent[Agent.VariablesUsedInCode.CommonPoolSubtupeProportion] = CalculateSubtypeProportion((int)agent[Agent.VariablesUsedInCode.AgentSubtype], currentSite);
        }

        protected override double CalculateAgentWellbeing(IAgent agent, Site centerSite)
        {
            //we take only adjacement sites because in some cases center site can be empty
            var commonPool = _siteList.AdjacentSites(centerSite).Where(s => s.IsOccupied).ToArray();

            int commonPoolC = commonPool.Sum(s => s.OccupiedBy[Agent.VariablesUsedInCode.AgentC]) + agent[Agent.VariablesUsedInCode.AgentC];

            double wellbeing = agent[Agent.VariablesUsedInCode.Engage] - agent[Agent.VariablesUsedInCode.AgentC]
                + agent[Agent.VariablesUsedInCode.MagnitudeOfExternalities] * commonPoolC / ((double)commonPool.Length + 1) - _disturbance;

            double penalties = commonPool.Select(s => s.OccupiedBy).Sum(e => (double)(e[Agent.VariablesUsedInCode.AgentP] * (1 - agent[Agent.VariablesUsedInCode.AgentC] / (double)agent[Agent.VariablesUsedInCode.Engage])));

            wellbeing -= penalties;

            double punishment = commonPool.Select(s => s.OccupiedBy).Sum(n => (double)(agent[Agent.VariablesUsedInCode.AgentP] * (1 - n[Agent.VariablesUsedInCode.AgentC] / (double)agent[Agent.VariablesUsedInCode.Engage])));

            wellbeing -= punishment;

            return wellbeing;
        }

        //private AvgWellbeingOutput CreateAvgWellbeingStatisticRecord(int iteration)
        //{
        //    AvgWellbeingOutput aw = new AvgWellbeingOutput { Iteration = iteration };

        //    aw.Avgs = _agentList.Agents.Where(a=>a[Agent.VariablesUsedInCode.AgentStatus] == "active")
        //        .GroupBy(a=>(AgentSubtype)a[Agent.VariablesUsedInCode.AgentSubtype])
        //        .OrderBy(g=>g.Key)
        //        .Select(g=> new AvgWellbeingItem { Type = EnumHelper.EnumValueAsString(g.Key), AvgValue = g.Average(a => (double)CalculateAgentWellbeing(a, a[Agent.VariablesUsedInCode.AgentCurrentSite]))}).ToArray();

        //    return aw;
        //}

        //private AvgVariablesOutput CreateAvgVariablesStatisticRecord(int iteration)
        //{
        //    List<AvgVariableItem> temp = new List<AvgVariableItem>(3);

        //    IAgent[] activeAgents = _agentList.Agents.Where(a => a[Agent.VariablesUsedInCode.AgentStatus] == "active").ToArray();

        //    temp.Add(new AvgVariableItem
        //    {
        //        Name = Agent.VariablesUsedInCode.AgentC,
        //        Value = activeAgents.Average(a=> (int)a[Agent.VariablesUsedInCode.AgentC])
        //    });

        //    temp.Add(new AvgVariableItem
        //    {
        //        Name = Agent.VariablesUsedInCode.AgentP,
        //        Value = activeAgents.Average(a => (int)a[Agent.VariablesUsedInCode.AgentP])
        //    });

        //    temp.Add(new AvgVariableItem
        //    {
        //        Name = Agent.VariablesUsedInCode.AgentSiteWellbeing,
        //        Value = activeAgents.Average(a => (double)CalculateAgentWellbeing(a, a[Agent.VariablesUsedInCode.AgentCurrentSite]))
        //    });

        //    return new AvgVariablesOutput { Iteration = iteration, Avgs = temp.ToArray() };
        //}


        //private double CalculateSubtypeProportion(AgentSubtype subtype, Site centerSite)
        //{
        //    var occupiedCommonPool = _siteList.CommonPool(centerSite).Where(s => s.IsOccupied).ToArray();

        //    return CalculateSubtypeProportion(subtype, occupiedCommonPool);
        //}

        //private double CalculateSubtypeProportion(AgentSubtype subtype, Site[] occupiedCommonPool)
        //{
        //    return occupiedCommonPool.Count(s => s.OccupiedBy[Agent.VariablesUsedInCode.AgentSubtype] == subtype)
        //        / (double)occupiedCommonPool.Length;
        //}


        //private SubtypeProportionOutput CreateSubtypeProportionRecord(int iteration)
        //{
        //    SubtypeProportionOutput sp = new SubtypeProportionOutput { Iteration = iteration };

        //    sp.Proportion = _siteList.AsSiteEnumerable().Where(s => s.IsOccupied && s.OccupiedBy[Agent.VariablesUsedInCode.AgentSubtype] == AgentSubtype.Co)
        //         .Average(site => CalculateSubtypeProportion(AgentSubtype.Co, site));

        //    return sp;
        //}

        //private void SaveCommonPoolStatistic()
        //{
        //    ResultSavingHelper.Save(_averageWellbeingStatistic, $@"{_outputFolder}\average_wellbeing_statistic.csv");
        //}

        //private void SaveAvgVariablesStatistic()
        //{
        //    ResultSavingHelper.Save(_averageVariablesStatistic, $@"{_outputFolder}\average_variables_statistic.csv");
        //}

        private void SaveCommonPoolFrequncyStatistic()
        {
            ResultSavingHelper.Save(_commonPoolFrequency, $@"{_outputFolder}\common_pool_frequncy_statistic.csv");
        }
    }
}