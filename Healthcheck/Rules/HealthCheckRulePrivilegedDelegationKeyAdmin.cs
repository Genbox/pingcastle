﻿//
// Copyright (c) Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//

using PingCastle.Rules;

namespace PingCastle.HealthCheck.Rules
{
    [RuleModel("P-DelegationKeyAdmin", RiskRuleCategory.PrivilegedAccounts, RiskModelCategory.ACLCheck)]
    [RuleComputation(RuleComputationType.TriggerOnPresence, 5)]
    [RuleANSSI("R18", "subsubsection.3.3.2")]
    [RuleIntroducedIn(2, 6)]
    [RuleDurANSSI(2, "adupdate_bad", "Bad Active Directory versions")]
    public class HealthCheckRulePrivilegedDelegationKeyAdmin : RuleBase<HealthCheckData>
    {
        protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
        {
            foreach (HealthCheckDelegationData delegation in healthcheckData.Delegations)
            {
                if (delegation.DistinguishedName.StartsWith("DC=") && delegation.SecurityIdentifier.EndsWith("-527"))
                {
                    AddRawDetail(delegation.DistinguishedName, delegation.Account, delegation.Right);
                }
            }
            return null;
        }
    }
}