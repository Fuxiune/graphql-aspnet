﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Internal.TypeTemplates
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using GraphQL.AspNet.Attributes;
    using GraphQL.AspNet.Common;
    using GraphQL.AspNet.Common.Extensions;
    using GraphQL.AspNet.Common.Generics;
    using GraphQL.AspNet.Execution;
    using GraphQL.AspNet.Execution.Exceptions;
    using GraphQL.AspNet.Interfaces.Controllers;
    using GraphQL.AspNet.Interfaces.Execution;
    using GraphQL.AspNet.Interfaces.TypeSystem;
    using GraphQL.AspNet.Internal.Interfaces;
    using GraphQL.AspNet.Schemas;
    using GraphQL.AspNet.Schemas.Structural;
    using GraphQL.AspNet.Schemas.TypeSystem;
    using GraphQL.AspNet.Security;

    /// <summary>
    /// A base definition for items required to generate a graph field.
    /// </summary>
    public abstract class GraphFieldTemplate : BaseItemTemplate, IGraphTypeFieldTemplate
    {
        private AppliedSecurityPolicyGroup _securityPolicies;
        private GraphFieldAttribute _fieldDeclaration;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphFieldTemplate" /> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="attributeProvider">The instance that will supply the various attributes used to generate this field template.
        /// This is usually <see cref="PropertyInfo"/> or <see cref="MethodInfo"/>.</param>
        protected GraphFieldTemplate(IGraphTypeTemplate parent, ICustomAttributeProvider attributeProvider)
            : base(attributeProvider)
        {
            this.Parent = Validation.ThrowIfNullOrReturn(parent, nameof(parent));
            _securityPolicies = AppliedSecurityPolicyGroup.Empty;
        }

        /// <inheritdoc />
        protected override void ParseTemplateDefinition()
        {
            base.ParseTemplateDefinition();

            _fieldDeclaration = this.AttributeProvider.SingleAttributeOfTypeOrDefault<GraphFieldAttribute>();

            // ------------------------------------
            // Common Metadata
            // ------------------------------------
            this.Route = this.GenerateFieldPath();
            this.Mode = _fieldDeclaration?.ExecutionMode ?? FieldResolutionMode.PerSourceItem;
            this.Complexity = _fieldDeclaration?.Complexity;
            this.Description = this.AttributeProvider.SingleAttributeOfTypeOrDefault<DescriptionAttribute>()?.Description;

            var objectType = GraphValidation.EliminateWrappersFromCoreType(this.DeclaredReturnType);
            var typeExpression = GraphValidation.GenerateTypeExpression(this.DeclaredReturnType, this);
            typeExpression = typeExpression.CloneTo(GraphTypeNames.ParseName(objectType, this.Parent.Kind));

            // ------------------------------------
            // Gather Possible Types and/or union definition
            // ------------------------------------
            this.BuildUnionProxyInstance();
            if (this.UnionProxy != null)
            {
                this.PossibleTypes = new List<Type>(this.UnionProxy.Types);
            }
            else
            {
                this.PossibleTypes = new List<Type>();

                // the possible types attribte is optional but expects that the concrete types are added
                // to the schema else where lest a runtime exception occurs of a missing graph type.
                var typesAttrib = this.AttributeProvider.SingleAttributeOfTypeOrDefault<PossibleTypesAttribute>();
                if (typesAttrib != null)
                {
                    foreach (var type in typesAttrib.PossibleTypes)
                        this.PossibleTypes.Add(type);
                }

                // add any types declared on the primary field declaration
                if (_fieldDeclaration != null)
                {
                    foreach (var type in _fieldDeclaration.Types)
                    {
                        var strippedType = GraphValidation.EliminateWrappersFromCoreType(type);
                        if (strippedType != null)
                        {
                            this.PossibleTypes.Add(strippedType);
                        }
                    }
                }

                if (objectType != null && !Validation.IsCastable<IGraphActionResult>(objectType) && GraphValidation.IsValidGraphType(objectType))
                {
                    this.PossibleTypes.Insert(0, objectType);
                }
            }

            this.PossibleTypes = this.PossibleTypes.Distinct().ToList();

            // ------------------------------------
            // Adjust the action result to the actual return type this field returns
            // ------------------------------------
            if (Validation.IsCastable<IGraphActionResult>(objectType))
            {
                if (this.UnionProxy != null)
                {
                    // if a union was decalred preserve whatever modifer elements
                    // were decalred but alter the return type to object (a known common element among all members of the union)
                    objectType = typeof(object);
                }
                else if (_fieldDeclaration != null && _fieldDeclaration.Types.Count > 0)
                {
                    objectType = _fieldDeclaration.Types[0];
                    typeExpression = GraphValidation.GenerateTypeExpression(objectType, this)
                        .CloneTo(GraphTypeNames.ParseName(objectType, this.Parent.Kind));
                    objectType = GraphValidation.EliminateWrappersFromCoreType(objectType);
                }
                else if (this.PossibleTypes.Count > 0)
                {
                    objectType = this.PossibleTypes[0];
                    typeExpression = GraphValidation.GenerateTypeExpression(objectType, this)
                        .CloneTo(GraphTypeNames.ParseName(objectType, this.Parent.Kind));
                    objectType = GraphValidation.EliminateWrappersFromCoreType(objectType);
                }
                else
                {
                    objectType = typeof(object);
                }
            }

            this.ObjectType = objectType;

            if (this.UnionProxy != null)
                this.TypeExpression = typeExpression.CloneTo(this.UnionProxy.Name);
            else
                this.TypeExpression = typeExpression;

            // ------------------------------------
            // Async Requirements
            // ------------------------------------
            this.IsAsyncField = Validation.IsCastable<Task>(this.DeclaredReturnType);

            // ------------------------------------
            // Security Policies
            // ------------------------------------
            _securityPolicies = AppliedSecurityPolicyGroup.FromAttributeCollection(this.AttributeProvider);
        }

        /// <inheritdoc />
        public override void ValidateOrThrow()
        {
            base.ValidateOrThrow();

            if (this.DeclaredReturnType == typeof(void))
            {
                throw new GraphTypeDeclarationException($"The graph field '{this.InternalFullName}' has a void return. All graph fields must return something.");
            }

            if (this.IsAsyncField)
            {
                // account for a mistake by the developer in using a potential return type of just "Task" instead of Task<T>
                var genericArgs = this.DeclaredReturnType.GetGenericArguments();
                if (genericArgs.Length != 1)
                {
                    throw new GraphTypeDeclarationException(
                        $"The field  '{this.InternalFullName}' defines a return type of'{typeof(Task).Name}' but " +
                        "defines no contained return type for the resultant model object yielding a void return after " +
                        "completion of the task. All graph methods must return a single model object. Consider using " +
                        $"'{typeof(Task<>).Name}' instead for asyncronous methods");
                }
            }

            if (this.UnionProxy != null)
            {
                GraphValidation.EnsureGraphNameOrThrow($"{this.InternalFullName}[{nameof(GraphFieldAttribute)}][{nameof(IGraphUnionProxy)}]", this.UnionProxy.Name);
                if (this.UnionProxy.Types.Count < 1)
                {
                    throw new GraphTypeDeclarationException(
                        $"The field '{this.InternalFullName}' declares union type of '{this.UnionProxy.Name}' " +
                        "but that type includes 0 possible types in the union. Unions require 1 or more possible types. Add additional types" +
                        "or remove the union.");
                }
            }

            // ensure the object type returned by the graph field is set correctly
            bool returnsActionResult = Validation.IsCastable<IGraphActionResult>(this.ObjectType);
            var enforceUnionRules = this.UnionProxy != null;

            if (this.PossibleTypes.Count == 0)
            {
                throw new GraphTypeDeclarationException(
                    $"The field '{this.InternalFullName}' declared no possible return types either on its attribute declarations or as the " +
                    "declared return type for the field. GraphQL requires the type information be known " +
                    $"to setup the schema and client tooling properly. If this field returns a '{nameof(IGraphActionResult)}' you must " +
                    "provide a graph field declaration attribute and add at least one type; be that a concrete type, an interface or a union.");
            }

            // validate each type in the list for "correctness"
            // Possible Types must conform to the rules of those required by sub type declarations of unions and interfaces
            // interfaces: https://graphql.github.io/graphql-spec/June2018/#sec-Interfaces
            // unions: https://graphql.github.io/graphql-spec/June2018/#sec-Unions
            foreach (var type in this.PossibleTypes)
            {
                if (enforceUnionRules)
                {
                    if (GraphQLProviders.ScalarProvider.IsScalar(type))
                    {
                        throw new GraphTypeDeclarationException(
                            $"The field '{this.InternalFullName}' declares union with a possible type of '{type.FriendlyName()}' " +
                            "but that type is a scalar. Scalars cannot be included in a field's possible type collection, only object types can.");
                    }

                    if (type.IsInterface)
                    {
                        throw new GraphTypeDeclarationException(
                            $"The field '{this.InternalFullName}'  declares union with  a possible type of '{type.FriendlyName()}' " +
                            "but that type is an interface. Interfaces cannot be included in a field's possible type collection, only object types can.");
                    }

                    if (type.IsEnum)
                    {
                        throw new GraphTypeDeclarationException(
                            $"The field '{this.InternalFullName}' declares a union with a possible type of '{type.FriendlyName()}' " +
                            "but that type is an enum. Only concrete, non-abstract classes may be used.  Value types, such as structs or enumerations, are not allowed.");
                    }

                    if (!type.IsClass)
                    {
                        throw new GraphTypeDeclarationException(
                            $"The field '{this.InternalFullName}' returns an interface named '{this.ObjectType.FriendlyName()}' and declares a possible type of '{type.FriendlyName()}' " +
                            "but that type is not a valid class. Only concrete, non-abstract classes may be used.  Value types, such as structs or enumerations, are also not allowed.");
                    }
                }

                foreach (var invalidFieldType in Constants.InvalidFieldTemplateTypes)
                {
                    if (Validation.IsCastable(type, invalidFieldType))
                    {
                        throw new GraphTypeDeclarationException(
                            $"The field '{this.InternalFullName}' declares a possible return type of '{type.FriendlyName()}' " +
                            $"but that type inherits from '{invalidFieldType.FriendlyName()}' which is a reserved type declared by the graphql-aspnet library. This type cannot cannot be returned by a graphql field.");
                    }
                }

                // to ensure an object isn't arbitrarly returned as null and lost
                // ensure that the any possible type returned from this field is returnable AS the type this field declares
                // as its return type. In doing this we know that, potentially, an object returned by this
                // field "could" cast to the return type and allow field execution to continue.
                //
                // This is a helpful developer safety check, not a complete guarantee as concrete types for interface
                // declarations are not required at this stage
                //
                // batch processed fields are not subject to this restriction
                if (!returnsActionResult && this.Mode == FieldResolutionMode.PerSourceItem && !Validation.IsCastable(type, this.ObjectType))
                {
                    throw new GraphTypeDeclarationException(
                        $"The field '{this.InternalFullName}' returns '{this.ObjectType.FriendlyName()}' and declares a possible type of '{type.FriendlyName()}' " +
                        $"but that type is not castable to '{this.ObjectType.FriendlyName()}' and therefore not returnable by this field. Due to the strongly-typed nature of C# any possible type on a field " +
                        "must be castable to the type of the field in order to ensure its not inadvertantly nulled out during processing. If this field returns a union " +
                        $"of multiple, disperate types consider returning '{typeof(object).Name}' from the field to ensure each possible return type can be successfully processed.");
                }
            }

            // general validation of any declaraed parameter for this field
            foreach (var argument in this.Arguments)
                argument.ValidateOrThrow();

            if (this.Complexity.HasValue && this.Complexity < 0)
            {
                throw new GraphTypeDeclarationException(
                    $"The field '{this.InternalFullName}' declares a complexity value of " +
                    $"`{this.Complexity.Value}`. The complexity factor must be greater than or equal to 0.");
            }

            this.ValidateBatchMethodSignatureOrThrow();
        }

        /// <summary>
        /// When overridden in a child class, this method builds the unique field path that will be assigned to this instance
        /// using the implementation rules of the concrete type.
        /// </summary>
        /// <returns>GraphRoutePath.</returns>
        protected abstract GraphFieldPath GenerateFieldPath();

        /// <summary>
        /// Type extensions used as batch methods required a speceial input and output signature for the runtime
        /// to properly supply and retrieve data from the batch. This method ensures the signature coorisponds to those requirements or
        /// throws an exception indicating the problem if one is found.
        /// </summary>
        private void ValidateBatchMethodSignatureOrThrow()
        {
            if (this.Mode != FieldResolutionMode.Batch)
                return;

            // the method MUST accept a parameter of type IEnumerable<TypeToExtend> in its signature somewhere
            // when declared in batch mode
            var requiredEnumerable = typeof(IEnumerable<>).MakeGenericType(this.SourceObjectType);
            if (this.Arguments.All(arg => arg.DeclaredArgumentType != requiredEnumerable))
            {
                throw new GraphTypeDeclarationException(
                    $"Invalid batch method signature. The field '{this.InternalFullName}' declares itself as batch method but does not accept a batch " +
                    $"of data as an input parameter. This method must accept a parameter of type '{requiredEnumerable.FriendlyName()}' somewhere in its method signature to " +
                    $"be used as a batch extension for the type '{this.SourceObjectType.FriendlyName()}'.");
            }

            var declaredType = GraphValidation.EliminateWrappersFromCoreType(this.DeclaredReturnType);
            if (declaredType == typeof(IGraphActionResult))
                return;

            // when a batch method doesn't return an action result, indicating the developer
            // opts to specify his return types explicitly; ensure that their chosen return type is a dictionary
            // keyed on the type being extended allowing the runtime to seperate the batch
            // for proper segmentation in the object graph.
            // --
            // when the return type is a graph action this check is deferred after results of the batch are produced
            if (!BatchResultProcessor.IsBatchDictionaryType(declaredType, this.SourceObjectType, this.ObjectType))
            {
                throw new GraphTypeDeclarationException(
                    $"Invalid batch method signature. The field '{this.InternalFullName}' declares a return type of '{declaredType.FriendlyName()}', however; " +
                    $"batch methods must return either an '{typeof(IGraphActionResult).FriendlyName()}' or a dictionary keyed " +
                    "on the provided source data (e.g. 'IDictionary<SourceType, ResultsPerSourceItem>').");
            }

            // ensure any possible type declared via attribution matches the value type of the resultant dictionary
            // e.g.. if they supply a union for the field but declare a dictionary of IDictionary<T,K>
            // each member of the union must be castable to 'K' in order for the runtime to properly seperate
            // and process the batch results
            var dictionaryValue = GraphValidation.EliminateWrappersFromCoreType(declaredType.GetValueTypeOfDictionary());
            foreach (var type in this.PossibleTypes)
            {
                if (!Validation.IsCastable(type, dictionaryValue))
                {
                    throw new GraphTypeDeclarationException(
                        $"The field '{this.InternalFullName}' returns '{this.ObjectType.FriendlyName()}' and declares a possible type of '{type.FriendlyName()}' " +
                        $"but that type is not castable to '{this.ObjectType.FriendlyName()}' and therefore not returnable by this field. Due to the strongly-typed nature of C# any possible type on a field " +
                        "must be castable to the type of the field in order to ensure its not inadvertantly nulled out during processing. If this field returns a union " +
                        $"of multiple, disperate types consider returning '{typeof(object).Name}' from the field to ensure each possible return type can be successfully processed.");
                }
            }
        }

        /// <inheritdoc />
        public abstract IGraphFieldResolver CreateResolver();

        /// <inheritdoc />
        public override IEnumerable<DependentType> RetrieveRequiredTypes()
        {
            var list = new List<DependentType>();
            list.AddRange(base.RetrieveRequiredTypes());

            if (this.PossibleTypes != null)
            {
                var dependentTypes = this.PossibleTypes
                    .Select(x => new DependentType(x, GraphValidation.ResolveTypeKind(x, this.OwnerTypeKind)));
                list.AddRange(dependentTypes);
            }

            if (this.Arguments != null)
            {
                foreach (var arg in this.Arguments)
                    list.AddRange(arg.RetrieveRequiredTypes());
            }

            return list;
        }

        /// <summary>
        /// Retrieves proxy instance defined on this attribute that is used to generate the <see cref="UnionGraphType" /> metadata.
        /// </summary>
        private void BuildUnionProxyInstance()
        {
            var fieldAttribute = this.AttributeProvider.SingleAttributeOfTypeOrDefault<GraphFieldAttribute>();
            if (fieldAttribute == null)
                return;

            IGraphUnionProxy proxy = null;

            if (fieldAttribute.Types.Count == 1)
            {
                var proxyType = fieldAttribute.Types.FirstOrDefault();
                if (proxyType != null)
                    proxy = GraphQLProviders.GraphTypeMakerProvider.CreateUnionProxyFromType(proxyType);
            }

            // when no proxy type is declared attempt to construct the proxy from types supplied
            // if and only if a name was supplied for the union
            if (proxy == null && !string.IsNullOrWhiteSpace(fieldAttribute.UnionTypeName))
            {
                proxy = new GraphUnionProxy(fieldAttribute.UnionTypeName, fieldAttribute.Types);
            }

            this.UnionProxy = proxy;
        }

        /// <inheritdoc />
        public abstract Type DeclaredReturnType { get; }

        /// <inheritdoc />
        public abstract string DeclaredName { get; }

        /// <inheritdoc />
        public IGraphTypeTemplate Parent { get; }

        /// <inheritdoc />
        public abstract GraphFieldSource FieldSource { get; }

        /// <inheritdoc />
        public abstract TypeKind OwnerTypeKind { get; }

        /// <summary>
        /// Gets a value indicating whether returning a value from this field, as its declared in the C# code base, represents a <see cref="Task" /> that must be awaited.
        /// </summary>
        /// <value><c>true</c> if this instance is asynchronous method; otherwise, <c>false</c>.</value>
        public bool IsAsyncField { get; private set; }

        /// <inheritdoc />
        public virtual Type SourceObjectType => this.Parent?.ObjectType;

        /// <inheritdoc />
        public FieldResolutionMode Mode { get; protected set; }

        /// <inheritdoc />
        public abstract IReadOnlyList<IGraphArgumentTemplate> Arguments { get; }

        /// <inheritdoc />
        public virtual AppliedSecurityPolicyGroup SecurityPolicies => _securityPolicies;

        /// <inheritdoc />
        public GraphTypeExpression TypeExpression { get; protected set; }

        /// <inheritdoc />
        public virtual IGraphUnionProxy UnionProxy { get; protected set; }

        /// <summary>
        /// Gets the possible types that can be returned by this field.
        /// </summary>
        /// <value>The possible types.</value>
        protected List<Type> PossibleTypes { get; private set; }

        /// <inheritdoc />
        public bool HasDefaultValue => false;

        /// <inheritdoc />
        public MetaGraphTypes[] TypeWrappers => _fieldDeclaration?.TypeDefinition;

        /// <inheritdoc />
        public float? Complexity { get; set; }
    }
}