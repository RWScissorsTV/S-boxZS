using Sandbox;

public static class ZombieSurvivalCitizenMorphs
{
	public static void ApplyZombieMorphs( SkinnedModelRenderer renderer )
	{
		if ( !renderer.IsValid() )
			return;

		var morphs = renderer.Morphs;

		morphs.Set( "browlowererL", 1 );
		morphs.Set( "browlowererR", 1 );
		morphs.Set( "eyelidbulgelower", 0 );
		morphs.Set( "innerbrowraiserL", 0 );
		morphs.Set( "innerbrowraiserR", 0 );
		morphs.Set( "cheekinflateL", 0.12f );
		morphs.Set( "cheekinflateR", 0.12f );
		morphs.Set( "ChinRaiser", 0 );
		morphs.Set( "nosewrinklerL", 1 );
		morphs.Set( "nosewrinklerR", 1 );
		morphs.Set( "nostrildilator", 0.85f );
		morphs.Set( "jawsidewaysL", 0 );
		morphs.Set( "jawsidewaysR", 1 );
		morphs.Set( "jawsuck", 0 );
		morphs.Set( "jawthrust", 0 );
		morphs.Set( "lipcornerdepressorL", 1 );
		morphs.Set( "lipcornerdepressorR", 1 );
		morphs.Set( "lipcornerpullerL", 1 );
		morphs.Set( "lipcornerpullerR", 1 );
		morphs.Set( "lippuckerer", 0.26f );
		morphs.Set( "LipSidewaysL", 0 );
		morphs.Set( "LipSidewaysR", 0 );
		morphs.Set( "LowerLipDrepressor", 0.51f );
		morphs.Set( "openjawL", 1 );
		morphs.Set( "openjawR", 1 );
		morphs.Set( "openmouthL", 1 );
		morphs.Set( "openmouthR", 1 );
		morphs.Set( "upperLipFunneler", 1 );
		morphs.Set( "upperLipsTowardAndPart", 1 );
		morphs.Set( "upperlipraiserL", 1 );
		morphs.Set( "upperlipraiserR", 1 );
		morphs.Set( "lowerLipsTowardAndPart", 0 );
		morphs.Set( "lowerLipFunneler", 0.48f );
	}

	public static void RevertToDefaultMorphs( SkinnedModelRenderer renderer )
	{
		if ( !renderer.IsValid() )
			return;

		var morphs = renderer.Morphs;

		morphs.Set( "browlowererL", 0 );
		morphs.Set( "browlowererR", 0 );
		morphs.Set( "eyelidbulgelower", 0 );
		morphs.Set( "innerbrowraiserL", 0 );
		morphs.Set( "innerbrowraiserR", 0 );
		morphs.Set( "cheekinflateL", 0 );
		morphs.Set( "cheekinflateR", 0 );
		morphs.Set( "ChinRaiser", 0 );
		morphs.Set( "nosewrinklerL", 0 );
		morphs.Set( "nosewrinklerR", 0 );
		morphs.Set( "nostrildilator", 0 );
		morphs.Set( "jawsidewaysL", 0 );
		morphs.Set( "jawsidewaysR", 0 );
		morphs.Set( "jawsuck", 0 );
		morphs.Set( "jawthrust", 0 );
		morphs.Set( "lipcornerdepressorL", 0 );
		morphs.Set( "lipcornerdepressorR", 0 );
		morphs.Set( "lipcornerpullerL", 0 );
		morphs.Set( "lipcornerpullerR", 0 );
		morphs.Set( "lippuckerer", 0 );
		morphs.Set( "LipSidewaysL", 0 );
		morphs.Set( "LipSidewaysR", 0 );
		morphs.Set( "LowerLipDrepressor", 0 );
		morphs.Set( "openjawL", 0 );
		morphs.Set( "openjawR", 0 );
		morphs.Set( "openmouthL", 0 );
		morphs.Set( "openmouthR", 0 );
		morphs.Set( "upperLipFunneler", 0 );
		morphs.Set( "upperLipsTowardAndPart", 0 );
		morphs.Set( "upperlipraiserL", 0 );
		morphs.Set( "upperlipraiserR", 0 );
		morphs.Set( "lowerLipsTowardAndPart", 0 );
		morphs.Set( "lowerLipFunneler", 0 );
	}
}
