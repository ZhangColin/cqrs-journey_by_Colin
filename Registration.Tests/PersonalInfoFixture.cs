using NUnit.Framework;
using Registration.Contracts;

namespace Registration.Tests {
    [TestFixture]
    public class PersonalInfoFixture {
        [Test]
        public void When_comparing_to_null_object_then_equals_false() {
            Assert.IsFalse(new PersonalInfo().Equals((object)null));
        }

        [Test]
        public void When_comparing_to_null_then_equals_false() {
            Assert.IsFalse(new PersonalInfo().Equals((PersonalInfo)null));
        }

        [Test]
        public void When_comparing_value_equal_then_returns_true() {
            Assert.AreEqual(new PersonalInfo { Email = "test@contoso.com" },
                new PersonalInfo { Email = "test@contoso.com" });

            Assert.AreEqual(new PersonalInfo {FirstName = "test@contoso.com"},
                new PersonalInfo {FirstName = "test@contoso.com"});

            Assert.AreEqual(new PersonalInfo {LastName = "test@contoso.com"},
                new PersonalInfo {LastName = "test@contoso.com"});

            Assert.AreEqual(new PersonalInfo {Email = "test@contoso.com", FirstName = "test"},
                new PersonalInfo {Email = "test@contoso.com", FirstName = "test"});

            Assert.AreEqual(new PersonalInfo {Email = "test@contoso.com", FirstName = "test", LastName = "one"},
                new PersonalInfo {Email = "test@contoso.com", FirstName = "test", LastName = "one"});

        }

        [Test]
        public void When_comparing_with_operator_overload_then_success() {
            Assert.True(new PersonalInfo{Email = "test@contoso.com"} 
                == new PersonalInfo{Email = "test@contoso.com"});

            Assert.True(new PersonalInfo{Email = "test@contoso.com"} 
                != new PersonalInfo{Email = "hello@world.com"});
        }

        [Test]
        public void when_comparing_to_null_with_operator_overload_then_succeeds() {
            Assert.IsTrue(((PersonalInfo)null)==null);
            Assert.IsTrue(new PersonalInfo() != null);
            Assert.IsTrue(null!=new PersonalInfo());
        }
    }
}