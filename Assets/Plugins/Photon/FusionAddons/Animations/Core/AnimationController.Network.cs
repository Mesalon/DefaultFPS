namespace Fusion.Animations
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using Unity.Collections.LowLevel.Unsafe;

	public unsafe partial class AnimationController
	{
		// PRIVATE MEMBERS

		private AnimationProperiesInfo[]     _animationProperties;
		private IAnimationPropertyProvider[] _animationPropertyProviders;

		// PRIVATE METHODS

		private int GetNetworkDataWordCount()
		{
			InitializeLayers();
			InitializeNetworkProperties();

			int wordCount = 0;

			AnimationProperiesInfo animationProperty;
			for (int i = 0, count = _animationProperties.Length; i < count; ++i)
			{
				animationProperty = _animationProperties[i];
				for (int j = 0; j < animationProperty.Count; ++j)
				{
					wordCount += animationProperty.WordCounts[j];
				}
			}

			for (int i = 0, count = _animationPropertyProviders.Length; i < count; ++i)
			{
				wordCount += _animationPropertyProviders[i].WordCount;
			}

			return wordCount;
		}

		private unsafe void ReadNetworkData()
		{
			int* ptr = Ptr;

			AnimationProperiesInfo   animationProperty;
			AnimationProperiesInfo[] animationProperties = _animationProperties;
			for (int i = 0, count = animationProperties.Length; i < count; ++i)
			{
				animationProperty = animationProperties[i];

				byte* objectPtr = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(animationProperty.Target, out ulong gcHandle);

				for (int j = 0; j < animationProperty.Count; ++j)
				{
					int  wordCount   = animationProperty.WordCounts[j];
					int* propertyPtr = (int*)(objectPtr + animationProperty.FieldOffsets[j]);

					for (int n = 0; n < wordCount; ++n)
					{
						*propertyPtr = *ptr;

						++ptr;
						++propertyPtr;
					}
				}

				UnsafeUtility.ReleaseGCObject(gcHandle);
			}

			IAnimationPropertyProvider   animationPropertyProvider;
			IAnimationPropertyProvider[] animationPropertyProviders = _animationPropertyProviders;
			for (int i = 0, count = animationPropertyProviders.Length; i < count; ++i)
			{
				animationPropertyProvider = animationPropertyProviders[i];
				animationPropertyProvider.Read(ptr);
				ptr += animationPropertyProvider.WordCount;
			}
		}

		private unsafe void WriteNetworkData()
		{
			int* ptr = Ptr;

			AnimationProperiesInfo   animationProperty;
			AnimationProperiesInfo[] animationProperties = _animationProperties;
			for (int i = 0, count = animationProperties.Length; i < count; ++i)
			{
				animationProperty = animationProperties[i];

				byte* objectPtr = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(animationProperty.Target, out ulong gcHandle);

				for (int j = 0; j < animationProperty.Count; ++j)
				{
					int  wordCount   = animationProperty.WordCounts[j];
					int* propertyPtr = (int*)(objectPtr + animationProperty.FieldOffsets[j]);

					for (int n = 0; n < wordCount; ++n)
					{
						*ptr = *propertyPtr;

						++ptr;
						++propertyPtr;
					}
				}

				UnsafeUtility.ReleaseGCObject(gcHandle);
			}

			IAnimationPropertyProvider   animationPropertyProvider;
			IAnimationPropertyProvider[] animationPropertyProviders = _animationPropertyProviders;
			for (int i = 0, count = animationPropertyProviders.Length; i < count; ++i)
			{
				animationPropertyProvider = animationPropertyProviders[i];
				animationPropertyProvider.Write(ptr);
				ptr += animationPropertyProvider.WordCount;
			}
		}

		private unsafe void InterpolateNetworkData(float alpha = -1.0f)
		{
			if (GetInterpolationData(out InterpolationData interpolationData) == false)
				return;

			if (alpha >= 0.0f && alpha <= 1.0f)
			{
				interpolationData.Alpha = alpha;
			}

			AnimationProperiesInfo   animationProperty;
			AnimationProperiesInfo[] animationProperties = _animationProperties;
			for (int i = 0, count = animationProperties.Length; i < count; ++i)
			{
				animationProperty = animationProperties[i];

				for (int j = 0; j < animationProperty.Count; ++j)
				{
					int wordCount = animationProperty.WordCounts[j];

					InterpolationDelegate interpolationDelegate = animationProperty.InterpolationDelegates[j];
					if (interpolationDelegate != null)
					{
						interpolationDelegate(interpolationData);
					}

					interpolationData.From += wordCount;
					interpolationData.To   += wordCount;
				}
			}

			IAnimationPropertyProvider   animationPropertyProvider;
			IAnimationPropertyProvider[] animationPropertyProviders = _animationPropertyProviders;
			for (int i = 0, count = animationPropertyProviders.Length; i < count; ++i)
			{
				animationPropertyProvider = animationPropertyProviders[i];
				animationPropertyProvider.Interpolate(interpolationData);
				interpolationData.From += animationPropertyProvider.WordCount;
				interpolationData.To   += animationPropertyProvider.WordCount;
			}
		}

		protected virtual void AddAnimationPropertyProviders(List<IAnimationPropertyProvider> animationPropertyProviders) {}

		// PRIVATE METHODS

		private void InitializeNetworkProperties()
		{
			if (_animationProperties != default)
				return;

			_animationProperties = GetAnimationProperties();

			List<IAnimationPropertyProvider> animationPropertyProviders = new List<IAnimationPropertyProvider>();
			animationPropertyProviders.Add(new AnimationTimeProvider());
			animationPropertyProviders.Add(new AnimationWeightProvider());
			animationPropertyProviders.Add(new AnimationFadingProvider());
			animationPropertyProviders.Add(new AnimationClipIDProvider());

			AddAnimationPropertyProviders(animationPropertyProviders);

			for (int i = 0, count = animationPropertyProviders.Count; i < count; ++i)
			{
				animationPropertyProviders[i].Initialize(this);
			}

			_animationPropertyProviders = animationPropertyProviders.ToArray();
		}

		private void DeinitializeNetworkProperties()
		{
			if (_animationPropertyProviders != default)
			{
				for (int i = 0, count = _animationPropertyProviders.Length; i < count; ++i)
				{
					_animationPropertyProviders[i].Deinitialize();
				}
			}

			_animationProperties        = default;
			_animationPropertyProviders = default;
		}

		private AnimationProperiesInfo[] GetAnimationProperties()
		{
			List<AnimationProperiesInfo> properties = new List<AnimationProperiesInfo>();

			AddTargetProperties(this, properties);

			IList<AnimationLayer> layers = _layers;
			for (int i = 0, count = layers.Count; i < count; ++i)
			{
				AddLayerProperties(layers[i], properties);
			}

			return properties.ToArray();
		}

		private static void AddLayerProperties(AnimationLayer layer, List<AnimationProperiesInfo> properties)
		{
			AddTargetProperties(layer, properties);

			IList<AnimationState> states = layer.States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				AddStateProperties(states[i], properties);
			}
		}

		private static void AddStateProperties(AnimationState state, List<AnimationProperiesInfo> properties)
		{
			AddTargetProperties(state, properties);

			IList<AnimationState> states = state.States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				AddStateProperties(states[i], properties);
			}
		}

		private static void AddTargetProperties(object target, List<AnimationProperiesInfo> properties)
		{
			bool                        hasProperties          = false;
			List<int>                   wordCounts             = default;
			List<int>                   fieldOffsets           = default;
			List<InterpolationDelegate> interpolationDelegates = default;

			FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < fields.Length; ++i)
			{
				FieldInfo field = fields[i];

				object[] attributes = field.GetCustomAttributes(typeof(AnimationPropertyAttribute), false);
				if (attributes.Length > 0)
				{
					if (field.FieldType.IsValueType == false)
					{
						throw new NotSupportedException(field.FieldType.FullName);
					}

					if (hasProperties == false)
					{
						hasProperties          = true;
						wordCounts             = new List<int>(8);
						fieldOffsets           = new List<int>(8);
						interpolationDelegates = new List<InterpolationDelegate>(8);
					}

					InterpolationDelegate interpolationDelegate = null;

					string interpolationDelegateName = ((AnimationPropertyAttribute)attributes[0]).InterpolationDelegate;
					if (string.IsNullOrEmpty(interpolationDelegateName) == false)
					{
						MethodInfo interpolationMethod = target.GetType().GetMethod(interpolationDelegateName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (interpolationMethod == null)
						{
							throw new ArgumentException($"Missing interpolation method {interpolationDelegateName}!");
						}

						interpolationDelegate = interpolationMethod.CreateDelegate(typeof(InterpolationDelegate), target) as InterpolationDelegate;
						if (interpolationMethod == null)
						{
							throw new ArgumentException($"Couldn't create delegate for interpolation method {interpolationDelegateName}!");
						}
					}

					wordCounts.Add(GetTypeWordCount(field.FieldType));
					fieldOffsets.Add(UnsafeUtility.GetFieldOffset(field));
					interpolationDelegates.Add(interpolationDelegate);
				}
			}

			if (hasProperties == true)
			{
				AnimationProperiesInfo animationObject = new AnimationProperiesInfo();
				animationObject.Count                  = fieldOffsets.Count;
				animationObject.Target                 = target;
				animationObject.WordCounts             = wordCounts.ToArray();
				animationObject.FieldOffsets           = fieldOffsets.ToArray();
				animationObject.InterpolationDelegates = interpolationDelegates.ToArray();

				properties.Add(animationObject);
			}
		}

		private static int GetTypeWordCount(Type type)
		{
			return (Marshal.SizeOf(type) + 3) / 4;
		}

		private sealed class AnimationProperiesInfo
		{
			public int                     Count;
			public object                  Target;
			public int[]                   WordCounts;
			public int[]                   FieldOffsets;
			public InterpolationDelegate[] InterpolationDelegates;
		}
	}
}
