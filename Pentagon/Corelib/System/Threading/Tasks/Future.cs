﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    // lol
    public class TaskScheduler {
        internal bool TryRunInline(Task task, bool taskWasPreviouslyQueued)
        {
            TaskScheduler? ets = task.ExecutingTaskScheduler;

            if (ets != this && ets != null) return ets.TryRunInline(task, taskWasPreviouslyQueued);

            // TODO: do we really need this?
            /*if ((ets == null) ||
                (task.m_action == null) ||
                task.IsDelegateInvoked ||
                task.IsCanceled ||
                !RuntimeHelpers.TryEnsureSufficientExecutionStack())
            {
                return false;
            }*/

            bool inlined = TryExecuteTaskInline(task, taskWasPreviouslyQueued);

            /*if (inlined && !(task.IsDelegateInvoked || task.IsCanceled))
            {
                throw new InvalidOperationException(SR.TaskScheduler_InconsistentStateAfterTryExecuteTaskInline);
            }*/

            return inlined;
        }
        protected bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            //if (SynchronizationContext.Current == m_synchronizationContext)
            //{
                return TryExecuteTask(task);
            //}
            //else
            //{
            //    return false;
            //}
        }
        protected bool TryExecuteTask(Task task)
        {
            if (task.ExecutingTaskScheduler != this)
            {
                //throw new InvalidOperationException(SR.TaskScheduler_ExecuteTask_WrongTaskScheduler);
            }

            return task.ExecuteEntry();
        }
    }
    public class CancellationToken
    {
        public bool CanBeCanceled { get => false; }
    }

    // Task<TResult> is here and not in Task.cs. WHYYY?????
    public class Task<TResult> : Task
    {
        internal TResult? m_result;
        // Construct a promise-style task without any options.
        internal Task()
        {
        }

        // Construct a promise-style task with state and options.
        internal Task(object? state, TaskCreationOptions options) :
            base(state, options, promiseStyle: true)
        {
        }

        internal Task(TResult result) :
            base(false, TaskCreationOptions.None, default)
        {
            m_result = result;
        }

        internal Task(bool canceled, TResult? result, TaskCreationOptions creationOptions, CancellationToken ct)
            : base(canceled, creationOptions, ct)
        {
            if (!canceled)
            {
                m_result = result;
            }
        }

        public Task(Func<TResult> function)
            : this(function, null, default,
                TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
        }

        public Task(Func<TResult> function, CancellationToken cancellationToken)
            : this(function, null, cancellationToken,
                TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
        }

        public Task(Func<TResult> function, TaskCreationOptions creationOptions)
            : this(function, Task.InternalCurrentIfAttached(creationOptions), default, creationOptions, InternalTaskOptions.None, null)
        {
        }

        public Task(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
            : this(function, Task.InternalCurrentIfAttached(creationOptions), cancellationToken, creationOptions, InternalTaskOptions.None, null)
        {
        }

        public Task(Func<object?, TResult> function, object? state)
            : this(function, state, null, default,
                TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
        }

        public Task(Func<object?, TResult> function, object? state, CancellationToken cancellationToken)
            : this(function, state, null, cancellationToken,
                    TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
        }

        public Task(Func<object?, TResult> function, object? state, TaskCreationOptions creationOptions)
            : this(function, state, Task.InternalCurrentIfAttached(creationOptions), default,
                    creationOptions, InternalTaskOptions.None, null)
        {
        }

        public Task(Func<object?, TResult> function, object? state, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
            : this(function, state, Task.InternalCurrentIfAttached(creationOptions), cancellationToken,
                    creationOptions, InternalTaskOptions.None, null)
        {
        }

        internal Task(Func<TResult> valueSelector, Task? parent, CancellationToken cancellationToken,
            TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler? scheduler) :
            base(valueSelector, null, parent, cancellationToken, creationOptions, internalOptions, scheduler)
        {
        }

        internal Task(Delegate valueSelector, object? state, Task? parent, CancellationToken cancellationToken,
            TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler? scheduler) :
            base(valueSelector, state, parent, cancellationToken, creationOptions, internalOptions, scheduler)
        {
        }



        internal bool TrySetResult(TResult? result)
        {
            bool returnValue = false;
            if (AtomicStateUpdate((int)TaskStateFlags.CompletionReserved,
                    (int)TaskStateFlags.CompletionReserved | (int)TaskStateFlags.RanToCompletion | (int)TaskStateFlags.Faulted | (int)TaskStateFlags.Canceled))
            {
                m_result = result;
                Interlocked.Exchange(ref m_stateFlags, m_stateFlags | (int)TaskStateFlags.RanToCompletion);
                ContingentProperties? props = m_contingentProperties;
                if (props != null)
                {
                    NotifyParentIfPotentiallyAttachedTask();
                    props.SetCompleted();
                }
                FinishContinuations();
                returnValue = true;
            }

            return returnValue;
        }



        internal void DangerousSetResult(TResult result)
        {
            if (m_contingentProperties?.m_parent != null)
            {
                TrySetResult(result);
                // TODO: check for success
            }
            else
            {
                m_result = result;
                m_stateFlags |= (int)TaskStateFlags.RanToCompletion;
            }
        }

        public TResult Result => m_result!;


        internal TResult GetResultCore(bool waitCompletionNotification)
        {
            // TODO: wait
            //if (!IsCompleted) InternalWait(Timeout.Infinite, default); 
            //if (!IsCompletedSuccessfully) ThrowIfExceptional(includeTaskCanceledExceptions: true);
            return m_result!;
        }

        public new TaskAwaiter<TResult> GetAwaiter()
        {
            return new TaskAwaiter<TResult>(this);
        }

        internal TResult ResultOnSuccess
        {
            get
            {
                return m_result!;
            }
        }
    }

}