sampler2D uTexture : register(s0);   // The colored texture, typically passed by the SpriteBatch
sampler2D uColorMask : register(s1); // The color mask, must be provided by the user 

// The max accepted color distance between the 
// provided color mask and the provided known color keys
const float MAX_COLOR_DIST = 0.1f;

// The color keys table; separated into multiple values for faster logic
float3 uColorKey0;
float3 uColorKey1;
float3 uColorKey2;
float3 uColorKey3;
float3 uColorKey4;
float3 uColorKey5;
float3 uColorKey6;
float3 uColorKey7;

// The user defined color values, per each existing key
float4 uColor0;
float4 uColor1;
float4 uColor2;
float4 uColor3;
float4 uColor4;
float4 uColor5;
float4 uColor6;
float4 uColor7;


float ColorDistance(float3 color1, float3 color2)
{
    float3 dist = color1 - color2;
    return length(dist);
}

float4 SimpleColorMaskShading(float2 texCoord : TEXCOORD) : COLOR0
{
    // Sample the input texture and the color mask (pattern)
    float4 color = tex2D(uTexture, texCoord);  
    float4 mask = tex2D(uColorMask, texCoord);  
        
    float4 newColor = color;
        
    // Ignore pixel if mask is transparent!
    if (mask.a < 1.0f)
        return float4(color.rgb, color.a);
        
    // Compare the sampled mask pixel with each of the color keys
    // This logic is intentionally unrolled (i.e. not in a loop)
    if (ColorDistance(mask.rgb, uColorKey0) < MAX_COLOR_DIST)
        newColor = uColor0;
      
    if (ColorDistance(mask.rgb, uColorKey1) < MAX_COLOR_DIST)
        newColor = uColor1;

    if (ColorDistance(mask.rgb, uColorKey2) < MAX_COLOR_DIST)
        newColor = uColor2;
    
    if (ColorDistance(mask.rgb, uColorKey3) < MAX_COLOR_DIST)
        newColor = uColor3;
     
    if (ColorDistance(mask.rgb, uColorKey4) < MAX_COLOR_DIST)
        newColor = uColor4;
     
    if (ColorDistance(mask.rgb, uColorKey5) < MAX_COLOR_DIST)
        newColor = uColor5;
       
    if (ColorDistance(mask.rgb, uColorKey6) < MAX_COLOR_DIST) 
        newColor = uColor6;
    
    if (ColorDistance(mask.rgb, uColorKey7) < MAX_COLOR_DIST)
        newColor = uColor7;
    
    // Blend the original color with the new found, accounting for the ambient brightness 
    // (will return the original if found unrecognized key in the mask) 
    color.rgb = lerp(color.rgb, newColor.rgb, newColor.a);
    return float4(color.rgb, color.a);
}

technique Technique1 
{
    pass SimpleColorMaskShading
    {
        PixelShader = compile ps_2_0 SimpleColorMaskShading();
    }
}

