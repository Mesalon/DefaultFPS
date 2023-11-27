namespace Fusion.Animations
{
	public unsafe interface IAnimationTimeProvider
	{
		float AnimationTime             { get; set; }
		float InterpolatedAnimationTime { get; set; }
	}

	public unsafe sealed class AnimationTimeProvider : AnimationPropertyProvider<IAnimationTimeProvider>
	{
		// AnimationPropertyProvider INTERFACE

		protected override int PropertyWordCount => 1;

		protected override void Read(IAnimationTimeProvider item, int* ptr)
		{
			item.AnimationTime = *((float*)ptr);
		}

		protected override void Write(IAnimationTimeProvider item, int* ptr)
		{
			*((float*)ptr) = item.AnimationTime;
		}

		protected override void Interpolate(IAnimationTimeProvider item, InterpolationData interpolationData)
		{
			item.InterpolatedAnimationTime = AnimationUtility.InterpolateTime(*((float*)interpolationData.From), *((float*)interpolationData.To), 1.0f, interpolationData.Alpha);
		}
	}
}
