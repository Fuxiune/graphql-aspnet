﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Execution
{
    using System;
    using GraphQL.AspNet.Attributes;
    using GraphQL.AspNet.Common;
    using GraphQL.AspNet.Common.Extensions;
    using GraphQL.AspNet.Configuration;
    using GraphQL.AspNet.Interfaces.TypeSystem;
    using GraphQL.AspNet.Schemas;

    /// <summary>
    /// Perform a set of standardized steps to setup and configure any graph schema according to the rules
    /// for document operation execution used by the various schema pipelines.
    /// </summary>
    /// <typeparam name="TSchema">The type of the schema that the initializer
    /// can work with.</typeparam>
    public class GraphSchemaInitializer<TSchema>
        where TSchema : class, ISchema
    {
        private readonly SchemaOptions _options;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphSchemaInitializer{TSchema}" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="serviceProvider">The service provider from which to draw componentry for
        /// initailization.</param>
        public GraphSchemaInitializer(SchemaOptions options, IServiceProvider serviceProvider)
        {
            _options = Validation.ThrowIfNullOrReturn(options, nameof(options));
            _serviceProvider = Validation.ThrowIfNullOrReturn(serviceProvider, nameof(serviceProvider));
        }

        /// <summary>
        /// Initializes the schema:
        /// <para>* Add any controllers to the schema instance that were configured during startup.</para>
        /// <para>* Add all methods, virtual graph types, return types parameters and property configurations.</para>
        /// <para>* Add any additional types added at startup.</para>
        /// <para>* Register introspection meta-fields.</para>
        /// </summary>
        /// <param name="schema">The schema to initialize.</param>
        public virtual void Initialize(TSchema schema)
        {
            Validation.ThrowIfNull(schema, nameof(schema));
            if (schema.IsInitialized)
                return;

            lock (schema)
            {
                if (schema.IsInitialized)
                    return;

                schema.Configuration.Merge(_options.CreateConfiguration());

                var manager = new GraphSchemaManager(schema);
                manager.AddBuiltInDirectives();

                // add any configured types to this instance
                foreach (var registration in _options.SchemaTypesToRegister)
                {
                    var typeDeclaration = registration.Type.SingleAttributeOrDefault<GraphTypeAttribute>();
                    if (typeDeclaration != null && typeDeclaration.PreventAutoInclusion)
                        continue;

                    manager.EnsureGraphType(registration.Type);
                }

                // execute any assigned schema configuration extensions
                //
                // this includes any late bound directives added to the type
                // system via .ApplyDirective()
                foreach (var extension in _options.ConfigurationExtensions)
                    extension.Configure(schema);

                // apply all queued type system directives
                var processor = new SchemaDirectiveProcessor<TSchema>(_serviceProvider);
                processor.ApplyDirectives(schema);

                manager.RebuildIntrospectionData();
                schema.IsInitialized = true;
            }
        }
    }
}