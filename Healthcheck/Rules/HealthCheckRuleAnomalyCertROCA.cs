﻿//
// Copyright (c) Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//

using System;
using System.Security.Cryptography.X509Certificates;
using PingCastle.Rules;
using System.Security.Cryptography;
using System.Diagnostics;
using PingCastle.Misc;

namespace PingCastle.HealthCheck.Rules
{
    [RuleModel("A-CertROCA", RiskRuleCategory.Anomalies, RiskModelCategory.CertificateTakeOver)]
    [RuleComputation(RuleComputationType.TriggerOnPresence, 15)]
    [RuleDurANSSI(1, "certificates_vuln", "Weak or vulnerable certificates")]
    [RuleIntroducedIn(2, 9)]
    public class HealthCheckRuleAnomalyCertROCA : RuleBase<HealthCheckData>
    {
        protected override int? AnalyzeDataNew(HealthCheckData healthcheckData)
        {
            foreach (HealthCheckCertificateData data in healthcheckData.TrustedCertificates)
            {
                X509Certificate2 cert = new X509Certificate2(data.Certificate);
                RSA key = null;
                try
                {
                    key = cert.PublicKey.Key as RSA;
                }
                catch (Exception)
                {
                    Trace.WriteLine("Non RSA key detected in certificate");
                }
                if (key != null)
                {
                    RSAParameters rsaparams = key.ExportParameters(false);
                    if (ROCAVulnerabilityTester.IsVulnerable(rsaparams))
                    {
                        AddRawDetail(data.Source, cert.Subject, cert.NotAfter.ToString("u"));
                    }
                }
            }
            return null;
        }
    }
}