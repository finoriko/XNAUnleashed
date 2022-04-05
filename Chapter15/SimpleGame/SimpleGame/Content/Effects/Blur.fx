sampler TextureSampler;

struct PixelInput
{
	float2 TexCoord : TEXCOORD0;	
};

float4 pixelShader(PixelInput input) : COLOR
{
	float4 color = tex2D( TextureSampler,
		float2(input.TexCoord.x+0.0025, input.TexCoord.y+0.0025));
	color += tex2D( TextureSampler,
		float2(input.TexCoord.x-0.0025, input.TexCoord.y-0.0025));
	color += tex2D( TextureSampler,
		float2(input.TexCoord.x+0.0025, input.TexCoord.y-0.0025));
	color += tex2D( TextureSampler,
		float2(input.TexCoord.x-0.0025, input.TexCoord.y+0.0025));

	color = color / 4;	

	return( color );
}

technique Default
{
	pass P0
	{
		PixelShader = compile ps_2_0 pixelShader();
	}
}

