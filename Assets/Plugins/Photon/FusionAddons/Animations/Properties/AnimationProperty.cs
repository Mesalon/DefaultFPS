namespace Fusion.Animations
{
	using System;

	public delegate void InterpolationDelegate(InterpolationData interpolationData);

	/// <summary>
	/// Attribute to automatically synchronize animation properties.
	/// Interpolation delegate is optional and can be used if you need the property to be interpolated in render.
	/// </summary>
	//
	//  Example usage:
	//
	//  [AnimationProperty(nameof(InterpolateMyProperty))]
	//  public int MyProperty;
	//
	//  private void InterpolateMyProperty(InterpolationData interpolationData)
	//  {
	//      // Interpolation
	//  }
	//
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class AnimationPropertyAttribute : Attribute
	{
		public readonly string InterpolationDelegate;

		public AnimationPropertyAttribute()
		{
		}

		public AnimationPropertyAttribute(string interpolationDelegate)
		{
			InterpolationDelegate = interpolationDelegate;
		}
	}
}
