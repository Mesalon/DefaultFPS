namespace Fusion.Animations
{
	public unsafe interface IAnimationFadingProvider
	{
		float FadingSpeed { get; set; }
	}

	public unsafe sealed class AnimationFadingProvider : AnimationPropertyProvider<IAnimationFadingProvider>
	{
		// AnimationPropertyProvider INTERFACE

		protected override int PropertyWordCount => 1;

		protected override void Read(IAnimationFadingProvider item, int* ptr)
		{
			item.FadingSpeed = *((float*)ptr);
		}

		protected override void Write(IAnimationFadingProvider item, int* ptr)
		{
			*((float*)ptr) = item.FadingSpeed;
		}

		protected override void Interpolate(IAnimationFadingProvider item, InterpolationData interpolationData)
		{
			item.FadingSpeed = interpolationData.Alpha < 0.5f ? *((float*)interpolationData.From) : *((float*)interpolationData.To);
		}
	}
}
