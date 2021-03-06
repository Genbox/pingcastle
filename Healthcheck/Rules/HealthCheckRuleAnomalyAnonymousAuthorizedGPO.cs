﻿//
// Copyright (c) Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//

using PingCastle.Rules;

namespace PingCastle.HealthCheck.Rules
{
    [RuleModel("A-AnonymousAuthorizedGPO", RiskRuleCategory.Anomalies, RiskModelCategory.Reconnaissance)]
    [RuleComputation(RuleComputationType.TriggerOnPresence, 5)]
    [RuleSTIG("V-14798", "Directory data (outside the root DSE) of a non-public directory must be configured to prevent anonymous access.", STIGFramework.ActiveDirectoryService2003)]
    [RuleMaturityLevel(2)]
    public class HealthCheckRuleAnomalyAnonymousAuthorizedGPO : RuleBase<HealthCheckData>
    {
        protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
        {
            foreach (GPPSecurityPolicy policy in healthcheckData.GPPPasswordPolicy)
            {
                foreach (GPPSecurityPolicyProperty property in policy.Properties)
                {
                    if (property.Property == "RestrictAnonymous" || property.Property == "RestrictAnonymousSAM")
                    {
                        if (property.Value == 0)
                        {
                            AddRawDetail(policy.GPOName);
                            break;
                        }
                    }
                }
            }
            return null;
        }
    }
}