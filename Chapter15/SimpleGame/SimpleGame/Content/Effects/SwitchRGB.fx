sampler TextureSampler;

struct PixelInput
{
    float2 TexCoord : TEXCOORD0;	
};

float4 pixelShader(PixelInput input) : COLOR
{
	float4 color = tex2D( TextureSampler, input.TexCoord);
	return( color.brga );
}

technique Default
{
    pass P0
    {
        PixelShader = compile ps_2_0 pixelShader();
    }
}
