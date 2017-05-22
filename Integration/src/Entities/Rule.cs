﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Landis.Extension.SOSIELHuman.Entities
{
    using Environments;
    using Helpers;

    public class Rule : ICloneable<Rule>
    {


        public RuleLayer Layer { get; set; }

        
        /// <summary>
        /// Set in configuration json only
        /// </summary>
        public int RuleSet { get; set; }

        /// <summary>
        /// Set in configuration json only
        /// </summary>
        public int RuleLayer { get; set; }

        public int RulePositionNumber { get; set; }

        public RuleAntecedentPart[] Antecedent { get; private set; }

        public RuleConsequent Consequent { get; private set; }

        public bool IsAction { get; private set; }

        public bool IsModifiable { get; private set; }

        public int RequiredParticipants { get; private set; }

        public bool AutoGenerated { get; private set; }

        public bool IsCollectiveAction
        {
            get
            {
                return RequiredParticipants > 1 || RequiredParticipants == 0;
            }
        }


        public string Id
        {
            get
            {
                return $"RS{Layer.Set.PositionNumber}_L{Layer.PositionNumber}_R{RulePositionNumber}";
            }
        }

        /// <summary>
        /// Checks agent variables on antecedent conditions
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>   
        public bool IsMatch(IAgent agent)
        {
            return Antecedent.All(a => a.IsMatch(agent));
        }


        /// <summary>
        /// Applies the rule. Copies consequent value or reference variable value to agent variables
        /// </summary>
        /// <param name="agent"></param>
        public void Apply(IAgent agent)
        {
            dynamic value;

            if (string.IsNullOrEmpty(Consequent.VariableValue) == false)
            {
                value = agent[Consequent.VariableValue];
            }
            else
            {
                value = Consequent.Value;
            }


            if (Consequent.SavePrevious)
            {
                string key = string.Format("{0}_{1}", VariablesUsedInCode.PreviousPrefix, Consequent.Param);

                agent[key] = agent[Consequent.Param];

                if (Consequent.CopyToCommon)
                {
                    agent.SetToCommon(string.Format("{0}_{1}_{2}", VariablesUsedInCode.AgentPrefix, agent.Id, key), agent[Consequent.Param]);
                }
            }

            if (Consequent.CopyToCommon)
            {
                string key = string.Format("{0}_{1}_{2}", VariablesUsedInCode.AgentPrefix, agent.Id, Consequent.Param);

                agent.SetToCommon(key, value);
            }

            agent[Consequent.Param] = value;


            agent.RuleActivationFreshness[this] = 0;
        }

        /// <summary>
        /// Creates new rule with passed parameters 
        /// </summary>
        /// <param name="antecedent"></param>
        /// <param name="consequent"></param>
        /// <returns></returns>
        public static Rule Create(RuleAntecedentPart[] antecedent, RuleConsequent consequent, bool isAction, bool isModifable, int requiredParticipants, bool autoGenerated = false)
        {
            Rule newRule = new Rule();

            newRule.Antecedent = antecedent;
            newRule.Consequent = consequent;
            newRule.IsAction = isAction;
            newRule.IsModifiable = isModifable;
            newRule.RequiredParticipants = requiredParticipants;
            newRule.AutoGenerated = autoGenerated;

            return newRule;
        }

        /// <summary>
        /// Creates shallow object copy 
        /// </summary>
        /// <returns></returns>
        public Rule Clone()
        {
            return (Rule)this.MemberwiseClone();
        }



        /// <summary>
        /// Creates rule copy but replaces antecedent parts and consequent by new values. 
        /// </summary>
        /// <param name="old"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static Rule Renew(Rule old, RuleAntecedentPart[] newAntecedent, RuleConsequent newConsequent)
        {
            Rule newRule = old.Clone();

            newRule.Antecedent = newAntecedent;
            newRule.Consequent = newConsequent;

            return newRule;
        }


    }
}
