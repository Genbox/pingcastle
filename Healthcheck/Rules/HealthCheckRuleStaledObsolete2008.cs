﻿//
// Copyright (c) Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//

using PingCastle.Rules;

namespace PingCastle.HealthCheck.Rules
{
    [RuleModel("S-OS-2008", RiskRuleCategory.StaleObjects, RiskModelCategory.ObsoleteOS)]
    [RuleComputation(RuleComputationType.TriggerOnThreshold, 15, Threshold: 15, Order: 1)]
    [RuleComputation(RuleComputationType.TriggerOnThreshold, 10, Threshold: 6, Order: 2)]
    [RuleComputation(RuleComputationType.TriggerOnPresence, 5, Order: 3)]
    [RuleCERTFR("CERTFR-2005-INF-003", "SECTION00032400000000000000")]
    [RuleMaturityLevel(2)]
    public class HealthCheckRuleStaledObsolete2008 : RuleBase<HealthCheckData>
    {
        protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
        {
            foreach (HealthCheckOSData os in healthcheckData.OperatingSystem)
            {
                if (os.OperatingSystem == "Windows 2008")
                {
                    return os.NumberOfOccurence;
                }
            }
            return 0;
        }
    }
}