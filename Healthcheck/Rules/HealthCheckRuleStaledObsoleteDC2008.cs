﻿//
// Copyright (c) Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//

using PingCastle.Rules;

namespace PingCastle.HealthCheck.Rules
{
    [RuleModel("S-DC-2008", RiskRuleCategory.StaleObjects, RiskModelCategory.ObsoleteOS)]
    [RuleComputation(RuleComputationType.TriggerOnPresence, 5)]
    [RuleSTIG("V-8551", "The domain functional level must be at a Windows Server version still supported by Microsoft.")]
    [RuleANSSI("R12", "subsection.3.1")]
    [RuleMaturityLevel(1)]
    public class HealthCheckRuleStaledObsoleteDC2008 : RuleBase<HealthCheckData>
    {
        protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
        {
            int w2008 = 0;
            foreach (var dc in healthcheckData.DomainControllers)
            {
                if (dc.OperatingSystem == "Windows 2008")
                {
                    w2008++;
                }
            }
            return w2008;
        }
    }
}