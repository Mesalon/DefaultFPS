namespace Fusion.Animations
{
	public unsafe interface IAnimationWeightProvider
	{
		float Weight             { get; set; }
		float InterpolatedWeight { get; set; }
	}

	public unsafe sealed class AnimationWeightProvider : AnimationPropertyProvider<IAnimationWeightProvider>
	{
		// AnimationPropertyProvider INTERFACE

		protected override int PropertyWordCount => 1;

		protected override void Read(IAnimationWeightProvider item, int* ptr)
		{
			item.Weight = *((float*)ptr);
		}

		protected override void Write(IAnimationWeightProvider item, int* ptr)
		{
			*((float*)ptr) = item.Weight;
		}

		protected override void Interpolate(IAnimationWeightProvider item, InterpolationData interpolationData)
		{
			item.InterpolatedWeight = AnimationUtility.InterpolateWeight(*((float*)interpolationData.From), *((float*)interpolationData.To), interpolationData.Alpha);
		}
	}
}
