using System;
using NUnit.Framework;
using Mailbox;

namespace Mailbox.Tests {
    [TestFixture]
    class When_adding_new_messages_to_a_mailbox_with_no_receivers {
        Inbox<int> _sut;

        [SetUp]
        public void Setup() {
            _sut = new Inbox<int>();
        }

        [Test]
        public void MessageCount_should_reflect_this() {
            _sut.Post(1);
            _sut.Post(2);

            Assert.AreEqual(2, _sut.MessageCount);
        }
    }
}

