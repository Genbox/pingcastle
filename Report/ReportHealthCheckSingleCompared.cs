﻿using PingCastle.HealthCheck;

namespace PingCastle.Report
{
	public class ReportHealthCheckSingleCompared : ReportHealthCheckSingle
	{
		HealthCheckData[] Reports;

		public string GenerateRawContent(HealthCheckData[] reports, ADHealthCheckingLicense aDHealthCheckingLicense)
		{
			_license = aDHealthCheckingLicense;
			Reports = reports;
			reports[0].InitializeReportingData();
			reports[1].InitializeReportingData();
			Report = Reports[0];
			sb.Length = 0;
			GenerateContent();
			return sb.ToString();
		}

		protected override void GenerateSection(string title, GenerateContentDelegate generateContent)
		{
			Report = Reports[0];
			base.GenerateSection(title + " - " + Report.GenerationDate.ToString("u"), generateContent);
			Report = Reports[1];
			base.GenerateSection(title + " - " + Report.GenerationDate.ToString("u"), generateContent);
		}
	}
}
