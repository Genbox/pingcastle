﻿//
// Copyright (c) Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//
using System;
using PingCastle.Rules;

namespace PingCastle.HealthCheck.Rules
{
	[RuleModel("A-NoNetSessionHardening", RiskRuleCategory.Anomalies, RiskModelCategory.Reconnaissance)]
	[RuleComputation(RuleComputationType.TriggerOnPresence, 0)]
	[RuleIntroducedIn(2, 9)]
    [RuleMaturityLevel(4)]
    public class HealthCheckRuleAnomalyNoNetSessionHardening : RuleBase<HealthCheckData>
	{
		protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
		{
			bool found = false;
            if (healthcheckData.GPOLsaPolicy != null)
            {
                foreach (GPPSecurityPolicy policy in healthcheckData.GPOLsaPolicy)
                {
                    if (healthcheckData.GPOInfoDic == null || !healthcheckData.GPOInfoDic.ContainsKey(policy.GPOId))
                    {
                        continue;
                    }
                    var refGPO = healthcheckData.GPOInfoDic[policy.GPOId];
                    if (refGPO.IsDisabled)
                    {
                        continue;
                    }
                    if (refGPO.AppliedTo == null || refGPO.AppliedTo.Count == 0)
                    {
                        continue;
                    }
                    foreach (GPPSecurityPolicyProperty property in policy.Properties)
                    {
                        if (string.Equals(property.Property, "SrvsvcSessionInfo", StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                        }
                    }
                }
            }
			if (!found)
				return 1;
			return 0;
		}
	}
}
