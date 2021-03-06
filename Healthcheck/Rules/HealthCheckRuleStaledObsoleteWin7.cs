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
    [RuleModel("S-OS-Win7", RiskRuleCategory.StaleObjects, RiskModelCategory.ObsoleteOS)]
    [RuleComputation(RuleComputationType.TriggerOnThreshold, 5, Threshold: 15, Order: 1)]
    [RuleComputation(RuleComputationType.TriggerOnThreshold, 2, Threshold: 6, Order: 2)]
    [RuleComputation(RuleComputationType.TriggerOnPresence, 1, Order: 3)]
    [RuleCERTFR("CERTFR-2005-INF-003", "SECTION00032400000000000000")]
    [RuleIntroducedIn(2, 9)]
    [RuleMaturityLevel(2)]
    public class HealthCheckRuleStaledObsoleteWin7 : RuleBase<HealthCheckData>
    {
        public static bool IPaidSupport { get; set; }

        protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
        {
            if (IPaidSupport)
                return 0;
            foreach (var loginscript in healthcheckData.GPOLoginScript)
            {
                if (string.Equals(loginscript.CommandLine, "Activate-ProductOnline.ps1", StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }
            }
            foreach (HealthCheckOSData os in healthcheckData.OperatingSystem)
            {
                if (os.OperatingSystem == "Windows 7")
                {
                    return os.NumberOfOccurence;
                }
            }
            return 0;
        }
    }
}