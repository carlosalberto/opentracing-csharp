using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTracing.Mock;
using Xunit;

using static OpenTracing.Examples.TestUtils;

namespace OpenTracing.Examples.LateSpanFinish
{
    public class LateSpanFinishTest
    {
        private readonly MockTracer tracer = new MockTracer();

        [Fact]
        public void test()
        {
            // Create a Span manually and use it as parent of a pair of subtasks
            ISpan parentSpan = tracer.BuildSpan("parent").Start();
            using (IScope scope = tracer.ScopeManager.Activate(parentSpan, false))
            {
                SubmitTasks();
            }

            WaitForSpanCount(tracer, 2, DefaultTimeout);

            // Late-finish the parent Span now
            parentSpan.Finish();

            var spans = tracer.FinishedSpans();
            Assert.Equal(3, spans.Count);
            Assert.Equal("task1", spans[0].OperationName);
            Assert.Equal("task2", spans[1].OperationName);
            Assert.Equal("parent", spans[2].OperationName);

            TestUtils.AssertSameTrace(spans);

            Assert.Null(tracer.ActiveSpan);
        }

        // Fire away a few subtasks, passing a parent ISpan whose lifetime
        // is not tied at-all to the children.
        // NOTE: As opposed to Java, there is not need to reactivate the parent Span,
        // as the context is propagated by AsyncLocalScopeManager.
        private void SubmitTasks()
        {
            Task.Run(async () =>
            {
                using (IScope childScope1 = tracer.BuildSpan("task1").StartActive(true))
                {
                    await Task.Delay(55);
                }
            });

            Task.Run(async () =>
            {
                using (IScope childScope2 = tracer.BuildSpan("task2").StartActive(true))
                {
                    await Task.Delay(85);
                }
            });
        }
    }
}
