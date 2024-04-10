using FastMoq.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;

namespace FastMoq.Tests
{
    public class ConstructorHistoryTests : MockerTestBase<ConstructorHistory>
    {
        [Fact]
        public void Create() => Component.Should().NotBeNull();

        [Fact]
        public void Add()
        {
            Component.Count.Should().Be(0);
            Component.AddOrUpdate(typeof(IFile), new ConstructorModel(Mocks.GetObject<ConstructorInfo>(), new List<object?>()));
            Component.Count.Should().Be(1);
            Component.Keys.Count().Should().Be(1);
            Component.Values.Count().Should().Be(1);
            Component.Contains(typeof(IFile)).Should().BeTrue();
            Component.ContainsKey(typeof(IFile)).Should().BeTrue();
            var firstItem = Component[0];
            firstItem.Key.Should().Be(typeof(IFile));
            firstItem.Value.Should().HaveCount(1);
            firstItem.Value.Should().BeOfType<List<IHistoryModel>>();
            firstItem.Value[0].Should().BeOfType<ConstructorModel>();

            Component.GetConstructor(typeof(IFile)).Should().NotBeNull();
            Component.GetConstructor(typeof(IFileSystem)).Should().BeNull();

            var item = Component[typeof(IFile)];
            item.Should().HaveCount(1);
            item.Should().BeOfType<List<IHistoryModel>>();
            item.First().Should().BeOfType<ConstructorModel>();

            Component.TryGetValue(typeof(IFile), out var value).Should().BeTrue();
            value.Should().NotBeNull();

            Component.TryGetValue(typeof(IFileSystem), out var invalid).Should().BeFalse();
        }

        [Fact]
        public void Add_ShouldAddTwo_WhenDifferentModels()
        {
            var model = new ConstructorModel(Mocks.GetObject<ConstructorInfo>(), new List<object?>());
            Component.Count.Should().Be(0);
            Component.AddOrUpdate(typeof(IFile), model);
            Component.Count.Should().Be(1);
            Component[typeof(IFile)].Should().HaveCount(1);
            Component.AddOrUpdate(typeof(IFile), model);
            Component.Count.Should().Be(1);
            Component[typeof(IFile)].Should().HaveCount(1);
            Component.AddOrUpdate(typeof(IFile), new ConstructorModel(Mocks.GetObject<ConstructorInfo>(), new List<object?> { "1" }));
            Component.Count.Should().Be(1);
            Component[typeof(IFile)].Should().HaveCount(2);

            Component.AddOrUpdate(typeof(IFile), new ConstructorModel(Mocks.GetObject<ConstructorInfo>(), new List<object?> { "1" }));
            Component.Count.Should().Be(1);
            Component[typeof(IFile)].Should().HaveCount(2);
        }

        [Fact]
        public void ToList()
        {
            var model = new ConstructorModel(Mocks.GetObject<ConstructorInfo>(), new List<object?>());
            Component.Count.Should().Be(0);
            Component.AddOrUpdate(typeof(IFile), model);

            var result = Component.AsEnumerable().ToDictionary(x=>x.Key, x=>x.Value);
            Component[0].Key.Should().Be(result.First().Key);
            Component[0].Key.FullName.Should().Be(result.First().Key.FullName);
            Component[0].Value.Should().BeEquivalentTo(result.First().Value);
        }

        [Fact]
        public void InterfacesAreValid_WhenUsingAsMethods()
        {
            Component.AsReadOnlyDictionary().GetType().IsAssignableTo(typeof(IReadOnlyDictionary<Type, ReadOnlyCollection<IHistoryModel>>)).Should().BeTrue();
            Component.AsEnumerable().GetType().IsAssignableTo(typeof(IReadOnlyDictionary<Type, ReadOnlyCollection<IHistoryModel>>)).Should().BeTrue();
            Component.AsEnumerable().GetType().IsAssignableTo(typeof(IEnumerable<KeyValuePair<Type, IEnumerable<IHistoryModel>>>)).Should().BeTrue();
            Component.AsLookup().GetType().IsAssignableTo(typeof(IEnumerable<KeyValuePair<Type, IEnumerable<IHistoryModel>>>)).Should().BeTrue();
            Component.AsLookup().GetType().IsAssignableTo(typeof(ILookup<Type, IHistoryModel>)).Should().BeTrue();
        }

        [Fact]
        public void Enumerator()
        {
            var model = new ConstructorModel(Mocks.GetObject<ConstructorInfo>(), new List<object?>());
            Component.Count.Should().Be(0);
            Component.AddOrUpdate(typeof(IFile), model);
            var count = 0;
            foreach (var keyValuePair in Component.AsEnumerable())
            {
                count++;
            }

            count.Should().Be(1);

            count = 0;
            foreach (var o in Component)
            {
                count++;
            }

            count.Should().Be(1);
        }
    }
}
