﻿//
// Copyright (c) Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//

using PingCastle.Rules;

namespace PingCastle.HealthCheck.Rules
{
    [RuleModel("A-DC-Spooler", RiskRuleCategory.Anomalies, RiskModelCategory.PassTheCredential)]
    [RuleComputation(RuleComputationType.TriggerOnPresence, 10)]
    [RuleIntroducedIn(2, 6)]
    [RuleMaturityLevel(2)]
    public class HealthCheckRuleAnomalyDCSpooler : RuleBase<HealthCheckData>
    {
        protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
        {
            foreach (var DC in healthcheckData.DomainControllers)
            {
                if (DC.RemoteSpoolerDetected)
                {
                    AddRawDetail(DC.DCName);
                }
            }
            return null;
        }
    }
}