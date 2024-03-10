﻿using FluentAssertions;

namespace Opsi.Common.Tests
{
    [TestClass]
    public class OptionTest
    {
        [TestMethod]
        public void IsNone_OptionIsNone_IsNone()
        {
            var option = Option<string>.None();

            option.IsNone.Should().BeTrue();
        }

        [TestMethod]
        public void IsSome_OptionWithSome_IsSome()
        {
            var option = Option<string>.Some("anything");

            option.IsSome.Should().BeTrue();
        }

        [TestMethod]
        public void IsSome_OptionWithSomeNull_ExceptionWillBeThrown()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            this.Invoking(x => Option<string>.Some(null)).Should().Throw<ArgumentNullException>();
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [TestMethod]
        public void IsSome_OptionWithSome_HasValue()
        {
            var anything = "anything";
            var option = Option<string>.Some(anything);

            option.Value.Should().Be(anything);
        }
    }
}