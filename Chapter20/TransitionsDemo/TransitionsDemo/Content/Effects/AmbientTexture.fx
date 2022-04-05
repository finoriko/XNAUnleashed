matrix <float, 4, 4> World	: WORLD;
matrix <float, 4, 4> View;
matrix <float, 4, 4> Projection;

float4 AmbientColor : COLOR0 = 0.8f;

static matrix <float, 4, 4> WorldViewProjection : WORLDVIEWPROJECTION;
extern texture ColorMap;
sampler ColorMapSampler = sampler_state
{
    texture = <ColorMap>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    addressU = mirror;
    addressV = mirror;
};

struct VertexInput
{
    vector <float, 4> Position : POSITION0;
    vector <float, 2> TexCoord : TEXCOORD0;
};

struct VertexOutput
{
    vector <float, 4> Position : POSITION0;
    vector <float, 2> TexCoord : TEXCOORD0;
};

VertexOutput vertexShader(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    WorldViewProjection = mul(mul(World, View), Projection);
    output.Position = mul(input.Position, WorldViewProjection);
    output.TexCoord = input.TexCoord;

    return( output );
}

struct PixelInput
{
	vector <float, 2> TexCoord : TEXCOORD0;	
};

vector <float, 4> pixelShader(PixelInput input) : COLOR
{
	return( tex2D(ColorMapSampler, input.TexCoord) * AmbientColor);
}

technique Default
{
	pass P0
	{
		VertexShader = compile vs_1_1 vertexShader();
		PixelShader = compile ps_1_1 pixelShader();
	}
}
