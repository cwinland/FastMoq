using FastMoq.Extensions;
using FastMoq.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMoq.Tests
{
    public class ObservableExceptionLogTests : MockerTestBase<ObservableExceptionLog>
    {
        [Fact]
        public void CanCreate()
        {
            Component.Should().NotBeNull();
            Component.Items.Should().BeEmpty();
            Component.Should().BeEmpty();
        }

        [Fact]
        public void CanAdd()
        {
            Component.Add("Test");
            Component.Should().HaveCount(1);
            Component.First().Should().Be("Test");
        }

        [Fact]
        public void IsReadonly()
        {
            (Component is IReadOnlyCollection<string> c).Should().BeTrue();
        }

        [Fact]
        public void Subscribe()
        {
            object? sender = null;
            NotifyCollectionChangedEventArgs? args = null;
            Component.CollectionChanged += (o, eventArgs) =>
            {
                sender = o;
                args = eventArgs;
            };
            Component.Add("Test2");
            sender.Should().NotBeNull();
            args.Should().NotBeNull();
            args.RaiseIfNull();
            args.NewItems.RaiseIfNull();
            args.Action.Should().Be(NotifyCollectionChangedAction.Add);
            args.NewItems.Count.Should().Be(1);
            Component.Count.Should().Be(1);

            sender = null;
            args = null;
            Component.Add("Test3");
            sender.Should().NotBeNull();
            args.Should().NotBeNull();
            args.RaiseIfNull();
            args.NewItems.RaiseIfNull();
            args.Action.Should().Be(NotifyCollectionChangedAction.Add);
            args.NewItems.Count.Should().Be(1);
            Component.Count.Should().Be(2);
        }
    }
}
