﻿using System;
using System.Threading.Tasks;

namespace EasyNetQ
{
    public static class TaskHelpers
    {
        public static Task ExecuteSynchronously(Action action)
        {
            var tcs = new TaskCompletionSource<NullStruct>();
            try
            {
                action();
                tcs.SetResult(new NullStruct());
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }

            return tcs.Task;
        }

        public static Task FromException(Exception ex)
        {
            var tcs = new TaskCompletionSource<NullStruct>();
            tcs.SetException(ex);

            return tcs.Task;
        }

        //http://blogs.msdn.com/b/pfxteam/archive/2010/11/21/10094564.aspx

        public static Task<T2> Then<T1, T2>(this Task<T1> first, Func<T2> next)
        {
            if (first == null) throw new ArgumentNullException("first");
            if (next == null) throw new ArgumentNullException("next");

            var tcs = new TaskCompletionSource<T2>();
            first.ContinueWith(x =>
                {
                    if (x.IsFaulted)
                        tcs.TrySetException(x.Exception.InnerExceptions);
                    else if (x.IsCanceled)
                        tcs.TrySetCanceled();
                    else
                    {
                        try
                        {
                            var result = next();
                            tcs.TrySetResult(result);
                        }
                        catch (Exception exc)
                        {
                            tcs.TrySetException(exc);
                        }
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        public static Task<T2> Then<T2>(this Task first, Func<T2> next)
        {
            if (first == null) throw new ArgumentNullException("first");
            if (next == null) throw new ArgumentNullException("next");

            var tcs = new TaskCompletionSource<T2>();
            first.ContinueWith(x =>
            {
                if (x.IsFaulted)
                    tcs.TrySetException(x.Exception.InnerExceptions);
                else if (x.IsCanceled)
                    tcs.TrySetCanceled();
                else
                {
                    try
                    {
                        var result = next();
                        tcs.TrySetResult(result);
                    }
                    catch (Exception exc)
                    {
                        tcs.TrySetException(exc);
                    }
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        public static Task Then<T1>(this Task<T1> first, Func<T1, Task> next)
        {
            if (first == null) throw new ArgumentNullException("first");
            if (next == null) throw new ArgumentNullException("next");

            var tcs = new TaskCompletionSource<NullStruct>();
            first.ContinueWith(delegate
            {
                if (first.IsFaulted) tcs.TrySetException(first.Exception.InnerExceptions);
                else if (first.IsCanceled) tcs.TrySetCanceled();
                else
                {
                    try
                    {
                        var t = next(first.Result);
                        if (t == null) tcs.TrySetCanceled();
                        else t.ContinueWith(delegate
                        {
                            if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
                            else if (t.IsCanceled) tcs.TrySetCanceled();
                            else tcs.TrySetResult(new NullStruct());
                        }, TaskContinuationOptions.ExecuteSynchronously);
                    }
                    catch (Exception exc) { tcs.TrySetException(exc); }
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        public static Task<T2> Then<T1, T2>(this Task<T1> first, Func<T1, Task<T2>> next)
        {
            if (first == null) throw new ArgumentNullException("first");
            if (next == null) throw new ArgumentNullException("next");

            var tcs = new TaskCompletionSource<T2>();
            first.ContinueWith(delegate
            {
                if (first.IsFaulted) tcs.TrySetException(first.Exception.InnerExceptions);
                else if (first.IsCanceled) tcs.TrySetCanceled();
                else
                {
                    try
                    {
                        var t = next(first.Result);
                        if (t == null) tcs.TrySetCanceled();
                        else t.ContinueWith(delegate
                        {
                            if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
                            else if (t.IsCanceled) tcs.TrySetCanceled();
                            else tcs.TrySetResult(t.Result);
                        }, TaskContinuationOptions.ExecuteSynchronously);
                    }
                    catch (Exception exc) { tcs.TrySetException(exc); }
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }


        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }

        private struct NullStruct
        {
        }
    }
}