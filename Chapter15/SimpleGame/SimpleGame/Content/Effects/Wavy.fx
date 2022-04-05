
sampler TextureSampler;

struct PixelInput
{
	float2 TexCoord : TEXCOORD0;	
};

float4 pixelShader(PixelInput input) : COLOR
{
	float y = input.TexCoord.y;
	float x = input.TexCoord.x;
	y = y + (sin(x*100)*0.001);
	float4 color = tex2D(TextureSampler, float2(x,y));	
	return( color );
}

technique Default
{
	pass P0
	{
		PixelShader = compile ps_2_0 pixelShader();
	}
}

