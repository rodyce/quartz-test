using System;
using System.Text.RegularExpressions;

namespace quartz_test.Util
{
    public static class Util
    {
        private const string EnvVarPattern = @"{{\.Env\.(\w+)}}";

        public static string ReplaceEnvVariables(this string template)
        {
            var envMatches = Regex.Matches(template, EnvVarPattern, RegexOptions.IgnoreCase);
            foreach (Match match in envMatches)
            {
                var envVarName = match.Groups[1].ToString();
                template = template.Replace(match.Value, Environment.GetEnvironmentVariable(envVarName));
            }
            return template;
        }
    }
}
