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
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using GraphQL.AspNet.Attributes;
    using GraphQL.AspNet.Common;
    using GraphQL.AspNet.Common.Extensions;
    using GraphQL.AspNet.Execution;
    using GraphQL.AspNet.Execution.Exceptions;
    using GraphQL.AspNet.Interfaces.TypeSystem;
    using GraphQL.AspNet.Internal.Interfaces;
    using GraphQL.AspNet.Schemas.Structural;
    using GraphQL.AspNet.Schemas.TypeSystem;
    using GraphQL.AspNet.Security;

    /// <summary>
    /// A template for harsing a C# enumeration to be used in the object graph.
    /// </summary>
    [DebuggerDisplay("Enum Template: {InternalName}")]
    public class EnumGraphTypeTemplate : BaseGraphTypeTemplate, IEnumGraphTypeTemplate
    {
        private readonly List<EnumValueTemplate> _values;
        private Dictionary<string, IList<string>> _valuesTolabels;
        private string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumGraphTypeTemplate"/> class.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        public EnumGraphTypeTemplate(Type enumType)
            : base(enumType)
        {
            Validation.ThrowIfNull(enumType, nameof(enumType));
            this.ObjectType = enumType;

            _values = new List<EnumValueTemplate>();
            _valuesTolabels = new Dictionary<string, IList<string>>();
        }

        /// <summary>
        /// Parses the item definition.
        /// </summary>
        protected override void ParseTemplateDefinition()
        {
            if (!this.ObjectType.IsEnum)
                return;

            base.ParseTemplateDefinition();

            _name = GraphTypeNames.ParseName(this.ObjectType, TypeKind.ENUM);
            this.Description = this.ObjectType.SingleAttributeOrDefault<DescriptionAttribute>()?.Description?.Trim();
            this.Route = new GraphFieldPath(GraphFieldPath.Join(GraphCollection.Enums, _name));

            // parse the enum values for later injection
            var labels = Enum.GetNames(this.ObjectType);
            foreach (var label in labels)
            {
                var fi = this.ObjectType.GetField(label);

                if (fi.SingleAttributeOrDefault<GraphSkipAttribute>() != null)
                    continue;

                var optionTemplate = new EnumValueTemplate(this, fi);
                optionTemplate.Parse();

                // we must have unique labels to values
                // (no enums with duplicate values)
                // to prevent potential ambiguity in down stream
                // requests
                //
                // for instance, if an enum existed such that:
                // enum SomeEnum
                // {
                //    Value1 = 1,
                //    Value2 = 1,
                // }
                //
                // then the operation
                // var enumValue = (SomeEnum)1;
                //
                // is indeterminate as to which is the appropriate
                // label to apply to the value for conversion
                // into the coorisponding enum graph type
                // and ultimately serialization
                // to a requestor
                //
                // keep track of the number of labels share a valid so we can
                // throw an exception during validation if there are any duplicates
                if (!_valuesTolabels.ContainsKey(optionTemplate.NumericValueAsString))
                    _valuesTolabels.Add(optionTemplate.NumericValueAsString, new List<string>());

                _valuesTolabels[optionTemplate.NumericValueAsString].Add(label);
                if (_valuesTolabels[optionTemplate.NumericValueAsString].Count != 1)
                    continue;

                _values.Add(optionTemplate);
            }
        }

        /// <summary>
        /// When overridden in a child class, allows the template to perform some final validation checks
        /// on the integrity of itself. An exception should be thrown to stop the template from being
        /// persisted if the object is unusable or otherwise invalid in the manner its been built.
        /// </summary>
        public override void ValidateOrThrow()
        {
            base.ValidateOrThrow();

            if (!this.ObjectType.IsEnum)
            {
                throw new GraphTypeDeclarationException(
                    $"Invalid Enumeration. The type '{this.ObjectType.FriendlyName()}' is not an enumeration and cannot " +
                    "be coerced to be one.",
                    this.ObjectType);
            }

            var duplicatedValues = _valuesTolabels.Where(x => x.Value.Count > 1).ToList();
            if (duplicatedValues.Count > 0)
            {
                var builder = new StringBuilder();
                for (var i = 0; i < duplicatedValues.Count; i++)
                {
                    var dupKVP = duplicatedValues[i];
                    builder.Append($"{{{string.Join(", ", dupKVP.Value)}}} == {dupKVP.Key}");
                    if (i < duplicatedValues.Count - 1)
                        builder.Append(" || ");
                }

                var msg = $"Invalid Enumeration. The type '{this.ObjectType.FriendlyName()}' is indeterminate and cannot " +
                    "be used as a graph type. Ensure all enum labels have unique values.  " +
                    $"({builder})";

                throw new GraphTypeDeclarationException(msg, this.ObjectType);
            }

            foreach (var option in this.Values)
                option.ValidateOrThrow();

            _valuesTolabels = null;
        }

        /// <summary>
        /// Gets the collected set of enumeration values that this template parsed.
        /// </summary>
        /// <value>The values.</value>
        public IReadOnlyList<IEnumValueTemplate> Values => _values;

        /// <summary>
        /// Gets the fully qualified name, including namespace, of this item as it exists in the .NET code (e.g. 'Namespace.ObjectType.MethodName').
        /// </summary>
        /// <value>The internal name given to this item.</value>
        public override string InternalFullName => this.ObjectType?.FriendlyName(true);

        /// <summary>
        /// Gets the name that defines this item within the .NET code of the application; typically a method name or property name.
        /// </summary>
        /// <value>The internal name given to this item.</value>
        public override string InternalName => this.ObjectType?.FriendlyName();

        /// <summary>
        /// Gets the security policies found via defined attributes on the item that need to be enforced.
        /// Enumerations do no enforce security policies.
        /// </summary>
        /// <value>The security policies.</value>
        public override AppliedSecurityPolicyGroup SecurityPolicies => AppliedSecurityPolicyGroup.Empty;

        /// <summary>
        /// Gets the kind of graph type that can be made from this template.
        /// </summary>
        /// <value>The kind.</value>
        public override TypeKind Kind => TypeKind.ENUM;
    }
}