using System.Text.RegularExpressions;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Matchers;
using PactNet.Verifier;
using Match = PactNet.Matchers.Match;

namespace Tests
{
    public class Tests
    {
        PactVerifier verifier;

        [SetUp]
        public void Setup()
        {
            verifier = new PactVerifier(new PactVerifierConfig()
            {

            });
        }

        [TearDown]
        public void Teardown()
        {
            verifier?.Dispose();
        }

        [Test]
        public void Publisher()
        {
            var message = new PublisherOrderReceived()
            {
                OrderId = Guid.NewGuid(),
                TheAnswer = 42
            };

            verifier.MessagingProvider("publisher").WithProviderMessages(scenarios =>
                {
                    scenarios.Add("order received message", () => message);
                    scenarios.Add("some event", b =>
                    {
                        b.WithContent(() => new
                        {
                            SomeProperty = "hi",
                            SomeOtherProperty = 21,
                            Yolo = Math.PI
                        });
                    });
                })
                .WithFileSource(new FileInfo(@"C:\tmp\pact\subscriber-publisher.json"))
                .Verify();


        }

        [Test]
        public void Consumer()
        {
            var message = new ConsumerOrderReceived()
            {
                OrderId = Guid.NewGuid()
            };

            var pact = PactNet.Pact.V3("subscriber", "publisher", new PactConfig()
            {
                PactDir = @"C:\tmp\pact"
            });

            var match = new
            {
                OrderId = Match.Type(message.OrderId)
            };

            var scenario = pact.WithMessageInteractions()
                .ExpectsToReceive("order received message");
            scenario
                .Given("a single order with valid guid")
                .WithJsonContent(match)
                .Verify<ConsumerOrderReceived>(t =>
                {
                    Assert.AreEqual(message.OrderId, t.OrderId);
                });

            pact.WithMessageInteractions().ExpectsToReceive("some event")
                .Given("an event")
                .WithJsonContent(Match.Type(new ConsumerEventSubscribed()
                {
                    SomeProperty = "hello world",
                    SomeOtherProperty = 42
                })).Verify<ConsumerEventSubscribed>(m =>
                {
                    Assert.AreEqual("hello world", m.SomeProperty);
                    Assert.AreEqual(42, m.SomeOtherProperty);
                });
        }

        class PublisherOrderReceived
        {
            public Guid OrderId { get; set; }
            public int TheAnswer { get; set; }
        }

        class ConsumerOrderReceived
        {
            public Guid OrderId { get; set; }
        }

        class ConsumerEventSubscribed
        {
            public string SomeProperty { get; set; }
            public int SomeOtherProperty { get; set; }
        }
    }
}