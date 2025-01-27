﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Middleware.FieldExecution.Components
{
    using System;
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using GraphQL.AspNet.Common;
    using GraphQL.AspNet.Common.Extensions;
    using GraphQL.AspNet.Execution;
    using GraphQL.AspNet.Execution.Contexts;
    using GraphQL.AspNet.Execution.Exceptions;
    using GraphQL.AspNet.Interfaces.Middleware;
    using GraphQL.AspNet.Interfaces.TypeSystem;
    using GraphQL.AspNet.Internal;
    using GraphQL.AspNet.Schemas.TypeSystem;

    /// <summary>
    /// A middleware component that will validate a <see cref="GraphFieldExecutionContext" /> prior to the
    /// pipeline being executed. This component should be the first component in a field execution pipeline if its
    /// included.
    /// </summary>
    /// <typeparam name="TSchema">The type of the schema this field validator works against.</typeparam>
    public class ValidateFieldExecutionMiddleware<TSchema> : IGraphFieldExecutionMiddleware
        where TSchema : ISchema
    {
        private readonly ISchema _schema;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateFieldExecutionMiddleware{TSchema}"/> class.
        /// </summary>
        /// <param name="schema">The schema.</param>
        public ValidateFieldExecutionMiddleware(TSchema schema)
        {
            _schema = Validation.ThrowIfNullOrReturn(schema, nameof(schema));
        }

        /// <summary>
        /// Invokes this middleware component allowing it to perform its work against the supplied context.
        /// </summary>
        /// <param name="context">The context containing the request passed through the pipeline.</param>
        /// <param name="next">The delegate pointing to the next piece of middleware to be invoked.</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <returns>Task.</returns>
        public Task InvokeAsync(GraphFieldExecutionContext context, GraphMiddlewareInvocationDelegate<GraphFieldExecutionContext> next, CancellationToken cancelToken)
        {
            // ensure that the data items on teh request match the field they are being executed against
            var field = context.Field;
            var expectedFieldGraphType = _schema.KnownTypes.FindGraphType(field);
            var dataSource = context.Request.Data;

            // ensure the source item(s) being supplied
            // matches the expected source of the field being resolved
            if (context.InvocationContext.ExpectedSourceType != null)
            {
                var expectedSourceType = context.InvocationContext.ExpectedSourceType;
                foreach (var sourceItem in dataSource.Items)
                {
                    this.ValidateDataSourceItemAgainstExpectedType(
                        field,
                        expectedFieldGraphType,
                        expectedSourceType,
                        sourceItem.SourceData);
                }
            }

            if (dataSource.Items.Count == 0)
            {
                var expected = context.InvocationContext.Field.Mode == FieldResolutionMode.PerSourceItem ? "1" :
                    "at least 1";

                throw new GraphExecutionException(
                        $"Operation failed. The field execution context for '{field.Route.Path}' was passed " +
                        $"0 items but expected {expected}.  (Field Mode: {field.Mode.ToString()})");
            }

            if (context.InvocationContext.Field.Mode == FieldResolutionMode.PerSourceItem
                && dataSource.Items.Count != 1)
            {
                throw new GraphExecutionException(
                        $"Operation failed. The field execution context for '{field.Route.Path}' was passed " +
                        $"{dataSource.Items.Count} items to resolve but expected 1. (Field Mode: {field.Mode.ToString()})");
            }

            return next(context, cancelToken);
        }

        private void ValidateDataSourceItemAgainstExpectedType(
            IGraphField field,
            IGraphType fieldType,
            Type expectedSourceType,
            object value)
        {
            var actualSourceType = GraphValidation.EliminateWrappersFromCoreType(value.GetType());
            if (expectedSourceType != actualSourceType)
            {
                var expectedSourceGraphType = _schema.KnownTypes.FindGraphType(expectedSourceType, TypeKind.OBJECT);
                var analysis = _schema.KnownTypes.AnalyzeRuntimeConcreteType(expectedSourceGraphType, actualSourceType);
                if (!analysis.ExactMatchFound)
                {
                    throw new GraphExecutionException(
                        $"Operation failed. The field execution context for '{field.Route.Path}' was passed " +
                        $"a source item of type '{value.GetType().FriendlyName()}' which could not be coerced " +
                        $"to '{expectedSourceType}' as requested by the target graph type '{fieldType.Name}'.");
                }

                if (field.Mode == FieldResolutionMode.Batch && !(value.GetType() is IEnumerable))
                {
                    throw new GraphExecutionException(
                        $"Operation failed. The field execution context for '{field.Route.Path}' was executed in batch mode " +
                        $"but was not passed an {nameof(IEnumerable)} for its source data.");
                }
            }
        }
    }
}