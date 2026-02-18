using System;
using System.Collections.Generic;
using System.Linq;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;

namespace MatthiasToolbox.Mathematics.Stochastics
{
    public static class Extensions
    {
        #region Random

        /// <summary>
        /// Executes the given Action only with the given probability.
        /// </summary>
        /// <param name="rnd">a random generator</param>
        /// <param name="todo">the action to execute conditionally</param>
        /// <param name="probability">a number between 0 and 1</param>
        /// <returns>true if the action was executed</returns>
        public static bool ExecuteConditionally(this Random rnd, Action todo, double probability)
        {
            if (rnd.NextDouble() <= probability)
            {
                todo.Invoke();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a number between 0 and probabilities.Count depending on the given probabilities.
        /// </summary>
        /// <param name="rnd">a random generator</param>
        /// <param name="probabilities">The probabilities associated with each index.</param>
        /// <returns>
        /// The zero based index which was actually chosen or -1 if none of the
        /// indices was chosen (possible only if the probabilities sum up to less than 100%).
        /// </returns>
        public static int ChooseIndex(this Random rnd, List<double> probabilities)
        {
            if (probabilities.Sum() > 1d) throw new ArgumentException("The probabilities must not sum up to more then 100% (= 1.0).");

            double randomNumber = rnd.Next();
            double tmp = 0;
            int counter = 0;

            foreach (double p in probabilities)
            {
                tmp += p;
                if (randomNumber <= tmp) return counter;
                counter += 1;
            }
            return -1;
        }

        /// <summary>
        /// Executes the given Actions only with the given probabilities.
        /// </summary>
        /// <param name="rnd">a random generator</param>
        /// <param name="todos">The actions with their probabilities.</param>
        /// <returns>
        /// The zero based index of the action which was actually executed or -1 if none of the
        /// actions were executed (possible only if the probabilities sum up to less than 100%).
        /// </returns>
        public static int ExecuteConditionally(this Random rnd, Dictionary<Action, double> todos)
        {
            if (todos.Values.Sum() > 1d) throw new ArgumentException("The probabilities must not sum up to more then 100% (= 1.0).");

            double randomNumber = rnd.NextDouble();
            double tmp = 0;
            int counter = 0;

            // foreach (double p in probabilities)
            foreach(KeyValuePair<Action, double> kvp in todos)
            {
                tmp += kvp.Value;
                if (randomNumber <= tmp)
                {
                    kvp.Key.Invoke();
                    return counter;
                }
                counter += 1;
            }
            return -1;
        }

        /// <summary>
        /// Returns an item from the list according to the given probability list.
        /// </summary>
        /// <param name="rnd">a random generator</param>
        /// <param name="todos">The return values with their probabilities.</param>
        /// <returns>
        /// </returns>
        public static T ReturnConditionally<T>(this Random rnd, List<double> probabilities, List<T> results)
        {
            if (probabilities.Sum() > 1d) throw new ArgumentException("The probabilities must not sum up to more then 100% (= 1.0).");
            if (probabilities.Count != results.Count) throw new ArgumentException("You must provide a result value for each probability value.");

            double randomNumber = rnd.Next();
            double tmp = 0;
            int counter = 0;

            for (int i = 0; i <= probabilities.Count; i++)
            {
                tmp += probabilities[i];
                if (randomNumber <= tmp)
                {
                    return results[i];
                }
                counter += 1;
            }

            return default(T);
        }

        #endregion
        #region Random<double>

        /// <summary>
        /// Executes the given Action only with the given probability. Caution: the random 
        /// distribution must be uniform, otherwise the probability will be distorted!
        /// </summary>
        /// <param name="rnd">a random generator</param>
        /// <param name="todo">the action to execute conditionally</param>
        /// <param name="probability">a number between 0 and 1</param>
        /// <returns>true if the action was executed</returns>
        public static bool ExecuteConditionally(this IRandomSource rnd, Action todo, double probability)
        {
            if (rnd.NextDouble() <= probability)
            {
                todo.Invoke();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a number between 0 and probabilities.Count depending on the given probabilities.
        /// </summary>
        /// <param name="rnd">a random generator</param>
        /// <param name="probabilities">The probabilities associated with each index.</param>
        /// <returns>
        /// The zero based index which was actually chosen or -1 if none of the
        /// indices was chosen (possible only if the probabilities sum up to less than 100%).
        /// </returns>
        public static int ChooseIndex(this IRandomSource rnd, List<double> probabilities)
        {
            if (probabilities.Sum() > 1d) throw new ArgumentException("The probabilities must not sum up to more then 100% (= 1.0).");

            double randomNumber = rnd.NextDouble();
            double tmp = 0;
            int counter = 0;

            foreach (double p in probabilities)
            {
                tmp += p;
                if (randomNumber <= tmp) return counter;
                counter += 1;
            }
            return -1;
        }

        /// <summary>
        /// Executes the given Actions only with the given probabilities.
        /// </summary>
        /// <param name="rnd">a random generator</param>
        /// <param name="todos">The actions with their probabilities.</param>
        /// <returns>
        /// The zero based index of the action which was actually executed or -1 if none of the
        /// actions were executed (possible only if the probabilities sum up to less than 100%).
        /// </returns>
        public static int ExecuteConditionally(this IRandomSource rnd, Dictionary<Action, double> todos)
        {
            if (todos.Values.Sum() > 1d) throw new ArgumentException("The probabilities must not sum up to more then 100% (= 1.0).");

            double randomNumber = rnd.NextDouble();
            double tmp = 0;
            int counter = 0;

            foreach (KeyValuePair<Action, double> kvp in todos)
            {
                tmp += kvp.Value;
                if (randomNumber <= tmp)
                {
                    kvp.Key.Invoke();
                    return counter;
                }
                counter += 1;
            }
            return -1;
        }

        /// <summary>
        /// Returns an item from the list according to the given probability list.
        /// </summary>
        /// <param name="rnd">a random generator</param>
        /// <param name="todos">The return values with their probabilities.</param>
        /// <returns>
        /// </returns>
        public static T ReturnConditionally<T>(this IRandomSource rnd, List<double> probabilities, List<T> results)
        {
            if (probabilities.Sum() > 1d) throw new ArgumentException("The probabilities must not sum up to more then 100% (= 1.0).");
            if (probabilities.Count != results.Count) throw new ArgumentException("You must provide a result value for each probability value.");

            double randomNumber = rnd.NextDouble();
            double tmp = 0;
            int counter = 0;

            for (int i = 0; i <= probabilities.Count; i++)
            {
                tmp += probabilities[i];
                if (randomNumber <= tmp)
                {
                    return results[i];
                }
                counter += 1;
            }

            return default(T);
        }

        #endregion
        #region List

        /// <summary>
        /// Select a random list item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="rnd"></param>
        /// <returns>A randomly selected item from the list.</returns>
        public static T RandomItem<T>(this List<T> source, Random rnd)
        {
            return source[(int)(rnd.NextDouble() * source.Count)];
        }

        /// <summary>
        /// Select a random list item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="rnd"></param>
        /// <returns>A randomly selected item from the list.</returns>
        public static T RandomItem<T>(this List<T> source, Random<double> rnd)
        {
            return source[(int)(rnd.Next() * source.Count)];
        }

        /// <summary>
        /// Select a random list item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="rnd"></param>
        /// <returns>A randomly selected item from the list.</returns>
        public static T RandomItem<T>(this List<T> source, IRandomSource rnd)
        {
            return source[(int)(rnd.NextDouble() * source.Count)];
        }

        /// <summary>
        /// Returns an item from the list according to the given probability list.
        /// </summary>
        /// <param name="rnd">a random generator</param>
        /// <param name="todos">The return values with their probabilities.</param>
        /// <returns>
        /// </returns>
        public static T RandomItem<T>(this List<T> source, List<double> probabilities, IRandomSource rnd)
        {
            if (probabilities.Sum() > 1d) throw new ArgumentException("The probabilities must not sum up to more then 100% (= 1.0).");
            if (probabilities.Count != source.Count) throw new ArgumentException("You must provide a probability value for each list item.");

            double randomNumber = rnd.NextDouble();
            double tmp = 0;
            int counter = 0;

            for (int i = 0; i <= probabilities.Count; i++)
            {
                tmp += probabilities[i];
                if (randomNumber <= tmp)
                {
                    return source[i];
                }
                counter += 1;
            }

            return default(T);
        }

        #endregion
    }
}