#if FUSION_ANIMATION_PROFILING_DETAILED
#define FUSION_ANIMATION_PROFILING
#endif

namespace Fusion.Animations
{
	public static partial class AnimationProfiler
	{
		// PUBLIC METHODS

#if FUSION_ANIMATION_PROFILING
		public static void BeginDefaultSample(string name)
		{
			UnityEngine.Profiling.Profiler.BeginSample(name);
		}

		public static void EndDefaultSample()
		{
			UnityEngine.Profiling.Profiler.EndSample();
		}
#else
		[System.Diagnostics.Conditional("FUSION_ANIMATION_PROFILING")]
		public static void BeginDefaultSample(string name)
		{
			UnityEngine.Profiling.Profiler.BeginSample(name);
		}

		[System.Diagnostics.Conditional("FUSION_ANIMATION_PROFILING")]
		public static void EndDefaultSample()
		{
			UnityEngine.Profiling.Profiler.EndSample();
		}
#endif

		[System.Diagnostics.Conditional("FUSION_ANIMATION_PROFILING_DETAILED")]
		public static void BeginDetailedSample(string name)
		{
			UnityEngine.Profiling.Profiler.BeginSample(name);
		}

		[System.Diagnostics.Conditional("FUSION_ANIMATION_PROFILING_DETAILED")]
		public static void EndDetailedSample()
		{
			UnityEngine.Profiling.Profiler.EndSample();
		}
	}
}
