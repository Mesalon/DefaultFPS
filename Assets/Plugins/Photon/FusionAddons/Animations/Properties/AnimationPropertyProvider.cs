namespace Fusion.Animations
{
	using System.Collections.Generic;

	public unsafe interface IAnimationPropertyProvider
	{
		int WordCount { get; }

		void Initialize(AnimationController controller);
		void Deinitialize();
		void Read(int* ptr);
		void Write(int* ptr);
		void Interpolate(InterpolationData interpolationData);
	}

	/// <summary>
	/// AnimationPropertyProvider handles synchronization and interpolation of animation properties.
	/// Animation properties are provided by type T which can be implemented by AnimationController, AnimationLayer or AnimationState.
	/// T can be implemented only once by a single controller/layer/state type.
	/// All animation property providers must be registered in <c>AnimationController.AddAnimationPropertyProviders()</c>.
	/// </summary>
	public abstract unsafe class AnimationPropertyProvider<T> : IAnimationPropertyProvider where T : class
	{
		// PROTECTED MEMBERS

		protected abstract int PropertyWordCount { get; }

		// PRIVATE MEMBERS

		private T[] _items;
		private int _wordCount;
		private int _propertyWordCount;

		// IAnimationPropertyProvider INTERFACE

		int IAnimationPropertyProvider.WordCount => _wordCount;

		void IAnimationPropertyProvider.Initialize(AnimationController controller)
		{
			List<T> items = new List<T>();

			AddController(controller, items);

			_items             = items.ToArray();
			_wordCount         = items.Count * PropertyWordCount;
			_propertyWordCount = PropertyWordCount;

			OnInitialize(controller);
		}

		void IAnimationPropertyProvider.Deinitialize()
		{
			OnDeinitialize();

			_items             = default;
			_wordCount         = default;
			_propertyWordCount = default;
		}

		void IAnimationPropertyProvider.Read(int* ptr)
		{
			for (int i = 0, count = _items.Length; i < count; ++i)
			{
				Read(_items[i], ptr);
				ptr += _propertyWordCount;
			}
		}

		void IAnimationPropertyProvider.Write(int* ptr)
		{
			for (int i = 0, count = _items.Length; i < count; ++i)
			{
				Write(_items[i], ptr);
				ptr += _propertyWordCount;
			}
		}

		void IAnimationPropertyProvider.Interpolate(InterpolationData interpolationData)
		{
			for (int i = 0, count = _items.Length; i < count; ++i)
			{
				Interpolate(_items[i], interpolationData);
				interpolationData.From += _propertyWordCount;
				interpolationData.To   += _propertyWordCount;
			}
		}

		// PROTECTED METHODS

		protected abstract void Read(T item, int* ptr);
		protected abstract void Write(T item, int* ptr);
		protected abstract void Interpolate(T item, InterpolationData interpolationData);

		protected virtual void OnInitialize(AnimationController controller) {}
		protected virtual void OnDeinitialize()                             {}

		// PRIVATE METHODS

		private void AddController(AnimationController controller, List<T> items)
		{
			if (controller is T item)
			{
				items.Add(item);
			}

			IList<AnimationLayer> layers = controller.Layers;
			for (int i = 0, count = layers.Count; i < count; ++i)
			{
				AddLayer(layers[i], items);
			}
		}

		private void AddLayer(AnimationLayer layer, List<T> items)
		{
			if (layer is T item)
			{
				items.Add(item);
			}

			IList<AnimationState> states = layer.States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				AddState(states[i], items);
			}
		}

		private void AddState(AnimationState state, List<T> items)
		{
			if (state is T item)
			{
				items.Add(item);
			}

			IList<AnimationState> states = state.States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				AddState(states[i], items);
			}
		}
	}
}
