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
    [RuleModel("P-PrivilegeEveryone", RiskRuleCategory.PrivilegedAccounts, RiskModelCategory.PrivilegeControl)]
    [RuleComputation(RuleComputationType.PerDiscover, 15)]
    [RuleANSSI("R18", "subsubsection.3.3.2")]
    [RuleIntroducedIn(2, 6)]
    [RuleMaturityLevel(2)]
    public class HealthCheckRulePrivilegedPrivilegeEveryone : RuleBase<HealthCheckData>
    {
        protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
        {
            var dangerousPrivileges = new List<string>()
            {
                "SeLoadDriverPrivilege",
                "SeTcbPrivilege",
                "SeDebugPrivilege",
                "SeRestorePrivilege",
                "SeBackupPrivilege",
                "SeTakeOwnershipPrivilege",
                "SeCreateTokenPrivilege",
                "SeImpersonatePrivilege",
                "SeAssignPrimaryTokenPrivilege",
                "SeSecurityPrivilege"
            };
            foreach (var privilege in healthcheckData.GPPRightAssignment)
            {
                if (!dangerousPrivileges.Contains(privilege.Privilege))
                    continue;
                if (privilege.User == "Authenticated Users" || privilege.User == "Everyone" || privilege.User == "Domain Users"
                    || privilege.User == "Domain Computers" || privilege.User == "Users"
                    || privilege.User == "Anonymous")
                {
                    AddRawDetail(privilege.GPOName, privilege.User, privilege.Privilege);
                }
            }
            return null;
        }
    }
}