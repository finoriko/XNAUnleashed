sampler TextureSampler;

struct PixelInput
{
	float2 TexCoord : TEXCOORD0;	
};

float4 pixelShader(PixelInput input) : COLOR
{
	float sharpAmount = 100.0f;
	float4 color = tex2D( TextureSampler, input.TexCoord);
	color += tex2D( TextureSampler, input.TexCoord - 0.001) * sharpAmount;
	color -= tex2D( TextureSampler, input.TexCoord + 0.001) * sharpAmount;
	return( color );
}

technique Default
{
	pass P0
	{
		PixelShader = compile ps_2_0 pixelShader();
	}
}
