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
    [RuleModel("S-NoPreAuth", RiskRuleCategory.StaleObjects, RiskModelCategory.ObjectConfig)]
    [RuleComputation(RuleComputationType.TriggerOnPresence, 5)]
    [RuleDurANSSI(2, "kerberos_properties_preauth", "Kerberos pre-authentication disabled")]
    public class HealthCheckRuleStaledNoPreAuth : RuleBase<HealthCheckData>
    {
        protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
        {
            if (healthcheckData.UserAccountData.ListNoPreAuth != null)
            {
                foreach (var i in healthcheckData.UserAccountData.ListNoPreAuth)
                {
                    bool fAdmin = false;
                    foreach (var j in healthcheckData.AllPrivilegedMembers)
                    {
                        if (i.DistinguishedName == j.DistinguishedName)
                        {
                            fAdmin = true;
                            break;
                        }
                    }
                    if (!fAdmin)
                    {
                        AddRawDetail(i.Name, i.CreationDate, i.LastLogonDate == DateTime.MinValue ? "Never" : i.LastLogonDate.ToString("u"));
                    }
                }
            }
            if (healthcheckData.ComputerAccountData.ListNoPreAuth != null)
            {
                foreach (var i in healthcheckData.ComputerAccountData.ListNoPreAuth)
                {
                    bool fAdmin = false;
                    foreach (var j in healthcheckData.AllPrivilegedMembers)
                    {
                        if (i.DistinguishedName == j.DistinguishedName)
                        {
                            fAdmin = true;
                            break;
                        }
                    }
                    if (!fAdmin)
                    {
                        AddRawDetail(i.Name, i.CreationDate, i.LastLogonDate == DateTime.MinValue ? "Never" : i.LastLogonDate.ToString("u"));
                    }
                }
            }
            return null;
        }
    }
}