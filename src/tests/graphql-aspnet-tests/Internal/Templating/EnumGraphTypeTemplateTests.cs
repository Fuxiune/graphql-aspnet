﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Tests.Internal.Templating
{
    using System;
    using System.Linq;
    using GraphQL.AspNet.Execution.Exceptions;
    using GraphQL.AspNet.Tests.Internal.Templating.DirectiveTestData;
    using GraphQL.AspNet.Tests.Internal.Templating.EnumTestData;
    using NUnit.Framework;

    [TestFixture]
    public class EnumGraphTypeTemplateTests
    {
        [Test]
        public void Parse_SimpleEnum_AllDefault_ParsesCorrectly()
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(SimpleEnum));
            template.Parse();
            template.ValidateOrThrow();

            Assert.AreEqual($"{Constants.Routing.ENUM_ROOT}/{nameof(SimpleEnum)}", template.Route.Path);
            Assert.AreEqual(nameof(SimpleEnum), template.Name);
            Assert.AreEqual(null, template.Description);
            Assert.AreEqual(1, template.Values.Count());
            Assert.AreEqual("Value1", template.Values[0].Name);
            Assert.AreEqual(null, template.Values[0].Description);
        }

        [Test]
        public void Parse_EnumWithDescription_ParsesCorrectly()
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumWithDescription));
            template.Parse();
            template.ValidateOrThrow();

            Assert.AreEqual("Enum Description", template.Description);
        }

        [Test]
        public void Parse_EnumWithGraphName_ParsesCorrectly()
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumWithGraphName));
            template.Parse();
            template.ValidateOrThrow();

            Assert.AreEqual("ValidGraphName", template.Name);
        }

        [Test]
        public void Parse_EnumWithDescriptionOnValues_ParsesCorrectly()
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumWithDescriptionOnValues));
            template.Parse();
            template.ValidateOrThrow();

            Assert.IsTrue(template.Values.Any(x => x.Name == "Value1" && x.Description == null));
            Assert.IsTrue(template.Values.Any(x => x.Name == "Value2" && x.Description == "Value2 Description"));
            Assert.IsTrue(template.Values.Any(x => x.Name == "Value3" && x.Description == null));
            Assert.IsTrue(template.Values.Any(x => x.Name == "Value4" && x.Description == "Value4 Description"));
        }

        [Test]
        public void Parse_EnumWithInvalidValueName_ThrowsException()
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumWithInvalidValueName));
            template.Parse();

            Assert.Throws<GraphTypeDeclarationException>(() =>
            {
                template.ValidateOrThrow();
            });
        }

        [Test]
        public void Parse_EnumWithValueWithGraphName_ParsesCorrectly()
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumWithValueWithGraphName));
            template.Parse();
            template.ValidateOrThrow();

            Assert.IsTrue(template.Values.Any(x => x.Name == "Value1"));
            Assert.IsTrue(template.Values.Any(x => x.Name == "AnotherName"));
            Assert.IsTrue(template.Values.All(x => x.Name != "Value2"));
        }

        [Test]
        public void Parse_EnumWithValueWithGraphName_ButGraphNameIsInvalid_ThrowsException()
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumWithValueWithGraphNameButGraphNameIsInvalid));
            template.Parse();
            Assert.Throws<GraphTypeDeclarationException>(() =>
            {
                template.ValidateOrThrow();
            });
        }

        [Test]
        public void Parse_EnumWithNonIntBase_ParsesCorrectly()
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumFromUInt));
            template.Parse();
            template.ValidateOrThrow();

            Assert.AreEqual(3, template.Values.Count);
            Assert.IsTrue(template.Values.Any(x => x.Name == "Value1"));
            Assert.IsTrue(template.Values.Any(x => x.Name == "Value2"));
            Assert.IsTrue(template.Values.Any(x => x.Name != "Value3"));
        }

        [Test]
        public void Parse_EnumWithDuplciateValues_ThrowsException()
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumWithDuplicateValues));
            template.Parse();

            Assert.Throws<GraphTypeDeclarationException>(() =>
            {
                template.ValidateOrThrow();
            });
        }

        [Test]
        public void Parse_EnumWithDuplciateValuesFromComposite_ThrowsException()
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumWithDuplicateValuesFromComposite));
            template.Parse();

            Assert.Throws<GraphTypeDeclarationException>(() =>
            {
                template.ValidateOrThrow();
            });
        }

        [TestCase(typeof(EnumFromSByte))]
        [TestCase(typeof(EnumFromShort))]
        [TestCase(typeof(EnumFromInt))]
        [TestCase(typeof(EnumFromLong))]
        public void Parse_EnsureEnumsOfSignedValues(Type type)
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(type);
            template.Parse();
            template.ValidateOrThrow();

            Assert.AreEqual(6, template.Values.Count);
            Assert.IsTrue(template.Values.Any(x => x.Name == "Value1"));
            Assert.IsTrue(template.Values.Any(x => x.Name == "Value2"));
            Assert.IsTrue(template.Values.Any(x => x.Name != "Value3"));
            Assert.IsTrue(template.Values.Any(x => x.Name == "Value4"));
            Assert.IsTrue(template.Values.Any(x => x.Name == "Value5"));
            Assert.IsTrue(template.Values.Any(x => x.Name != "Value6"));
        }

        [TestCase(typeof(EnumFromByte))]
        [TestCase(typeof(EnumFromUShort))]
        [TestCase(typeof(EnumFromUInt))]
        [TestCase(typeof(EnumFromULong))]
        public void Parse_EnsureEnumsOfUnSignedValues(Type type)
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(type);
            template.Parse();
            template.ValidateOrThrow();

            Assert.AreEqual(3, template.Values.Count);
            Assert.IsTrue(template.Values.Any(x => x.Name == "Value1"));
            Assert.IsTrue(template.Values.Any(x => x.Name == "Value2"));
            Assert.IsTrue(template.Values.Any(x => x.Name != "Value3"));
        }

        [Test]
        public void Parse_SignedEnum_CompleteKeySpace_ParsesCorrectly()
        {
            // the enum defines EVERY value for its key space
            // ensure it parses
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumCompleteSByte));
            template.Parse();
            template.ValidateOrThrow();

            // -128 => 127
            Assert.AreEqual(256, template.Values.Count);
        }

        [Test]
        public void Parse_UnsignedEnum_CompleteKeySpace_ParsesCorrectly()
        {
            // the enum defines EVERY value for its key space
            // ensure it parses
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumCompleteByte));
            template.Parse();
            template.ValidateOrThrow();

            // 0 => 255
            Assert.AreEqual(256, template.Values.Count);
        }

        [Test]
        public void Parse_AssignedDirective_IsTemplatized()
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumWithDirective));
            template.Parse();
            template.ValidateOrThrow();

            Assert.AreEqual(1, template.AppliedDirectives.Count());

            var appliedDirective = template.AppliedDirectives.First();
            Assert.AreEqual(typeof(DirectiveWithArgs), appliedDirective.DirectiveType);
            Assert.AreEqual(new object[] { 5, "bob" }, appliedDirective.Arguments);
        }

        [Test]
        public void Parse_EnumOption_AssignedDirective_IsTemplatized()
        {
            var template = new AspNet.Internal.TypeTemplates.EnumGraphTypeTemplate(typeof(EnumWithDirectiveOnOption));
            template.Parse();
            template.ValidateOrThrow();

            Assert.AreEqual(0, template.AppliedDirectives.Count());

            var optionTemplate = template.Values.FirstOrDefault(x => x.Name == "Value1");
            Assert.AreEqual(0, optionTemplate.AppliedDirectives.Count());

            optionTemplate = template.Values.FirstOrDefault(x => x.Name == "Value2");
            Assert.AreEqual(1, optionTemplate.AppliedDirectives.Count());

            var appliedDirective = optionTemplate.AppliedDirectives.First();
            Assert.AreEqual(typeof(DirectiveWithArgs), appliedDirective.DirectiveType);
            Assert.AreEqual(new object[] { 88, "enum option arg" }, appliedDirective.Arguments);
        }
    }
}