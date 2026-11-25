using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SimOpt.McpServer.Models;

namespace SimOpt.McpServer.Optimization
{
    /// <summary>
    /// SIM-140 (UN-015): a linear sum over the decision variables, the left side of a constraint on what a search
    /// may change ("at most 12 staff over triage and exam": <c>triage.servers + exam.servers</c>). Terms are a
    /// variable's path, a number, or a number times a path (<c>2 * exam.servers</c>), joined by + and -; a leading
    /// - negates the first term. A path is matched against the variables' own paths, longest first, so a path
    /// may contain any character a patch path does.
    /// </summary>
    public sealed class VariableExpression
    {
        private static readonly Regex Number = new(@"\G\d+(\.\d+)?([eE][+-]?\d+)?", RegexOptions.CultureInvariant);

        private VariableExpression(string text, double[] coefficients, double constant)
        {
            Text = text;
            Coefficients = coefficients;
            Constant = constant;
        }

        /// <summary>The expression as written, trimmed.</summary>
        public string Text { get; }

        /// <summary>The factor of each decision variable, in the order of the paths it was read against.</summary>
        public IReadOnlyList<double> Coefficients { get; }

        public double Constant { get; }

        /// <summary>The expression's value at <paramref name="point"/>, one value per variable.</summary>
        public double Evaluate(IReadOnlyList<double> point)
        {
            double sum = Constant;
            for (int i = 0; i < Coefficients.Count; i++) sum += Coefficients[i] * point[i];
            return sum;
        }

        /// <summary>The least and the greatest value the expression takes within the variables' bounds.</summary>
        public (double Least, double Greatest) Range(IReadOnlyList<DecisionVariable> variables)
        {
            double least = Constant, greatest = Constant;
            for (int i = 0; i < Coefficients.Count; i++)
            {
                double a = Coefficients[i] * variables[i].Min, b = Coefficients[i] * variables[i].Max;
                least += Math.Min(a, b);
                greatest += Math.Max(a, b);
            }
            return (least, greatest);
        }

        /// <summary>Reads <paramref name="text"/> against the variables' <paramref name="paths"/>.</summary>
        /// <exception cref="ArgumentException">The expression is empty or cannot be read; the message says where.</exception>
        public static VariableExpression Parse(string? text, IReadOnlyList<string> paths)
        {
            string s = (text ?? string.Empty).Trim();
            if (s.Length == 0) throw new ArgumentException("The expression is empty; write a sum of variable paths, e.g. \"triage.servers + exam.servers\".");
            List<(string Path, int Index)> byLength = paths.Select((p, i) => (p, i)).OrderByDescending(p => p.p.Length).ToList();
            var coefficients = new double[paths.Count];
            double constant = 0d, sign = 1d;
            bool term = true;
            int at = 0;
            string Where() => $"at \"{s.Substring(at)}\" in \"{s}\"";

            while (true)
            {
                while (at < s.Length && char.IsWhiteSpace(s[at])) at++;
                if (at == s.Length)
                {
                    if (term) throw new ArgumentException($"The expression \"{s}\" ends after an operator; it needs a term there.");
                    break;
                }
                char c = s[at];
                if (!term)
                {
                    if (c != '+' && c != '-')
                        throw new ArgumentException($"Expected + or - {Where()}; terms are joined by + and -.");
                    sign = c == '-' ? -1d : 1d;
                    term = true;
                    at++;
                    continue;
                }
                if (c == '+' || c == '-')
                {
                    if (at > 0) throw new ArgumentException($"Two operators in a row {Where()}.");
                    sign = c == '-' ? -1d : 1d;
                    at++;
                    continue;
                }

                Match number = Number.Match(s, at);
                if (number.Success)
                {
                    double factor = double.Parse(number.Value, CultureInfo.InvariantCulture);
                    at += number.Length;
                    int afterNumber = at;
                    while (at < s.Length && char.IsWhiteSpace(s[at])) at++;
                    if (at < s.Length && s[at] == '*')
                    {
                        at++;
                        while (at < s.Length && char.IsWhiteSpace(s[at])) at++;
                        if (at == s.Length) throw new ArgumentException($"The expression \"{s}\" ends after '*'; a variable path goes there.");
                        coefficients[Path(s, ref at, byLength, Where)] += sign * factor;
                    }
                    else
                    {
                        at = afterNumber;
                        constant += sign * factor;
                    }
                }
                else
                {
                    coefficients[Path(s, ref at, byLength, Where)] += sign;
                }
                term = false;
                sign = 1d;
            }
            return new VariableExpression(s, coefficients, constant);
        }

        /// <summary>The index of the variable whose path starts at <paramref name="at"/>, ending at a space, an operator or the end.</summary>
        private static int Path(string s, ref int at, List<(string Path, int Index)> byLength, Func<string> where)
        {
            foreach (var (path, index) in byLength)
            {
                int end = at + path.Length;
                if (path.Length > 0 && string.CompareOrdinal(s, at, path, 0, path.Length) == 0 &&
                    (end == s.Length || char.IsWhiteSpace(s[end]) || s[end] is '+' or '-' or '*'))
                {
                    at = end;
                    return index;
                }
            }
            int stop = at;
            while (stop < s.Length && !char.IsWhiteSpace(s[stop]) && s[stop] is not ('+' or '*')) stop++;
            string word = s.Substring(at, stop - at);
            throw new ArgumentException($"'{word}' is not a decision variable's path {where()}; " +
                                        $"the variables are {string.Join(", ", byLength.OrderBy(p => p.Index).Select(p => $"'{p.Path}'"))}.");
        }
    }
}
