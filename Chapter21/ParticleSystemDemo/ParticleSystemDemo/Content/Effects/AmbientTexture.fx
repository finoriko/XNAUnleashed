float4x4 World : WORLD;
float4x4 View;
float4x4 Projection;

float4 AmbientColor : COLOR0;

float4x4 WorldViewProjection : WORLDVIEWPROJECTION;
texture ColorMap;
sampler ColorMapSampler = sampler_state
{
    texture = < ColorMap >;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    addressU = mirror;
    addressV = mirror;
};

struct VertexInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
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
    float2 TexCoord : TEXCOORD0;	
};

float4 pixelShader(PixelInput input) : COLOR
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
