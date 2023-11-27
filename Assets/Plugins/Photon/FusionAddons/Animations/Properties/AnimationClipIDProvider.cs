namespace Fusion.Animations
{
	public unsafe interface IAnimationClipIDProvider
	{
		int ClipID { get; set; }
	}

	public unsafe sealed class AnimationClipIDProvider : AnimationPropertyProvider<IAnimationClipIDProvider>
	{
		// AnimationPropertyProvider INTERFACE

		protected override int PropertyWordCount => 1;

		protected override void Read(IAnimationClipIDProvider item, int* ptr)
		{
			item.ClipID = *ptr;
		}

		protected override void Write(IAnimationClipIDProvider item, int* ptr)
		{
			*ptr = item.ClipID;
		}

		protected override void Interpolate(IAnimationClipIDProvider item, InterpolationData interpolationData)
		{
			item.ClipID = interpolationData.Alpha < 0.5f ? *interpolationData.From : *interpolationData.To;
		}
	}
}
