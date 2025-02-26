﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Configuration.Mvc
{
    using System;
    using System.Linq;
    using System.Reflection;
    using GraphQL.AspNet.Common;
    using GraphQL.AspNet.Common.Extensions;
    using GraphQL.AspNet.Defaults;
    using GraphQL.AspNet.Execution;
    using GraphQL.AspNet.Interfaces.Configuration;
    using GraphQL.AspNet.Interfaces.Engine;
    using GraphQL.AspNet.Interfaces.Execution;
    using GraphQL.AspNet.Interfaces.Logging;
    using GraphQL.AspNet.Interfaces.Middleware;
    using GraphQL.AspNet.Interfaces.TypeSystem;
    using GraphQL.AspNet.Interfaces.Web;
    using GraphQL.AspNet.Internal.Interfaces;
    using GraphQL.AspNet.Middleware.DirectiveExecution;
    using GraphQL.AspNet.Middleware.FieldExecution;
    using GraphQL.AspNet.Middleware.FieldSecurity;
    using GraphQL.AspNet.Middleware.QueryExecution;
    using GraphQL.AspNet.Parsing;
    using GraphQL.AspNet.Web;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// A builder for adding and configuring the controller based <see cref="ISchema" /> to a DI container
    /// and any MVC settings if applicable.
    /// </summary>
    /// <typeparam name="TSchema">The type of the graphql schema to inject.</typeparam>
    public class GraphQLSchemaInjector<TSchema> : ISchemaInjector<TSchema>
        where TSchema : class, ISchema
    {
        /// <summary>
        /// Creates the factory for the DI container to createa  pipeline of the given type.
        /// </summary>
        /// <typeparam name="TMiddleware">The type of middleware supported by the pipeline.</typeparam>
        /// <typeparam name="TContext">The type of the context the middleware components can handle.</typeparam>
        /// <param name="pipelineBuilder">The pipeline builder.</param>
        /// <returns>ISchemaPipeline&lt;TSchema, TContext&gt;.</returns>
        public static Func<IServiceProvider, ISchemaPipeline<TSchema, TContext>>
            CreatePipelineFactory<TMiddleware, TContext>(ISchemaPipelineBuilder<TSchema, TMiddleware, TContext> pipelineBuilder)
                where TMiddleware : class, IGraphMiddlewareComponent<TContext>
                where TContext : class, IGraphExecutionContext
        {
            return (sp) =>
            {
                var pipeline = pipelineBuilder.Build();
                sp.WriteLogEntry((l) => l.SchemaPipelineRegistered<TSchema>(pipeline));
                return pipeline;
            };
        }

        private readonly Action<SchemaOptions<TSchema>> _configureOptions;
        private readonly SchemaBuilder<TSchema> _schemaBuilder;
        private readonly SchemaOptions<TSchema> _options;
        private GraphQueryHandler<TSchema> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSchemaInjector{TSchema}" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="configureOptions">The use supplied action method to configure the
        /// primary options used to govern schema runtime operations.</param>
        public GraphQLSchemaInjector(SchemaOptions<TSchema> options, Action<SchemaOptions<TSchema>> configureOptions = null)
        {
            _configureOptions = configureOptions;
            _options = Validation.ThrowIfNullOrReturn(options, nameof(options));
            _schemaBuilder = new SchemaBuilder<TSchema>(_options);

            Validation.ThrowIfNull(options.ServiceCollection, $"{nameof(SchemaOptions)}.{nameof(SchemaOptions.ServiceCollection)}");
        }

        /// <summary>
        /// Configures the services and settings for the schema using the supplied configuration function.
        /// </summary>
        public void ConfigureServices()
        {
            // create the builder to guide the rest of the setup operations
            _configureOptions?.Invoke(_options);

            // register global directives to the schema
            foreach (var type in Constants.GlobalDirectives)
            {
                _options.AddType(type);
            }

            // add execution assembly for auto loading of graph types
            if (_options.AutoRegisterLocalGraphEntities)
            {
                var assembly = Assembly.GetEntryAssembly();
                _options.AddAssembly(assembly);
            }

            // ensure an http processor is set
            if (_options.QueryHandler.HttpProcessorType == null)
            {
                if (_options.QueryHandler.AuthenticatedRequestsOnly)
                    _options.QueryHandler.HttpProcessorType = typeof(SecureGraphQLHttpProcessor<TSchema>);
                else
                    _options.QueryHandler.HttpProcessorType = typeof(DefaultGraphQLHttpProcessor<TSchema>);
            }

            // ensure a runtime is set
            var runtimeDescriptor = new ServiceDescriptor(
                typeof(IGraphQLRuntime<TSchema>),
                typeof(DefaultGraphQLRuntime<TSchema>),
                ServiceLifetime.Scoped);
            _options.ServiceCollection.TryAdd(runtimeDescriptor);

            // register the schema
            _options.ServiceCollection.TryAddSingleton(this.BuildNewSchemaInstance);

            // setup default middleware for each required pipeline
            var queryPipelineHelper = new QueryExecutionPipelineHelper<TSchema>(_schemaBuilder.QueryExecutionPipeline);
            queryPipelineHelper.AddDefaultMiddlewareComponents(_options);

            var fieldPipelineHelper = new FieldExecutionPipelineHelper<TSchema>(_schemaBuilder.FieldExecutionPipeline);
            fieldPipelineHelper.AddDefaultMiddlewareComponents(_options);

            var authPipelineHelper = new FieldSecurityPipelineHelper<TSchema>(_schemaBuilder.FieldAuthorizationPipeline);
            authPipelineHelper.AddDefaultMiddlewareComponents(_options);

            var directivePipelineHelper = new DirectiveExecutionPipelineHelper<TSchema>(_schemaBuilder.DirectiveExecutionPipeline);
            directivePipelineHelper.AddDefaultMiddlewareComponents(_options);

            // register the DI entries for each pipeline
            _options.ServiceCollection.TryAddSingleton(CreatePipelineFactory(_schemaBuilder.FieldExecutionPipeline));
            _options.ServiceCollection.TryAddSingleton(CreatePipelineFactory(_schemaBuilder.FieldAuthorizationPipeline));
            _options.ServiceCollection.TryAddSingleton(CreatePipelineFactory(_schemaBuilder.QueryExecutionPipeline));
            _options.ServiceCollection.TryAddSingleton(CreatePipelineFactory(_schemaBuilder.DirectiveExecutionPipeline));

            this.RegisterEngineComponents();

            _options.FinalizeServiceRegistration();
        }

        /// <summary>
        /// Registers the default engine components for this schema. These components are used by the default pipelines
        /// registered as part of this injector.
        /// </summary>
        private void RegisterEngineComponents()
        {
            // "per schema" engine components
            _options.ServiceCollection.TryAddSingleton<IQueryOperationComplexityCalculator<TSchema>, DefaultOperationComplexityCalculator<TSchema>>();
            _options.ServiceCollection.TryAddSingleton<IGraphResponseWriter<TSchema>, DefaultResponseWriter<TSchema>>();
            _options.ServiceCollection.TryAddSingleton<IGraphQueryDocumentGenerator<TSchema>, DefaultGraphQueryDocumentGenerator<TSchema>>();
            _options.ServiceCollection.TryAddSingleton<IGraphQueryPlanGenerator<TSchema>, DefaultGraphQueryPlanGenerator<TSchema>>();
            _options.ServiceCollection.TryAddSingleton<IGraphQueryExecutionMetricsFactory<TSchema>, DefaultGraphQueryExecutionMetricsFactory<TSchema>>();

            // "per request per schema" components
            _options.ServiceCollection.TryAddTransient(typeof(IGraphQLHttpProcessor<TSchema>), _options.QueryHandler.HttpProcessorType);

            // "per application server" instance
            _options.ServiceCollection.TryAddSingleton<IGraphQLDocumentParser, GraphQLParser>();
            _options.ServiceCollection.TryAddScoped<IGraphLogger>(sp => sp?.GetService<IGraphEventLogger>());
            _options.ServiceCollection.TryAddScoped<IGraphEventLogger>((sp) =>
            {
                var factory = sp?.GetService<ILoggerFactory>();
                if (factory == null)
                    return null;

                return new DefaultGraphLogger(factory);
            });
        }

        /// <summary>
        /// Attempts to create a new instance of the schema using whatever constructore setup is required by it then initialize all preconfigured graph types into it.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>TSchema.</returns>
        private TSchema BuildNewSchemaInstance(IServiceProvider serviceProvider)
        {
            var schemaInstance = GraphSchemaBuilder.BuildSchema<TSchema>(serviceProvider);
            var initializer = new GraphSchemaInitializer<TSchema>(_options, serviceProvider);
            initializer.Initialize(schemaInstance);

            serviceProvider.WriteLogEntry(
                  (l) => l.SchemaInstanceCreated(schemaInstance));

            return schemaInstance;
        }

        /// <summary>
        /// Invoke the schema to be part of the application performing final setup and configuration.
        /// </summary>
        /// <param name="appBuilder">The application builder.</param>
        public void UseSchema(IApplicationBuilder appBuilder)
        {
            Validation.ThrowIfNullOrReturn(appBuilder, nameof(appBuilder));

            this.UseSchema(appBuilder.ApplicationServices, false);

            if (_options.ServerExtensions != null)
            {
                foreach (var additionalOptions in _options.ServerExtensions)
                    additionalOptions.Value.UseExtension(appBuilder, appBuilder.ApplicationServices);
            }

            if (!_options.QueryHandler.DisableDefaultRoute)
            {
                // when possible, create the singleton of the processor up front to avoid any
                // calls into the DI container at runtime.
                _handler = new GraphQueryHandler<TSchema>();
                appBuilder.MapWhen(
                    context => context.Request.Path == _options.QueryHandler.Route,
                    _handler.Execute);

                appBuilder?.ApplicationServices.WriteLogEntry(
                      (l) => l.SchemaRouteRegistered<TSchema>(
                      _options.QueryHandler.Route));
            }
        }

        /// <summary>
        /// Performs final configuration on graphql and preparses any referenced types for their meta data.
        /// Will NOT attempt to register an HTTP route for the schema.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public void UseSchema(IServiceProvider serviceProvider)
        {
            this.UseSchema(serviceProvider, true);
        }

        /// <summary>
        /// Invoke the schema, performing final setup and configuration.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="invokeServerExtensions">if set to <c>true</c> any configured
        /// server options on this instance are invoked with just the service provider.</param>
        private void UseSchema(IServiceProvider serviceProvider, bool invokeServerExtensions)
        {
            // pre-parse any types known to this schema
            var preCacher = new SchemaPreCacher();
            preCacher.PrecacheTemplates(_options.SchemaTypesToRegister.Select(x => x.Type));

            // only when the service provider is used for final configuration do we
            // invoke extensions with just the service provider
            // (mostly just for test harnessing, but may be used by developers as well)
            if (invokeServerExtensions)
            {
                foreach (var additionalOptions in _options.ServerExtensions)
                    additionalOptions.Value.UseExtension(serviceProvider: serviceProvider);
            }

            // try and build the schema
            // this will surface any runtime validation errors that may occur
            // when assembling the schema instance and throw those at app start up
            // rather than when a query is inbound
            //
            // The schema is a singleton when added through the injector so this should be the only
            // build operation, but in case it was added by a user with a different scope
            // make a scoped provider instance to pull it through
            using (var scope = serviceProvider.CreateScope())
            {
                var schemaInstance = scope.ServiceProvider.GetRequiredService<TSchema>();
            }
        }

        /// <summary>
        /// Gets the pipeline builder for the schema being tracked by this instance.
        /// </summary>
        /// <value>The pipeline builder.</value>
        public ISchemaBuilder<TSchema> SchemaBuilder => _schemaBuilder;
    }
}