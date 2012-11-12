uniform extern texture SurfaceTexture;
uniform extern float2 LightDirection = 0;
uniform extern float TextureOffset = 0;
uniform extern float RenderRadius = 1;
uniform extern float HorizontalScale = 1;
uniform extern float4 SurfaceTint = 1;
uniform extern bool HasNormals = false;
float4x4 World;
float4x4 View;
float4x4 Projection;

// TODO: add effect parameters here.
SamplerState textureSampler = sampler_state
{
    Texture = <SurfaceTexture>;
    MipFilter = Linear;
    MagFilter = Anisotropic;
    MinFilter = Anisotropic;
    MaxAnisotropy = 16;
    AddressU = Clamp;
    AddressV = Clamp;
};
struct VertexShaderInput
{
    float4 Position : POSITION0;

    // TODO: add input channels such as texture
    // coordinates and vertex colors here.
};
struct VSData
{
    float4 Position : POSITION0;
    float2 TextureCoordinate : TEXCOORD0;
};
struct VertexShaderOutput
{
    float4 Position : POSITION0;

    // TODO: add vertex shader outputs such as colors and texture
    // coordinates here. These values will automatically be interpolated
    // over the triangle, and provided as input to your pixel shader.
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    // TODO: add your vertex shader code here.

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // TODO: add your pixel shader code here.

    return float4(1, 0, 0, 1);
}
// Gets basic variables used in all pixel shaders.
void getOffsets(float2 t, inout float2 pOuter, inout float2 pInner, inout float rInner, inout float rOffset) {
    // Outer point.
    pOuter = t;
    // Also get a version mapped down even further, to allow the
    // atmosphere to overflow the planet in the freed area.
    pInner = pOuter ;// / (1 - relativeAtmosphereArea);
    // And lastly, the same thing, but offset away from the light
    // source, which we'll use for shadowing.
    float2 pOffset = pInner + LightDirection * 0.5;

    // Compute the squared radii for both variants.
    rInner = dot(pInner, pInner);
    rOffset = dot(pOffset, pOffset);
    // Increase the size of the shadow a bit, but steepen the fall-off.
    rOffset *= 0.5;
    rOffset *= rOffset;
}

// ------------------------------------------------------------------------- //
// Used to make sure texture coordinates are within [0, 1].
float2 wrap(float2 v) {
    if (v.x < 0) {
        v.x += 1;
    }
    if (v.y < 0) {
        v.y += 1;
    }
    if (v.x >= 1) {
        v.x -= 1;
    }
    if (v.x >= 1) {
        v.x -= 1;
    }
    return v;
}
// Renders the planet surface.
float4 SurfacePS(VSData input) : COLOR0
{
    float2 pOuter, pInner;
    float rInner, rOffset;
    getOffsets(input.TextureCoordinate, pOuter, pInner, rInner, rOffset);

    if (rInner > 1)
    {
        // Outside the sphere, do nothing.
        discard;
    }

    // Compute the spherized coordinate of the pixel.
    float f = (1 - sqrt(1 - rInner)) / rInner;
    float2 uvSphere = (pInner * f + 1) / 2;
    float2 uvCloud = uvSphere;
    uvSphere.x = ((uvSphere.x + TextureOffset) * HorizontalScale) % 1;
    uvSphere = wrap(uvSphere);

    // Actual color at position.
    float4 color = tex2D(textureSampler, uvSphere) * SurfaceTint;
  //  if (HasNormals) {
  //      color.rgb *= 4 * dot((tex2D(normalsSampler, uvSphere) - 0.5), LightDirection) + 1;
  //  }

    // Get cloud shadow.
    float cloud = 1;
  //  if (HasClouds) {
  //      uvCloud += LightDirection / RenderRadius;
  //      uvCloud.x = ((uvCloud.x - TextureOffset) * HorizontalScale) % 1;
  //      uvCloud = wrap(uvCloud);
  //      cloud -= tex2D(cloudSampler, uvCloud).a;
  //  }

    // Self-shadowing based on light source position.
    color.rgb *= saturate(rOffset) * cloud;

    // Alpha for smoother border.
    float alpha = clamp(0.5 * (1 - rInner) * RenderRadius, 0, 1);
    return float4(color.rgb, alpha);
}
technique TestObject
{
    pass Pass1
    {
        AlphaBlendEnable = True;
        DestBlend = InvSrcAlpha;
        SrcBlend = SrcAlpha;
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 SurfacePS();
    }
}
