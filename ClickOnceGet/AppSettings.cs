using System.Configuration;

namespace ClickOnceGet
{
    [System.Diagnostics.DebuggerNonUserCodeAttribute]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
    public static class AppSettings
    {
        public static string ClientValidationEnabled
        {
            get { return ConfigurationManager.AppSettings["ClientValidationEnabled"]; }
        }

        public static class Key
        {
            public static string GitHub
            {
                get { return ConfigurationManager.AppSettings["Key.GitHub"]; }
            }

            public static string Microsoft
            {
                get { return ConfigurationManager.AppSettings["Key.Microsoft"]; }
            }
        }

        public static string Salt
        {
            get { return ConfigurationManager.AppSettings["Salt"]; }
        }

        public static class SourceCode
        {
            public static string FaviconURL
            {
                get { return ConfigurationManager.AppSettings["SourceCode.FaviconURL"]; }
            }

            public static string RepositoryURL
            {
                get { return ConfigurationManager.AppSettings["SourceCode.RepositoryURL"]; }
            }
        }

        public static string UnobtrusiveJavaScriptEnabled
        {
            get { return ConfigurationManager.AppSettings["UnobtrusiveJavaScriptEnabled"]; }
        }

        public static class Webpages
        {
            public static string Enabled
            {
                get { return ConfigurationManager.AppSettings["webpages:Enabled"]; }
            }

            public static string Version
            {
                get { return ConfigurationManager.AppSettings["webpages:Version"]; }
            }
        }
    }
}

