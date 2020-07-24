using DefaultEcs;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Interfaces;
using NUnit.Framework;

namespace GameHost.Simulation.Tests
{
	public class TestComponent
	{
		public struct Buffer : IComponentBuffer
		{
			public int Value;
		}

		public GameWorld World;

		[SetUp]
		public void Setup()
		{
			World = new GameWorld();
		}

		[Test]
		public void TestAddToBuffer()
		{
			var entity = World.CreateEntity();
			World.AddComponent(entity, World.AsComponentType<Buffer>());

			var buffer = World.GetBuffer<Buffer>(entity);
			buffer.Add(new Buffer {Value = 1});
			buffer.AddReinterpret(2);
			buffer.Add(new Buffer {Value = 3});

			var i = 1;
			foreach (var element in buffer.Span)
			{
				Assert.AreEqual(i++, element.Value);
			}
		}

		[Test]
		public void TestReference()
		{
			var entityOwner   = World.CreateEntity();
			var entityWithRef = World.CreateEntity();

			var ownerComponent = World.AddComponent<IntComponent>(entityOwner);
			World.AssignComponent(entityWithRef, ownerComponent);

			var ref1 = World.UpdateOwnedComponent(entityOwner, new IntComponent {Value = 8});
			Assert.AreEqual(ownerComponent.Id, ref1.Id);
			Assert.AreEqual(World.GetComponentData<IntComponent>(entityOwner).Value, World.GetComponentData<IntComponent>(entityWithRef).Value);

			var ref2 = World.UpdateOwnedComponent(entityWithRef, new IntComponent {Value = 4});
			Assert.AreNotEqual(ref1.Id, ref2.Id);
			Assert.AreNotEqual(World.GetComponentData<IntComponent>(entityOwner).Value, World.GetComponentData<IntComponent>(entityWithRef).Value);
		}

		[Test]
		public void TestDependence()
		{
			var root   = World.CreateEntity();
			var parent = World.CreateEntity();
			var child  = World.CreateEntity();

			World.AddComponent(root, new IntComponent {Value = 42});
			World.DependOnEntityComponent(parent, root, World.AsComponentType<IntComponent>());
			World.DependOnEntityComponent(child, parent, World.AsComponentType<IntComponent>());

			int component_val(GameEntity entity) => World.GetComponentData<IntComponent>(entity).Value;

			// Child depend on parent, and parent depend on root.
			Assert.AreEqual(component_val(root), component_val(parent));
			Assert.AreEqual(component_val(root), component_val(child));
			Assert.AreEqual(component_val(parent), component_val(child));

			World.GetComponentData<IntComponent>(root).Value = 8;

			// All entities still depend on root component, but the value changed.
			Assert.AreEqual(component_val(root), component_val(parent));
			Assert.AreEqual(component_val(root), component_val(child));
			Assert.AreEqual(component_val(parent), component_val(child));

			// Assign an owned component to parent, this will remove the shared component between Root and Parent.
			World.UpdateOwnedComponent(parent, new IntComponent {Value = 4});

			// The parent now has a different value from root, but child still depend on the parent.
			// So the value between child and parent are the same, but parent/child are different from root.
			Assert.AreNotEqual(component_val(root), component_val(parent));
			Assert.AreNotEqual(component_val(root), component_val(child));
			Assert.AreEqual(component_val(parent), component_val(child));

			// Remove the component from parent
			World.RemoveComponent(parent, World.AsComponentType<IntComponent>());

			// Root still has the component, but since the parent of the child has broken the component dependence the child shouldn't have the component anymore.
			Assert.IsTrue(World.HasComponent<IntComponent>(root));
			Assert.IsFalse(World.HasComponent<IntComponent>(child));
		}

		public struct IntComponent : IComponentData
		{
			public int Value;
		}
	}
}