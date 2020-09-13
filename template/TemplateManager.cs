﻿//
// Copyright (c) Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace PingCastle.template
{
    public class TemplateManager
    {
        private static string LoadTemplate(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Stream stream = null;
            GZipStream gzip = null;
            string html = null;
            StreamReader reader = null;
            try
            {
                stream = assembly.GetManifestResourceStream(resourceName);
                gzip = new GZipStream(stream, CompressionMode.Decompress);
                reader = new StreamReader(gzip);
                html = reader.ReadToEnd();
            }
            finally
            {
                if (reader  != null)
                    reader.Dispose();
            }
            return html;
        }


        public static string LoadResponsiveTemplate()
        {
			return LoadTemplate(typeof(TemplateManager).Namespace + ".responsivetemplate.html.gz"); 
        }

		public static string LoadBootstrapCss()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".bootstrap.min.css.gz");
		}

		public static string LoadBootstrapJs()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".bootstrap.min.js.gz");
		}

		public static string LoadPopperJs()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".popper.min.js.gz");
		}

		public static string LoadJqueryJs()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".jquery.min.js.gz");
		}

		public static string LoadVisJs()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".vis.min.js.gz");
		}

		public static string LoadVisCss()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".vis.min.css.gz");
		}

		public static string LoadDatatableJs()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".dataTables.bootstrap4.min.js.gz");
		}

		public static string LoadDatatableCss()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".dataTables.bootstrap4.min.css.gz");
		}

		public static string LoadJqueryDatatableJs()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".jquery.dataTables.min.js.gz");
		}

		public static string LoadReportBaseCss()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".ReportBase.css.gz");
		}

		public static string LoadReportRiskControlsCss()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".ReportRiskControls.css.gz");
		}

		public static string LoadReportHealthCheckConsolidationCss()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".ReportHealthCheckConsolidation.css.gz");
		}

		public static string LoadReportHealthCheckRulesCss()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".ReportHealthCheckRules.css.gz");
		}

		public static string LoadReportCompromiseGraphCss()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".ReportCompromiseGraph.css.gz");
		}

		public static string LoadReportCompromiseGraphJs()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".ReportCompromiseGraph.js.gz");
		}

		public static string LoadReportMapBuilderCss()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".ReportMapBuilder.css.gz");
		}

		public static string LoadReportMapBuilderJs()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".ReportMapBuilder.js.gz");
		}

		public static string LoadReportNetworkMapCss()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".ReportNetworkMap.css.gz");
		}

		public static string LoadReportNetworkMapJs()
		{
			return LoadTemplate(typeof(TemplateManager).Namespace + ".ReportNetworkMap.js.gz");
		}
	}
}
