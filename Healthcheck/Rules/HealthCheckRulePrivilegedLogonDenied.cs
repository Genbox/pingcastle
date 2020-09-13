﻿//
// Copyright (c) Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using PingCastle.Rules;

namespace PingCastle.HealthCheck.Rules
{
    [RuleModel("P-LogonDenied", RiskRuleCategory.PrivilegedAccounts, RiskModelCategory.AccountTakeOver)]
    [RuleComputation(RuleComputationType.TriggerOnPresence, 1)]
    [RuleIntroducedIn(2, 8)]
    [RuleMaturityLevel(4)]
    public class HealthCheckRulePrivilegedLogonDenied : RuleBase<HealthCheckData>
    {
        protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
        {
            if (healthcheckData.UserAccountData.NumberActive > 200 && healthcheckData.ComputerAccountData.NumberActive > 200)
            {
                var dangerousGroups = new List<string>()
                {
                    "Domain Admins",
                    "Administrators"
                };
                bool restrictionFound = false;
                foreach (var policy in healthcheckData.GPPLoginAllowedOrDeny)
                {
                    if (policy.Privilege == "SeDenyRemoteInteractiveLogonRight" || policy.Privilege == "SeDenyInteractiveLogonRight")
                    {
                        if (policy.User == "Administrators" || policy.User == "Domain Admins")
                        {
                            restrictionFound = true;
                            break;
                        }
                    }
                }
                if (!restrictionFound)
                    return 1;
            }
            return 0;
        }
    }
}