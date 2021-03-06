﻿//fixed parameters
float4x4 Projection;
float2 TileSize; //used for position to room masking. percentage of position space (0, 1) a tile takes up.

//change between rendering light passes
float RoomTarget; //room number for room masking
float2 RoomUVRescale;
float2 RoomUVOff;

float2 LightPosition; //in position space (0,1)
float LightSize; //in position space (0,1)
float LightPower = 2.0; //gamma correction on lights. can get some nicer distributions.
float LightIntensity = 1.0;
float TargetRoom;
float2 MapLayout;
float2 UVBase;

float2 ShadowPowers;
float2 SSAASize;

bool IsOutdoors;

texture roomMap : Diffuse;
texture shadowMap : Diffuse; //alpha texture containing occlusion for this light. White = full occlusion.
texture floorShadowMap : Diffuse; //same as above, but floors only.

sampler roomSampler = sampler_state {
    texture = <roomMap>;
    AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
    MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

sampler shadowSampler = sampler_state {
    texture = <shadowMap>;
    AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
    MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

sampler floorShadowSampler = sampler_state {
	texture = <floorShadowMap>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};


struct VertexShaderInput
{
    float4 Position : SV_Position0;
};

struct VertexShaderOutput
{
    float4 Position : SV_Position0;
    float2 p : TexCoord0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.Position = mul(float4(input.Position.xy, 0, 1), Projection);
    output.p = input.Position.xy * MapLayout - UVBase;
	output.p += SSAASize / 20000000;

    return output;
}

bool OutsideRoomCheck(float2 uv) {

	uv *= RoomUVRescale;
	uv += RoomUVOff;
	float4 test = tex2D(roomSampler, uv);
	float room1 = round(test.x * 255 + (test.y * 65280));
	float room2 = round(test.z * 255 + (test.w * 65280));
	bool diagType = (room2 > 32767);
	if (diagType == true) {
		room2 -= 32768;
	}
	if (room1 != room2) {
		//diagonal mode
		uv /= TileSize;
		if (diagType == true) {
			//horizontal diag:
			if ((uv.x % 1) + (uv.y % 1) >= 1)
				return (TargetRoom != room2); //hi room
			else
				return (TargetRoom != room1); //low room
		}
		else {
			//vertical diag:
			if ((uv.x % 1) - (uv.y % 1) > 0)
				return (TargetRoom != room1); //low room
			else
				return (TargetRoom != room2); //hi room
		}
	}
	else {
		return TargetRoom != room1;
	}
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
    float light = clamp(1.0 - distance(uv, LightPosition) / LightSize, 0.0, 1.0);
    light = pow(light, LightPower);

	float2 shadow = float2(tex2D(shadowSampler, uv).r, tex2D(floorShadowSampler, uv).r);
	shadow *= ShadowPowers;

	light -= light*shadow.x;
	float floorLight = light - light*shadow.y;
	// RGBA: LightIntensity, OutdoorsIntensity, LightIntensityShad, OutdoorsIntensityShad
	// output different under diff conditions
	if (IsOutdoors == true) {
		return float4(0.0, light, 0.0, floorLight) * LightIntensity;
	} else {
		return float4(light, 0.0, floorLight, 0.0) * LightIntensity;
	}
}

float4 PixelShaderFunctionOutdoors(VertexShaderOutput input) : COLOR0{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	float light = 1;

	float2 shadow = float2(tex2D(shadowSampler, uv).r, tex2D(floorShadowSampler, uv).r);
	shadow *= ShadowPowers;

	light -= light*shadow.x;
	float floorLight = light - light*shadow.y;
	// RGBA: LightIntensity, OutdoorsIntensity, LightIntensityShad, OutdoorsIntensityShad
	// output different under diff conditions
	if (IsOutdoors == true) {
		return float4(0.0, light, 0.0, floorLight);
	}
	else {
		return float4(light, 0.0, floorLight, 0.0);
	}
}

float SSAASample(float2 uv) {
	float result = 0;
	uv += SSAASize / 2;
	result += tex2D(shadowSampler, uv).r;
	result += tex2D(shadowSampler, uv + float2(SSAASize.x, 0)).r;
	result += tex2D(shadowSampler, uv + float2(0, SSAASize.y)).r;
	result += tex2D(shadowSampler, uv + float2(SSAASize.x, SSAASize.y)).r;
	return result / 4;
}

float4 PixelShaderFunctionOutdoorsSSAA(VertexShaderOutput input) : COLOR0{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	float light = 1;

	float2 shadow = float2(SSAASample(uv), tex2D(floorShadowSampler, uv).r);
	shadow *= ShadowPowers;

	light -= light*shadow.x;
	float floorLight = light - light*shadow.y;
	// RGBA: LightIntensity, OutdoorsIntensity, LightIntensityShad, OutdoorsIntensityShad
	// output different under diff conditions
	if (IsOutdoors == true) {
		return float4(0.0, light, 0.0, floorLight);
	}
	else {
		return float4(light, 0.0, floorLight, 0.0);
	}
}

float4 PixelShaderFunctionLightBleed(VertexShaderOutput input) : COLOR0{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	float light = 1;

	float2 shadow = float2(SSAASample(uv) * 5/4, 0);

	light -= light*shadow.x;
	float floorLight = light - light*shadow.y;
	// RGBA: LightIntensity, OutdoorsIntensity, LightIntensityShad, OutdoorsIntensityShad
	// output different under diff conditions
	return float4(0.0, light, 0.0, floorLight);
}

float4 PixelShaderFunctionMask(VertexShaderOutput input) : COLOR0{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	return float4(0, 0, 0, 0);
}


technique Draw2D
{
    pass MainPass
    {
        AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
        VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
        PixelShader = compile ps_4_0_level_9_3 PixelShaderFunction();
#else
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
#endif;

    }

	pass OutsidePass
	{
		AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 PixelShaderFunctionOutdoors();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionOutdoors();
#endif;

	}

	pass ClearPass
	{
		AlphaBlendEnable = TRUE; DestBlend = ZERO; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 PixelShaderFunctionMask();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionMask();
#endif;

	}

	pass BleedPass
	{
		AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 PixelShaderFunctionLightBleed();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionLightBleed();
#endif;

	}

	pass SSAAPass
	{
		AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 PixelShaderFunctionOutdoorsSSAA();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionOutdoorsSSAA();
#endif;

	}
}
