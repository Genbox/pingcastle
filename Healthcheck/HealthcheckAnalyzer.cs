﻿//
// Copyright (c) Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//

using PingCastle.ADWS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Threading;
using System.DirectoryServices;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using PingCastle.RPC;
using PingCastle.Scanners;
using System.Security.Permissions;
using PingCastle.Rules;
using PingCastle.Data;
using PingCastle.Graph.Reporting;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using PingCastle.Misc;

namespace PingCastle.HealthCheck
{
    public class HealthCheckAnalyzer : IPingCastleAnalyzer<HealthCheckData>
    {
        public static bool SkipNullSession { get; set; }
        HealthCheckData healthcheckData;

        public HealthCheckAnalyzer()
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                SkipNullSession = true;
            }
            else if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 0)
            {
                SkipNullSession = true;
            }
        }

        private void DisplayAdvancement(string data)
        {
            string value = "[" + DateTime.Now.ToLongTimeString() + "] " + data;
            Console.WriteLine(value);
            Trace.WriteLine(value);
        }

        public HealthCheckData GenerateCartoReport(string server, int port, NetworkCredential credential, bool AnalyzeReachableDomains)
        {
            healthcheckData = new HealthCheckData();
            ADDomainInfo domainInfo = null;
            using (ADWebService adws = new ADWebService(server, port, credential))
            {
                domainInfo = adws.DomainInfo;
                GenerateGeneralData(domainInfo, adws);
                GenerateTrustData(domainInfo, adws);
                if (AnalyzeReachableDomains)
                {
                    GenerateReachableTrustData(domainInfo, adws);
                }
            }
            return healthcheckData;
        }

        public HealthCheckData PerformAnalyze(PingCastleAnalyzerParameters parameters)
        {
            healthcheckData = new HealthCheckData();
            LoadHoneyPotData();
            ADDomainInfo domainInfo = null;
            DisplayAdvancement("Getting domain information (" + parameters.Server + ")");
            using (ADWebService adws = new ADWebService(parameters.Server, parameters.Port, parameters.Credential))
            {
                domainInfo = adws.DomainInfo;
                if (adws.useLdap)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Performance warning: using LDAP instead of ADWS");
                    Console.ResetColor();
                }
                DisplayAdvancement("Gathering general data");
                GenerateGeneralData(domainInfo, adws);
                DisplayAdvancement("Gathering user data");
                GenerateUserData(domainInfo, adws);
                GenerateGroupSidHistoryData(domainInfo, adws);
                DisplayAdvancement("Gathering computer data");
                GenerateComputerData(domainInfo, adws);
                DisplayAdvancement("Gathering trust data");
                GenerateTrustData(domainInfo, adws);
                if (parameters.PerformExtendedTrustDiscovery)
                {
                    DisplayAdvancement("Gathering reachable domains data");
                    GenerateReachableTrustData(domainInfo, adws);
                }
                DisplayAdvancement("Gathering privileged group and permissions data");
                GeneratePrivilegedGroupAndPermissionsData(domainInfo, adws, parameters);
                DisplayAdvancement("Gathering delegation data");
                GenerateDelegationData(domainInfo, adws);
                DisplayAdvancement("Gathering gpo data");
                GenerateGPOData(domainInfo, adws, parameters.Credential);
                GeneratePSOData(domainInfo, adws, parameters.Credential);
                DisplayAdvancement("Gathering anomaly data");
                GenerateAnomalies(domainInfo, adws);
                DisplayAdvancement("Gathering dns data");
                GenerateDnsData(domainInfo, adws);
                DisplayAdvancement("Gathering domain controller data" + (SkipNullSession ? null : " (including null session)"));
                GenerateDomainControllerData(domainInfo);
                GenerateRODCData(domainInfo, adws);
                GenerateFSMOData(domainInfo, adws);
                GenerateCheckDCConfig(domainInfo, adws);
                GenerateCheckFRS(domainInfo, adws);
                DisplayAdvancement("Gathering network data");
                GenerateNetworkData(domainInfo, adws);
            }
            DisplayAdvancement("Computing risks");
            var rules = new RuleSet<HealthCheckData>();
            healthcheckData.RiskRules = new List<HealthCheckRiskRule>();
            foreach (var rule in rules.ComputeRiskRules(healthcheckData))
            {
                HealthCheckRiskRule risk = new HealthCheckRiskRule();
                risk.Points = rule.Points;
                risk.Category = rule.Category;
                risk.Model = rule.Model;
                risk.RiskId = rule.RiskId;
                risk.Rationale = rule.Rationale;
                risk.Details = rule.Details;
                healthcheckData.RiskRules.Add(risk);
            }
            DisplayAdvancement("Export completed");
            return healthcheckData;
        }

        private void LoadHoneyPotData()
        {
            var s = HoneyPotSettings.GetEncryptionSettings();
            if (s == null)
                return;
            if (s.HoneyPots.Count > 25)
            {
                throw new PingCastleException("You entered more than 25 HoneyPots in the configuration. Honey Pots should not be used as a way to setup exceptions to rules");
            }
            healthcheckData.ListHoneyPot = new List<HealthCheckAccountDetailData>();
            foreach (SingleHoneyPotSettings h in s.HoneyPots)
            {
                healthcheckData.ListHoneyPot.Add(new HealthCheckAccountDetailData() { Name = h.samAccountName });
            }
        }

        public class ReachableDomainInfo : IComparable<ReachableDomainInfo>
        {
            public string domain { get; set; }

            public ReachableDomainInfo(string domain)
            {
                this.domain = domain;
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                ReachableDomainInfo d = obj as ReachableDomainInfo;
                if (d == null)
                    return false;
                return domain.Equals(d.domain, StringComparison.InvariantCultureIgnoreCase);
            }

            public override int GetHashCode()
            {
                return domain.GetHashCode();
            }

            public int CompareTo(ReachableDomainInfo other)
            {
                return domain.CompareTo(other.domain);
            }
        }

        public List<ReachableDomainInfo> GetAllReachableDomains(int port, NetworkCredential credential)
        {
            List<ReachableDomainInfo> domains = new List<ReachableDomainInfo>();
            string root = IPGlobalProperties.GetIPGlobalProperties().DomainName.ToLowerInvariant();
            if (string.IsNullOrEmpty(root))
                return domains;
            ExploreReachableDomain(root, "current domain", port, credential, domains);

            // sort the domain by name
            domains.Sort();
            return domains;
        }

        private void ExploreReachableDomain(string domainToExplore,
                                            string sourceForDisplay,
                                            int port,
                                            NetworkCredential credential,
                                            List<ReachableDomainInfo> domainlist)
        {
            string forestToExplore = null;

            // classic graph exploration algorithm
            string[] properties = new string[]
            {
                "trustPartner",
                "trustAttributes",
                "trustDirection",
                "trustType",
                "msDS-TrustForestTrustInfo"
            };
            ADWebService adws = null;
            try
            {
                DisplayAdvancement("Exploring " + domainToExplore + " (source:" + sourceForDisplay + ")");
                adws = new ADWebService(domainToExplore, port, credential);
                ADDomainInfo domainInfo = adws.DomainInfo;

                // if we are here that means that ADWS works
                ReachableDomainInfo rdi = new ReachableDomainInfo(domainToExplore);
                if (!domainlist.Contains(rdi))
                    domainlist.Add(rdi);
                if (domainInfo.ForestName != domainInfo.DomainName)
                    forestToExplore = domainInfo.ForestName;
                WorkOnReturnedObjectByADWS callback =
                    (ADItem x) =>
                    {
                        // inbound trust
                        if (x.TrustDirection == 2)
                            return;
                        rdi = new ReachableDomainInfo(x.TrustPartner);
                        if (!domainlist.Contains(rdi))
                            domainlist.Add(rdi);
                        if (x.msDSTrustForestTrustInfo != null)
                        {
                            foreach (HealthCheckTrustDomainInfoData di in x.msDSTrustForestTrustInfo)
                            {
                                rdi = new ReachableDomainInfo(di.DnsName);
                                if (!domainlist.Contains(rdi))
                                    domainlist.Add(rdi);
                            }
                        }
                    };
                adws.Enumerate(domainInfo.DefaultNamingContext, "(ObjectCategory=trustedDomain)", properties, callback);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unable to explore " + domainToExplore + " (" + ex.Message + ")");
                Console.ResetColor();
                Trace.WriteLine("Unable to explore " + domainToExplore + " (" + ex.Message + ")");
                Trace.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (adws != null)
                    adws.Dispose();
            }
            if (!string.IsNullOrEmpty(forestToExplore))
            {
                ExploreReachableDomain(forestToExplore, domainToExplore, port, credential, domainlist);
            }
        }

        private void GenerateGeneralData(ADDomainInfo domainInfo, ADWebService adws)
        {
            // adding the domain sid
            string[] properties = new string[] { "objectSid", "whenCreated" };
            adws.Enumerate(domainInfo.DefaultNamingContext,
                "(&(objectClass=domain)(distinguishedName=" + domainInfo.DefaultNamingContext + "))",
                properties, (ADItem aditem) =>
                {
                    domainInfo.DomainSid = aditem.ObjectSid;
                    domainInfo.CreationDate = aditem.WhenCreated;
                });

            healthcheckData.DomainFQDN = domainInfo.DomainName;
            healthcheckData.ForestFQDN = domainInfo.ForestName;
            if (domainInfo.DomainSid != null)
                healthcheckData.DomainSid = domainInfo.DomainSid.Value;
            healthcheckData.DomainCreation = domainInfo.CreationDate;

            // adding the domain Netbios name
            string[] propertiesNetbios = new string[] { "nETBIOSName" };
            adws.Enumerate("CN=Partitions," + domainInfo.ConfigurationNamingContext,
                "(&(objectCategory=crossRef)(systemFlags:1.2.840.113556.1.4.803:=3)(nETBIOSName=*)(nCName=" + domainInfo.DefaultNamingContext + "))",
                propertiesNetbios,
                (ADItem aditem) =>
                {
                    domainInfo.NetBIOSName = aditem.NetBIOSName;
                }
                , "OneLevel");

            // adding the schema version
            string[] propertiesSchema = new string[] { "objectVersion", "replPropertyMetaData", "schemaInfo" };
            adws.Enumerate(domainInfo.SchemaNamingContext,
                "(objectClass=dMD)",
                propertiesSchema, (ADItem aditem) =>
                {
                    domainInfo.SchemaVersion = aditem.ObjectVersion;

                    // version stored in big endian
                    if (aditem.SchemaInfo != null)
                        domainInfo.SchemaInternalVersion = aditem.SchemaInfo[1] * 0x1000000 + aditem.SchemaInfo[2] * 0x10000 + aditem.SchemaInfo[3] * 0x100 + aditem.SchemaInfo[4];
                    if (aditem.ReplPropertyMetaData.ContainsKey(0x9054E))
                    {
                        domainInfo.SchemaLastChanged = aditem.ReplPropertyMetaData[0x9054E].LastOriginatingChange;
                    }
                }, "Base");
            healthcheckData.NetBIOSName = domainInfo.NetBIOSName;
            healthcheckData.ForestFunctionalLevel = domainInfo.ForestFunctionality;
            healthcheckData.DomainFunctionalLevel = domainInfo.DomainFunctionality;
            healthcheckData.SchemaVersion = domainInfo.SchemaVersion;
            healthcheckData.SchemaInternalVersion = domainInfo.SchemaInternalVersion;
            healthcheckData.SchemaLastChanged = domainInfo.SchemaLastChanged;
            healthcheckData.GenerationDate = DateTime.Now;

            string[] propertiesEnabledFeature = new string[] { "msDS-EnabledFeature" };
            adws.Enumerate("CN=Partitions," + domainInfo.ConfigurationNamingContext,
                "(objectClass=*)",
                propertiesEnabledFeature, (ADItem aditem) =>
                {
                    if (aditem.msDSEnabledFeature != null)
                    {
                        foreach (string feature in aditem.msDSEnabledFeature)
                        {
                            if (feature.StartsWith("CN=Recycle Bin Feature,", StringComparison.InvariantCultureIgnoreCase))
                            {
                                healthcheckData.IsRecycleBinEnabled = true;
                            }
                        }
                    }
                }, "Base");

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            healthcheckData.EngineVersion = version.ToString(4);
#if DEBUG
            healthcheckData.EngineVersion += " Beta";
#endif
            healthcheckData.Level = PingCastleReportDataExportLevel.Full;
        }

        private void GenerateUserData(ADDomainInfo domainInfo, ADWebService adws)
        {
            var pwdDistribution = new Dictionary<int, int>();

            Dictionary<string, int> loginscript = new Dictionary<string, int>();
            string[] properties = new string[]
            {
                "objectSid",
                "distinguishedName",
                "name",
                "sAMAccountName",
                "scriptPath",
                "primaryGroupID",
                "sIDHistory",
                "lastLogonTimestamp",
                "userAccountControl",
                "pwdLastSet",
                "msDS-SupportedEncryptionTypes",
                "whenCreated"
            };

            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    try
                    {
                        // krbtgt
                        if (x.ObjectSid.IsWellKnown(System.Security.Principal.WellKnownSidType.AccountKrbtgtSid))
                        {
                            // krbtgt will be processed after - this avoid applying a filter on the object class
                            return;
                        }

                        // admin account
                        if (x.ObjectSid.IsWellKnown(System.Security.Principal.WellKnownSidType.AccountAdministratorSid))
                        {
                            healthcheckData.AdminLastLoginDate = x.LastLogonTimestamp;
                            healthcheckData.AdminAccountName = x.SAMAccountName;
                        }

                        // ignore trust account
                        if (x.Name.EndsWith("$", StringComparison.InvariantCultureIgnoreCase) && ((x.UserAccountControl & 2048) != 0))
                        {
                            return;
                        }
                        ProcessAccountData(healthcheckData.UserAccountData, x, false);

                        // only enabled accounts and no guest account
                        if ((x.UserAccountControl & 0x00000002) == 0)
                        {
                            // login script
                            string scriptName = "None";
                            if (!string.IsNullOrEmpty(x.ScriptPath))
                            {
                                scriptName = x.ScriptPath.ToLowerInvariant();
                            }
                            if (!loginscript.ContainsKey(scriptName))
                                loginscript[scriptName] = 1;
                            else
                                loginscript[scriptName]++;

                            // avoid system objects where the whenCreated date in not set
                            // ex: guest, krbtgt for rodc, ...
                            if (x.WhenCreated != DateTime.MinValue)
                            {
                                var i = ConvertPwdLastSetToKey(x);
                                if (pwdDistribution.ContainsKey(i))
                                    pwdDistribution[i]++;
                                else
                                    pwdDistribution[i] = 1;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Trace.WriteLine("Exception while working on " + x.DistinguishedName);
                        throw;
                    }
                };

            string filter = "(|(&(objectClass=user)(objectCategory=person))(objectcategory=msDS-GroupManagedServiceAccount)(objectcategory=msDS-ManagedServiceAccount))";
            adws.Enumerate(() =>
                {
                    healthcheckData.UserAccountData = new HealthCheckAccountData();
                    loginscript.Clear();
                },
                domainInfo.DefaultNamingContext, filter, properties, callback, "SubTree");

            healthcheckData.LoginScript = new List<HealthCheckLoginScriptData>();
            foreach (string key in loginscript.Keys)
            {
                var script = new HealthCheckLoginScriptData(key, loginscript[key]);
                script.Delegation = CheckScriptPermission(domainInfo, script.LoginScript);
                healthcheckData.LoginScript.Add(script);
            }

            healthcheckData.PasswordDistribution = new List<HealthCheckPwdDistributionData>();

            foreach (var p in pwdDistribution)
            {
                healthcheckData.PasswordDistribution.Add(new HealthCheckPwdDistributionData() { HigherBound = p.Key, Value = p.Value });
            }
        }

        private int ConvertPwdLastSetToKey(ADItem x)
        {
            var dateTime = x.PwdLastSet;
            if (x.PwdLastSet == DateTime.MinValue)
                dateTime = x.WhenCreated;
            return ConvertDateToKey(dateTime);

        }

        public static int ConvertDateToKey(DateTime dateTime)
        {
            var t = (DateTime.Now - dateTime).Days;
            if (t < 0) t = 0;
            return t / 30;
        }

        List<HealthCheckScriptDelegationData> CheckScriptPermission(ADDomainInfo domainInfo, string file)
        {
            var output = new List<HealthCheckScriptDelegationData>();
            if (file == "None")
                return output;
            try
            {
                if (!Path.IsPathRooted(file))
                {
                    file = Path.Combine(@"\\" + domainInfo.DnsHostName + @"\SYSVOL\" + domainInfo.DomainName + @"\scripts", file);
                }
                if (!File.Exists(file))
                {
                    return output;
                }
                var security = File.GetAccessControl(file);
                var accessRules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
                if (accessRules == null)
                    return output;

                foreach (FileSystemAccessRule rule in accessRules)
                {
                    if (rule.AccessControlType == AccessControlType.Deny)
                        continue;
                    if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                        continue;

                    var sid = (SecurityIdentifier)rule.IdentityReference;
                    var account = MatchesBadUsersToCheck(sid);
                    if (!account.HasValue)
                        continue;
                    output.Add(new HealthCheckScriptDelegationData() { Account = account.Value.Value, Right = rule.FileSystemRights.ToString() });
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception CheckScriptPermission " + ex.Message);
            }
            return output;
        }

        internal void ProcessAccountData(HealthCheckAccountData data, ADItem x, bool computerCheck)
        {
            // see https://msdn.microsoft.com/fr-fr/library/windows/desktop/ms680832%28v=vs.85%29.aspx for the flag
            if (!string.IsNullOrEmpty(x.SAMAccountName) && healthcheckData.ListHoneyPot != null && healthcheckData.ListHoneyPot.Count > 0)
            {
                foreach (var h in healthcheckData.ListHoneyPot)
                {
                    if (string.Equals(h.Name, x.SAMAccountName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // ignore the account
                        h.Name = x.SAMAccountName;
                        h.CreationDate = x.WhenCreated;
                        h.DistinguishedName = x.DistinguishedName;
                        h.LastLogonDate = x.LastLogonTimestamp;
                        return;
                    }
                }
            }
            data.Number++;
            if (x.DistinguishedName.Contains("cnf:"))
            {
                data.NumberDuplicate++;
                if (data.ListDuplicate == null)
                    data.ListDuplicate = new List<HealthCheckAccountDetailData>();
                data.ListDuplicate.Add(GetAccountDetail(x));
            }
            else if (!string.IsNullOrEmpty(x.SAMAccountName) && x.SAMAccountName.StartsWith("$duplicate-", StringComparison.InvariantCultureIgnoreCase))
            {
                data.NumberDuplicate++;
                if (data.ListDuplicate == null)
                    data.ListDuplicate = new List<HealthCheckAccountDetailData>();
                data.ListDuplicate.Add(GetAccountDetail(x));
            }
            if ((x.UserAccountControl & 0x00000002) != 0)
                data.NumberDisabled++;
            else
            {
                data.NumberEnabled++;

                if (x.WhenCreated.AddDays(6 * 31) > DateTime.Now || x.LastLogonTimestamp.AddDays(6 * 31) > DateTime.Now)
                    data.NumberActive++;
                else
                {
                    data.NumberInactive++;
                    if (data.ListInactive == null)
                        data.ListInactive = new List<HealthCheckAccountDetailData>();
                    data.ListInactive.Add(GetAccountDetail(x));
                }
                if ((x.UserAccountControl & 0x400000) != 0)
                {
                    data.NumberNoPreAuth++;
                    if (data.ListNoPreAuth == null)
                        data.ListNoPreAuth = new List<HealthCheckAccountDetailData>();
                    data.ListNoPreAuth.Add(GetAccountDetail(x));
                }
                if ((x.UserAccountControl & 0x00000010) != 0)
                {
                    data.NumberLocked++;
                    if (data.ListLocked == null)
                        data.ListLocked = new List<HealthCheckAccountDetailData>();
                    data.ListLocked.Add(GetAccountDetail(x));
                }
                if ((x.UserAccountControl & 0x00010000) != 0)
                {
                    data.NumberPwdNeverExpires++;
                    if (data.ListPwdNeverExpires == null)
                        data.ListPwdNeverExpires = new List<HealthCheckAccountDetailData>();
                    data.ListPwdNeverExpires.Add(GetAccountDetail(x));
                }
                if ((x.UserAccountControl & 0x00000020) != 0)
                {
                    // avoid to alert about exchange mailboxes
                    if (!x.DistinguishedName.Contains(",CN=Monitoring Mailboxes,"))
                    {
                        data.NumberPwdNotRequired++;
                        if (data.ListPwdNotRequired == null)
                            data.ListPwdNotRequired = new List<HealthCheckAccountDetailData>();
                        data.ListPwdNotRequired.Add(GetAccountDetail(x));
                    }
                }
                ProcessSIDHistory(x, data);

                // check for bad primary group
                if (!computerCheck)
                {
                    // not domain users & guest or the guest account
                    if (x.PrimaryGroupID != 513 && x.PrimaryGroupID != 514 && x.ObjectSid != null && !x.ObjectSid.IsWellKnown(System.Security.Principal.WellKnownSidType.AccountGuestSid)
                        && !(x.PrimaryGroupID == 515 && (string.Equals(x.Class, "msDS-GroupManagedServiceAccount", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Class, "msDS-ManagedServiceAccount", StringComparison.OrdinalIgnoreCase))))
                    {
                        data.NumberBadPrimaryGroup++;
                        if (data.ListBadPrimaryGroup == null)
                            data.ListBadPrimaryGroup = new List<HealthCheckAccountDetailData>();
                        data.ListBadPrimaryGroup.Add(GetAccountDetail(x));
                    }
                }
                else
                {
                    // not domain computers & guests
                    if (x.PrimaryGroupID != 515 && x.PrimaryGroupID != 514)
                    {
                        if ((x.PrimaryGroupID == 516 || x.PrimaryGroupID == 521) && x.DistinguishedName.Contains("OU=Domain Controllers,DC="))
                        {
                            // ignore domain controller group
                            // 516 = RW DC, 521 = RO DC
                        }
                        else
                        {
                            data.NumberBadPrimaryGroup++;
                            if (data.ListBadPrimaryGroup == null)
                                data.ListBadPrimaryGroup = new List<HealthCheckAccountDetailData>();
                            data.ListBadPrimaryGroup.Add(GetAccountDetail(x));
                        }
                    }
                }

                // see [MS-KILE] && https://blogs.msdn.microsoft.com/openspecification/2011/05/30/windows-configurations-for-kerberos-supported-encryption-type/
                // msDSSupportedEncryptionTypes =1 => DES-CBC-CRC ; 2 => DES-CBC-MD5
                if (((x.UserAccountControl & 0x00200000) != 0) || ((x.msDSSupportedEncryptionTypes & (1 | 2)) > 0))
                {
                    data.NumberDesEnabled++;
                    if (data.ListDesEnabled == null)
                        data.ListDesEnabled = new List<HealthCheckAccountDetailData>();
                    data.ListDesEnabled.Add(GetAccountDetail(x));
                }
                if ((x.UserAccountControl & 0x80000) != 0)
                {
                    data.NumberTrustedToAuthenticateForDelegation++;
                    if (data.ListTrustedToAuthenticateForDelegation == null)
                        data.ListTrustedToAuthenticateForDelegation = new List<HealthCheckAccountDetailData>();
                    data.ListTrustedToAuthenticateForDelegation.Add(GetAccountDetail(x));
                }
                if ((x.UserAccountControl & 0x0080) != 0)
                {
                    data.NumberReversibleEncryption++;
                    if (data.ListReversibleEncryption == null)
                        data.ListReversibleEncryption = new List<HealthCheckAccountDetailData>();
                    data.ListReversibleEncryption.Add(GetAccountDetail(x));
                }

            }

        }

        private HealthCheckAccountDetailData GetAccountDetail(ADItem x)
        {
            HealthCheckAccountDetailData data = new HealthCheckAccountDetailData();
            data.DistinguishedName = x.DistinguishedName;
            data.Name = x.SAMAccountName;
            data.CreationDate = x.WhenCreated;
            data.LastLogonDate = x.LastLogonTimestamp;
            data.PwdLastSet = x.PwdLastSet;
            return data;
        }

        private void GenerateComputerData(ADDomainInfo domainInfo, ADWebService adws)
        {
            string[] properties = new string[]
            {
                "objectSid",
                "distinguishedName",
                "name",
                "sAMAccountName",
                "operatingSystem",
                "primaryGroupID",
                "sIDHistory",
                "userAccountControl",
                "whenCreated",
                "lastLogonTimestamp",
                "pwdLastSet"
            };

            Dictionary<string, HealthCheckOSData> operatingSystems = new Dictionary<string, HealthCheckOSData>();
            Dictionary<string, int> operatingSystemsDC = new Dictionary<string, int>();

            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    string os = GetOperatingSystem(x.OperatingSystem);
                    if (!operatingSystems.ContainsKey(os))
                    {
                        operatingSystems[os] = new HealthCheckOSData(os);
                        operatingSystems[os].data = new HealthCheckAccountData();
                        operatingSystems[os].data.SetProxy(healthcheckData.ComputerAccountData);
                    }
                    ProcessAccountData(operatingSystems[os].data, x, true);

                    // process only not disabled computers
                    if ((x.UserAccountControl & 0x00000002) == 0)
                    {
                        // we consider DC as a computer in the special OU or having the primary group ID of DC or Enterprise DC
                        // known problem: if the DC is a member (not primary group) & not located in the DC OU
                        if (x.DistinguishedName.Contains("OU=Domain Controllers,DC=") || x.PrimaryGroupID == 516 || x.PrimaryGroupID == 521)
                        {
                            healthcheckData.NumberOfDC++;
                            if (!operatingSystemsDC.ContainsKey(os))
                            {
                                operatingSystemsDC[os] = 1;
                            }
                            else
                            {
                                operatingSystemsDC[os]++;
                            }
                            HealthCheckDomainController dc = new HealthCheckDomainController();
                            dc.DCName = x.Name;
                            dc.CreationDate = x.WhenCreated;

                            // last logon timestam can have a delta of 14 days
                            dc.LastComputerLogonDate = x.LastLogonTimestamp;
                            dc.DistinguishedName = x.DistinguishedName;
                            dc.OperatingSystem = os;
                            dc.PwdLastSet = x.PwdLastSet;
                            if (x.PrimaryGroupID == 521) // RODC
                            {
                                if ((x.UserAccountControl & 0x05001000) != 0x05001000)
                                {
                                    dc.RegistrationProblem = "InvalidUserAccount";
                                }
                            }
                            else // Normal DC
                            {
                                if ((x.UserAccountControl & 0x00082000) != 0x00082000)
                                {
                                    dc.RegistrationProblem = "InvalidUserAccount";
                                }
                            }
                            healthcheckData.DomainControllers.Add(dc);
                        }
                        else
                        {
                            // this checks excludes the DC because a special case is in place
                            if (x.WhenCreated.AddDays(45) <= DateTime.Now && x.LastLogonTimestamp.AddDays(45) > DateTime.Now)
                            {
                                // computer active for at least 45 days
                                if (x.PwdLastSet.AddDays(45) <= DateTime.Now)
                                {
                                    // computer password not changed
                                    if (healthcheckData.ListComputerPwdNotChanged == null)
                                        healthcheckData.ListComputerPwdNotChanged = new List<HealthCheckAccountDetailData>();
                                    healthcheckData.ListComputerPwdNotChanged.Add(GetAccountDetail(x));
                                }
                            }
                        }
                    }
                };

            string filter = "(&(ObjectCategory=computer))";
            adws.Enumerate(() =>
                {
                    healthcheckData.ComputerAccountData = new HealthCheckAccountData();
                    healthcheckData.DomainControllers = new List<HealthCheckDomainController>();
                    healthcheckData.OperatingSystem = new List<HealthCheckOSData>();
                    operatingSystems.Clear();
                    operatingSystemsDC.Clear();
                },
                domainInfo.DefaultNamingContext, filter, properties, callback, "SubTree");

            foreach (string key in operatingSystems.Keys)
            {
                operatingSystems[key].NumberOfOccurence = operatingSystems[key].data.NumberActive;
                healthcheckData.OperatingSystem.Add(operatingSystems[key]);
                healthcheckData.ComputerAccountData.Add(operatingSystems[key].data);
                operatingSystems[key].data.ClearProxy();
            }
        }

        private void ProcessSIDHistory(ADItem x, HealthCheckAccountData data)
        {
            if (x.SIDHistory != null && x.SIDHistory.Length > 0)
            {
                data.NumberSidHistory++;
                if (data.ListSidHistory == null)
                    data.ListSidHistory = new List<HealthCheckAccountDetailData>();
                data.ListSidHistory.Add(GetAccountDetail(x));

                // sum up the count of sid history per remote domain
                foreach (SecurityIdentifier sid in x.SIDHistory)
                {
                    if (data.ListDomainSidHistory == null)
                        data.ListDomainSidHistory = new List<HealthCheckSIDHistoryData>();
                    SecurityIdentifier domainSid = sid.AccountDomainSid;
                    bool dangerousSID = false;

                    // special case when SIDHistory has been modified ...
                    if (domainSid == null)
                    {
                        domainSid = sid;
                        dangerousSID = true;
                    }
                    else
                    {
                        var sidparts = sid.Value.Split('-');
                        if (sidparts.Length > 1)
                        {
                            var lastPart = int.Parse(sidparts[sidparts.Length - 1]);
                            if (lastPart < 1000)
                                dangerousSID = true;
                        }
                    }
                    bool found = false;
                    foreach (HealthCheckSIDHistoryData domainSIDHistory in data.ListDomainSidHistory)
                    {
                        if (domainSIDHistory.DomainSid == domainSid.Value)
                        {
                            domainSIDHistory.Count++;
                            found = true;

                            if ((domainSIDHistory.FirstDate > x.WhenCreated && x.WhenCreated != DateTime.MinValue) || domainSIDHistory.FirstDate == DateTime.MinValue)
                                domainSIDHistory.FirstDate = x.WhenCreated;
                            if (domainSIDHistory.LastDate < x.WhenCreated)
                                domainSIDHistory.LastDate = x.WhenCreated;
                            if (dangerousSID)
                                domainSIDHistory.DangerousSID = dangerousSID;
                            break;
                        }
                    }
                    if (!found)
                    {
                        HealthCheckSIDHistoryData domainSIDHistory = new HealthCheckSIDHistoryData();
                        data.ListDomainSidHistory.Add(domainSIDHistory);
                        domainSIDHistory.DomainSid = domainSid.Value;
                        domainSIDHistory.Count = 1;
                        domainSIDHistory.LastDate = x.WhenCreated;
                        domainSIDHistory.FirstDate = x.WhenCreated;
                        domainSIDHistory.DangerousSID = dangerousSID;
                    }
                }
            }
        }

        private void GenerateGroupSidHistoryData(ADDomainInfo domainInfo, ADWebService adws)
        {
            string[] properties = new string[]
            {
                "distinguishedName",
                "sIDHistory"
            };
            Trace.WriteLine("checking sid history for groups");
            int count = 0;
            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    ProcessSIDHistory(x, healthcheckData.UserAccountData);
                    count++;
                };
            string groupselection = "(|(|(|(objectClass=posixGroup)(objectClass=groupOfUniqueNames))(objectClass=groupOfNames))(objectClass=group))";
            adws.Enumerate(domainInfo.DefaultNamingContext, "(&(sidhistory=*)" + groupselection + ")", properties, callback);
            Trace.WriteLine("Having found " + count + " groups with sid history");
        }

        private string GetOperatingSystem(string os)
        {
            if (string.IsNullOrEmpty(os))
            {
                return "OperatingSystem not set";
            }
            os = os.Replace('\u00A0', ' ');
            if (Regex.Match(os, @"windows(.*) 2000", RegexOptions.IgnoreCase).Success)
            {
                return "Windows 2000";
            }
            if (Regex.Match(os, @"windows(.*) 2003", RegexOptions.IgnoreCase).Success)
            {
                return "Windows 2003";
            }
            if (Regex.Match(os, @"windows(.*) 2008", RegexOptions.IgnoreCase).Success)
            {
                return "Windows 2008";
            }
            if (Regex.Match(os, @"windows(.*) 2012", RegexOptions.IgnoreCase).Success)
            {
                return "Windows 2012";
            }
            if (Regex.Match(os, @"windows(.*) 2016", RegexOptions.IgnoreCase).Success)
            {
                return "Windows 2016";
            }
            if (Regex.Match(os, @"windows(.*) 2019", RegexOptions.IgnoreCase).Success)
            {
                return "Windows 2019";
            }
            if (Regex.Match(os, @"windows(.*) 7", RegexOptions.IgnoreCase).Success)
            {
                return "Windows 7";
            }
            if (Regex.Match(os, @"windows(.*) 8", RegexOptions.IgnoreCase).Success)
            {
                return "Windows 8";
            }
            if (Regex.Match(os, @"windows(.*) Embedded", RegexOptions.IgnoreCase).Success)
            {
                return "Windows Embedded";
            }
            if (Regex.Match(os, @"windows(.*) XP", RegexOptions.IgnoreCase).Success)
            {
                return "Windows XP";
            }
            if (Regex.Match(os, @"windows(.*) 10", RegexOptions.IgnoreCase).Success)
            {
                return "Windows 10";
            }
            if (Regex.Match(os, @"windows(.*) Vista", RegexOptions.IgnoreCase).Success)
            {
                return "Windows Vista";
            }
            if (Regex.Match(os, @"windows(.*) NT", RegexOptions.IgnoreCase).Success)
            {
                return "Windows NT";
            }
            return os;
        }

        private void GenerateTrustData(ADDomainInfo domainInfo, ADWebService adws)
        {
            healthcheckData.Trusts = new List<HealthCheckTrustData>();
            string[] properties = new string[]
            {
                "distinguishedName",
                "securityIdentifier",
                "trustPartner",
                "trustAttributes",
                "trustDirection",
                "trustType",
                "whenCreated",
                "whenChanged",
                "msDS-TrustForestTrustInfo"
            };
            DomainLocator dl = new DomainLocator(domainInfo.DnsHostName);

            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    HealthCheckTrustData trust = new HealthCheckTrustData();
                    healthcheckData.Trusts.Add(trust);
                    trust.TrustPartner = x.TrustPartner.ToLowerInvariant();
                    trust.TrustAttributes = x.TrustAttributes;
                    trust.TrustDirection = x.TrustDirection;
                    trust.TrustType = x.TrustType;
                    trust.CreationDate = x.WhenCreated;

                    // if a trust is active, the password is changed every 30 days
                    // so the object will be changed
                    trust.IsActive = (x.WhenChanged.AddDays(40) > DateTime.Now);

                    // sid is used to translate unknown FSP
                    if (x.SecurityIdentifier != null)
                    {
                        trust.SID = x.SecurityIdentifier.Value;
                    }
                    if (x.msDSTrustForestTrustInfo != null)
                    {
                        trust.KnownDomains = x.msDSTrustForestTrustInfo;

                        // remove the trustpartner from the domain infos
                        foreach (HealthCheckTrustDomainInfoData di in trust.KnownDomains)
                        {
                            di.ForestName = x.TrustPartner;
                            di.ForestSid = trust.SID;
                        }
                        foreach (HealthCheckTrustDomainInfoData di in trust.KnownDomains)
                        {
                            if (di.DnsName == x.TrustPartner)
                            {
                                trust.KnownDomains.Remove(di);
                                break;
                            }
                        }
                        if (trust.KnownDomains.Count == 0)
                            trust.KnownDomains = null;
                    }
                    string netbios, forest;
                    if (dl.LocateNetbiosFromFQDN(trust.TrustPartner, out netbios, out forest))
                    {
                        trust.NetBiosName = netbios;

                        // copy the netbios info to the forest trust info
                        if (trust.KnownDomains != null)
                        {
                            foreach (HealthCheckTrustDomainInfoData di in trust.KnownDomains)
                            {
                                di.ForestNetbios = trust.NetBiosName;
                            }
                        }

                        // if the trusted domain is part of a forest, add it
                        if (domainInfo.ForestName != domainInfo.DomainName
                            && !trust.TrustPartner.Equals(domainInfo.ForestName, StringComparison.InvariantCultureIgnoreCase)
                            && !forest.Equals(trust.TrustPartner, StringComparison.InvariantCultureIgnoreCase))
                        {
                            trust.KnownDomains = new List<HealthCheckTrustDomainInfoData>();
                            HealthCheckTrustDomainInfoData data = new HealthCheckTrustDomainInfoData();
                            data.DnsName = forest;
                            data.CreationDate = DateTime.MinValue;
                            data.ForestName = forest;
                            SecurityIdentifier sid = NativeMethods.GetSidFromDomainName(domainInfo.DnsHostName, forest);
                            if (sid != null)
                            {
                                data.Sid = sid.Value;
                                data.ForestSid = data.Sid;
                            }
                            if (dl.LocateNetbiosFromFQDN(forest, out netbios, out forest))
                            {
                                data.NetbiosName = netbios;
                            }
                            trust.KnownDomains.Add(data);
                        }
                    }
                };

            // enumerate trustedDomain objects
            adws.Enumerate(domainInfo.DefaultNamingContext, "(ObjectCategory=trustedDomain)", properties, callback);

            if (domainInfo.ForestName != domainInfo.DomainName)
            {
                HealthCheckTrustData forestTrust = null;

                // enumerate child domains found in configuration
                // we do this in case we can access only this domain through a trust and not the whole forest
                foreach (HealthCheckTrustData trust in healthcheckData.Trusts)
                {
                    if (trust.TrustPartner != null
                        && trust.TrustPartner.Equals(domainInfo.ForestName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        forestTrust = trust;
                        break;
                    }
                }
                if (forestTrust != null)
                {
                    if (forestTrust.KnownDomains == null)
                        forestTrust.KnownDomains = new List<HealthCheckTrustDomainInfoData>();
                    string[] propertiesCrossRefDomains = new string[] { "dnsRoot", "nETBIOSName", "whenCreated" };
                    WorkOnReturnedObjectByADWS callbackCrossRef =
                        (ADItem x) =>
                        {
                            if (x.DnsRoot.Equals(domainInfo.DomainName, StringComparison.InvariantCultureIgnoreCase)
                                || x.DnsRoot.Equals(domainInfo.ForestName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                return;
                            }
                            HealthCheckTrustDomainInfoData data = new HealthCheckTrustDomainInfoData();
                            forestTrust.KnownDomains.Add(data);
                            data.DnsName = x.DnsRoot;
                            data.NetbiosName = x.NetBIOSName;
                            data.CreationDate = x.WhenCreated;
                            data.ForestName = domainInfo.ForestName;
                            data.ForestNetbios = forestTrust.NetBiosName;
                            data.ForestSid = forestTrust.SID;
                            SecurityIdentifier sid = NativeMethods.GetSidFromDomainName(domainInfo.DnsHostName, x.DnsRoot);
                            if (sid != null)
                                data.Sid = sid.Value;
                        };
                    adws.Enumerate("CN=Partitions," + domainInfo.ConfigurationNamingContext,
                        "(&(objectCategory=crossRef)(systemFlags:1.2.840.113556.1.4.803:=3)(dnsRoot=*))",
                        propertiesCrossRefDomains, callbackCrossRef, "OneLevel");
                    if (forestTrust.KnownDomains.Count == 0)
                        forestTrust.KnownDomains = null;
                }
            }

            // add information about trust info
            if (healthcheckData.UserAccountData != null && healthcheckData.UserAccountData.ListDomainSidHistory != null)
            {
                EnrichSIDHistoryWithTrustData(healthcheckData.UserAccountData.ListDomainSidHistory);
            }
            if (healthcheckData.ComputerAccountData != null && healthcheckData.ComputerAccountData.ListDomainSidHistory != null)
            {
                EnrichSIDHistoryWithTrustData(healthcheckData.ComputerAccountData.ListDomainSidHistory);
            }

            // process AzureAD SSO
            string[] AzureAccountproperties = new string[]
            {
                "distinguishedName",
                "securityIdentifier",
                "pwdLastSet",
                "replPropertyMetaData",
                "whenCreated"
            };
            WorkOnReturnedObjectByADWS callbackAzureAccount =
                (ADItem x) =>
                {
                    DateTime d = x.WhenCreated;
                    if (x.PwdLastSet > d)
                    {
                        d = x.PwdLastSet;
                    }
                    else
                    {
                        if (d < x.ReplPropertyMetaData[0x9005A].LastOriginatingChange)
                        {
                            d = x.ReplPropertyMetaData[0x9005A].LastOriginatingChange;
                            healthcheckData.AzureADSSOVersion = x.ReplPropertyMetaData[0x9005A].Version;
                        }
                    }
                    healthcheckData.AzureADSSOLastPwdChange = d;
                };

            adws.Enumerate(domainInfo.DefaultNamingContext, "(SamAccountName=AZUREADSSOACC$)", AzureAccountproperties, callbackAzureAccount);
        }

        private void EnrichSIDHistoryWithTrustData(List<HealthCheckSIDHistoryData> list)
        {
            foreach (HealthCheckSIDHistoryData data in list)
            {
                foreach (HealthCheckTrustData trustdata in healthcheckData.Trusts)
                {
                    // if the infomration is found and TrustData and KnownDomain, TrustData has the priority
                    if (trustdata.SID == data.DomainSid)
                    {
                        data.FriendlyName = trustdata.TrustPartner;
                        data.NetBIOSName = trustdata.NetBiosName;
                        break;
                    }
                    if (trustdata.KnownDomains != null)
                    {
                        foreach (HealthCheckTrustDomainInfoData trustdomainInfo in trustdata.KnownDomains)
                        {
                            if (trustdomainInfo.Sid == data.DomainSid)
                            {
                                data.FriendlyName = trustdomainInfo.DnsName;
                                data.NetBIOSName = trustdomainInfo.NetbiosName;
                                break;
                            }
                        }
                    }
                }
                if (data.DomainSid == healthcheckData.DomainSid)
                {
                    data.FriendlyName = healthcheckData.DomainFQDN;
                    data.NetBIOSName = healthcheckData.NetBIOSName;
                }
                if (string.IsNullOrEmpty(data.FriendlyName))
                    data.FriendlyName = data.DomainSid;
            }
        }

        private void GenerateReachableTrustData(ADDomainInfo domainInfo, ADWebService adws)
        {
            // prepare a list of all SID that doesn't belong to the domain nor to a known trust
            // foreign security principals
            List<SecurityIdentifier> UnknownSids = GetExternalFSPDomainSids(domainInfo, adws);
            UnknownSids = FilterKnownDomains(UnknownSids);

            // sid history data
            if (healthcheckData.UserAccountData != null && healthcheckData.UserAccountData.ListDomainSidHistory != null)
            {
                foreach (HealthCheckSIDHistoryData data in healthcheckData.UserAccountData.ListDomainSidHistory)
                {
                    if (data.FriendlyName.StartsWith("S-1-", StringComparison.InvariantCultureIgnoreCase))
                    {
                        UnknownSids.Add(new SecurityIdentifier(data.DomainSid));
                    }
                }
            }
            if (healthcheckData.ComputerAccountData != null && healthcheckData.ComputerAccountData.ListDomainSidHistory != null)
            {
                foreach (HealthCheckSIDHistoryData data in healthcheckData.ComputerAccountData.ListDomainSidHistory)
                {
                    if (data.FriendlyName.StartsWith("S-1-", StringComparison.InvariantCultureIgnoreCase))
                    {
                        UnknownSids.Add(new SecurityIdentifier(data.DomainSid));
                    }
                }
            }
            DomainLocator dl = new DomainLocator(domainInfo.DnsHostName);

            // discovering reachable domains by resolving the SID by the domain
            foreach (SecurityIdentifier domainSid in UnknownSids)
            {
                string name = NativeMethods.ConvertSIDToName(domainSid.Value, domainInfo.DnsHostName);

                // name resolved
                if (name.Contains("\\"))
                {
                    string[] values = name.Split('\\');
                    HealthCheckTrustDomainInfoData data = new HealthCheckTrustDomainInfoData();
                    data.Sid = domainSid.Value;
                    data.DnsName = values[0];
                    data.NetbiosName = values[0];
                    data.CreationDate = DateTime.MinValue;
                    if (healthcheckData.ReachableDomains == null)
                        healthcheckData.ReachableDomains = new List<HealthCheckTrustDomainInfoData>();
                    healthcheckData.ReachableDomains.Add(data);
                    string fqdn, forestname;
                    if (dl.LocateDomainFromNetbios(data.NetbiosName, out fqdn, out forestname))
                    {
                        data.DnsName = fqdn;
                        data.ForestName = forestname;
                        if (!forestname.Equals(data.DnsName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            SecurityIdentifier sid = NativeMethods.GetSidFromDomainName(domainInfo.DnsHostName, forestname);
                            if (sid != null)
                                data.ForestSid = sid.Value;
                            string netbios;
                            if (dl.LocateNetbiosFromFQDN(forestname, out netbios, out forestname))
                            {
                                data.ForestNetbios = netbios;
                            }
                        }
                    }
                }
            }

            // enrich SID History data
            if (healthcheckData.ReachableDomains != null)
            {
                if (healthcheckData.UserAccountData != null && healthcheckData.UserAccountData.ListDomainSidHistory != null)
                {
                    foreach (HealthCheckSIDHistoryData data in healthcheckData.UserAccountData.ListDomainSidHistory)
                    {
                        if (data.FriendlyName.StartsWith("S-1-", StringComparison.InvariantCultureIgnoreCase))
                        {
                            foreach (HealthCheckTrustDomainInfoData di in healthcheckData.ReachableDomains)
                            {
                                if (di.Sid.Equals(data.FriendlyName, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    data.FriendlyName = di.DnsName;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (healthcheckData.ComputerAccountData != null && healthcheckData.ComputerAccountData.ListDomainSidHistory != null)
                {
                    foreach (HealthCheckSIDHistoryData data in healthcheckData.ComputerAccountData.ListDomainSidHistory)
                    {
                        if (data.FriendlyName.StartsWith("S-1-", StringComparison.InvariantCultureIgnoreCase))
                        {
                            foreach (HealthCheckTrustDomainInfoData di in healthcheckData.ReachableDomains)
                            {
                                if (di.Sid.Equals(data.FriendlyName, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    data.FriendlyName = di.DnsName;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private List<SecurityIdentifier> GetExternalFSPDomainSids(ADDomainInfo domainInfo, ADWebService adws)
        {
            string[] properties = new string[]
            {
                "name"
            };
            string filter = "(name=S-1-5-21-*)";
            List<SecurityIdentifier> externalDomainSids = new List<SecurityIdentifier>();
            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    // beware: can have replicated pb on FSP => SID end with cnf string and should be discarded
                    string sidPattern = @"^S-\d-\d+-(\d+-){1,14}\d+$";
                    bool isValidFormat = Regex.IsMatch(x.Name, sidPattern);
                    if (isValidFormat)
                    {
                        SecurityIdentifier domainSid = new SecurityIdentifier(x.Name).AccountDomainSid;
                        if (!externalDomainSids.Contains(domainSid))
                            externalDomainSids.Add(domainSid);
                    }
                };
            adws.Enumerate("CN=ForeignSecurityPrincipals," + domainInfo.DefaultNamingContext, filter, properties, callback, "OneLevel");
            return externalDomainSids;
        }

        private List<SecurityIdentifier> FilterKnownDomains(List<SecurityIdentifier> domainSidsToFilter)
        {
            List<SecurityIdentifier> output = new List<SecurityIdentifier>();
            if (healthcheckData.Trusts == null)
                return output;
            foreach (SecurityIdentifier domainSid in domainSidsToFilter)
            {
                bool found = false;
                foreach (HealthCheckTrustData trust in healthcheckData.Trusts)
                {
                    if (!string.IsNullOrEmpty(trust.SID) && new SecurityIdentifier(trust.SID) == domainSid)
                    {
                        found = true;
                        break;
                    }
                    if (trust.KnownDomains != null)
                    {
                        foreach (HealthCheckTrustDomainInfoData di in trust.KnownDomains)
                        {
                            if (!string.IsNullOrEmpty(di.Sid) && new SecurityIdentifier(di.Sid) == domainSid)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            break;
                    }
                }
                if (!found)
                    output.Add(domainSid);
            }
            return output;
        }

        private void GeneratePrivilegedGroupAndPermissionsData(ADDomainInfo domainInfo, ADWebService adws, PingCastleAnalyzerParameters parameters)
        {
            var generator = new ReportGenerator();
            generator.PerformAnalyze(healthcheckData, domainInfo, adws, parameters);

            // compute other analyses
            // distribution
            healthcheckData.PrivilegedDistributionLastLogon = new List<HealthCheckPwdDistributionData>();
            healthcheckData.PrivilegedDistributionPwdLastSet = new List<HealthCheckPwdDistributionData>();

            var pwdDistribution = new Dictionary<int, int>();
            var logonDistribution = new Dictionary<int, int>();
            foreach (var user in healthcheckData.AllPrivilegedMembers)
            {
                if (user.IsEnabled)
                {
                    {
                        var i = ConvertDateToKey(user.PwdLastSet == DateTime.MinValue ? user.Created : user.PwdLastSet);

                        if (pwdDistribution.ContainsKey(i))
                            pwdDistribution[i]++;
                        else
                            pwdDistribution[i] = 1;
                    }
                    if (user.LastLogonTimestamp != DateTime.MinValue)
                    {
                        var i = ConvertDateToKey(user.LastLogonTimestamp);

                        if (logonDistribution.ContainsKey(i))
                            logonDistribution[i]++;
                        else
                            logonDistribution[i] = 1;
                    }
                }
            }
            foreach (var p in logonDistribution)
            {
                healthcheckData.PrivilegedDistributionLastLogon.Add(new HealthCheckPwdDistributionData() { HigherBound = p.Key, Value = p.Value });
            }
            foreach (var p in pwdDistribution)
            {
                healthcheckData.PrivilegedDistributionPwdLastSet.Add(new HealthCheckPwdDistributionData() { HigherBound = p.Key, Value = p.Value });
            }
        }

        private void GenerateDelegationData(ADDomainInfo domainInfo, ADWebService adws)
        {
            healthcheckData.Delegations = new List<HealthCheckDelegationData>();
            InspectAdminSDHolder(domainInfo, adws);
            InspectDelegation(domainInfo, adws);
        }

        // SDDL reference from MSDN based on schema version 35 and next
        static string sddlReference = "D:PAI(OA;;RP;4c164200-20c0-11d0-a768-00aa006e0529;4828cc14-1437-45bc-9b07-ad6f015e5f28;RU)(OA;;RP;4c164200-20c0-11d0-a768-00aa006e0529;bf967aba-0de6-11d0-a285-00aa003049e2;RU)(OA;;RP;5f202010-79a5-11d0-9020-00c04fc2d4cf;4828cc14-1437-45bc-9b07-ad6f015e5f28;RU)(OA;;RP;5f202010-79a5-11d0-9020-00c04fc2d4cf;bf967aba-0de6-11d0-a285-00aa003049e2;RU)(OA;;RP;bc0ac240-79a9-11d0-9020-00c04fc2d4cf;4828cc14-1437-45bc-9b07-ad6f015e5f28;RU)(OA;;RP;bc0ac240-79a9-11d0-9020-00c04fc2d4cf;bf967aba-0de6-11d0-a285-00aa003049e2;RU)(OA;;RP;59ba2f42-79a2-11d0-9020-00c04fc2d3cf;4828cc14-1437-45bc-9b07-ad6f015e5f28;RU)(OA;;RP;59ba2f42-79a2-11d0-9020-00c04fc2d3cf;bf967aba-0de6-11d0-a285-00aa003049e2;RU)(OA;;RP;037088f8-0ae1-11d2-b422-00a0c968f939;4828cc14-1437-45bc-9b07-ad6f015e5f28;RU)(OA;;RP;037088f8-0ae1-11d2-b422-00a0c968f939;bf967aba-0de6-11d0-a285-00aa003049e2;RU)(OA;;RPWP;bf967a7f-0de6-11d0-a285-00aa003049e2;;CA)(OA;;RP;46a9b11d-60ae-405a-b7e8-ff8a58d456d2;;S-1-5-32-560)(OA;;RPWP;6db69a1c-9422-11d1-aebd-0000f80367c1;;S-1-5-32-561)(OA;;RPWP;5805bc62-bdc9-4428-a5e2-856a0f4c185e;;S-1-5-32-561)(OA;;LCRPLORC;;4828cc14-1437-45bc-9b07-ad6f015e5f28;RU)(OA;;LCRPLORC;;bf967aba-0de6-11d0-a285-00aa003049e2;RU)(OA;;CR;ab721a53-1e2f-11d0-9819-00aa0040529b;;WD)(OA;;CR;ab721a53-1e2f-11d0-9819-00aa0040529b;;PS)(OA;CI;RPWPCR;91e647de-d96f-4b70-9557-d63ff4f3ccd8;;PS)(A;;CCDCLCSWRPWPLOCRRCWDWO;;;DA)(A;;CCDCLCSWRPWPLOCRRCWDWO;;;EA)(A;;CCDCLCSWRPWPLOCRSDRCWDWO;;;BA)(A;;LCRPLORC;;;AU)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;SY)";

        private void InspectAdminSDHolder(ADDomainInfo domainInfo, ADWebService adws)
        {
            string[] properties = new string[]
            {
                "distinguishedName",
                "name",
                "nTSecurityDescriptor"
            };
            ActiveDirectorySecurity AdminSDHolder = null;
            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    AdminSDHolder = x.NTSecurityDescriptor;
                };

            adws.Enumerate(domainInfo.DefaultNamingContext, "(distinguishedName=CN=AdminSDHolder,CN=System," + domainInfo.DefaultNamingContext + ")", properties, callback);

            if (AdminSDHolder != null)
            {
                ActiveDirectorySecurity reference = new ActiveDirectorySecurity();
                string sddlToCheck = AdminSDHolder.GetSecurityDescriptorSddlForm(AccessControlSections.Access);

                //reference.SetSecurityDescriptorSddlForm(AdminSDHolderSDDL44);
                List<string> rulesAdded = CompareSecurityDescriptor(sddlToCheck, sddlReference, domainInfo.DomainSid);
                AddAdminSDHolderSDDLRulesToDelegation(rulesAdded, domainInfo);
            }
            else
            {
                HealthCheckDelegationData data = new HealthCheckDelegationData();
                data.DistinguishedName = "AdminSDHolder";
                data.Account = "Authenticated Users";
                data.Right = "Not allowed to read AdminSDHolder";
                data.SecurityIdentifier = string.Empty;
            }
        }

        private void AddAdminSDHolderSDDLRulesToDelegation(List<string> rulesAdded, ADDomainInfo domainInfo)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            foreach (string rule in rulesAdded)
            {
                string[] SDDL = rule.Split(';');
                string sid = SDDL[5];
                if (sid.StartsWith("S-1-5-21"))
                {
                    if (!dic.ContainsKey(sid))
                        dic[sid] = "Rule in AdminSDHolder (modified or added)";
                }
            }
            foreach (string key in dic.Keys)
            {
                HealthCheckDelegationData data = new HealthCheckDelegationData();
                data.DistinguishedName = "AdminSDHolder";
                data.SecurityIdentifier = key;
                data.Account = NativeMethods.ConvertSIDToName(key, domainInfo.DnsHostName);
                data.Right = dic[key];
                healthcheckData.Delegations.Add(data);
            }
        }

        private List<string> CompareSecurityDescriptor(string sddlToCheck, string sddlReference, SecurityIdentifier domain)
        {
            List<string> output = new List<string>();
            sddlToCheck = sddlToCheck.Substring(5);
            sddlReference = sddlReference.Substring(5);
            sddlToCheck = sddlToCheck.Replace(new SecurityIdentifier(WellKnownSidType.AccountDomainAdminsSid, domain).Value, "DA");
            sddlToCheck = Regex.Replace(sddlToCheck, @"S-1-5-21-\d+-\d+-\d+-519", "EA");

            //sddlToCheck = sddlToCheck.Replace(new SecurityIdentifier(WellKnownSidType.AccountEnterpriseAdminsSid, domain).Value, "EA");
            sddlToCheck = sddlToCheck.Replace(new SecurityIdentifier(WellKnownSidType.AccountCertAdminsSid, domain).Value, "CA");

            string[] values = sddlToCheck.Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string value in values)
            {
                if (!sddlReference.Contains("(" + value + ")"))
                {
                    output.Add(value);
                    Trace.WriteLine("AdminSDHolder unknown entry:" + value);
                }
            }
            return output;
        }

        private void InspectDelegation(ADDomainInfo domainInfo, ADWebService adws)
        {
            string[] propertiesLaps = new string[] { "schemaIDGUID" };

            // note: the LDAP request does not contain ms-MCS-AdmPwd because in the old time, MS consultant was installing customized version of the attriute, * being replaced by the company name
            // check the oid instead ? (which was the same even if the attribute name was not)
            adws.Enumerate(domainInfo.SchemaNamingContext, "(name=ms-*-AdmPwd)", propertiesLaps, (ADItem aditem) =>
            {
                ReadGuidsControlProperties.Add(new KeyValuePair<Guid, string>(aditem.SchemaIDGUID, "READ_PROP_ms-mcs-admpwd"));
            }, "OneLevel");

            string[] properties = new string[]
            {
                "distinguishedName",
                "name",
                "nTSecurityDescriptor"
            };
            Dictionary<string, string> sidCache = new Dictionary<string, string>();
            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    Dictionary<string, string> permissions = new Dictionary<string, string>();
                    FilterDelegation(x,
                        (SecurityIdentifier sid, string right)
                            =>
                        {
                            if (!permissions.ContainsKey(sid.Value))
                            {
                                permissions[sid.Value] = right;
                            }
                            else
                            {
                                permissions[sid.Value] += ", " + right;
                            }
                        }
                    );
                    foreach (string sid in permissions.Keys)
                    {
                        HealthCheckDelegationData delegation = new HealthCheckDelegationData();
                        healthcheckData.Delegations.Add(delegation);
                        delegation.DistinguishedName = x.DistinguishedName;
                        delegation.SecurityIdentifier = sid;

                        // avoid translation for anomaly detection later
                        if (sid == "S-1-1-0")
                        {
                            delegation.Account = "Everyone";
                        }
                        else if (sid == "S-1-5-7")
                        {
                            delegation.Account = "Anonymous";
                        }
                        else if (sid == "S-1-5-11")
                        {
                            delegation.Account = "Authenticated Users";
                        }
                        else if (sid.EndsWith("-513"))
                        {
                            delegation.Account = "Domain Users";
                        }
                        else if (sid.EndsWith("-515"))
                        {
                            delegation.Account = "Domain Computers";
                        }
                        else if (sid == "S-1-5-32-545")
                        {
                            delegation.Account = "Users";
                        }
                        else
                        {
                            if (!sidCache.ContainsKey(sid))
                            {
                                sidCache[sid] = NativeMethods.ConvertSIDToName(sid, domainInfo.DnsHostName);
                            }
                            delegation.Account = sidCache[sid];
                        }
                        delegation.Right = permissions[sid];
                    }

                };

            adws.Enumerate(
                () =>
                {
                    healthcheckData.Delegations.Clear();
                },
                domainInfo.DefaultNamingContext, "(|(objectCategory=organizationalUnit)(objectCategory=container)(objectCategory=domain)(objectCategory=buitinDomain))", properties, callback, "SubTree");
        }

        // removed unexpire password because permissions given to authenticated users at the root of the domain
        static KeyValuePair<Guid, string>[] GuidsControlExtendedRights = new KeyValuePair<Guid, string>[]
        {
            new KeyValuePair<Guid, string>(new Guid("00299570-246d-11d0-a768-00aa006e0529"), "EXT_RIGHT_FORCE_CHANGE_PWD"),
            new KeyValuePair<Guid, string>(new Guid("1131f6ad-9c07-11d1-f79f-00c04fc2dcd2"), "EXT_RIGHT_REPLICATION_GET_CHANGES_ALL"),
            new KeyValuePair<Guid, string>(new Guid("45ec5156-db7e-47bb-b53f-dbeb2d03c40f"), "EXT_RIGHT_REANIMATE_TOMBSTONE")

            //                    new KeyValuePair<Guid, string>(new Guid("ccc2dc7d-a6ad-4a7a-8846-c04e3cc53501"), "EXT_RIGHT_UNEXPIRE_PASSWORD"),
            //                    new KeyValuePair<Guid, string>(new Guid("ba33815a-4f93-4c76-87f3-57574bff8109"), "EXT_RIGHT_MIGRATE_SID_HISTORY"),
        };

        static KeyValuePair<Guid, string>[] GuidsControlValidatedWrites = new KeyValuePair<Guid, string>[]
        {
            new KeyValuePair<Guid, string>(new Guid("bc0ac240-79a9-11d0-9020-00c04fc2d4cf"), "WRITE_PROPSET_MEMBERSHIP")
        };

        List<KeyValuePair<Guid, string>> GuidsControlProperties = new List<KeyValuePair<Guid, string>>
        {
            new KeyValuePair<Guid, string>(new Guid("bf9679c0-0de6-11d0-a285-00aa003049e2"), "WRITE_PROP_MEMBER"),
            new KeyValuePair<Guid, string>(new Guid("f30e3bbe-9ff0-11d1-b603-0000f80367c1"), "WRITE_PROP_GPLINK"),
            new KeyValuePair<Guid, string>(new Guid("f30e3bc1-9ff0-11d0-b603-0000f80367c1"), "WRITE_PROP_GPC_FILE_SYS_PATH")
        };
        List<KeyValuePair<Guid, string>> ReadGuidsControlProperties = new List<KeyValuePair<Guid, string>> { };
        static KeyValuePair<Guid, string>[] GuidsControlPropertiesSets = new KeyValuePair<Guid, string>[]
        {
            new KeyValuePair<Guid, string>(new Guid("bf9679c0-0de6-11d0-a285-00aa003049e2"), "VAL_WRITE_SELF_MEMBERSHIP")
        };

        delegate void WorkOnDelegation(SecurityIdentifier sid, string right);

        void FilterAdminSDHolder(ADItem x, WorkOnDelegation gotDelegation)
        {
            ActiveDirectorySecurity sd = x.NTSecurityDescriptor;
            if (sd == null)
                return;

            // check the owner
            SecurityIdentifier owner = (SecurityIdentifier)sd.GetOwner(typeof(SecurityIdentifier));
            if (!owner.IsWellKnown(WellKnownSidType.AccountDomainAdminsSid))
            {
                gotDelegation(owner, "OWNER");
            }
            foreach (ActiveDirectoryAccessRule accessrule in sd.GetAccessRules(true, false, typeof(SecurityIdentifier)))
            {
                if (accessrule.AccessControlType != AccessControlType.Allow)
                    continue;
                SecurityIdentifier si = (SecurityIdentifier)accessrule.IdentityReference;

                // SYSTEM
                if (si.Value == "S-1-5-18")
                    continue;

                // DS SELF
                if (si.Value == "S-1-3-0")
                    continue;

                // build in admin
                if (si.Value == "S-1-5-32-544")
                    continue;

                if (si.IsWellKnown(WellKnownSidType.AccountDomainAdminsSid))
                    continue;
                if (si.IsWellKnown(WellKnownSidType.AccountEnterpriseAdminsSid))
                    continue;

                Trace.WriteLine(si.Value);
                Trace.WriteLine(accessrule.ActiveDirectoryRights);
                Trace.WriteLine(accessrule.ObjectType);
                Trace.WriteLine(accessrule.InheritedObjectType);
                Trace.WriteLine(accessrule.ObjectFlags);
            }
        }

        void FilterDelegation(ADItem x, WorkOnDelegation gotDelegation)
        {
            ActiveDirectorySecurity sd = x.NTSecurityDescriptor;
            if (sd == null)
                return;
            foreach (ActiveDirectoryAccessRule accessrule in sd.GetAccessRules(true, false, typeof(SecurityIdentifier)))
            {
                // ignore audit / denied ace
                if (accessrule.AccessControlType != AccessControlType.Allow)
                    continue;
                SecurityIdentifier si = (SecurityIdentifier)accessrule.IdentityReference;

                // SYSTEM
                if (si.Value == "S-1-5-18")
                    continue;

                // DS SELF
                if (si.Value == "S-1-3-0")
                    continue;

                // build in admin
                if (si.Value == "S-1-5-32-544")
                    continue;

                if (si.IsWellKnown(WellKnownSidType.AccountDomainAdminsSid))
                    continue;
                if (si.IsWellKnown(WellKnownSidType.AccountEnterpriseAdminsSid))
                    continue;

                // ADS_RIGHT_GENERIC_ALL
                if ((accessrule.ActiveDirectoryRights & ActiveDirectoryRights.GenericAll) == ActiveDirectoryRights.GenericAll)
                {
                    gotDelegation((SecurityIdentifier)accessrule.IdentityReference, "GenericAll");
                }

                // ADS_RIGHT_GENERIC_WRITE
                if ((accessrule.ActiveDirectoryRights & ActiveDirectoryRights.GenericWrite) == ActiveDirectoryRights.GenericWrite)
                {
                    gotDelegation((SecurityIdentifier)accessrule.IdentityReference, "GenericWrite");
                }

                // ADS_RIGHT_WRITE_DAC
                if ((accessrule.ActiveDirectoryRights & ActiveDirectoryRights.WriteDacl) == ActiveDirectoryRights.WriteDacl)
                {
                    gotDelegation((SecurityIdentifier)accessrule.IdentityReference, "WriteDacl");
                }

                // ADS_RIGHT_WRITE_OWNER
                if ((accessrule.ActiveDirectoryRights & ActiveDirectoryRights.WriteOwner) == ActiveDirectoryRights.WriteOwner)
                {
                    gotDelegation((SecurityIdentifier)accessrule.IdentityReference, "WriteOwner");
                }
                if (accessrule.ObjectFlags == ObjectAceFlags.None)
                {
                    // ADS_RIGHT_DS_CONTROL_ACCESS
                    if ((accessrule.ActiveDirectoryRights & ActiveDirectoryRights.ExtendedRight) == ActiveDirectoryRights.ExtendedRight)
                    {
                        gotDelegation((SecurityIdentifier)accessrule.IdentityReference, "All extended right");
                    }

                    // ADS_RIGHT_DS_SELF
                    if ((accessrule.ActiveDirectoryRights & ActiveDirectoryRights.Self) == ActiveDirectoryRights.Self)
                    {
                        gotDelegation((SecurityIdentifier)accessrule.IdentityReference, "DSSelf");
                    }

                    // ADS_RIGHT_DS_WRITE_PROP
                    if ((accessrule.ActiveDirectoryRights & ActiveDirectoryRights.WriteProperty) == ActiveDirectoryRights.WriteProperty)
                    {
                        gotDelegation((SecurityIdentifier)accessrule.IdentityReference, "Write all prop");
                    }
                }
                else if ((accessrule.ObjectFlags & ObjectAceFlags.ObjectAceTypePresent) == ObjectAceFlags.ObjectAceTypePresent)
                {
                    // ADS_RIGHT_DS_CONTROL_ACCESS
                    if ((accessrule.ActiveDirectoryRights & ActiveDirectoryRights.ExtendedRight) == ActiveDirectoryRights.ExtendedRight)
                    {
                        foreach (KeyValuePair<Guid, string> extendedright in GuidsControlExtendedRights)
                        {
                            if (extendedright.Key == accessrule.ObjectType)
                            {
                                gotDelegation((SecurityIdentifier)accessrule.IdentityReference, extendedright.Value);
                            }
                        }
                    }

                    // ADS_RIGHT_DS_SELF
                    if ((accessrule.ActiveDirectoryRights & ActiveDirectoryRights.Self) == ActiveDirectoryRights.Self)
                    {
                        foreach (KeyValuePair<Guid, string> validatewrite in GuidsControlValidatedWrites)
                        {
                            if (validatewrite.Key == accessrule.ObjectType)
                            {
                                gotDelegation((SecurityIdentifier)accessrule.IdentityReference, validatewrite.Value);
                            }
                        }
                    }

                    // ADS_RIGHT_DS_WRITE_PROP
                    if ((accessrule.ActiveDirectoryRights & ActiveDirectoryRights.WriteProperty) == ActiveDirectoryRights.WriteProperty)
                    {
                        foreach (KeyValuePair<Guid, string> controlproperty in GuidsControlProperties)
                        {
                            if (controlproperty.Key == accessrule.ObjectType)
                            {
                                gotDelegation((SecurityIdentifier)accessrule.IdentityReference, controlproperty.Value);
                            }
                        }
                        foreach (KeyValuePair<Guid, string> controlpropertyset in GuidsControlPropertiesSets)
                        {
                            if (controlpropertyset.Key == accessrule.ObjectType)
                            {
                                gotDelegation((SecurityIdentifier)accessrule.IdentityReference, controlpropertyset.Value);
                            }
                        }
                    }

                    // ADS_RIGHT_DS_READ_PROP
                    if ((accessrule.ActiveDirectoryRights & ActiveDirectoryRights.ReadProperty) == ActiveDirectoryRights.ReadProperty)
                    {
                        foreach (KeyValuePair<Guid, string> controlproperty in ReadGuidsControlProperties)
                        {
                            if (controlproperty.Key == accessrule.ObjectType)
                            {
                                gotDelegation((SecurityIdentifier)accessrule.IdentityReference, controlproperty.Value);
                            }
                        }
                    }
                }
            }
        }

        private class GPO
        {
            public string InternalName { get; set; }
            public string DisplayName { get; set; }
            public bool IsDisabled { get; set; }
            public string DN { get; set; }

            public List<string> AppliedTo { get; set; }
        }

        private void GenerateGPOData(ADDomainInfo domainInfo, ADWebService adws, NetworkCredential credential)
        {
            healthcheckData.GPPPassword = new List<GPPPassword>();
            healthcheckData.GPPRightAssignment = new List<GPPRightAssignment>();
            healthcheckData.GPPLoginAllowedOrDeny = new List<GPPRightAssignment>();
            healthcheckData.GPPPasswordPolicy = new List<GPPSecurityPolicy>();
            healthcheckData.GPOLsaPolicy = new List<GPPSecurityPolicy>();
            healthcheckData.GPOScreenSaverPolicy = new List<GPPSecurityPolicy>();
            healthcheckData.TrustedCertificates = new List<HealthCheckCertificateData>();
            healthcheckData.GPOLoginScript = new List<HealthCheckGPOLoginScriptData>();
            healthcheckData.GPOLocalMembership = new List<GPOMembership>();
            healthcheckData.GPOEventForwarding = new List<GPOEventForwardingInfo>();
            healthcheckData.GPODelegation = new List<GPODelegationData>();
            healthcheckData.GPPFileDeployed = new List<GPPFileDeployed>();
            healthcheckData.GPOAuditSimple = new List<GPOAuditSimpleData>();
            healthcheckData.GPOAuditAdvanced = new List<GPOAuditAdvancedData>();

            // subitility: GPOList = all active and not active GPO (but not the deleted ones)
            Dictionary<string, GPO> GPOList = new Dictionary<string, GPO>(StringComparer.OrdinalIgnoreCase);
            GetGPOList(domainInfo, adws, GPOList);
            SaveGPOListToHCData(GPOList);

            ParseGPOFiles(domainInfo, credential, GPOList);
            GenerateNTLMStoreData(domainInfo, adws);
            GenerateMsiData(domainInfo, adws, GPOList);
        }

        private void SaveGPOListToHCData(Dictionary<string, GPO> GPOList)
        {
            healthcheckData.GPOInfo = new List<GPOInfo>();
            foreach (var GPO in GPOList.Values)
            {
                healthcheckData.GPOInfo.Add(new GPOInfo()
                {
                    GPOId = GPO.InternalName,
                    GPOName = GPO.DisplayName,
                    IsDisabled = GPO.IsDisabled,
                    AppliedTo = GPO.AppliedTo
                });
            }
        }

        private void ParseGPOFiles(ADDomainInfo domainInfo, NetworkCredential credential, Dictionary<string, GPO> GPOList)
        {
            WindowsIdentity identity = null;
            WindowsImpersonationContext context = null;
            BlockingQueue<DirectoryInfo> queue = new BlockingQueue<DirectoryInfo>(200);
            int numberOfThread = 20;
            Thread[] threads = new Thread[numberOfThread];
            string uri = null;
            try
            {
                uri = "\\\\" + domainInfo.DnsHostName + "\\sysvol\\" + domainInfo.DomainName + "\\Policies";
                if (credential != null)
                {
                    identity = NativeMethods.GetWindowsIdentityForUser(credential, domainInfo.DomainName);
                    context = identity.Impersonate();
                }

                ThreadStart threadFunction = () =>
                {
                    for (;;)
                    {
                        DirectoryInfo directoryInfo = null;
                        if (!queue.Dequeue(out directoryInfo)) break;
                        string ADGPOName = directoryInfo.Name.ToLowerInvariant();
                        GPO gpo = null;
                        if (GPOList.ContainsKey(ADGPOName))
                        {
                            gpo = GPOList[ADGPOName];
                        }
                        ThreadGPOAnalysis(directoryInfo, gpo, domainInfo);
                    }
                };

                // Consumers
                for (int i = 0; i < numberOfThread; i++)
                {
                    threads[i] = new Thread(threadFunction);
                    threads[i].Start();
                }

                DirectoryInfo di = new DirectoryInfo(uri);
                DirectoryInfo[] AllDirectories = di.GetDirectories();
                foreach (DirectoryInfo directoryInfo in AllDirectories)
                {
                    queue.Enqueue(directoryInfo);
                }

                queue.Quit();
                Trace.WriteLine("examining file completed. Waiting for worker thread to complete");
                for (int i = 0; i < numberOfThread; i++)
                {
                    threads[i].Join();
                }
                Trace.WriteLine("Done insert file");
            }
            catch (UnauthorizedAccessException ex)
            {
                Trace.WriteLine("Exception while generating GPO Data: " + ex.Message);
                Trace.WriteLine(ex.StackTrace);
                lock (this)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Exception while generating GPO Data: " + ex.Message);
                    Console.ResetColor();
                }
            }
            catch (IOException ex)
            {
                Trace.WriteLine("Exception while generating GPO Data: " + ex.Message);
                Trace.WriteLine(ex.StackTrace);
                lock (this)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Exception while generating GPO Data: " + ex.Message);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception while generating GPO Data: " + ex.Message);
                Trace.WriteLine(ex.StackTrace);
                lock (this)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Exception while generating GPO Data: " + ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                }
            }
            finally
            {
                queue.Quit();
                for (int i = 0; i < numberOfThread; i++)
                {
                    if (threads[i] != null)
                        if (threads[i].ThreadState == System.Threading.ThreadState.Running)
                            threads[i].Abort();
                }
                if (context != null)
                    context.Undo();
                if (identity != null)
                    identity.Dispose();
            }
        }

        private void GenerateNTLMStoreData(ADDomainInfo domainInfo, ADWebService adws)
        {
            string[] properties = new string[]
            {
                "distinguishedName",
                "cACertificate"
            };
            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    if (x.CACertificate != null)
                    {
                        foreach (X509Certificate2 certificate in x.CACertificate)
                        {
                            HealthCheckCertificateData data = new HealthCheckCertificateData();
                            data.Source = "NTLMStore";
                            data.Store = "NTLMStore";
                            data.Certificate = certificate.GetRawCertData();
                            healthcheckData.TrustedCertificates.Add(data);
                        }
                    }
                };

            // do it only on forest
            string test = domainInfo.ConfigurationNamingContext.Replace(domainInfo.DefaultNamingContext, "");
            if (!test.Contains("DC="))
            {
                adws.Enumerate(domainInfo.ConfigurationNamingContext, "(distinguishedName=CN=NTAuthCertificates,CN=Public Key Services,CN=Services," + domainInfo.ConfigurationNamingContext + ")", properties, callback);
            }

        }

        private void GenerateMsiData(ADDomainInfo domainInfo, ADWebService adws, Dictionary<string, GPO> GPOList)
        {
            string[] properties = new string[]
            {
                "distinguishedName",
                "msiFileList"
            };
            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    if (x.msiFileList == null)
                        return;
                    int pos1 = x.DistinguishedName.IndexOf('{');
                    if (pos1 < 0)
                        return;
                    int pos2 = x.DistinguishedName.IndexOf('}', pos1);
                    string GPOGuid = x.DistinguishedName.Substring(pos1, pos2 - pos1 + 1).ToLowerInvariant();
                    if (!GPOList.ContainsKey(GPOGuid))
                        return;
                    var GPO = GPOList[GPOGuid];
                    if (GPO.IsDisabled)
                        return;
                    string section = (x.DistinguishedName.Contains("Machine") ? "Computer" : "User");
                    foreach (var msiFileItem in x.msiFileList)
                    {
                        var msiFile = msiFileItem.Split(':');
                        if (msiFile.Length < 2)
                            continue;
                        var file = new GPPFileDeployed();
                        file.GPOName = GPO.DisplayName;
                        file.GPOId = GPO.InternalName;
                        file.Type = "Application (" + section + " section)";
                        file.FileName = msiFile[1];
                        file.Delegation = new List<HealthCheckScriptDelegationData>();
                        healthcheckData.GPPFileDeployed.Add(file);
                        if (File.Exists(file.FileName))
                        {
                            try
                            {
                                var ac = File.GetAccessControl(file.FileName);
                                foreach (var value in AnalyzeFileSystemSecurity(ac, true))
                                {
                                    file.Delegation.Add(new HealthCheckScriptDelegationData()
                                        {
                                            Account = value.Value,
                                            Right = value.Key
                                        }
                                    );
                                }
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine("Unable to analyze " + file.FileName + " " + ex.Message);
                            }
                        }
                    }

                };
            adws.Enumerate(domainInfo.DefaultNamingContext, "(objectClass=packageRegistration)", properties, callback);
        }

        void ThreadGPOAnalysis(DirectoryInfo directoryInfo, GPO GPO, ADDomainInfo domainInfo)
        {
            string step = "initial";
            try
            {
                string path;

                // work on all GPO including disabled ones
                step = "extract GPP passwords";
                foreach (string target in new string[] { "user", "machine" })
                {
                    foreach (string shortname in new string[]
                    {
                        "groups.xml", "services.xml", "scheduledtasks.xml",
                        "datasources.xml", "printers.xml", "drives.xml"
                    })
                    {
                        path = directoryInfo.FullName + @"\" + target + @"\Preferences\" + shortname.Replace(".xml", "") + "\\" + shortname;
                        if (File.Exists(path))
                        {
                            ExtractGPPPassword(shortname, path, GPO, "Unknown [" + directoryInfo.Name + "]");
                        }
                    }
                }
                path = directoryInfo.FullName + @"\Machine\Preferences\Registry\Registry.xml";
                if (File.Exists(path))
                {
                    ExtractLoginPassword(path, GPO, "Unknown [" + directoryInfo.Name + "]");
                }

                // work only on active GPO
                if (GPO == null || GPO.IsDisabled)
                    return;
                path = directoryInfo.FullName + @"\Machine\Microsoft\Windows nt\SecEdit\GptTmpl.inf";
                if (File.Exists(path))
                {
                    step = "extract GPP privileges";
                    ExtractGPPPrivilegePasswordLsaSettingEtc(path, GPO, domainInfo);
                }
                path = directoryInfo.FullName + @"\MACHINE\Microsoft\Windows NT\Audit\audit.csv";
                if (File.Exists(path))
                {
                    step = "extract audit";
                    ExtractGPOAudit(path, GPO, domainInfo);
                }
                step = "extract GPO login script";
                ExtractGPOLoginScript(domainInfo, directoryInfo, GPO);
                path = directoryInfo.FullName + @"\User\Preferences\Files\Files.xml";
                if (File.Exists(path))
                {
                    step = "extract Files info";
                    ExtractGPPFile(path, GPO, domainInfo, "User");
                }
                path = directoryInfo.FullName + @"\Machine\Preferences\Files\Files.xml";
                if (File.Exists(path))
                {
                    step = "extract Files info";
                    ExtractGPPFile(path, GPO, domainInfo, "Computer");
                }
                try
                {
                    step = "extract Registry Pol info";
                    ExtractRegistryPolInfo(domainInfo, directoryInfo, GPO);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("GPO Pol info failed " + directoryInfo.Name + " " + GPO.DisplayName);
                    Trace.WriteLine("Exception " + ex.Message);
                    Trace.WriteLine(ex.StackTrace);
                }
                step = "check GPO permissions";
                ExtractGPODelegation(directoryInfo.FullName, GPO);
                step = "check GPO settings";
                path = directoryInfo.FullName + @"\Machine\Preferences\Registry\Registry.xml";
                if (File.Exists(path))
                {
                    ExtractGPOSettingsFromRegistryXml(path, GPO);
                }

            }
            catch (UnauthorizedAccessException ex)
            {
                lock (this)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to analyze the GPO: " + directoryInfo.Name + "(" + ex.Message + ")");
                    Trace.WriteLine("Unable to analyze the GPO: " + directoryInfo.Name + "(" + ex.Message + ")");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to analyze the GPO: " + directoryInfo.Name + "(" + ex.Message + ")");
                    Trace.WriteLine("Unable to analyze the GPO: " + directoryInfo.Name + "(" + ex.Message + ")");
                    Console.WriteLine("More details are available in the trace log (step: " + step + ")");
                    Trace.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                }
            }
        }

        private void ExtractLoginPassword(string path, GPO GPO, string alternateNameIfGPODoesNotExists)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNodeList nodeList = doc.SelectNodes(@"//Registry[@name=""DefaultPassword""]");
            foreach (XmlNode node in nodeList)
            {
                XmlNode password = node.SelectSingleNode("Properties/@value");

                // no password
                if (password == null)
                    continue;

                // password has been manually changed
                if (string.IsNullOrEmpty(password.Value))
                    continue;
                GPPPassword PasswordData = new GPPPassword();
                PasswordData.GPOName = (GPO == null ? alternateNameIfGPODoesNotExists : GPO.DisplayName);
                PasswordData.GPOId = (GPO == null ? null : GPO.InternalName);
                PasswordData.Password = password.Value;

                XmlNode userNameNode = node.SelectSingleNode(@"//Registry[@name=""DefaultUserName""]/Properties/@value");
                PasswordData.UserName = (userNameNode != null ? userNameNode.Value : string.Empty);

                XmlNode changed = node.SelectSingleNode("@changed");
                if (changed != null)
                {
                    PasswordData.Changed = DateTime.Parse(changed.Value);
                }
                else
                {
                    FileInfo fi = new FileInfo(path);
                    PasswordData.Changed = fi.LastWriteTime;
                }
                PasswordData.Type = "registry.xml";
                PasswordData.Other = "Autologon info";
                lock (healthcheckData.GPPPassword)
                {
                    healthcheckData.GPPPassword.Add(PasswordData);
                }
            }
        }

        private void ExtractGPOSettingsFromRegistryXml(string path, GPO GPO)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNodeList nodeList = doc.SelectNodes(@"//Registry/Properties[@name=""SrvsvcSessionInfo""][@key=""SYSTEM\CurrentControlSet\Services\LanmanServer\DefaultSecurity""]");
            if (nodeList.Count > 0)
            {
                GPPSecurityPolicy SecurityPolicy = null;
                foreach (GPPSecurityPolicy policy in healthcheckData.GPOLsaPolicy)
                {
                    if (policy.GPOId == GPO.InternalName)
                    {
                        SecurityPolicy = policy;
                        break;
                    }
                }
                if (SecurityPolicy == null)
                {
                    SecurityPolicy = new GPPSecurityPolicy();
                    SecurityPolicy.GPOName = GPO.DisplayName;
                    SecurityPolicy.GPOId = GPO.InternalName;

                    lock (healthcheckData.GPOLsaPolicy)
                    {
                        healthcheckData.GPOLsaPolicy.Add(SecurityPolicy);
                    }
                    SecurityPolicy.Properties = new List<GPPSecurityPolicyProperty>();
                }
                SecurityPolicy.Properties.Add(new GPPSecurityPolicyProperty("SrvsvcSessionInfo", 1));
            }
        }

        private void ExtractRegistryPolInfo(ADDomainInfo domainInfo, DirectoryInfo directoryInfo, GPO GPO)
        {
            GPPSecurityPolicy PSO = null;
            foreach (string gpotarget in new string[] { "Machine", "User" })
            {
                string path = directoryInfo.FullName + "\\" + gpotarget + "\\registry.pol";
                if (File.Exists(path))
                {
                    RegistryPolReader reader = new RegistryPolReader();
                    reader.LoadFile(path);
                    int intvalue = 0;
                    if (gpotarget == "Machine")
                    {
                        //https://support.microsoft.com/en-us/kb/221784
                        if (reader.IsValueSetIntAsStringValue(@"Software\Microsoft\Windows NT\CurrentVersion\Winlogon", "ScreenSaverGracePeriod", out intvalue))
                        {
                            if (PSO == null)
                            {
                                PSO = new GPPSecurityPolicy();
                                PSO.GPOName = GPO.DisplayName;
                                PSO.GPOId = GPO.InternalName;
                                lock (healthcheckData.GPOScreenSaverPolicy)
                                {
                                    healthcheckData.GPOScreenSaverPolicy.Add(PSO);
                                }
                                PSO.Properties = new List<GPPSecurityPolicyProperty>();
                            }
                            PSO.Properties.Add(new GPPSecurityPolicyProperty("ScreenSaverGracePeriod", intvalue));
                        }
                        if (reader.IsValueSet(@"Software\Policies\Microsoft\Windows NT\DNSClient", "EnableMulticast", out intvalue))
                        {
                            GPPSecurityPolicy SecurityPolicy = null;
                            foreach (GPPSecurityPolicy policy in healthcheckData.GPOLsaPolicy)
                            {
                                if (policy.GPOId == GPO.InternalName)
                                {
                                    SecurityPolicy = policy;
                                    break;
                                }
                            }
                            if (SecurityPolicy == null)
                            {
                                SecurityPolicy = new GPPSecurityPolicy();
                                SecurityPolicy.GPOName = GPO.DisplayName;
                                SecurityPolicy.GPOId = GPO.InternalName;

                                lock (healthcheckData.GPOLsaPolicy)
                                {
                                    healthcheckData.GPOLsaPolicy.Add(SecurityPolicy);
                                }
                                SecurityPolicy.Properties = new List<GPPSecurityPolicyProperty>();
                            }
                            SecurityPolicy.Properties.Add(new GPPSecurityPolicyProperty("EnableMulticast", intvalue));
                        }
                        if (reader.IsValueSet(@"Software\Policies\Microsoft\Windows\PowerShell\ModuleLogging", "EnableModuleLogging", out intvalue))
                        {
                            GPPSecurityPolicy SecurityPolicy = null;
                            foreach (GPPSecurityPolicy policy in healthcheckData.GPOLsaPolicy)
                            {
                                if (policy.GPOId == GPO.InternalName)
                                {
                                    SecurityPolicy = policy;
                                    break;
                                }
                            }
                            if (SecurityPolicy == null)
                            {
                                SecurityPolicy = new GPPSecurityPolicy();
                                SecurityPolicy.GPOName = GPO.DisplayName;
                                SecurityPolicy.GPOId = GPO.InternalName;

                                lock (healthcheckData.GPOLsaPolicy)
                                {
                                    healthcheckData.GPOLsaPolicy.Add(SecurityPolicy);
                                }
                                SecurityPolicy.Properties = new List<GPPSecurityPolicyProperty>();
                            }
                            SecurityPolicy.Properties.Add(new GPPSecurityPolicyProperty("EnableModuleLogging", intvalue));
                        }
                        if (reader.IsValueSet(@"Software\Policies\Microsoft\Windows\PowerShell\ScriptBlockLogging", "EnableScriptBlockLogging", out intvalue))
                        {
                            GPPSecurityPolicy SecurityPolicy = null;
                            foreach (GPPSecurityPolicy policy in healthcheckData.GPOLsaPolicy)
                            {
                                if (policy.GPOId == GPO.InternalName)
                                {
                                    SecurityPolicy = policy;
                                    break;
                                }
                            }
                            if (SecurityPolicy == null)
                            {
                                SecurityPolicy = new GPPSecurityPolicy();
                                SecurityPolicy.GPOName = GPO.DisplayName;
                                SecurityPolicy.GPOId = GPO.InternalName;

                                lock (healthcheckData.GPOLsaPolicy)
                                {
                                    healthcheckData.GPOLsaPolicy.Add(SecurityPolicy);
                                }
                                SecurityPolicy.Properties = new List<GPPSecurityPolicyProperty>();
                            }
                            SecurityPolicy.Properties.Add(new GPPSecurityPolicyProperty("EnableScriptBlockLogging", intvalue));
                        }

                        for (int i = 1;; i++)
                        {
                            string server = null;
                            if (reader.IsValueSet(@"Software\Policies\Microsoft\Windows\EventLog\EventForwarding\SubscriptionManager", i.ToString(), out server))
                            {
                                lock (healthcheckData.GPOEventForwarding)
                                {
                                    GPOEventForwardingInfo info = new GPOEventForwardingInfo();
                                    info.GPOName = GPO.DisplayName;
                                    info.GPOId = GPO.InternalName;
                                    info.Order = i;
                                    info.Server = server;
                                    healthcheckData.GPOEventForwarding.Add(info);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        //https://msdn.microsoft.com/fr-fr/library/cc781591(v=ws.10).aspx
                        if (reader.IsValueSetIntAsStringValue(@"software\policies\microsoft\windows\Control Panel\Desktop", "ScreenSaveTimeOut", out intvalue))
                        {
                            if (PSO == null)
                            {
                                PSO = new GPPSecurityPolicy();
                                PSO.GPOName = GPO.DisplayName;
                                PSO.GPOId = GPO.InternalName;
                                lock (healthcheckData.GPOScreenSaverPolicy)
                                {
                                    healthcheckData.GPOScreenSaverPolicy.Add(PSO);
                                }
                                PSO.Properties = new List<GPPSecurityPolicyProperty>();
                            }
                            PSO.Properties.Add(new GPPSecurityPolicyProperty("ScreenSaveTimeOut", intvalue));
                        }

                        //https://msdn.microsoft.com/fr-fr/library/cc787364(v=ws.10).aspx
                        if (reader.IsValueSetIntAsStringValue(@"software\policies\microsoft\windows\Control Panel\Desktop", "ScreenSaveActive", out intvalue))
                        {
                            if (PSO == null)
                            {
                                PSO = new GPPSecurityPolicy();
                                PSO.GPOName = GPO.DisplayName;
                                PSO.GPOId = GPO.InternalName;
                                lock (healthcheckData.GPOScreenSaverPolicy)
                                {
                                    healthcheckData.GPOScreenSaverPolicy.Add(PSO);
                                }
                                PSO.Properties = new List<GPPSecurityPolicyProperty>();
                            }
                            PSO.Properties.Add(new GPPSecurityPolicyProperty("ScreenSaveActive", intvalue));
                        }

                        // https://technet.microsoft.com/en-us/library/cc959646.aspx
                        if (reader.IsValueSetIntAsStringValue(@"software\policies\microsoft\windows\Control Panel\Desktop", "ScreenSaverIsSecure", out intvalue))
                        {
                            if (PSO == null)
                            {
                                PSO = new GPPSecurityPolicy();
                                PSO.GPOName = GPO.DisplayName;
                                PSO.GPOId = GPO.InternalName;
                                lock (healthcheckData.GPOScreenSaverPolicy)
                                {
                                    healthcheckData.GPOScreenSaverPolicy.Add(PSO);
                                }
                                PSO.Properties = new List<GPPSecurityPolicyProperty>();
                            }
                            PSO.Properties.Add(new GPPSecurityPolicyProperty("ScreenSaverIsSecure", intvalue));
                        }
                    }

                    // search for certificates
                    foreach (string storename in new string[] { "Root", "CA", "Trust", "TrustedPeople", "TrustedPublisher" })
                    {
                        X509Certificate2Collection store = null;
                        if (reader.HasCertificateStore(storename, out store))
                        {
                            foreach (X509Certificate2 certificate in store)
                            {
                                HealthCheckCertificateData data = new HealthCheckCertificateData();
                                data.Source = "GPO:" + GPO.DisplayName + ";" + gpotarget;
                                data.Store = storename;
                                data.Certificate = certificate.GetRawCertData();
                                lock (healthcheckData.TrustedCertificates)
                                {
                                    healthcheckData.TrustedCertificates.Add(data);
                                }
                            }
                        }
                    }
                    foreach (RegistryPolRecord record in reader.SearchRecord(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run"))
                    {
                        if (record.Value == "**delvals.")
                            continue;
                        string filename = Encoding.Unicode.GetString(record.ByteValue).Trim();
                        if (string.IsNullOrEmpty(filename))
                            continue;
                        filename = filename.Replace("\0", string.Empty);
                        HealthCheckGPOLoginScriptData loginscript = new HealthCheckGPOLoginScriptData();
                        loginscript.GPOName = GPO.DisplayName;
                        loginscript.GPOId = GPO.InternalName;
                        loginscript.Action = "Logon";

                        // this is bad, I'm assuming that the file name doesn't contain any space which is wrong.
                        // but a real command line parsing will bring more anomalies.
                        var filePart = NativeMethods.SplitArgs(filename);
                        loginscript.Source = "Registry.pol (" + (gpotarget == "Machine" ? "Computer" : "User") + " section)";
                        loginscript.CommandLine = filePart[0];
                        if (loginscript.CommandLine.StartsWith("\\\\"))
                        {
                            loginscript.Delegation = CheckScriptPermission(domainInfo, loginscript.CommandLine);
                        }
                        if (filePart.Length > 1)
                        {
                            for (int i = 1; i < filePart.Length; i++)
                            {
                                if (i > 1)
                                    loginscript.Parameters += " ";
                                loginscript.Parameters += filePart[i];
                            }
                        }
                        lock (healthcheckData.GPOLoginScript)
                        {
                            healthcheckData.GPOLoginScript.Add(loginscript);
                        }
                    }
                }
            }
        }

        private KeyValuePair<SecurityIdentifier, string>? MatchesBadUsersToCheck(SecurityIdentifier sid)
        {
            if (sid.Value == "S-1-1-0")
            {
                return new KeyValuePair<SecurityIdentifier, string>(sid, "Everyone");
            }
            else if (sid.Value == "S-1-5-7")
            {
                return new KeyValuePair<SecurityIdentifier, string>(sid, "Anonymous");
            }
            else if (sid.Value == "S-1-5-11")
            {
                return new KeyValuePair<SecurityIdentifier, string>(sid, "Authenticated Users");
            }
            else if (sid.Value == "S-1-5-32-545")
            {
                return new KeyValuePair<SecurityIdentifier, string>(sid, "Users");
            }
            else if (sid.IsWellKnown(WellKnownSidType.AccountDomainGuestsSid) || sid.IsWellKnown(WellKnownSidType.AccountDomainUsersSid) || sid.IsWellKnown(WellKnownSidType.AuthenticatedUserSid))
            {
                try
                {
                    return new KeyValuePair<SecurityIdentifier, string>(sid, ((NTAccount)sid.Translate(typeof(NTAccount))).Value);
                }
                catch (Exception)
                {
                    return new KeyValuePair<SecurityIdentifier, string>(sid, sid.Value);
                }
            }
            return null;
        }

        private void ExtractGPODelegation(string path, GPO GPO)
        {
            if (!Directory.Exists(path))
                return;

            if (!Directory.Exists(path))
                return;
            var dirs = new List<string>(Directory.GetDirectories(path, "*", SearchOption.AllDirectories));
            dirs.Insert(0, path);
            foreach (var dirname in dirs)
            {
                try
                {
                    ExtractGPODelegationAnalyzeAccessControl(GPO, Directory.GetAccessControl(dirname), dirname, (path == dirname));
                }
                catch (Exception) { }
            }
            foreach (var filename in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    ExtractGPODelegationAnalyzeAccessControl(GPO, File.GetAccessControl(filename), filename, false);
                }
                catch (Exception) { }
            }
        }

        void ExtractGPODelegationAnalyzeAccessControl(GPO GPO, FileSystemSecurity security, string name, bool includeInherited)
        {
            foreach (var value in AnalyzeFileSystemSecurity(security, includeInherited))
            {
                healthcheckData.GPODelegation.Add(new GPODelegationData()
                    {
                        GPOName = GPO.DisplayName,
                        GPOId = GPO.InternalName,
                        Item = name,
                        Right = value.Key,
                        Account = value.Value
                    }
                );
            }
        }

        List<KeyValuePair<string, string>> AnalyzeFileSystemSecurity(FileSystemSecurity security, bool includeInherited)
        {
            var output = new List<KeyValuePair<string, string>>();
            var Owner = (SecurityIdentifier)security.GetOwner(typeof(SecurityIdentifier));
            var matchOwner = MatchesBadUsersToCheck(Owner);
            if (matchOwner.HasValue)
            {
                output.Add(new KeyValuePair<string, string>("Owner", matchOwner.Value.Value));
            }
            var accessRules = security.GetAccessRules(true, includeInherited, typeof(SecurityIdentifier));
            if (accessRules == null)
                return output;

            foreach (FileSystemAccessRule accessrule in accessRules)
            {
                if (accessrule.AccessControlType == AccessControlType.Deny)
                    continue;
                if ((FileSystemRights.Write & accessrule.FileSystemRights) == 0)
                    continue;

                var match = MatchesBadUsersToCheck((SecurityIdentifier)accessrule.IdentityReference);
                if (!match.HasValue)
                    continue;
                output.Add(new KeyValuePair<string, string>(accessrule.FileSystemRights.ToString(), match.Value.Value));
            }
            return output;
        }

        private void GetGPOList(ADDomainInfo domainInfo, ADWebService adws, Dictionary<string, GPO> GPOList)
        {
            string[] properties = new string[]
            {
                "distinguishedName",
                "name",
                "displayName",
                "flags"
            };

            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    string GPOName = x.Name.ToLowerInvariant();
                    if (!GPOList.ContainsKey(x.Name))
                        GPOList.Add(x.Name, new GPO()
                        {
                            InternalName = x.Name,
                            DisplayName = x.DisplayName,
                            IsDisabled = (x.Flags == 3),
                            DN = x.DistinguishedName,
                            AppliedTo = new List<string>()
                        });
                };

            adws.Enumerate(domainInfo.DefaultNamingContext, "(objectClass=groupPolicyContainer)", properties, callback);

            string[] GPproperties = new string[]
            {
                "distinguishedName",
                "gPLink"
            };

            WorkOnReturnedObjectByADWS callback2 =
                (ADItem x) =>
                {
                    foreach (var dn in x.GetApplicableGPO())
                    {
                        foreach (var gpo in GPOList.Keys)
                        {
                            if (string.Equals(GPOList[gpo].DN, dn, StringComparison.OrdinalIgnoreCase))
                            {
                                GPOList[gpo].AppliedTo.Add(x.DistinguishedName);
                                break;
                            }
                        }
                    }
                };

            adws.Enumerate(domainInfo.DefaultNamingContext, "(gPLink=*)", GPproperties, callback2);
        }

        private void ExtractGPOLoginScript(ADDomainInfo domainInfo, DirectoryInfo directoryInfo, GPO GPO)
        {
            foreach (string gpoType in new[] { "User", "Machine" })
            {
                foreach (string filename in new[] { "scripts.ini", "psscripts.ini" })
                {
                    string path = directoryInfo.FullName + "\\" + gpoType + "\\Scripts\\" + filename;
                    if (File.Exists(path))
                    {
                        try
                        {
                            ParseGPOLoginScript(domainInfo, path, GPO, gpoType, filename);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("Unable to parse " + GPO.DisplayName + " " + filename);
                            Trace.WriteLine("Exception " + ex.Message);
                            Trace.WriteLine(ex.StackTrace);
                        }
                    }
                }
            }
        }

        private void ParseGPOLoginScript(ADDomainInfo domainInfo, string path, GPO GPO, string gpoType, string filename)
        {
            StreamReader file = new System.IO.StreamReader(path);
            string line = null;
            int state = 0;
            Dictionary<string, string> logonscript = new Dictionary<string, string>();
            Dictionary<string, string> logoffscript = new Dictionary<string, string>();
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("[Logon]", StringComparison.InvariantCultureIgnoreCase))
                {
                    state = 1;
                }
                else if (line.StartsWith("[Logoff]", StringComparison.InvariantCultureIgnoreCase))
                {
                    state = 2;
                }
                else if (line.StartsWith("[", StringComparison.InvariantCultureIgnoreCase))
                {
                    state = 0;
                }
                else if (state > 0)
                {
                    int pos = line.IndexOf('=');
                    if (pos >= 1)
                    {
                        string key = line.Substring(0, pos).ToLowerInvariant();
                        string value = line.Substring(pos + 1).Trim();
                        if (state == 1)
                        {
                            logonscript[key] = value;
                        }
                        else if (state == 2)
                        {
                            logoffscript[key] = value;
                        }
                    }
                }
            }
            for (int i = 0;; i++)
            {
                if (!logonscript.ContainsKey(i + "cmdline"))
                {
                    break;
                }
                HealthCheckGPOLoginScriptData loginscript = new HealthCheckGPOLoginScriptData();
                loginscript.GPOName = GPO.DisplayName;
                loginscript.GPOId = GPO.InternalName;
                loginscript.Action = "Logon";
                loginscript.Source = filename + " (" + (gpoType == "Machine" ? "Computer" : "User") + " section)";
                loginscript.CommandLine = logonscript[i + "cmdline"];
                loginscript.Delegation = CheckScriptPermission(domainInfo, loginscript.CommandLine);
                if (logonscript.ContainsKey(i + "parameters"))
                {
                    loginscript.Parameters = logonscript[i + "parameters"];
                }
                lock (healthcheckData.GPOLoginScript)
                {
                    healthcheckData.GPOLoginScript.Add(loginscript);
                }
            }
            for (int i = 0;; i++)
            {
                if (!logoffscript.ContainsKey(i + "cmdline"))
                {
                    break;
                }
                HealthCheckGPOLoginScriptData loginscript = new HealthCheckGPOLoginScriptData();
                loginscript.GPOName = GPO.DisplayName;
                loginscript.GPOId = GPO.InternalName;
                loginscript.Action = "Logoff";
                loginscript.Source = filename + " (" + (gpoType == "Machine" ? "Computer" : "User") + " section)";
                loginscript.CommandLine = logoffscript[i + "cmdline"];
                loginscript.Delegation = CheckScriptPermission(domainInfo, loginscript.CommandLine);
                if (logoffscript.ContainsKey(i + "parameters"))
                {
                    loginscript.Parameters = logoffscript[i + "parameters"];
                }
                lock (healthcheckData.GPOLoginScript)
                {
                    healthcheckData.GPOLoginScript.Add(loginscript);
                }
            }
        }

        private void ExtractGPPFile(string path, GPO GPO, ADDomainInfo domainInfo, string UserOrComputer)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNodeList nodeList = doc.SelectNodes("/Files/File");
            foreach (XmlNode node in nodeList)
            {
                XmlNode action = node.SelectSingleNode("Properties/@action");
                if (action == null)
                    continue;
                if (action.Value == "D")
                    continue;
                XmlNode fromPath = node.SelectSingleNode("Properties/@fromPath");
                if (fromPath == null)
                    continue;
                if (!fromPath.Value.StartsWith("\\\\"))
                    continue;
                var file = new GPPFileDeployed();
                file.GPOName = GPO.DisplayName;
                file.GPOId = GPO.InternalName;
                file.Type = "Files (" + UserOrComputer + " section)";
                file.FileName = fromPath.Value;
                file.Delegation = new List<HealthCheckScriptDelegationData>();
                healthcheckData.GPPFileDeployed.Add(file);
                if (File.Exists(file.FileName))
                {
                    var ac = File.GetAccessControl(file.FileName);
                    foreach (var value in AnalyzeFileSystemSecurity(ac, true))
                    {
                        file.Delegation.Add(new HealthCheckScriptDelegationData()
                            {
                                Account = value.Value,
                                Right = value.Key
                            }
                        );
                    }
                }
            }
        }

        private void ExtractGPPPassword(string shortname, string fullname, GPO GPO, string alternateNameIfGPODoesNotExists)
        {
            string[] xpaths = null;
            string xpathUser = "Properties/@userName";
            string xpathNewName = null;
            switch (shortname)
            {
                case "groups.xml":
                    xpaths = new string[] { "/Groups/User" };
                    xpathNewName = "Properties/@newName";
                    break;
                case "services.xml":
                    xpaths = new string[] { "/NTServices/NTService" };
                    xpathUser = "Properties/@accountName";
                    break;
                case "scheduledtasks.xml":
                    xpaths = new string[] { "/ScheduledTasks/Task", "/ScheduledTasks/ImmediateTask", "/ScheduledTasks/TaskV2", "/ScheduledTasks/ImmediateTaskV2" };
                    xpathUser = "Properties/@runAs";
                    break;
                case "datasources.xml":
                    xpaths = new string[] { "/DataSources/DataSource" };
                    break;
                case "printers.xml":
                    xpaths = new string[] { "/Printers/SharedPrinter" };
                    break;
                case "drives.xml":
                    xpaths = new string[] { "/Drives/Drive" };
                    break;
                default:
                    return;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(fullname);
            foreach (string xpath in xpaths)
            {
                XmlNodeList nodeList = doc.SelectNodes(xpath);
                foreach (XmlNode node in nodeList)
                {
                    XmlNode password = node.SelectSingleNode("Properties/@cpassword");

                    // no password
                    if (password == null)
                        continue;

                    // password has been manually changed
                    if (string.IsNullOrEmpty(password.Value))
                        continue;
                    GPPPassword PasswordData = new GPPPassword();
                    PasswordData.GPOName = (GPO == null ? alternateNameIfGPODoesNotExists : GPO.DisplayName);
                    PasswordData.GPOId = (GPO == null ? null : GPO.InternalName);
                    PasswordData.Password = DecodeGPPPassword(password.Value);

                    XmlNode userNameNode = node.SelectSingleNode(xpathUser);
                    PasswordData.UserName = (userNameNode != null ? userNameNode.Value : string.Empty);

                    XmlNode changed = node.SelectSingleNode("@changed");
                    if (changed != null)
                    {
                        PasswordData.Changed = DateTime.Parse(changed.Value);
                    }
                    else
                    {
                        FileInfo fi = new FileInfo(fullname);
                        PasswordData.Changed = fi.LastWriteTime;
                    }
                    if (xpathNewName != null)
                    {
                        XmlNode newNameNode = node.SelectSingleNode(xpathNewName);
                        if (newNameNode != null && !string.IsNullOrEmpty(newNameNode.Value))
                        {
                            PasswordData.Other = "NewName:" + newNameNode.Value;
                        }
                    }
                    XmlNode pathNode = node.SelectSingleNode("Properties/@path");
                    if (pathNode != null && !string.IsNullOrEmpty(pathNode.Value))
                    {
                        PasswordData.Other = "Path:" + pathNode.Value;
                    }
                    PasswordData.Type = shortname;
                    lock (healthcheckData.GPPPassword)
                    {
                        healthcheckData.GPPPassword.Add(PasswordData);
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Ne pas supprimer d'objets plusieurs fois")]
        private string DecodeGPPPassword(string encryptedPassword)
        {
            byte[] aesKey =
            {
                0x4e, 0x99, 0x06, 0xe8, 0xfc, 0xb6, 0x6c, 0xc9, 0xfa, 0xf4, 0x93, 0x10, 0x62, 0x0f, 0xfe, 0xe8,
                0xf4, 0x96, 0xe8, 0x06, 0xcc, 0x05, 0x79, 0x90, 0x20, 0x9b, 0x09, 0xa4, 0x33, 0xb6, 0x6c, 0x1b
            };
            string decrypted = null;
            switch (encryptedPassword.Length % 4)
            {
                case 2:
                    encryptedPassword += "==";
                    break;
                case 3:
                    encryptedPassword += "=";
                    break;
            }
            byte[] buffer = Convert.FromBase64String(encryptedPassword);
            try
            {
                using (Rijndael aes = new RijndaelManaged())
                {
                    aes.Key = aesKey;
                    aes.IV = new byte[aes.IV.Length];
                    var transform = aes.CreateDecryptor();
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, transform, CryptoStreamMode.Write))
                        {
                            cs.Write(buffer, 0, buffer.Length);
                            cs.FlushFinalBlock();
                            decrypted = Encoding.Unicode.GetString(ms.ToArray());
                            cs.Close();
                            ms.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Unable to decrypt " + encryptedPassword);
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(ex.StackTrace);
                return encryptedPassword;
            }
            return decrypted;
        }

        private void ExtractGPPPrivilegePasswordLsaSettingEtc(string filename, GPO GPO, ADDomainInfo domainInfo)
        {
            using (StreamReader file = new System.IO.StreamReader(filename))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    SubExtractPrivilege(line, GPO, domainInfo);
                    SubExtractDCGPOPrivilege(line, GPO, domainInfo);
                    SubExtractLsaSettings(line, GPO);
                    SubExtractLsaSettingsBis(line, GPO);
                    SubExtractPasswordSettings(line, GPO);
                    SubExtractGroupMembership(line, GPO, domainInfo);
                    SubExtractSimpleAuditData(line, GPO);
                }
            }
        }

        private void SubExtractGroupMembership(string line, GPO GPO, ADDomainInfo domainInfo)
        {
            try
            {
                bool found = false;
                bool MemberOf = false;
                if (line.Contains("__MemberOf"))
                {
                    found = true;
                    MemberOf = true;
                }
                else if (line.Contains("__Members"))
                {
                    found = true;
                }
                if (!found)
                    return;
                int index = line.IndexOf("=");
                if (index < 0)
                    return;
                string rights = line.Substring(index + 1).TrimStart();
                if (string.IsNullOrEmpty(rights))
                    return;
                string left = line.Substring(0, line.IndexOf("__"));

                foreach (string right in rights.Split(','))
                {

                    string user1 = right.Trim();

                    // ignore some well known accounts to save space
                    if (user1.StartsWith("*S-1") && user1.EndsWith("-512"))
                        continue;
                    if (user1.StartsWith("*S-1") && user1.EndsWith("-510"))
                        continue;
                    if (user1 == "*S-1-1-0")
                    {
                        user1 = "Everyone";
                    }
                    else if (user1 == "*S-1-5-7")
                    {
                        user1 = "Anonymous";
                    }
                    else if (user1 == "*S-1-5-11")
                    {
                        user1 = "Authenticated Users";
                    }
                    else if (user1 == "*S-1-5-32-545")
                    {
                        user1 = "Users";
                    }
                    else if (user1.StartsWith("*S-1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        user1 = NativeMethods.ConvertSIDToName(user1.Substring(1), domainInfo.DnsHostName);
                    }
                    string user2 = left.Trim();
                    if (user2 == "*S-1-1-0")
                    {
                        user2 = "Everyone";
                    }
                    else if (user2 == "*S-1-5-7")
                    {
                        user2 = "Anonymous";
                    }
                    else if (user2 == "*S-1-5-11")
                    {
                        user2 = "Authenticated Users";
                    }
                    else if (user1 == "*S-1-5-32-545")
                    {
                        user1 = "Users";
                    }
                    else if (user2.StartsWith("*S-1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        user2 = NativeMethods.ConvertSIDToName(user2.Substring(1), domainInfo.DnsHostName);
                    }
                    GPOMembership membership = new GPOMembership();
                    healthcheckData.GPOLocalMembership.Add(membership);
                    membership.GPOName = GPO.DisplayName;
                    membership.GPOId = GPO.InternalName;
                    if (MemberOf)
                    {
                        membership.User = user2;
                        membership.MemberOf = user1;
                    }
                    else
                    {
                        membership.User = user1;
                        membership.MemberOf = user2;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception " + ex.Message + " while analysing membership of " + GPO.DisplayName);
            }
        }

        // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-gpsb/01f8e057-f6a8-4d6e-8a00-99bcd241b403
        private void SubExtractSimpleAuditData(string line, GPO GPO)
        {
            string[] AuditSettings = new string[]
            {
                "AuditSystemEvents",
                "AuditLogonEvents",
                "AuditPrivilegeUse",
                "AuditPolicyChange",
                "AuditAccountManage",
                "AuditProcessTracking",
                "AuditDSAccess",
                "AuditObjectAccess",
                "AuditAccountLogon"
            };
            foreach (string auditSetting in AuditSettings)
            {
                if (line.StartsWith(auditSetting, StringComparison.InvariantCultureIgnoreCase))
                {
                    int pos = line.IndexOf('=') + 1;
                    if (pos > 1)
                    {
                        var a = new GPOAuditSimpleData()
                        {
                            GPOId = GPO.InternalName,
                            GPOName = GPO.DisplayName,
                            Category = auditSetting,
                            Value = int.Parse(line.Substring(pos))
                        };
                        lock (healthcheckData.GPOAuditSimple)
                        {
                            healthcheckData.GPOAuditSimple.Add(a);
                        }
                    }
                }
            }
        }

        private void SubExtractPasswordSettings(string line, GPO GPO)
        {
            string[] PasswordSettings = new string[]
            {
                "MinimumPasswordAge",
                "MaximumPasswordAge",
                "MinimumPasswordLength",
                "PasswordComplexity",
                "PasswordHistorySize",
                "LockoutBadCount",
                "ResetLockoutCount",
                "LockoutDuration",

                //"RequireLogonToChangePassword",
                //"ForceLogoffWhenHourExpire",
                "ClearTextPassword"
            };

            foreach (string passwordSetting in PasswordSettings)
            {
                if (line.StartsWith(passwordSetting, StringComparison.InvariantCultureIgnoreCase))
                {
                    int pos = line.IndexOf('=') + 1;
                    if (pos > 1)
                    {
                        lock (healthcheckData.GPPPasswordPolicy)
                        {
                            GPPSecurityPolicy PSO = null;
                            foreach (GPPSecurityPolicy policy in healthcheckData.GPPPasswordPolicy)
                            {
                                if (policy.GPOId == GPO.InternalName)
                                {
                                    PSO = policy;
                                    break;
                                }
                            }
                            if (PSO == null)
                            {
                                PSO = new GPPSecurityPolicy();
                                PSO.GPOName = GPO.DisplayName;
                                PSO.GPOId = GPO.InternalName;
                                healthcheckData.GPPPasswordPolicy.Add(PSO);
                                PSO.Properties = new List<GPPSecurityPolicyProperty>();
                            }
                            int value = int.Parse(line.Substring(pos).Trim());
                            PSO.Properties.Add(new GPPSecurityPolicyProperty(passwordSetting, value));
                        }
                    }
                }
            }
        }

        private void SubExtractLsaSettings(string line, GPO GPO)
        {
            string[] LsaSettings = new string[]
            {
                @"EveryoneIncludesAnonymous",
                @"ForceGuest",
                @"LimitBlankPasswordUse",
                @"LmCompatibilityLevel",

                //@"NTLMMinClientSec",
                //@"NTLMMinServerSec",
                @"NoLMHash",
                @"RestrictAnonymous",
                @"RestrictAnonymousSAM"
            };
            if (line.StartsWith(@"MACHINE\System\CurrentControlSet\Control\Lsa\", StringComparison.InvariantCultureIgnoreCase))
            {
                int pos = line.IndexOf('=') + 1;
                if (pos > 1)
                {
                    string[] values = line.Substring(pos).Trim().Split(',');
                    if (values.Length == 2)
                    {
                        foreach (string lsasetting in LsaSettings)
                        {
                            if (line.ToLowerInvariant().Contains(lsasetting.ToLowerInvariant()))
                            {
                                int value = int.Parse(values[1]);

                                // eliminate false positive
                                if (lsasetting == "EveryoneIncludesAnonymous" && value == 0)
                                    continue;
                                if (lsasetting == "ForceGuest" && value == 0)
                                    continue;
                                if (lsasetting == "LimitBlankPasswordUse" && value == 1)
                                    continue;
                                if (lsasetting == "LmCompatibilityLevel" && (value == 3 || value == 5))
                                    continue;
                                if (lsasetting == "NoLMHash" && value == 1)
                                    continue;
                                if (lsasetting == "RestrictAnonymous" && value >= 1)
                                    continue;
                                if (lsasetting == "RestrictAnonymousSAM" && value == 1)
                                    continue;
                                AddGPOLsaPolicy(GPO, lsasetting, value);
                            }
                        }
                    }
                }
            }
            else if (line.StartsWith(@"MACHINE\Software\Microsoft\Windows NT\CurrentVersion\Setup\RecoveryConsole\SecurityLevel=4,1", StringComparison.InvariantCultureIgnoreCase))
            {
                AddGPOLsaPolicy(GPO, "recoveryconsole_securitylevel", 1);
            }
            else if (line.StartsWith(@"MACHINE\System\CurrentControlSet\Services\LDAP\LDAPClientIntegrity=4,0", StringComparison.InvariantCultureIgnoreCase))
            {
                AddGPOLsaPolicy(GPO, "LDAPClientIntegrity", 0);
            }
            else if (line.StartsWith(@"MACHINE\System\CurrentControlSet\Services\Netlogon\Parameters\RefusePasswordChange=4,1", StringComparison.InvariantCultureIgnoreCase))
            {
                AddGPOLsaPolicy(GPO, "RefusePasswordChange", 1);
            }
            else if (line.StartsWith(@"MACHINE\System\CurrentControlSet\Services\LanManServer\Parameters\EnableSecuritySignature=4,0", StringComparison.InvariantCultureIgnoreCase))
            {
                AddGPOLsaPolicy(GPO, "EnableSecuritySignature", 0);
            }
        }

        private void AddGPOLsaPolicy(GPO GPO, string setting, int value)
        {
            lock (healthcheckData.GPOLsaPolicy)
            {
                GPPSecurityPolicy PSO = null;
                foreach (GPPSecurityPolicy policy in healthcheckData.GPOLsaPolicy)
                {
                    if (policy.GPOId == GPO.InternalName)
                    {
                        PSO = policy;
                        break;
                    }
                }
                if (PSO == null)
                {
                    PSO = new GPPSecurityPolicy();
                    PSO.GPOName = GPO.DisplayName;
                    PSO.GPOId = GPO.InternalName;
                    healthcheckData.GPOLsaPolicy.Add(PSO);
                    PSO.Properties = new List<GPPSecurityPolicyProperty>();
                }
                PSO.Properties.Add(new GPPSecurityPolicyProperty(setting, value));
            }
        }

        private void SubExtractLsaSettingsBis(string line, GPO GPO)
        {
            string[] LsaSettings = new string[]
            {
                @"LSAAnonymousNameLookup",
                @"EnableGuestAccount"
            };
            foreach (string lsasetting in LsaSettings)
            {
                if (line.StartsWith(lsasetting, StringComparison.InvariantCultureIgnoreCase))
                {
                    int pos = line.IndexOf('=') + 1;
                    if (pos > 1)
                    {
                        int value = int.Parse(line.Substring(pos).Trim());
                        if (lsasetting == "EnableGuestAccount" && value == 0)
                            continue;
                        if (lsasetting == "LSAAnonymousNameLookup" && value == 0)
                            continue;
                        lock (healthcheckData.GPOLsaPolicy)
                        {
                            GPPSecurityPolicy PSO = null;
                            foreach (GPPSecurityPolicy policy in healthcheckData.GPOLsaPolicy)
                            {
                                if (policy.GPOId == GPO.InternalName)
                                {
                                    PSO = policy;
                                    break;
                                }
                            }
                            if (PSO == null)
                            {
                                PSO = new GPPSecurityPolicy();
                                PSO.GPOName = GPO.DisplayName;
                                PSO.GPOId = GPO.InternalName;
                                healthcheckData.GPOLsaPolicy.Add(PSO);
                                PSO.Properties = new List<GPPSecurityPolicyProperty>();
                            }
                            PSO.Properties.Add(new GPPSecurityPolicyProperty(lsasetting, value));
                        }
                    }
                }
            }
        }

        private void SubExtractPrivilege(string line, GPO GPO, ADDomainInfo domainInfo)
        {
            string[] privileges = new string[]
            {
                "SeBackupPrivilege",
                "SeCreateTokenPrivilege",
                "SeDebugPrivilege",
                "SeEnableDelegationPrivilege",
                "SeSyncAgentPrivilege",
                "SeTakeOwnershipPrivilege",
                "SeTcbPrivilege",
                "SeTrustedCredManAccessPrivilege",
                "SeMachineAccountPrivilege",
                "SeLoadDriverPrivilege",
                "SeRestorePrivilege",
                "SeImpersonatePrivilege",
                "SeAssignPrimaryTokenPrivilege",
                "SeSecurityPrivilege"
            };
            foreach (string privilege in privileges)
            {
                if (line.StartsWith(privilege, StringComparison.InvariantCultureIgnoreCase))
                {
                    int pos = line.IndexOf('=') + 1;
                    if (pos > 1)
                    {
                        string value = line.Substring(pos).Trim();
                        string[] values = value.Split(',');
                        foreach (string user in values)
                        {
                            var user2 = ConvertGPOUserToUserFriendlyUser(user, domainInfo);

                            // ignore empty privilege assignment
                            if (string.IsNullOrEmpty(user2))
                                continue;

                            GPPRightAssignment right = new GPPRightAssignment();
                            lock (healthcheckData.GPPRightAssignment)
                            {
                                healthcheckData.GPPRightAssignment.Add(right);
                            }
                            right.GPOName = GPO.DisplayName;
                            right.GPOId = GPO.InternalName;
                            right.Privilege = privilege;
                            right.User = user2;
                        }

                    }
                }
            }
        }

        private void SubExtractDCGPOPrivilege(string line, GPO GPO, ADDomainInfo domainInfo)
        {
            string[] privileges = new string[]
            {
                "SeInteractiveLogonRight",
                "SeRemoteInteractiveLogonRight",
                "SeNetworkLogonRight",
                "SeServiceLogonRight",
                "SeBatchLogonRight",
                "SeDenyServiceLogonRight",
                "SeDenyRemoteInteractiveLogonRight",
                "SeDenyNetworkLogonRight",
                "SeDenyInteractiveLogonRight",
                "SeDenyBatchLogonRight"
            };
            foreach (string privilege in privileges)
            {
                if (line.StartsWith(privilege, StringComparison.InvariantCultureIgnoreCase))
                {
                    int pos = line.IndexOf('=') + 1;
                    if (pos > 1)
                    {
                        string value = line.Substring(pos).Trim();
                        string[] values = value.Split(',');
                        foreach (string user in values)
                        {
                            var user2 = ConvertGPOUserToUserFriendlyUser(user, domainInfo);

                            // ignore empty privilege assignment
                            if (string.IsNullOrEmpty(user2))
                                continue;

                            GPPRightAssignment right = new GPPRightAssignment();
                            lock (healthcheckData.GPPLoginAllowedOrDeny)
                            {
                                healthcheckData.GPPLoginAllowedOrDeny.Add(right);
                            }
                            right.GPOName = GPO.DisplayName;
                            right.GPOId = GPO.InternalName;
                            right.Privilege = privilege;
                            right.User = user2;
                        }

                    }
                }
            }
        }

        private string ConvertGPOUserToUserFriendlyUser(string user, ADDomainInfo domainInfo)
        {
            /*// ignore well known sid
            // 
            if (user.StartsWith("*S-1-5-32-", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            // Local system
            if (user.StartsWith("*S-1-5-18", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            // SERVICE
            if (user.StartsWith("*S-1-5-6", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            // LOCAL_SERVICE
            if (user.StartsWith("*S-1-5-19", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            // NETWORK_SERVICE
            if (user.StartsWith("*S-1-5-20", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            */
            if (user == "*S-1-1-0")
            {
                return "Everyone";
            }
            else if (user == "*S-1-5-7")
            {
                return "Anonymous";
            }
            else if (user == "*S-1-5-11")
            {
                return "Authenticated Users";
            }
            else if (user == "*S-1-5-32-545")
            {
                return "Users";
            }
            else if (user == "*S-1-5-32-544")
            {
                return "Administrators";
            }
            else if (user.StartsWith("*S-1", StringComparison.InvariantCultureIgnoreCase))
            {
                if (user.EndsWith("-513"))
                {
                    return "Domain Users";
                }
                else if (user.EndsWith("-515"))
                {
                    return "Domain Computers";
                }
                else if (user.EndsWith("-512"))
                {
                    return "Domain Admins";
                }
                else
                {
                    return NativeMethods.ConvertSIDToName(user.Substring(1), domainInfo.DnsHostName);
                }
            }
            else
            {
                return user;
            }
        }

        private void ExtractGPOAudit(string path, GPO GPO, ADDomainInfo domainInfo)
        {
            try
            {
                using (TextReader tr = new StreamReader(path))
                {
                    // skip first line
                    string line = tr.ReadLine();
                    if (line == null)
                        return;
                    while ((line = tr.ReadLine()) != null)
                    {
                        var b = line.Split(',');
                        if (b.Length < 7)
                            continue;
                        var sub = b[3];
                        if (string.IsNullOrEmpty(sub))
                            continue;
                        var a = new GPOAuditAdvancedData()
                        {
                            GPOId = GPO.InternalName,
                            GPOName = GPO.DisplayName,
                            SubCategory = new Guid(sub),
                            Value = int.Parse(b[6])
                        };
                        lock (healthcheckData.GPOAuditAdvanced)
                        {
                            healthcheckData.GPOAuditAdvanced.Add(a);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception when analyzing GPO " + ex.Message);
            }
        }

        private void GeneratePSOData(ADDomainInfo domainInfo, ADWebService adws, NetworkCredential credential)
        {
            if (healthcheckData.GPPPasswordPolicy == null)
                healthcheckData.GPPPasswordPolicy = new List<GPPSecurityPolicy>();

            // adding the domain sid
            string[] properties = new string[]
            {
                "distinguishedName",
                "name",
                "msDS-MinimumPasswordAge",
                "msDS-MaximumPasswordAge",
                "msDS-MinimumPasswordLength",
                "msDS-PasswordComplexityEnabled",
                "msDS-PasswordHistoryLength",
                "msDS-LockoutThreshold",
                "msDS-LockoutObservationWindow",
                "msDS-LockoutDuration",
                "msDS-PasswordReversibleEncryptionEnabled"
            };

            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    GPPSecurityPolicy PSO = new GPPSecurityPolicy();
                    PSO.GPOName = "PSO:" + x.Name;
                    healthcheckData.GPPPasswordPolicy.Add(PSO);
                    PSO.Properties = new List<GPPSecurityPolicyProperty>();
                    PSO.Properties.Add(new GPPSecurityPolicyProperty("MinimumPasswordAge", (int)(x.msDSMinimumPasswordAge / -864000000000)));
                    if (x.msDSMaximumPasswordAge == -9223372036854775808)
                        PSO.Properties.Add(new GPPSecurityPolicyProperty("MaximumPasswordAge", -1));
                    else
                        PSO.Properties.Add(new GPPSecurityPolicyProperty("MaximumPasswordAge", (int)(x.msDSMaximumPasswordAge / -864000000000)));
                    PSO.Properties.Add(new GPPSecurityPolicyProperty("MinimumPasswordLength", x.msDSMinimumPasswordLength));
                    if (x.msDSPasswordComplexityEnabled)
                        PSO.Properties.Add(new GPPSecurityPolicyProperty("PasswordComplexity", 1));
                    else
                        PSO.Properties.Add(new GPPSecurityPolicyProperty("PasswordComplexity", 0));
                    PSO.Properties.Add(new GPPSecurityPolicyProperty("PasswordHistorySize", x.msDSPasswordHistoryLength));
                    PSO.Properties.Add(new GPPSecurityPolicyProperty("LockoutBadCount", x.msDSLockoutThreshold));
                    PSO.Properties.Add(new GPPSecurityPolicyProperty("ResetLockoutCount", (int)(x.msDSLockoutObservationWindow / -600000000)));
                    PSO.Properties.Add(new GPPSecurityPolicyProperty("LockoutDuration", (int)(x.msDSLockoutDuration / -600000000)));
                    if (x.msDSPasswordReversibleEncryptionEnabled)
                        PSO.Properties.Add(new GPPSecurityPolicyProperty("ClearTextPassword", 1));
                    else
                        PSO.Properties.Add(new GPPSecurityPolicyProperty("ClearTextPassword", 0));
                };

            adws.Enumerate(domainInfo.DefaultNamingContext, "(&(objectClass=msDS-PasswordSettings)(msDS-PSOAppliesTo=*))", properties, callback);
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        private void GenerateAnomalies(ADDomainInfo domainInfo, ADWebService adws)
        {

            healthcheckData.LastADBackup = DateTime.MaxValue;

            string[] propertiesRoot = new string[] { "distinguishedName", "replPropertyMetaData" };
            adws.Enumerate(domainInfo.DefaultNamingContext, "(objectClass=*)", propertiesRoot,
                (ADItem aditem) =>
                {
                    // check replication data for dsaSignature
                    if (aditem.ReplPropertyMetaData.ContainsKey(0x2004A))
                        healthcheckData.LastADBackup = aditem.ReplPropertyMetaData[0x2004A].LastOriginatingChange;

                }
                , "Base");

            string[] propertieskrbtgt = new string[] { "distinguishedName", "replPropertyMetaData", "pwdLastSet" };
            adws.Enumerate(domainInfo.DefaultNamingContext, "(objectSid=" + ADConnection.EncodeSidToString(domainInfo.DomainSid.Value + "-502") + ")", propertieskrbtgt,
                (ADItem aditem) =>
                {
                    Trace.WriteLine("krbtgt found");
                    healthcheckData.KrbtgtLastVersion = aditem.ReplPropertyMetaData[0x9005A].Version;
                    healthcheckData.KrbtgtLastChangeDate = aditem.PwdLastSet;
                    if (healthcheckData.KrbtgtLastChangeDate < aditem.ReplPropertyMetaData[0x9005A].LastOriginatingChange)
                    {
                        healthcheckData.KrbtgtLastChangeDate = aditem.ReplPropertyMetaData[0x9005A].LastOriginatingChange;
                    }
                }
            );

            healthcheckData.LAPSInstalled = DateTime.MaxValue;
            string[] propertiesLaps = new string[] { "whenCreated" };

            // note: the LDAP request does not contain ms-MCS-AdmPwd because in the old time, MS consultant was installing customized version of the attriute, * being replaced by the company name
            // check the oid instead ? (which was the same even if the attribute name was not)
            adws.Enumerate(domainInfo.SchemaNamingContext, "(name=ms-*-AdmPwd)", propertiesLaps, (ADItem aditem) =>
            {
                healthcheckData.LAPSInstalled = aditem.WhenCreated;
            }, "OneLevel");

            // adding the domain sid
            string[] propertiesAdminCount = new string[]
            {
                "distinguishedName",
                "name",
                "sAMAccountName",
                "whenCreated",
                "lastLogonTimestamp",
                "replPropertyMetaData"
            };

            List<string> privilegedUser = new List<string>();
            foreach (var member in healthcheckData.AllPrivilegedMembers)
            {
                privilegedUser.Add(member.DistinguishedName);
            }
            if (healthcheckData.ProtectedUsersNotPrivileged != null && healthcheckData.ProtectedUsersNotPrivileged.Members != null)
            {
                foreach (var member in healthcheckData.ProtectedUsersNotPrivileged.Members)
                {
                    privilegedUser.Add(member.DistinguishedName);
                }
            }

            healthcheckData.AdminSDHolderNotOK = new List<HealthCheckAccountDetailData>();

            WorkOnReturnedObjectByADWS callbackAdminSDHolder =
                (ADItem x) =>
                {
                    if (!privilegedUser.Contains(x.DistinguishedName))
                    {
                        // ignore honey pot accounts
                        if (healthcheckData.ListHoneyPot != null)
                        {
                            foreach (var u in healthcheckData.ListHoneyPot)
                            {
                                if (u.DistinguishedName == x.DistinguishedName)
                                    return;
                            }
                        }
                        var w = GetAccountDetail(x);
                        if (x.ReplPropertyMetaData.ContainsKey(589974))
                        {
                            w.Event = x.ReplPropertyMetaData[589974].LastOriginatingChange;
                        }
                        healthcheckData.AdminSDHolderNotOK.Add(w);
                    }
                };

            adws.Enumerate(domainInfo.DefaultNamingContext, "(&(objectClass=user)(objectCategory=person)(admincount=1)(!(userAccountControl:1.2.840.113556.1.4.803:=2))(!(sAMAccountName=krbtgt)))", propertiesAdminCount, callbackAdminSDHolder);
            healthcheckData.AdminSDHolderNotOKCount = healthcheckData.AdminSDHolderNotOK.Count;

            string[] smartCardNotOKProperties = new string[]
            {
                "distinguishedName",
                "name",
                "sAMAccountName",
                "whenCreated",
                "lastLogonTimestamp",
                "replPropertyMetaData"
            };

            // enumerates the account with the flag "smart card required" and not disabled
            healthcheckData.SmartCardNotOK = new List<HealthCheckAccountDetailData>();
            WorkOnReturnedObjectByADWS callbackSmartCard =
                (ADItem x) =>
                {
                    // apply a filter on the last nt hash change (attribute unicodePwd) via replication metadata
                    if (x.ReplPropertyMetaData != null && x.ReplPropertyMetaData.ContainsKey(589914)
                                                       && x.ReplPropertyMetaData[589914].LastOriginatingChange.AddDays(91) < DateTime.Now)
                    {
                        healthcheckData.SmartCardNotOK.Add(GetAccountDetail(x));
                    }
                };

            adws.Enumerate(domainInfo.DefaultNamingContext, "(&(objectCategory=person)(objectClass=user)(!(userAccountControl:1.2.840.113556.1.4.803:=2))(userAccountControl:1.2.840.113556.1.4.803:=262144))", smartCardNotOKProperties, callbackSmartCard);
            healthcheckData.SmartCardNotOKCount = healthcheckData.SmartCardNotOK.Count;

            string[] PreWin2000properties = new string[]
            {
                "distinguishedName",
                "member"
            };
            WorkOnReturnedObjectByADWS callbackPreWin2000 =
                (ADItem x) =>
                {
                    if (x.Member != null)
                    {
                        foreach (string member in x.Member)
                        {
                            if (member.Contains("S-1-5-7") || member.Contains("S-1-1-0"))
                            {
                                healthcheckData.PreWindows2000AnonymousAccess = true;
                                continue;
                            }
                            if (!member.StartsWith("CN=S-"))
                            {
                                healthcheckData.PreWindows2000NoDefault = true;
                                continue;
                            }
                        }
                    }
                };
            adws.Enumerate(domainInfo.DefaultNamingContext, "(distinguishedName=CN=Pre-Windows 2000 Compatible Access,CN=Builtin," + domainInfo.DefaultNamingContext + ")", PreWin2000properties, callbackPreWin2000);

            string[] DsHeuristicsproperties = new string[]
            {
                "distinguishedName",
                "dSHeuristics"
            };
            WorkOnReturnedObjectByADWS callbackdSHeuristics =
                (ADItem x) =>
                {
                    if (!string.IsNullOrEmpty(x.DSHeuristics))
                    {
                        if (x.DSHeuristics.Length >= 7 && x.DSHeuristics.Substring(6, 1) == "2")
                        {
                            healthcheckData.DsHeuristicsAnonymousAccess = true;
                        }
                        if (x.DSHeuristics.Length >= 8 && x.DSHeuristics.Substring(7, 1) != "0")
                        {
                            healthcheckData.DsHeuristicsAllowAnonNSPI = true;
                        }
                        if (x.DSHeuristics.Length >= 16 && x.DSHeuristics.Substring(15, 1) != "0")
                        {
                            healthcheckData.DsHeuristicsAdminSDExMaskModified = true;
                        }
                        if (x.DSHeuristics.Length >= 3 && x.DSHeuristics.Substring(2, 1) != "0")
                        {
                            healthcheckData.DsHeuristicsDoListObject = true;
                        }
                    }
                };
            adws.Enumerate(domainInfo.ConfigurationNamingContext, "(distinguishedName=CN=Directory Service,CN=Windows NT,CN=Services," + domainInfo.ConfigurationNamingContext + ")", DsHeuristicsproperties, callbackdSHeuristics);

            string[] SIDHistoryproperties = new string[]
            {
                "sAMAccountName"
            };
            WorkOnReturnedObjectByADWS callbackSIDHistory =
                (ADItem x) =>
                {
                    healthcheckData.SIDHistoryAuditingGroupPresent = true;
                };
            adws.Enumerate(domainInfo.DefaultNamingContext, "(sAMAccountName=" + domainInfo.NetBIOSName + "$$$)", SIDHistoryproperties, callbackSIDHistory);

            WorkOnReturnedObjectByADWS callbackDSQuota =
                (ADItem x) =>
                {
                    healthcheckData.MachineAccountQuota = x.DSMachineAccountQuota;
                };
            string[] DSQuotaproperties = new string[]
            {
                "ms-DS-MachineAccountQuota"
            };
            adws.Enumerate(domainInfo.DefaultNamingContext, "(&(objectClass=domain)(distinguishedName=" + domainInfo.DefaultNamingContext + "))", DSQuotaproperties, callbackDSQuota, "Base");

            WorkOnReturnedObjectByADWS callbackDomainControllers =
                (ADItem x) =>
                {
                    foreach (var DC in healthcheckData.DomainControllers)
                    {
                        if (string.Equals(DC.DistinguishedName, x.DistinguishedName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            DC.OwnerSID = x.NTSecurityDescriptor.GetOwner(typeof(SecurityIdentifier)).Value;
                            DC.OwnerName = NativeMethods.ConvertSIDToName(DC.OwnerSID, domainInfo.DnsHostName);
                            return;
                        }
                    }
                };
            string[] DCProperties = new string[]
            {
                "distinguishedName",
                "nTSecurityDescriptor"
            };
            foreach (var DC in healthcheckData.DomainControllers)
            {
                adws.Enumerate(domainInfo.DefaultNamingContext, "(distinguishedName=" + ADConnection.EscapeLDAP(DC.DistinguishedName) + ")", DCProperties, callbackDomainControllers);
            }

            string[] ExchangePrivEscProperties = new string[]
            {
                "distinguishedName",
                "nTSecurityDescriptor"
            };
            WorkOnReturnedObjectByADWS callbackExchangePrivEscProperties =
                (ADItem x) =>
                {
                    if (x.NTSecurityDescriptor != null)
                    {
                        foreach (ActiveDirectoryAccessRule rule in x.NTSecurityDescriptor.GetAccessRules(true, false, typeof(SecurityIdentifier)))
                        {
                            if (((rule.ActiveDirectoryRights & ActiveDirectoryRights.WriteDacl) != 0)
                                && (rule.ObjectType == new Guid("00000000-0000-0000-0000-000000000000"))
                                && rule.PropagationFlags == PropagationFlags.None)
                            {
                                string principal = NativeMethods.ConvertSIDToName(rule.IdentityReference.Value, domainInfo.DnsHostName);
                                if (principal.EndsWith("\\Exchange Windows Permissions"))
                                {

                                    healthcheckData.ExchangePrivEscVulnerable = true;
                                }
                            }
                        }
                    }
                };
            adws.Enumerate(domainInfo.DefaultNamingContext, "(objectClass=*)", ExchangePrivEscProperties, callbackExchangePrivEscProperties, "Base");

            // adding the domain sid
            string[] propertiesUnixPassword = new string[]
            {
                "distinguishedName",
                "name",
                "sAMAccountName",
                "whenCreated",
                "lastLogonTimestamp",
                "replPropertyMetaData"
            };

            healthcheckData.UnixPasswordUsers = new List<HealthCheckAccountDetailData>();

            WorkOnReturnedObjectByADWS callbackUnixPassword =
                (ADItem x) =>
                {
                    var w = GetAccountDetail(x);
                    if (x.ReplPropertyMetaData.ContainsKey(35)) // userPassword
                    {
                        w.Event = x.ReplPropertyMetaData[35].LastOriginatingChange;
                    }
                    if (x.ReplPropertyMetaData.ContainsKey(591734)) // unixUserPassword
                    {
                        w.Event = x.ReplPropertyMetaData[591734].LastOriginatingChange;
                    }
                    healthcheckData.UnixPasswordUsers.Add(w);
                };
            adws.Enumerate(domainInfo.DefaultNamingContext, "(|(unixUserPassword=*)(userPassword=*))", propertiesUnixPassword, callbackUnixPassword);
            healthcheckData.UnixPasswordUsersCount = healthcheckData.UnixPasswordUsers.Count;

        }

        private void GenerateDomainControllerData(ADDomainInfo domainInfo)
        {
            BlockingQueue<HealthCheckDomainController> queue = new BlockingQueue<HealthCheckDomainController>(200);
            int numberOfThread = 50;
            Thread[] threads = new Thread[numberOfThread];
            try
            {

                ThreadStart threadFunction = () =>
                {
                    for (;;)
                    {
                        HealthCheckDomainController DC = null;
                        if (!queue.Dequeue(out DC)) break;
                        string dns = DC.DCName + "." + domainInfo.DomainName;
                        DC.IP = new List<string>();
                        IPAddress[] addresses = null;
                        try
                        {
                            addresses = Dns.GetHostEntry(dns).AddressList;
                        }
                        catch (Exception)
                        {
                            Trace.WriteLine("Unable to resolve DC " + dns);
                            continue;
                        }
                        foreach (var address in addresses)
                        {
                            string addressString = address.ToString();
                            switch (addressString)
                            {
                                // avoid registering the loopback address
                                case "::1":
                                case "127.0.0.1":
                                    break;
                                default:
                                    DC.IP.Add(addressString);
                                    break;
                            }
                        }
                        DC.StartupTime = NativeMethods.GetStartupTime(dns);
                        if (DC.StartupTime == DateTime.MinValue)
                        {
                            // startup time could not be obtained - consider the DC as down
                        }
                        if (!SkipNullSession)
                        {
                            NullSessionTester session = new NullSessionTester(dns);
                            if (session.EnumerateAccount(1))
                            {
                                DC.HasNullSession = true;
                            }
                        }
                        SMBSecurityModeEnum securityMode;
                        if (SmbScanner.SupportSMB1(dns, out securityMode))
                        {
                            DC.SupportSMB1 = true;
                        }
                        DC.SMB1SecurityMode = securityMode;
                        if (SmbScanner.SupportSMB2And3(dns, out securityMode))
                        {
                            DC.SupportSMB2OrSMB3 = true;
                        }
                        DC.SMB2SecurityMode = securityMode;
                        if (!SkipNullSession)
                        {
                            DC.RemoteSpoolerDetected = SpoolerScanner.CheckIfTheSpoolerIsActive(dns);
                        }
                        DC.LDAPSProtocols = GenerateTLSConnectionInfo(dns);
                    }
                };

                // Consumers
                for (int i = 0; i < numberOfThread; i++)
                {
                    threads[i] = new Thread(threadFunction);
                    threads[i].Start();
                }

                foreach (HealthCheckDomainController DC in healthcheckData.DomainControllers)
                {
                    queue.Enqueue(DC);
                }
                queue.Quit();
                Trace.WriteLine("examining dc completed. Waiting for worker thread to complete");
                for (int i = 0; i < numberOfThread; i++)
                {
                    threads[i].Join();
                }
                Trace.WriteLine("Done testing null session");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception while generating null session Data: " + ex.Message);
                Trace.WriteLine(ex.StackTrace);
                lock (this)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Exception while generating null session Data: " + ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                }
            }
            finally
            {
                queue.Quit();
                for (int i = 0; i < numberOfThread; i++)
                {
                    if (threads[i] != null)
                        if (threads[i].ThreadState == System.Threading.ThreadState.Running)
                            threads[i].Abort();
                }
            }
            foreach (var DC in healthcheckData.DomainControllers)
            {
                if (DC.HasNullSession)
                    healthcheckData.DomainControllerWithNullSessionCount++;
            }
        }

        private List<string> GenerateTLSConnectionInfo(string dns)
        {
            var protocols = new List<string>();
            foreach (SslProtocols protocol in Enum.GetValues(typeof(SslProtocols)))
            {
                if (protocol == SslProtocols.None)
                    continue;
                if (protocol == SslProtocols.Default)
                    continue;
                try
                {
                    using (TcpClient client = new TcpClient(dns, 636))
                    {
                        using (SslStream sslstream = new SslStream(client.GetStream(), false, TlsValidationCallback, null))
                        {
                            Trace.WriteLine(protocol + " before auth for " + dns);
                            sslstream.AuthenticateAsClient(dns, null, protocol, false);
                            Trace.WriteLine(protocol + " supported for " + dns);
                            protocols.Add(protocol.ToString());
                        }
                    }
                }
                catch (SocketException)
                {
                    Trace.WriteLine("LDAPS not supported for " + dns);
                    return null;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(protocol + " not supported for " + dns + " (" + ex.Message + (ex.InnerException == null ? null : " - " + ex.InnerException.Message) + ")");
                }
            }
            return protocols;
        }

        private bool TlsValidationCallback(object sender, X509Certificate CACert, X509Chain CAChain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private void GenerateNetworkData(ADDomainInfo domainInfo, ADWebService adws)
        {
            string[] properties = new string[]
            {
                "distinguishedName",
                "name",
                "description",
                "location",
                "siteObjectBL"
            };
            healthcheckData.Sites = new List<HealthCheckSite>();
            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    var site = new HealthCheckSite();
                    site.SiteName = x.Name;
                    site.Description = x.Description;
                    site.Location = x.Location;
                    site.Networks = new List<string>();
                    if (x.SiteObjectBL != null)
                    {
                        foreach (var network in x.SiteObjectBL)
                        {
                            string networkString = network.Substring(3, network.IndexOf(',') - 3);

                            // avoid duplicate network (when replication conflicts occur)
                            if (!networkString.Contains("CNF"))
                            {
                                site.Networks.Add(networkString);
                            }
                        }
                    }
                    healthcheckData.Sites.Add(site);
                };

            adws.Enumerate(domainInfo.ConfigurationNamingContext, "(objectClass=site)", properties, callback);
        }

        private void GenerateRODCData(ADDomainInfo domainInfo, ADWebService adws)
        {

            string[] properties = new string[] { "distinguishedName", "member" };

            healthcheckData.DeniedRODCPasswordReplicationGroup = new List<string>();
            var members = new List<string>();
            adws.Enumerate(domainInfo.DefaultNamingContext, "(objectSid=" + ADConnection.EncodeSidToString(domainInfo.DomainSid.Value + "-572") + ")", properties,
                (ADItem aditem) =>
                {
                    Trace.WriteLine("CN=Denied RODC Password Replication Group found");
                    if (aditem.Member != null)
                    {
                        foreach (var member in aditem.Member)
                        {
                            members.Add(member);
                        }
                    }
                }
            );

            string[] properties2 = new string[] { "objectSid" };
            foreach (var member in members)
            {
                bool found = false;
                adws.Enumerate(domainInfo.DefaultNamingContext,
                    "(distinguishedName=" + member + ")",
                    properties2,
                    (ADItem aditem) =>
                    {
                        found = true;
                        healthcheckData.DeniedRODCPasswordReplicationGroup.Add(aditem.ObjectSid.Value);
                    }
                );
                if (!found)
                {
                    healthcheckData.DeniedRODCPasswordReplicationGroup.Add(member);
                }
            }

            healthcheckData.AllowedRODCPasswordReplicationGroup = new List<string>();
            adws.Enumerate(domainInfo.DefaultNamingContext, "(objectSid=" + ADConnection.EncodeSidToString(domainInfo.DomainSid.Value + "-571") + ")", properties,
                (ADItem aditem) =>
                {
                    Trace.WriteLine("CN=Allowed RODC Password Replication Group found");
                    if (aditem.Member != null)
                    {
                        foreach (var member in aditem.Member)
                        {
                            healthcheckData.AllowedRODCPasswordReplicationGroup.Add(member);
                        }
                    }
                }
            );

            var msDSRevealOnDemandGroup = new Dictionary<string, List<string>>();
            var msDSNeverRevealGroup = new Dictionary<string, List<string>>();

            var sidResolution = new Dictionary<string, string>();

            adws.Enumerate(domainInfo.DefaultNamingContext, "(primaryGroupID=521)", new string[] { "distinguishedName", "msDS-RevealedUsers", "msDS-RevealOnDemandGroup", "msDS-NeverRevealGroup" },
                (ADItem aditem) =>
                {
                    HealthCheckDomainController dc = null;
                    foreach (var d in healthcheckData.DomainControllers)
                    {
                        if (d.DistinguishedName == aditem.DistinguishedName)
                        {
                            dc = d;
                            break;
                        }
                    }
                    if (dc == null)
                        return;
                    dc.RODC = true;

                    dc.msDSRevealedUsers = new List<string>();
                    if (aditem.msDSRevealedUsers != null)
                    {
                        foreach (var u in aditem.msDSRevealedUsers)
                        {
                            var i = u.IndexOf(':');
                            var j = u.IndexOf(':', i + 1);
                            var k = u.IndexOf(':', j + 1);
                            var v = u.Substring(k + 1);
                            if (!dc.msDSRevealedUsers.Contains(v))
                                dc.msDSRevealedUsers.Add(v);
                        }
                    }
                    if (aditem.msDSRevealOnDemandGroup != null)
                        msDSRevealOnDemandGroup[aditem.DistinguishedName] = new List<string>(aditem.msDSRevealOnDemandGroup);
                    if (aditem.msDSNeverRevealGroup != null)
                        msDSNeverRevealGroup[aditem.DistinguishedName] = new List<string>(aditem.msDSNeverRevealGroup);
                }
            );
            foreach (var v in msDSRevealOnDemandGroup.Values)
                foreach (var w in v)
                {
                    if (!sidResolution.ContainsKey(w))
                        sidResolution[w] = null;
                }
            foreach (var v in msDSNeverRevealGroup.Values)
                foreach (var w in v)
                {
                    if (!sidResolution.ContainsKey(w))
                        sidResolution[w] = null;
                }
            foreach (var dn in new List<string>(sidResolution.Keys))
            {
                adws.Enumerate(domainInfo.DefaultNamingContext,
                    "(distinguishedName=" + dn + ")",
                    properties2,
                    (ADItem aditem) =>
                    {
                        sidResolution[dn] = aditem.ObjectSid.Value;
                    }
                );
            }
            foreach (var dc in healthcheckData.DomainControllers)
            {
                if (msDSRevealOnDemandGroup.ContainsKey(dc.DistinguishedName))
                {
                    dc.msDSRevealOnDemandGroup = new List<string>();
                    foreach (var u in msDSRevealOnDemandGroup[dc.DistinguishedName])
                    {
                        if (sidResolution.ContainsKey(u))
                            dc.msDSRevealOnDemandGroup.Add(sidResolution[u]);
                    }
                }
                if (msDSNeverRevealGroup.ContainsKey(dc.DistinguishedName))
                {
                    dc.msDSNeverRevealGroup = new List<string>();
                    foreach (var u in msDSNeverRevealGroup[dc.DistinguishedName])
                    {
                        if (sidResolution.ContainsKey(u))
                            dc.msDSNeverRevealGroup.Add(sidResolution[u]);
                    }
                }
            }

            //Search for RODC without Readonly flag on sysvol
            adws.Enumerate(domainInfo.DefaultNamingContext,
                "(&(msDFSR-ReadOnly=FALSE)(cn=SYSVOL Subscription))",
                new string[] { "distinguishedName" },
                (ADItem aditem) =>
                {
                    foreach (var dc in healthcheckData.DomainControllers)
                    {
                        if (aditem.DistinguishedName.EndsWith(dc.DistinguishedName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (dc.RODC)
                            {
                                dc.SYSVOLOverwrite = true;
                            }
                            break;
                        }
                    }
                }
            );
        }

        // this function has been designed to avoid LDAP query reentrance (to avoid the 5 connection limit)
        private void GenerateFSMOData(ADDomainInfo domainInfo, ADWebService adws)
        {
            //query the NTDS objects
            string[] properties = new string[]
            {
                "distinguishedName",
                "fSMORoleOwner"
            };

            var computerToQuery = new Dictionary<string, string>();
            string role = null;
            WorkOnReturnedObjectByADWS callback =
                (ADItem x) =>
                {
                    string DN = x.fSMORoleOwner;
                    if (DN.Contains("\0DEL:"))
                    {
                        Trace.WriteLine(DN + " FSMO Warning !");
                    }
                    string parent = DN.Substring(DN.IndexOf(",") + 1);
                    computerToQuery.Add(role, parent);
                };
            role = "PDC";
            adws.Enumerate(domainInfo.DefaultNamingContext, "(&(objectClass=domainDNS)(fSMORoleOwner=*))", properties, callback);
            role = "RID pool manager";
            adws.Enumerate(domainInfo.DefaultNamingContext, "(&(objectClass=rIDManager)(fSMORoleOwner=*))", properties, callback);
            role = "Infrastructure master";
            adws.Enumerate(domainInfo.DefaultNamingContext, "(&(objectClass=infrastructureUpdate)(fSMORoleOwner=*))", properties, callback);
            role = "Schema master";
            adws.Enumerate(domainInfo.SchemaNamingContext, "(&(objectClass=dMD)(fSMORoleOwner=*))", properties, callback);
            role = "Domain naming Master";
            adws.Enumerate(domainInfo.ConfigurationNamingContext, "(&(objectClass=crossRefContainer)(fSMORoleOwner=*))", properties, callback);

            foreach (var computerRole in computerToQuery.Keys)
            {
                string dns = null;
                WorkOnReturnedObjectByADWS computerCallback =
                    (ADItem x) =>
                    {
                        dns = x.DNSHostName;
                    };
                adws.Enumerate(domainInfo.ConfigurationNamingContext, "(distinguishedName=" + ADConnection.EscapeLDAP(computerToQuery[computerRole]) + ")", new string[] { "dnsHostName" }, computerCallback);

                if (string.IsNullOrEmpty(dns))
                {
                    Trace.WriteLine("Unable to get DNSHostName for " + computerToQuery[computerRole]);
                    continue;
                }
                HealthCheckDomainController theDC = null;
                foreach (var DC in healthcheckData.DomainControllers)
                {
                    if (string.Equals(DC.DCName + "." + domainInfo.DomainName, dns, StringComparison.OrdinalIgnoreCase))
                    {
                        theDC = DC;
                        break;
                    }
                }
                if (theDC == null)
                {
                    Trace.WriteLine("Unable to get DC for " + dns);
                    continue;
                }
                if (theDC.FSMO == null)
                    theDC.FSMO = new List<string>();
                theDC.FSMO.Add(computerRole);
            }
        }

        private void GenerateCheckFRS(ADDomainInfo domainInfo, ADWebService adws)
        {
            adws.Enumerate("CN=File Replication Service,CN=System," + domainInfo.DefaultNamingContext, "(objectClass=*)", new string[] { "distinguishedName" },
                (ADItem x) =>
                {
                    if (x.DistinguishedName.Contains(",CN=Domain System Volume (SYSVOL share),"))
                    {
                        healthcheckData.UsingNTFRSForSYSVOL = true;
                    }
                }
            );
        }

        private void GenerateCheckDCConfig(ADDomainInfo domainInfo, ADWebService adws)
        {
            foreach (var dc in healthcheckData.DomainControllers)
            {
                string dn = null;
                adws.Enumerate("CN=Sites," + domainInfo.ConfigurationNamingContext, "(&(ServerReference=" + dc.DistinguishedName + ")(objectClass=server))", new string[] { "distinguishedName" },
                    (ADItem x) =>
                    {
                        dn = x.DistinguishedName;
                    }
                );
                if (string.IsNullOrEmpty(dn))
                {
                    dc.RegistrationProblem += (string.IsNullOrEmpty(dc.RegistrationProblem) ? null : " ") + "NoConfiguration";
                }
                else
                {
                    bool NTDSNotOK = false;
                    adws.Enumerate("CN=Sites," + domainInfo.ConfigurationNamingContext, "(DistinguishedName=CN=NTDS Settings," + dn + ")", new string[] { "objectClass" },
                        (ADItem x) =>
                        {
                            if (string.Equals(x.Class, "nTDSDSA", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Class, "nTDSDSARO", StringComparison.OrdinalIgnoreCase))
                            {
                                NTDSNotOK = true;
                            }
                        }
                    );
                    if (!NTDSNotOK)
                        dc.RegistrationProblem += (string.IsNullOrEmpty(dc.RegistrationProblem) ? null : " ") + "NoNTDS";
                }

            }
        }

        private void GenerateDnsData(ADDomainInfo domainInfo, ADWebService adws)
        {
            if (!domainInfo.NamingContexts.Contains("DC=DomainDnsZones," + domainInfo.DefaultNamingContext))
            {
                Trace.WriteLine("No naming context for DC=DomainDnsZones," + domainInfo.DefaultNamingContext);
                return;
            }

            healthcheckData.DnsZones = new List<HealthCheckDnsZones>();
            var dn = "CN=MicrosoftDNS,DC=DomainDnsZones," + domainInfo.DefaultNamingContext;
            try
            {
                adws.Enumerate(dn, "(objectClass=dnsZone)", new string[] { "distinguishedName", "dnsProperty", "name" },
                    (ADItem x) =>
                    {
                        var o = new HealthCheckDnsZones();
                        o.name = x.Name;
                        foreach (var p in x.dnsProperty)
                        {
                            if (p.PropertyId == ADItem.DnsPropertyId.DSPROPERTY_ZONE_ALLOW_UPDATE)
                            {
                                if (p.Data.Length == 1 && p.Data[0] == 1)
                                {
                                    o.InsecureUpdate = true;
                                    break;
                                }
                            }
                        }
                        healthcheckData.DnsZones.Add(o);
                    }
                );
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Unable to get Dns Data");
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(ex.StackTrace);
            }
        }
    }
}