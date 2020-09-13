﻿//
// Copyright (c) Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PingCastle.Rules;

namespace PingCastle.HealthCheck.Rules
{
	[RuleModel("A-WeakRSARootCert", RiskRuleCategory.Anomalies, RiskModelCategory.CertificateTakeOver)]
	[RuleComputation(RuleComputationType.TriggerOnPresence, 5)]
	[RuleSTIG("V-14820", "PKI certificates (server and clients) must be issued by the DoD PKI or an approved External Certificate Authority (ECA).", STIGFramework.ActiveDirectoryService2003)]
    [RuleDurANSSI(1, "certificates_vuln", "Weak or vulnerable certificates")]
    public class HealthCheckRuleAnomalyCertWeakRSA : RuleBase<HealthCheckData>
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
                catch(Exception)
                {
                    Trace.WriteLine("Non RSA key detected in certificate");
                }
                if (key != null)
                {
                    RSAParameters rsaparams = key.ExportParameters(false);
                    {
                        if (rsaparams.Modulus.Length * 8 < 1024)
                        {
                            Trace.WriteLine("Modulus len = " + rsaparams.Modulus.Length * 8);
							AddRawDetail(data.Source, cert.Subject, rsaparams.Modulus.Length * 8, cert.NotAfter); 
                        }
                    }
                }
            }
            return null;
        }
    }
}
