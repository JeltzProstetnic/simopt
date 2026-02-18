using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MatthiasToolbox.Basics.Utilities
{
    public static class ExceptionHelper
    {
        #region with success flag

        #region no return value

        /// <summary>
        /// automate some try and retry proccess
        /// </summary>
        /// <param name="operationSuccessfulDelegate"></param>
        /// <param name="numberOfAttempts"></param>
        /// <param name="operationName"></param>
        /// <returns>A success flag</returns>
        public static bool RetryOperation(Func<bool> operationSuccessfulDelegate,
            int numberOfAttempts, out Exception ex)
        {
            ex = null;

            if (numberOfAttempts < 1) return true;

            bool success;
            int remainingAttempts = numberOfAttempts - 1;

            // first attempt
            try
            {
                success = operationSuccessfulDelegate.Invoke();
            }
            catch (Exception e)
            {
                ex = e;
                success = false;
            }

            if (success || numberOfAttempts < 2) return success;

            // first attempt failed
            while (remainingAttempts > 0)
            {
                remainingAttempts -= 1;

                try
                {
                    success = operationSuccessfulDelegate.Invoke();
                }
                catch (Exception e)
                {
                    ex = e;
                    success = false;
                }

                if (success) break;
            }

            return success;
        }

        /// <summary>
        /// automate some try and retry proccess
        /// </summary>
        /// <param name="operationSuccessfulDelegate"></param>
        /// <param name="numberOfAttempts"></param>
        /// <param name="operationName"></param>
        /// <returns>A success flag</returns>
        public static bool RetryOperationDelayed(Func<bool> operationSuccessfulDelegate,
            int numberOfAttempts, TimeSpan repeatDelay, out Exception ex)
        {
            ex = null;
            
            if (numberOfAttempts < 1) return true;

            bool success;
            int remainingAttempts = numberOfAttempts - 1;

            // first attempt
            try
            {
                success = operationSuccessfulDelegate.Invoke();
            }
            catch (Exception e)
            {
                ex = e;
                success = false;
            }

            if (success || numberOfAttempts < 2) return success;

            // first attempt failed
            while (remainingAttempts > 0)
            {
                remainingAttempts -= 1;

                try
                {
                    Thread.Sleep(repeatDelay);
                    success = operationSuccessfulDelegate.Invoke();
                }
                catch (Exception e)
                {
                    ex = e;
                    success = false;
                }

                if (success) break;
            }

            return success;
        }

        #endregion
        #region for return value with Tuple result

        ///// <summary>
        ///// automate some try and retry proccess
        ///// </summary>
        ///// <param name="operationDelegate">Must return a success flag and a result.</param>
        ///// <param name="numberOfAttempts"></param>
        ///// <param name="operationName"></param>
        ///// <returns>A success flag and the result of the operation.</returns>
        //public static Tuple<bool, T> RetryOperation<T>(Func<Tuple<bool, T>> operationDelegate,
        //    int numberOfAttempts, out Exception ex)
        //{
        //    ex = null;

        //    if (numberOfAttempts < 1) return new Tuple<bool, T>(true, default(T));

        //    bool success;
        //    Tuple<bool, T> tmp;
        //    T result = default(T);
        //    int remainingAttempts = numberOfAttempts - 1;

        //    // first attempt
        //    try
        //    {

        //        tmp = operationDelegate.Invoke();
        //        success = tmp.Item1;
        //        result = tmp.Item2;
        //    }
        //    catch (Exception e)
        //    {
        //        ex = e;
        //        success = false;
        //    }

        //    if (success || numberOfAttempts < 2) return new Tuple<bool, T>(success, result);

        //    // first attempt failed
        //    while (remainingAttempts > 0)
        //    {
        //        remainingAttempts -= 1;

        //        try
        //        {
        //            tmp = operationDelegate.Invoke();
        //            success = tmp.Item1;
        //            result = tmp.Item2;
        //        }
        //        catch (Exception e)
        //        {
        //            ex = e;
        //            success = false;
        //        }

        //        if (success) break;
        //    }

        //    return new Tuple<bool, T>(success, result);
        //}

        ///// <summary>
        ///// automate some try and retry proccess
        ///// </summary>
        ///// <param name="operationDelegate">Must return a success flag and a result.</param>
        ///// <param name="numberOfAttempts"></param>
        ///// <param name="operationName"></param>
        ///// <returns>A success flag and the result of the operation.</returns>
        //public static Tuple<bool, T> RetryOperationDelayed<T>(Func<Tuple<bool, T>> operationDelegate,
        //    int numberOfAttempts, TimeSpan repeatDelay, out Exception ex)

        //    {
        //    ex = null;

        //    if (numberOfAttempts < 1) return new Tuple<bool, T>(true, default(T));

        //    bool success;
        //    Tuple<bool, T> tmp;
        //    T result = default(T);
        //    int remainingAttempts = numberOfAttempts - 1;

        //    // first attempt
        //    try
        //    {

        //        tmp = operationDelegate.Invoke();
        //        success = tmp.Item1;
        //        result = tmp.Item2;
        //    }
        //    catch (Exception e)
        //    {
        //        ex = e;
        //        success = false;
        //    }

        //    if (success || numberOfAttempts < 2) return new Tuple<bool, T>(success, result);

        //    // first attempt failed
        //    while (remainingAttempts > 0)
        //    {
        //        remainingAttempts -= 1;

        //        try
        //        {
        //            Thread.Sleep(repeatDelay);
        //            tmp = operationDelegate.Invoke();
        //            success = tmp.Item1;
        //            result = tmp.Item2;
        //        }
        //        catch (Exception e)
        //        {
        //            ex = e;
        //            success = false;
        //        }

        //        if (success) break;
        //    }

        //    return new Tuple<bool, T>(success, result);
        //}

        #endregion
        #region for return value with "out" result

        ///// <summary>
        ///// automate some try and retry proccess
        ///// </summary>
        ///// <param name="operationDelegate">Must return a success flag and a result.</param>
        ///// <param name="numberOfAttempts"></param>
        ///// <param name="operationName"></param>
        ///// <returns>A success flag and the result of the operation.</returns>
        //public static bool RetryOperation<T>(Func<Tuple<bool, T>> operationDelegate,
        //    int numberOfAttempts, out T result, out Exception ex)
        //{
        //    ex = null;
        //    result = default(T);

        //    if (numberOfAttempts < 1) return true;

        //    bool success;
        //    Tuple<bool, T> tmp;
        //    int remainingAttempts = numberOfAttempts - 1;

        //    // first attempt
        //    try
        //    {

        //        tmp = operationDelegate.Invoke();
        //        success = tmp.Item1;
        //        result = tmp.Item2;
        //    }
        //    catch (Exception e)
        //    {
        //        ex = e;
        //        success = false;
        //    }

        //    if (success || numberOfAttempts < 2) return success;

        //    // first attempt failed
        //    while (remainingAttempts > 0)
        //    {
        //        remainingAttempts -= 1;

        //        try
        //        {
        //            tmp = operationDelegate.Invoke();
        //            success = tmp.Item1;
        //            result = tmp.Item2;
        //        }
        //        catch (Exception e)
        //        {
        //            ex = e;
        //            success = false;
        //        }

        //        if (success) break;
        //    }

        //    return success;
        //}

        ///// <summary>
        ///// automate some try and retry proccess
        ///// </summary>
        ///// <param name="operationDelegate">Must return a success flag and a result.</param>
        ///// <param name="numberOfAttempts"></param>
        ///// <param name="operationName"></param>
        ///// <returns>A success flag and the result of the operation.</returns>
        //public static bool RetryOperationDelayed<T>(Func<Tuple<bool, T>> operationDelegate,
        //    int numberOfAttempts, TimeSpan repeatDelay, out T result, out Exception ex)
        //{
        //    ex = null;
        //    result = default(T);

        //    if (numberOfAttempts < 1) return true;

        //    bool success;
        //    Tuple<bool, T> tmp;
        //    int remainingAttempts = numberOfAttempts - 1;

        //    // first attempt
        //    try
        //    {

        //        tmp = operationDelegate.Invoke();
        //        success = tmp.Item1;
        //        result = tmp.Item2;
        //    }
        //    catch (Exception e)
        //    {
        //        ex = e;
        //        success = false;
        //    }

        //    if (success || numberOfAttempts < 2) return success;

        //    // first attempt failed
        //    while (remainingAttempts > 0)
        //    {
        //        remainingAttempts -= 1;

        //        try
        //        {
        //            Thread.Sleep(repeatDelay);
        //            tmp = operationDelegate.Invoke();
        //            success = tmp.Item1;
        //            result = tmp.Item2;
        //        }
        //        catch (Exception e)
        //        {
        //            ex = e;
        //            success = false;
        //        }

        //        if (success) break;
        //    }

        //    return success;
        //}

        #endregion
        
        #endregion
        #region without success flag

        #region for return value with "out" result

        /// <summary>
        /// automate some try and retry proccess
        /// </summary>
        /// <param name="operationDelegate">Must return a success flag and a result.</param>
        /// <param name="numberOfAttempts"></param>
        /// <param name="operationName"></param>
        /// <returns>A success flag and the result of the operation.</returns>
        public static bool RetryOperation<T>(Func<T> operationDelegate,
            int numberOfAttempts, out T result, out Exception ex)
        {
            ex = null;
            result = default(T);

            if (numberOfAttempts < 1) return true;

            bool success;
            int remainingAttempts = numberOfAttempts - 1;

            // first attempt
            try
            {
                result = operationDelegate.Invoke();
                success = true;
            }
            catch (Exception e)
            {
                ex = e;
                success = false;
            }

            if (success || numberOfAttempts < 2) return success;

            // first attempt failed
            while (remainingAttempts > 0)
            {
                remainingAttempts -= 1;

                try
                {
                    result = operationDelegate.Invoke();
                    success = true;
                }
                catch (Exception e)
                {
                    ex = e;
                    success = false;
                }

                if (success) break;
            }

            return success;
        }

        /// <summary>
        /// automate some try and retry proccess
        /// </summary>
        /// <param name="operationDelegate">Must return a success flag and a result.</param>
        /// <param name="numberOfAttempts"></param>
        /// <param name="operationName"></param>
        /// <returns>A success flag and the result of the operation.</returns>
        public static bool RetryOperationDelayed<T>(Func<T> operationDelegate,
            int numberOfAttempts, TimeSpan repeatDelay, out T result, out Exception ex)
        {
            ex = null;
            result = default(T);

            if (numberOfAttempts < 1) return true;

            bool success;
            int remainingAttempts = numberOfAttempts - 1;

            // first attempt
            try
            {

                result = operationDelegate.Invoke();
                success = true;
            }
            catch (Exception e)
            {
                ex = e;
                success = false;
            }

            if (success || numberOfAttempts < 2) return success;

            // first attempt failed
            while (remainingAttempts > 0)
            {
                remainingAttempts -= 1;

                try
                {
                    Thread.Sleep(repeatDelay);
                    result = operationDelegate.Invoke();
                    success = true;
                }
                catch (Exception e)
                {
                    ex = e;
                    success = false;
                }

                if (success) break;
            }

            return success;
        }

        #endregion

        #endregion
    }
}
