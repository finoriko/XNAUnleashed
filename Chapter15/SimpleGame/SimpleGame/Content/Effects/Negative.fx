sampler TextureSampler;

struct PixelInput
{
    float2 TexCoord : TEXCOORD0;	
};

float4 pixelShader(PixelInput input) : COLOR
{
	float4 color = 1.0f - tex2D(TextureSampler, input.TexCoord);
	return( color );
}

technique Default
{
    pass P0
    {
        PixelShader = compile ps_2_0 pixelShader();
    }
}
