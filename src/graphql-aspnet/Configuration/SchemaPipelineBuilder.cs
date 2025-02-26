﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using GraphQL.AspNet.Common;
    using GraphQL.AspNet.Common.Extensions;
    using GraphQL.AspNet.Interfaces.Configuration;
    using GraphQL.AspNet.Interfaces.Execution;
    using GraphQL.AspNet.Interfaces.Middleware;
    using GraphQL.AspNet.Interfaces.TypeSystem;
    using GraphQL.AspNet.Middleware;
    using GraphQL.AspNet.Middleware.Common;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// An builder class that can construct a schema pipeline for the given middleware type and context.
    /// </summary>
    /// <typeparam name="TSchema">The type of the schema this pipeline builder is creating a pipelien for.</typeparam>
    /// <typeparam name="TMiddleware">The type of middleware supported by the pipeline.</typeparam>
    /// <typeparam name="TContext">The type of the context the middleware components can handle.</typeparam>
    public class SchemaPipelineBuilder<TSchema, TMiddleware, TContext> : ISchemaPipelineBuilder<TSchema, TMiddleware, TContext>
        where TSchema : class, ISchema
        where TMiddleware : class, IGraphMiddlewareComponent<TContext>
        where TContext : class, IGraphExecutionContext
    {
        private readonly SchemaOptions _options;
        private readonly LinkedList<GraphMiddlewareDefinition<TContext>> _middleware;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaPipelineBuilder{TSchema,TMiddleware,TContext}" /> class.
        /// </summary>
        /// <param name="options">The schema options representing the schema being built.</param>
        /// <param name="name">The human friendly name to assign to this pipeline.</param>
        public SchemaPipelineBuilder(SchemaOptions options, string name = null)
        {
            this.PipelineName = name?.Trim() ?? "-unknown-";
            _options = Validation.ThrowIfNullOrReturn(options, nameof(options));
            _middleware = new LinkedList<GraphMiddlewareDefinition<TContext>>();
        }

        /// <inheritdoc/>
        public ISchemaPipelineBuilder<TSchema, TMiddleware, TContext> AddMiddleware(TMiddleware middlewareInstance, string name = null)
        {
            var definition = new GraphMiddlewareDefinition<TContext>(middlewareInstance, name);
            _middleware.AddLast(definition);
            return this;
        }

        /// <inheritdoc/>
        public ISchemaPipelineBuilder<TSchema, TMiddleware, TContext> AddMiddleware(
            Func<TContext, GraphMiddlewareInvocationDelegate<TContext>, CancellationToken, Task> operation,
            string name = null)
        {
            var middleware = new SingleFunctionMiddleware<TContext>(operation);
            var definition = new GraphMiddlewareDefinition<TContext>(middleware, name);
            _middleware.AddLast(definition);
            return this;
        }

        /// <inheritdoc/>
        public ISchemaPipelineBuilder<TSchema, TMiddleware, TContext> AddMiddleware<TComponent>(
            ServiceLifetime lifetime = ServiceLifetime.Singleton,
            string name = null)
            where TComponent : class, TMiddleware
        {
            var definition = new GraphMiddlewareDefinition<TContext>(typeof(TComponent), lifetime, name);
            _middleware.AddLast(definition);
            _options.ServiceCollection.Add(new ServiceDescriptor(typeof(TComponent), typeof(TComponent), lifetime));

            return this;
        }

        /// <inheritdoc/>
        public ISchemaPipelineBuilder<TSchema, TMiddleware, TContext> AddMiddleware<TComponent>(
            Func<IServiceProvider, TMiddleware> instanceFactory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton,
            string name = null)
            where TComponent : class, TMiddleware
        {
            var definition = new GraphMiddlewareDefinition<TContext>(typeof(TComponent), lifetime, name);
            _middleware.AddLast(definition);
            _options.ServiceCollection.Add(new ServiceDescriptor(typeof(TComponent), instanceFactory, lifetime));

            return this;
        }

        /// <inheritdoc/>
        public ISchemaPipelineBuilder<TSchema, TMiddleware, TContext> Clear()
        {
            _middleware.Clear();
            return this;
        }

        /// <inheritdoc/>
        public ISchemaPipeline<TSchema, TContext> Build()
        {
            GraphMiddlewareInvocationDelegate<TContext> leadInvoker = null;

            // walk backwards up the chained middleware, setting up the component creators and their call chain
            // maintain a list of component names for logging
            var node = _middleware.Last;
            var middlewareNameList = new List<string>();
            while (node != null)
            {
                var invoker = new GraphMiddlewareInvoker<TContext>(node.Value, leadInvoker);

                if (!string.IsNullOrWhiteSpace(node.Value?.Name))
                    middlewareNameList.Insert(0, node.Value.Name);
                else if (node.Value.Component != null)
                    middlewareNameList.Insert(0, node.Value.Component.GetType().FriendlyName());
                else if (node.Value.MiddlewareType != null)
                    middlewareNameList.Insert(0, node.Value.MiddlewareType.FriendlyName());
                else
                    middlewareNameList.Insert(0, "-unknown-");

                node = node.Previous;
                leadInvoker = invoker.InvokeAsync;
            }

            return new GraphSchemaPipeline<TSchema, TContext>(leadInvoker, this.PipelineName, middlewareNameList);
        }

        /// <inheritdoc/>
        public int Count => _middleware.Count;

        /// <summary>
        /// Gets or sets the name to be assigned to the pipeline when its generated.
        /// </summary>
        /// <value>The name of the pipeline.</value>
        public string PipelineName { get; set; }
    }
}