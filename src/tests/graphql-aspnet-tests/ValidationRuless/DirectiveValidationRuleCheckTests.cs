﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************
namespace GraphQL.AspNet.Tests.ValidationRuless
{
    using System.Threading.Tasks;
    using GraphQL.AspNet.Common.Extensions;
    using GraphQL.AspNet.Common.Source;
    using GraphQL.AspNet.Directives;
    using GraphQL.AspNet.Execution;
    using GraphQL.AspNet.Execution.Contexts;
    using GraphQL.AspNet.Interfaces.Execution;
    using GraphQL.AspNet.Interfaces.TypeSystem;
    using GraphQL.AspNet.Middleware.DirectiveExecution.Components;
    using GraphQL.AspNet.PlanGeneration.InputArguments;
    using GraphQL.AspNet.Schemas;
    using GraphQL.AspNet.Schemas.TypeSystem;
    using GraphQL.AspNet.Tests.Framework;
    using GraphQL.AspNet.Tests.Framework.CommonHelpers;
    using GraphQL.AspNet.Tests.ValidationRuless.DirectiveTestData;
    using GraphQL.AspNet.ValidationRules;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class DirectiveValidationRuleCheckTests
    {
        [Test]
        public void UnknownLocation_FailsValidation()
        {
            var server = new TestServerBuilder()
                .AddType<ObjectTypeDirective>()
                .Build();

            var obj = new Mock<IObjectGraphType>();

            var context = server.CreateDirectiveExecutionContext<ObjectTypeDirective>(
                DirectiveLocation.NONE,
                obj.Object);

            var ruleSet = new DirectiveValidationRuleProcessor();
            var complete = ruleSet.Execute(context.AsEnumerable());

            Assert.IsFalse(complete);
            Assert.IsFalse(context.Messages.IsSucessful);
            Assert.AreEqual(1, context.Messages.Count);
        }

        [Test]
        public void UnknownPhase_FailsValidation()
        {
            var server = new TestServerBuilder()
                .AddType<ObjectTypeDirective>()
                .Build();
            var obj = new Mock<IObjectGraphType>();

            var context = server.CreateDirectiveExecutionContext<ObjectTypeDirective>(
                DirectiveLocation.OBJECT,
                obj.Object,
                DirectiveInvocationPhase.Unknown);

            var ruleSet = new DirectiveValidationRuleProcessor();
            var complete = ruleSet.Execute(context.AsEnumerable());

            Assert.IsFalse(complete);
            Assert.IsFalse(context.Messages.IsSucessful);
            Assert.AreEqual(1, context.Messages.Count);
        }

        [Test]
        public void LocationMisMatch_FailsValidation()
        {
            var server = new TestServerBuilder()
                .AddType<ObjectTypeDirective>()
                .Build();
            var obj = new Mock<IObjectGraphType>();

            var context = server.CreateDirectiveExecutionContext<ObjectTypeDirective>(
                DirectiveLocation.FIELD,
                obj.Object,
                DirectiveInvocationPhase.SchemaGeneration);

            var ruleSet = new DirectiveValidationRuleProcessor();
            var complete = ruleSet.Execute(context.AsEnumerable());

            Assert.IsFalse(complete);
            Assert.IsFalse(context.Messages.IsSucessful);
            Assert.AreEqual(1, context.Messages.Count);
            Assert.AreEqual("5.7.2", context.Messages[0].MetaData[Constants.Messaging.REFERENCE_RULE_NUMBER]);
        }

        [Test]
        public void NotADirective_FailsValidation()
        {
            var server = new TestServerBuilder()
                .AddType<TwoPropertyObject>()
                .Build();
            var obj = new Mock<IObjectGraphType>();

            var context = server.CreateDirectiveExecutionContext<TwoPropertyObject>(
                DirectiveLocation.OBJECT,
                obj.Object,
                DirectiveInvocationPhase.SchemaGeneration);

            var ruleSet = new DirectiveValidationRuleProcessor();
            var complete = ruleSet.Execute(context.AsEnumerable());

            Assert.IsFalse(complete);
            Assert.IsFalse(context.Messages.IsSucessful);
            Assert.AreEqual(1, context.Messages.Count);
            Assert.AreEqual("5.7.1", context.Messages[0].MetaData[Constants.Messaging.REFERENCE_RULE_NUMBER]);
        }

        [Test]
        public void ValidateRequest_PassedValidation()
        {
            var server = new TestServerBuilder()
                .AddType<ObjectTypeDirectiveWithParams>()
                .Build();
            var obj = new Mock<IObjectGraphType>();

            var context = server.CreateDirectiveExecutionContext<ObjectTypeDirectiveWithParams>(
                DirectiveLocation.OBJECT,
                obj.Object,
                DirectiveInvocationPhase.SchemaGeneration,
                SourceOrigin.None,
                new object[] { 5, "someValue" });

            var ruleSet = new DirectiveValidationRuleProcessor();
            var complete = ruleSet.Execute(context.AsEnumerable());

            Assert.IsTrue(complete);
            Assert.IsTrue(context.Messages.IsSucessful);
            Assert.AreEqual(0, context.Messages.Count);
        }

        [Test]
        public void IncorrectNumberOfArguments_FailsValidation()
        {
            var server = new TestServerBuilder()
                .AddType<ObjectTypeDirectiveWithParams>()
                .Build();

            var obj = new Mock<IObjectGraphType>();

            var context = server.CreateDirectiveExecutionContext<ObjectTypeDirectiveWithParams>(
                DirectiveLocation.OBJECT,
                obj.Object,
                DirectiveInvocationPhase.SchemaGeneration,
                SourceOrigin.None,
                new object[] { 5 }); // directive requires 2 argument, only 1 supplied

            var ruleSet = new DirectiveValidationRuleProcessor();
            var complete = ruleSet.Execute(context.AsEnumerable());

            Assert.IsFalse(complete);
            Assert.IsFalse(context.Messages.IsSucessful);
            Assert.AreEqual(1, context.Messages.Count);
            Assert.AreEqual("5.7", context.Messages[0].MetaData[Constants.Messaging.REFERENCE_RULE_NUMBER]);
        }

        [Test]
        public void InvalidArgument_FailsValidation()
        {
            var server = new TestServerBuilder()
                .AddType<ObjectTypeDirectiveWithParams>()
                .Build();

            var obj = new Mock<IObjectGraphType>();

            var context = server.CreateDirectiveExecutionContext<ObjectTypeDirectiveWithParams>(
                DirectiveLocation.OBJECT,
                obj.Object,
                DirectiveInvocationPhase.SchemaGeneration,
                SourceOrigin.None,
                new object[] { "notAInt", "validString" }); // arg 1 should be an int

            var ruleSet = new DirectiveValidationRuleProcessor();
            var complete = ruleSet.Execute(context.AsEnumerable());

            Assert.IsFalse(complete);
            Assert.IsFalse(context.Messages.IsSucessful);
            Assert.AreEqual(1, context.Messages.Count);
            Assert.AreEqual("5.7", context.Messages[0].MetaData[Constants.Messaging.REFERENCE_RULE_NUMBER]);
        }
    }
}