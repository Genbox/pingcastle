﻿//
// Copyright (c) Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//

using PingCastle.Rules;

namespace PingCastle.HealthCheck.Rules
{
    [RuleModel("P-DNSAdmin", RiskRuleCategory.PrivilegedAccounts, RiskModelCategory.ACLCheck)]
    [RuleComputation(RuleComputationType.TriggerOnPresence, 5)]
    [RuleIntroducedIn(2, 9)]
    [RuleDurANSSI(1, "dnsadmins", "DnsAdmins group members")]
    public class HealthCheckRulePrivilegedDNSAdmin : RuleBase<HealthCheckData>
    {
        protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
        {
            foreach (var group in healthcheckData.PrivilegedGroups)
            {
                if (group.GroupName == "Dns Admins")
                {
                    return group.NumberOfMemberEnabled;
                }
            }
            return 0;
        }
    }
}