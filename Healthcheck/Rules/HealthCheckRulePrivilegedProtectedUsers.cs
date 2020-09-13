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
	[RuleModel("P-ProtectedUsers", RiskRuleCategory.PrivilegedAccounts, RiskModelCategory.AccountTakeOver)]
	[RuleComputation(RuleComputationType.TriggerOnPresence, 10)]
    [RuleSTIG("V-78131", "Accounts with domain level administrative privileges must be members of the Protected Users group in domains with a domain functional level of Windows 2012 R2 or higher.")]
	[RuleCERTFR("CERTFR-2017-ALE-012")]
    [RuleIntroducedIn(2,9)]
    [RuleDurANSSI(3, "protected_users", "Privileged accounts outside of the Protected Users group")]
    public class HealthCheckRulePrivilegedProtectedUsers : RuleBase<HealthCheckData>
    {
		protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
        {
			if (healthcheckData.SchemaVersion < 69)
				return 0;
            var users = new List<string>();
            foreach (var group in healthcheckData.PrivilegedGroups)
            {
                foreach (var user in group.Members)
                {
                    if (user.IsExternal)
                        continue;
                    if (!user.IsInProtectedUser)
                    {
                        if (!users.Contains(user.Name))
                            users.Add(user.Name);
                    }
                }
            }
            foreach(var user in users)
                AddRawDetail(user);
			return null;
        }
    }
}
