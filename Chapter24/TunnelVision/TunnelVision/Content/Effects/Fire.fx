texture Fire;
             
sampler FireMapSampler = sampler_state 
{
    texture = <Fire>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    addressU = wrap;
    addressV = wrap;
};

struct PixelInput
{
	float2 TexCoord	: TEXCOORD0;
};

float4 pixelShader(PixelInput input) : COLOR0
{
    float4 color;    

	float2 Right, Left, Above, Below; 

	Left = Right = Above = Below = input.TexCoord;

	Right.x+=.001;
	Left.x-=.001;
	Above.y+=.001;
	Below.y-=.001;

    //Sample the four texture positions 
    color = tex2D(FireMapSampler, Left);
    color += tex2D(FireMapSampler, Right);
    color += tex2D(FireMapSampler, Above);
    color += tex2D(FireMapSampler, Below);
    
    //Get the average
    color *= 0.25; // divided by 4

	//Cool down flame
    color.rgb -= .035;

	return(color);
}

technique Default
{
	pass P0
	{
		PixelShader = compile ps_2_0 pixelShader();
	}
}
